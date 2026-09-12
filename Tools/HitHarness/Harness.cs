using System;
using System.Collections.Generic;
using UnityEngine;
using ScalePunch.Combat;
using ScalePunch.Enemies;

// Drives the REAL Ballistics.Intercept and the REAL
// EnemyRegistry.FindFirstAlongSegment through a full engagement, frame by
// frame, with the shipped constants. Everything below the stub line is the
// game's own code.
static class Harness
{
    const float DT            = 1f / 60f;
    const float SPEED         = 45f;    // StatSheet ProjectileSpeed
    const float LOCK_ON       = 720f;   // Weapon_Pistol lockOnDegreesPerSecond
    const float ROUND_RADIUS  = 0.12f;  // Weapon_Pistol hitRadius
    const float LIFETIME      = 2.5f;
    const float RANGE         = 9f;
    const float TURN_SPEED    = 720f;
    const float FIRE_INTERVAL = 1f / 3f;
    const float MUZZLE_FWD    = 1.16f;
    const float MIN_ARC = 1.5f, MAX_ARC = 25f;
    const float INACCURACY = 1f;

    class Round
    {
        public Vector3 pos, dir;
        public float life = LIFETIME, rangeLeft = RANGE;
        public List<Enemy> alreadyHit = new List<Enemy>();
    }

    static Vector3 Rot(Vector3 v, float deg)
    {
        float r = deg * Mathf.Deg2Rad, c = Mathf.Cos(r), s = Mathf.Sin(r);
        return new Vector3(v.x * c - v.z * s, 0f, v.x * s + v.z * c);
    }

    static float AngleBetween(Vector3 a, Vector3 b)
    {
        float d = Mathf.Clamp(Vector3.Dot(a.normalized, b.normalized), -1f, 1f);
        return (float)Math.Acos(d) * Mathf.Rad2Deg;
    }

    static Vector3 RotateTowards(Vector3 cur, Vector3 target, float maxDeg)
    {
        float a = AngleBetween(cur, target);
        if (a <= maxDeg || a < 1e-5f) return target.normalized;
        float cross = cur.x * target.z - cur.z * target.x;
        return Rot(cur.normalized, maxDeg * (cross > 0 ? 1f : -1f)).normalized;
    }

    static void Engage(float zombieSpeed, float bodyRadius, float approachDeg, int seed,
                       ref int fired, ref int hit, ref int missed, float startDistance = 16f)
    {
        var rnd = new Random(seed);
        EnemyRegistry.Clear();

        var z = new Enemy { BodyRadius = bodyRadius };
        float ang = approachDeg * Mathf.Deg2Rad;
        z.transform.position = new Vector3(Mathf.Cos(ang) * startDistance, 0f, Mathf.Sin(ang) * startDistance);
        EnemyRegistry.Register(z);

        Vector3 turretDir = new Vector3(0, 0, 1);
        Vector3 prev = z.transform.position;
        float cooldown = 0f;
        var rounds = new List<Round>();

        for (int f = 0; f < (int)(25f / DT); f++)
        {
            // zombie walks in, with a crossing component - worst case for lead
            Vector3 toPlayer = (Vector3.zero - z.transform.position).normalized;
            Vector3 drift = Rot(toPlayer, 25f);
            Vector3 dir = (toPlayer * 0.8f + drift * 0.2f).normalized;
            z.transform.position = z.transform.position + dir * (zombieSpeed * DT);
            Vector3 zvel = (z.transform.position - prev) * (1f / DT);
            prev = z.transform.position;
            if (z.transform.position.magnitude < 0.6f) break;

            // ---- turret (AutoShoot.SlewToTarget / Fire) ----
            cooldown -= DT;
            if (z.transform.position.magnitude <= RANGE && !z.IsDead)
            {
                Vector3 aim = Ballistics.Intercept(Vector3.zero, z.transform.position, zvel, SPEED);
                turretDir = RotateTowards(turretDir, aim, TURN_SPEED * DT);
                float dist = aim.magnitude;

                if (dist <= RANGE)
                {
                    float angSize = Mathf.Atan2(ROUND_RADIUS + z.BodyRadius, Mathf.Max(0.01f, dist)) * Mathf.Rad2Deg;
                    float steerBudget = LOCK_ON * (dist / SPEED) * 0.5f;
                    float allowed = Mathf.Clamp(angSize + steerBudget, MIN_ARC, MAX_ARC);

                    if (cooldown <= 0f && AngleBetween(turretDir, aim) <= allowed)
                    {
                        float err = (float)(rnd.NextDouble() * 2 - 1) * INACCURACY;

                        // AutoShoot.Fire: the spawn slides down the barrel as the
                        // target closes, so the round never starts past it.
                        float spawnAlong = Mathf.Min(MUZZLE_FWD, dist * 0.5f);

                        rounds.Add(new Round
                        {
                            pos = turretDir * spawnAlong,
                            dir = Rot(aim.normalized, err).normalized
                        });
                        fired++;
                        cooldown = FIRE_INTERVAL;
                    }
                }
            }

            // ---- rounds (Projectile.Update / Steer / Sweep) ----
            for (int i = rounds.Count - 1; i >= 0; i--)
            {
                Round r = rounds[i];
                r.life -= DT;
                if (r.life <= 0f) { rounds.RemoveAt(i); missed++; continue; }

                if (!z.IsDead)
                {
                    Vector3 aim = Ballistics.Intercept(r.pos, z.transform.position, zvel, SPEED);
                    Vector3 toAim = aim - r.pos;
                    if (toAim.sqrMagnitude > 1e-6f)
                        r.dir = RotateTowards(r.dir, toAim, LOCK_ON * DT);
                }

                Vector3 from = r.pos;
                Vector3 to = from + r.dir * (SPEED * DT);

                Enemy struck = EnemyRegistry.FindFirstAlongSegment(from, to, ROUND_RADIUS, r.alreadyHit);
                if (struck != null) { rounds.RemoveAt(i); hit++; continue; }

                r.pos = to;
            }
        }
        EnemyRegistry.Unregister(z);
    }

