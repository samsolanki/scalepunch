using UnityEngine;

namespace ScalePunch.Combat
{
    /// <summary>
    /// The one intercept solver in the project.
    ///
    /// It lives here because there were two: the turret solved a proper
    /// intercept and launched the round at where the target *would be*, and then
    /// the round's own homing steered it back toward where the target *was*,
    /// every frame, until the lead was gone. Two pieces of aiming code, each
    /// correct in isolation, cancelling each other out.
    /// </summary>
    public static class Ballistics
    {
        /// <summary>
        /// Where a projectile leaving <paramref name="origin"/> now at
        /// <paramref name="speed"/> meets a target moving at constant velocity.
        ///
        /// Returns the target's current position when there is no solution — a
        /// target outrunning the projectile, or closing straight down the barrel
        /// where leading it changes nothing.
        ///
        /// Solves |D + Vt| = st for t, which is
        /// (V·V - s²)t² + 2(D·V)t + D·D = 0.
        /// </summary>
        public static Vector3 Intercept(Vector3 origin, Vector3 targetPosition,
                                        Vector3 targetVelocity, float speed,
                                        float maxLeadSeconds = 1f)
        {
            Vector3 delta = targetPosition - origin;
            delta.y = 0f;
            targetVelocity.y = 0f;

            float a = Vector3.Dot(targetVelocity, targetVelocity) - speed * speed;
            float b = 2f * Vector3.Dot(delta, targetVelocity);
            float c = Vector3.Dot(delta, delta);
            float time;

            if (Mathf.Abs(a) < 0.0001f)
            {
                // Target speed equals projectile speed — the quadratic degenerates.
                if (Mathf.Abs(b) < 0.0001f) return targetPosition;
                time = -c / b;
            }
            else
            {
                float discriminant = b * b - 4f * a * c;
                if (discriminant < 0f) return targetPosition;

                float root = Mathf.Sqrt(discriminant);
                float t1 = (-b + root) / (2f * a);
                float t2 = (-b - root) / (2f * a);

                // Soonest positive intercept.
                time = Mathf.Min(t1, t2);
                if (time < 0f) time = Mathf.Max(t1, t2);
            }

            if (time < 0f) return targetPosition;

            time = Mathf.Min(time, maxLeadSeconds);
            return targetPosition + targetVelocity * time;
        }
    }
}
