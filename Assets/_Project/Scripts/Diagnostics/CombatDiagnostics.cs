using UnityEngine;
using ScalePunch.Combat;
using ScalePunch.Enemies;
using ScalePunch.Player;

namespace ScalePunch.Diagnostics
{
    /// <summary>
    /// Prints one line a second describing whether the combat loop is actually
    /// working: are zombies spawning, is one being targeted, are rounds leaving
    /// the barrel, and are they connecting.
    ///
    /// Off by default and editor-only. Toggle with ScalePunch > Combat
    /// Diagnostics, then press Play - it installs itself, so no scene change or
    /// rebuild is needed. It found the 12-degree firing arc that was sending
    /// half of all rounds wide, which was invisible from the Game view.
    ///
    /// Read the numbers as:
    ///   target=NONE while nearest &lt;= range  -> acquisition is broken
    ///   target=NONE while nearest &gt;  range  -> nothing in range yet, fine
    ///   shots/s &gt; 0 with hits/s stuck at 0  -> rounds fire but never connect
    ///   a rising missed count next to hits   -> some rounds expire at the ring
    ///
    /// One caveat when reading it: the sample is taken once a second, so a line
    /// can legitimately show shots and kills alongside target=NONE - the target
    /// was acquired, shot and killed between two samples.
    /// </summary>
    public class CombatDiagnostics : MonoBehaviour
    {
        const float Interval = 1f;

        AutoShoot _weapon;
        float _timer;
        int _shots;
        int _kills;
        int _lastHits;
        int _lastShots;

        public const string EnabledPref = "ScalePunch.CombatDiagnostics";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            // Editor-only and opt-in: a per-second Debug.Log has no business in a
            // build, and a console full of telemetry hides the one error that
            // matters.
#if UNITY_EDITOR
            if (!UnityEditor.EditorPrefs.GetBool(EnabledPref, false)) return;

            var go = new GameObject("~CombatDiagnostics");
            go.AddComponent<CombatDiagnostics>();
#endif
        }

        void Start()
        {
            // Deliberately does not reset WeaponTelemetry - that is owned by the
            // run, and zeroing it here would wipe counts the run is relying on.
            _weapon = FindAnyObjectByType<AutoShoot>();

            if (_weapon == null)
            {
                Debug.LogError("[Diag] No AutoShoot in the scene - the player is missing or " +
                               "this is not the Run scene.");
                enabled = false;
                return;
            }

            _weapon.Fired += OnFired;
            EnemyRegistry.Killed += OnKill;

            Debug.Log($"[Diag] started. range={_weapon.Range:0.#}m");
        }

        void OnDestroy()
        {
            if (_weapon != null) _weapon.Fired -= OnFired;
            EnemyRegistry.Killed -= OnKill;
        }

        void OnFired() => _shots++;
        void OnKill(Enemy e) => _kills++;

        void Update()
        {
            _timer += Time.unscaledDeltaTime;
            if (_timer < Interval) return;
            _timer = 0f;

            // Nearest zombie regardless of range, so "nothing is being targeted"
            // can be told apart from "nothing is close enough to target".
            Enemy nearest = EnemyRegistry.FindNearest(_weapon.transform.position, 999f);
            float nearestDist = nearest == null
                ? -1f
                : Vector3.Distance(_weapon.transform.position, nearest.transform.position);

            string target = _weapon.CurrentTarget == null
                ? "NONE"
                : $"{_weapon.CurrentTarget.Definition?.id} @{Vector3.Distance(_weapon.transform.position, _weapon.CurrentTarget.transform.position):0.#}m";

            int shotsThisSecond = _shots - _lastShots;
            int hitsThisSecond = WeaponTelemetry.Hit - _lastHits;
            _lastShots = _shots;
            _lastHits = WeaponTelemetry.Hit;

            Debug.Log(
                $"[Diag] zombies={EnemyRegistry.Count} " +
                $"nearest={(nearest == null ? "none" : nearestDist.ToString("0.#") + "m")} " +
                $"range={_weapon.Range:0.#}m " +
                $"target={target} | " +
                $"shots/s={shotsThisSecond} hits/s={hitsThisSecond} | " +
                $"totals shots={WeaponTelemetry.Fired} hits={WeaponTelemetry.Hit} " +
                $"expired={WeaponTelemetry.Expired} wasted={WeaponTelemetry.Wasted} " +
                $"acc={WeaponTelemetry.Accuracy:P0} kills={_kills}");
        }
    }
}
