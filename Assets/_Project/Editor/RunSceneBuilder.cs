using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

using ScalePunch.Abilities;
using ScalePunch.Abilities.Effects;
using ScalePunch.Combat;
using ScalePunch.Core;
using ScalePunch.Data;
using ScalePunch.Enemies;
using ScalePunch.Feedback;
using ScalePunch.Player;
using ScalePunch.Progression;
using ScalePunch.UI;
using ScalePunch.Weapons;

using Object = UnityEngine.Object;

namespace ScalePunch.EditorTools
{
    /// <summary>
    /// Builds the entire Run scene — data assets, prefabs, materials, hierarchy
    /// and every wired reference — from code.
    ///
    /// Done as an editor script rather than by hand because it is deterministic
    /// and re-runnable: if a reference gets unset or a prefab breaks, rebuild
    /// instead of hunting through inspectors. Assets are loaded-or-created, never
    /// blindly recreated, so GUIDs survive a rebuild and nothing else that
    /// references them breaks.
    /// </summary>
    public static partial class RunSceneBuilder
    {
        const string Root = "Assets/_Project";
        const string DataDir = Root + "/Data";
        const string PrefabDir = Root + "/Prefabs";
        const string MatDir = Root + "/Materials";
        const string SceneDir = Root + "/Scenes";
        const string ScenePath = SceneDir + "/Run.unity";

        [MenuItem("ScalePunch/Build Run Scene", false, 0)]
        public static void Build()
        {
            if (!EnsureTextMeshPro()) return;

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            try
            {
                AssetDatabase.StartAssetEditing();
                EnsureFolder(DataDir);
                EnsureFolder(PrefabDir);
                EnsureFolder(MatDir);
                EnsureFolder(SceneDir);
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            var mats = BuildMaterials();
            var data = BuildData();
            var prefabs = BuildPrefabs(data, mats);

            // Persist before the scene references any of this.
            //
            // Deliberately NO AssetDatabase.Refresh() here: a refresh reimports
            // the assets just created, which destroys the managed objects still
            // held in `data` and `prefabs`. Every one of them then throws
            // MissingReferenceException the moment BuildScene touches it.
            AssetDatabase.SaveAssets();

            // Re-resolve from disk regardless, so an import triggered from
            // anywhere else cannot leave stale references being wired into the
            // scene.
            Reacquire(data);
            Reacquire(prefabs);

            BuildScene(data, prefabs, mats);

            Debug.Log("<b>[ScalePunch]</b> Run scene built at " + ScenePath +
                      ". Press Play. Tune CombatTuning and the player's StatSheet from the inspector.");
        }

        // ------------------------------------------------------------------ TMP

        static bool _awaitingTmpImport;

        /// <summary>
        /// The HUD and draft cards are TextMeshPro, which cannot render until its
        /// essential resources are imported once. The .unitypackage ships inside
        /// the uGUI package, so import it directly rather than sending the user
        /// hunting through menus, then resume the build when it lands.
        /// </summary>
        static bool EnsureTextMeshPro()
        {
            if (Resources.Load<TMP_Settings>("TMP Settings") != null) return true;
            if (_awaitingTmpImport) return false;

            string package = FindTmpEssentials();
            if (package == null)
            {
                Debug.LogError("[ScalePunch] Could not locate 'TMP Essential Resources.unitypackage'. " +
                               "Import it manually via Window ▸ TextMeshPro ▸ Import TMP Essential " +
                               "Resources, then run ScalePunch ▸ Build Run Scene again.");
                return false;
            }

            _awaitingTmpImport = true;
            AssetDatabase.importPackageCompleted += OnTmpImported;
            AssetDatabase.importPackageCancelled += OnTmpImportEnded;
            AssetDatabase.importPackageFailed += OnTmpImportFailed;

            Debug.Log("[ScalePunch] Importing TextMeshPro essential resources, then continuing the build...");
            AssetDatabase.ImportPackage(package, false);
            return false;
        }

        static void OnTmpImported(string packageName)
        {
            OnTmpImportEnded(packageName);

            // delayCall so the build runs after the import's asset refresh has
            // settled, rather than mid-import.
            EditorApplication.delayCall += Build;
        }

        static void OnTmpImportFailed(string packageName, string error)
        {
            Debug.LogError($"[ScalePunch] TextMeshPro import failed: {error}");
            OnTmpImportEnded(packageName);
        }

        static void OnTmpImportEnded(string packageName)
        {
            AssetDatabase.importPackageCompleted -= OnTmpImported;
            AssetDatabase.importPackageCancelled -= OnTmpImportEnded;
            AssetDatabase.importPackageFailed -= OnTmpImportFailed;
            _awaitingTmpImport = false;
        }

        static string FindTmpEssentials()
        {
            const string File = "TMP Essential Resources.unitypackage";

            string[] roots =
            {
                Path.Combine(Directory.GetCurrentDirectory(), "Library/PackageCache"),
                Path.Combine(EditorApplication.applicationContentsPath,
                             "Resources/PackageManager/BuiltInPackages")
            };

            foreach (string root in roots)
            {
                if (!Directory.Exists(root)) continue;

                // Checking one known sub-path per package beats recursing the whole
                // cache, which holds tens of thousands of files.
                foreach (string package in Directory.GetDirectories(root))
                {
                    string candidate = Path.Combine(package, "Package Resources", File);
                    if (System.IO.File.Exists(candidate)) return candidate;
                }
            }
            return null;
        }

        // -------------------------------------------------------------- helpers

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;

            string parent = Path.GetDirectoryName(path).Replace(Path.DirectorySeparatorChar, '/');
            string leaf = Path.GetFileName(path);

            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }

