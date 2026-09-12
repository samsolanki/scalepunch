using UnityEditor;
using ScalePunch.Diagnostics;

namespace ScalePunch.EditorTools
{
    /// <summary>Menu toggle for the per-second combat telemetry.</summary>
    static class CombatDiagnosticsToggle
    {
        const string MenuPath = "ScalePunch/Combat Diagnostics";

        [MenuItem(MenuPath, false, 20)]
        static void Toggle()
        {
            bool enabled = !EditorPrefs.GetBool(CombatDiagnostics.EnabledPref, false);
            EditorPrefs.SetBool(CombatDiagnostics.EnabledPref, enabled);

            UnityEngine.Debug.Log($"[ScalePunch] Combat diagnostics {(enabled ? "ON" : "OFF")}. " +
                                  "Press Play to see per-second targeting and hit-rate telemetry.");
        }

        [MenuItem(MenuPath, true)]
        static bool ToggleValidate()
        {
            Menu.SetChecked(MenuPath, EditorPrefs.GetBool(CombatDiagnostics.EnabledPref, false));
            return true;
        }
    }
}
