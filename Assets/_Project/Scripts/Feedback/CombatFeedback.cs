using UnityEngine;
using ScalePunch.Combat;
using ScalePunch.Data;
using ScalePunch.Enemies;

namespace ScalePunch.Feedback
{
    /// <summary>
    /// Single subscriber that turns Health events into everything the player
    /// actually perceives. Keeping it in one place means the whole feel of the
    /// game is tunable from one component plus one CombatTuning asset.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public class CombatFeedback : MonoBehaviour
    {
        [SerializeField] CombatTuning tuning;
        [SerializeField] Health health;
        [SerializeField] HitFlash flash;
        [SerializeField] EnemyMovement movement;

        [Tooltip("Vertical offset for the damage number anchor.")]
        [SerializeField] float numberHeight = 1.6f;

        void Reset()
        {
            health = GetComponent<Health>();
            flash = GetComponent<HitFlash>();
            movement = GetComponent<EnemyMovement>();
        }

        void Awake()
        {
            if (health == null) health = GetComponent<Health>();
            if (flash == null) flash = GetComponent<HitFlash>();
            if (movement == null) movement = GetComponent<EnemyMovement>();

            health.Damaged += OnDamaged;
            health.Died += OnDied;
        }

        void OnDestroy()
        {
            health.Damaged -= OnDamaged;
            health.Died -= OnDied;
        }

        void OnDamaged(DamageInfo info) => React(info, killed: false);
        void OnDied(DamageInfo info) => React(info, killed: true);

        void React(DamageInfo info, bool killed)
        {
            if (tuning == null) return;

            if (flash != null && !killed) flash.Flash();

            if (DamageNumberService.Exists)
                DamageNumberService.Instance.Show(
                    info.Amount, info.IsCrit, transform.position + Vector3.up * numberHeight);

            if (movement != null)
                movement.ApplyKnockback(
                    transform.position - info.Origin,
                    info.IsCrit ? tuning.knockbackCrit : tuning.knockbackNormal);

            if (Hitstop.Exists)
                Hitstop.Instance.Freeze(killed  ? tuning.hitstopKill
                                      : info.IsCrit ? tuning.hitstopCrit
                                                    : tuning.hitstopNormal);

            if (CameraShake.Exists)
                CameraShake.Instance.Shake(killed  ? tuning.shakeKill
                                          : info.IsCrit ? tuning.shakeCrit
                                                        : tuning.shakeNormal);
        }
    }
}