        /// <summary>Load-or-create, so rebuilding keeps GUIDs and existing references.</summary>
        static T Asset<T>(string file) where T : ScriptableObject
        {
            string path = $"{DataDir}/{file}.asset";
            T existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null) return existing;

            T created = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(created, path);
            return created;
        }

        /// <summary>
        /// Writes a [SerializeField] private field. Every wiring call goes through
        /// here: a typo'd name warns loudly instead of silently leaving a null
        /// reference to be discovered at runtime.
        /// </summary>
        static void Set(Object target, string field, object value)
        {
            if (target == null) { Debug.LogWarning($"[ScalePunch] null target setting '{field}'"); return; }

            var so = new SerializedObject(target);
            SerializedProperty p = so.FindProperty(field);

            if (p == null)
            {
                Debug.LogWarning($"[ScalePunch] {target.GetType().Name} has no serialized field '{field}'");
                return;
            }

            switch (value)
            {
                case null: p.objectReferenceValue = null; break;
                case int i: p.intValue = i; break;
                case float f: p.floatValue = f; break;
                case double d: p.floatValue = (float)d; break;
                case bool b: p.boolValue = b; break;
                case string s: p.stringValue = s; break;
                case Color c: p.colorValue = c; break;
                case Vector2 v2: p.vector2Value = v2; break;
                case Vector3 v3: p.vector3Value = v3; break;
                case Enum e: p.enumValueIndex = Convert.ToInt32(e); break;
                case Object o: p.objectReferenceValue = o; break;
                default: Debug.LogWarning($"[ScalePunch] unsupported type {value.GetType()} for '{field}'"); return;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetArray(Object target, string field, IList<Object> values)
        {
            var so = new SerializedObject(target);
            SerializedProperty p = so.FindProperty(field);

            if (p == null)
            {
                Debug.LogWarning($"[ScalePunch] {target.GetType().Name} has no serialized field '{field}'");
                return;
            }

            p.arraySize = values.Count;
            for (int i = 0; i < values.Count; i++)
                p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static GameObject Child(string name, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go;
        }
    }
}
