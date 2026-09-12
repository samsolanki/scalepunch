using UnityEngine;
using ScalePunch.Enemies;
using ScalePunch.Player;
using ScalePunch.Weapons;

namespace ScalePunch.Diagnostics
{
    /// <summary>
    /// TEMPORARY. Prints one line a second describing whether the combat loop is
    /// actually working: are zombies spawning, is one being targeted, are rounds
    /// leaving the barrel, and are they connecting.
    ///
    /// It installs itself when play starts, so it needs no scene change and no
    /// rebuild. Delete this file and the two counters in Projectile when the
    /// question is answered.
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

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            var go = new GameObject("~CombatDiagnostics");
            go.AddComponent<CombatDiagnostics>();
        }

        void Start()
        {
            Projectile.TotalHits = 0;
            Projectile.TotalMisses = 0;

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
            int hitsThisSecond = Projectile.TotalHits - _lastHits;
            _lastShots = _shots;
            _lastHits = Projectile.TotalHits;

            Debug.Log(
                $"[Diag] zombies={EnemyRegistry.Count} " +
                $"nearest={(nearest == null ? "none" : nearestDist.ToString("0.#") + "m")} " +
                $"range={_weapon.Range:0.#}m " +
                $"target={target} | " +
                $"shots/s={shotsThisSecond} hits/s={hitsThisSecond} | " +
                $"totals shots={_shots} hits={Projectile.TotalHits} missed={Projectile.TotalMisses} kills={_kills}");
        }
    }
}