    static int Main()
    {
        var tiers = new[]
        {
            new { Name = "Shambler", Speed = 2.5f, Scale = 1.00f },
            new { Name = "Runner",   Speed = 4.5f, Scale = 0.85f },
            new { Name = "Brute",    Speed = 1.4f, Scale = 1.45f },
            new { Name = "Boss",     Speed = 1.6f, Scale = 2.20f }
        };

        Console.WriteLine("  tier      body    fired    hit  missed   rate");
        int failures = 0, tf = 0, th = 0, tm = 0;

        foreach (var t in tiers)
        {
            int fired = 0, hit = 0, missed = 0;
            float body = 0.5f * t.Scale;
            for (int i = 0; i < 90; i++)
                Engage(t.Speed, body, i * 4f, i, ref fired, ref hit, ref missed);

            // Point blank. The barrel is 1.16 m long, so a target inside that is
            // the case where a round can spawn PAST what it was fired at — which
            // the far-approach cases never reach and so never caught.
            for (int i = 0; i < 60; i++)
                Engage(t.Speed, body, i * 6f, 1000 + i, ref fired, ref hit, ref missed,
                       startDistance: 0.3f + (i % 5) * 0.25f);

            float rate = fired == 0 ? 0 : 100f * hit / fired;
            Console.WriteLine($"  {t.Name,-9} {body,5:0.00} {fired,8} {hit,6} {missed,7}  {rate,5:0.0}%");

            // Every round must connect, not merely "not be logged as a miss".
            // The first version failed only on `missed > 0`, and a round that
            // spawned past its target and spent its life turning around is not
            // counted as a miss — it is still in the air when the engagement
            // ends. That let a 92% hit rate report PASS.
            if (hit < fired) failures++;
            tf += fired; th += hit; tm += missed;
        }

        Console.WriteLine($"  {"TOTAL",-9} {"",5} {tf,8} {th,6} {tm,7}  {(100f * th / tf),5:0.0}%");
        Console.WriteLine();
        float total = tf == 0 ? 0f : 100f * th / tf;
        Console.WriteLine(failures == 0
            ? "PASS - every round connected, at every range including point blank."
            : $"FAIL - {failures} tier(s) below 100%. Overall {total:0.0}%, {tf - th} rounds never landed.");
        return failures == 0 ? 0 : 1;
    }
}
