// Minimal UnityEngine surface: only what the shipped files under test actually
// reference. Everything here is the real Unity semantics, not a reinterpretation.
using System;

namespace UnityEngine
{
    public struct Vector3
    {
        public float x, y, z;
        public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }

        public static Vector3 zero => new Vector3(0, 0, 0);
        public static Vector3 up => new Vector3(0, 1, 0);
        public static Vector3 forward => new Vector3(0, 0, 1);

        public float sqrMagnitude => x * x + y * y + z * z;
        public float magnitude => (float)Math.Sqrt(sqrMagnitude);

        public Vector3 normalized
        {
            get { float m = magnitude; return m < 1e-9f ? zero : new Vector3(x / m, y / m, z / m); }
        }

        public void Normalize()
        {
            float m = magnitude;
            if (m < 1e-9f) { x = y = z = 0f; return; }
            x /= m; y /= m; z /= m;
        }

        public static float Dot(Vector3 a, Vector3 b) => a.x * b.x + a.y * b.y + a.z * b.z;

        public static Vector3 operator +(Vector3 a, Vector3 b) => new Vector3(a.x + b.x, a.y + b.y, a.z + b.z);
        public static Vector3 operator -(Vector3 a, Vector3 b) => new Vector3(a.x - b.x, a.y - b.y, a.z - b.z);
        public static Vector3 operator *(Vector3 a, float s) => new Vector3(a.x * s, a.y * s, a.z * s);
        public static Vector3 operator *(float s, Vector3 a) => a * s;
        public static Vector3 operator /(Vector3 a, float s) => new Vector3(a.x / s, a.y / s, a.z / s);

        public override string ToString() => $"({x:0.00}, {y:0.00}, {z:0.00})";
    }

    public static class Mathf
    {
        public const float Deg2Rad = 0.01745329f;
        public const float Rad2Deg = 57.29578f;
        public static float Abs(float v) => Math.Abs(v);
        public static float Max(float a, float b) => Math.Max(a, b);
        public static float Min(float a, float b) => Math.Min(a, b);
        public static float Sqrt(float v) => (float)Math.Sqrt(v);
        public static float Clamp01(float v) => v < 0 ? 0 : (v > 1 ? 1 : v);
        public static float Clamp(float v, float lo, float hi) => v < lo ? lo : (v > hi ? hi : v);
        public static float Atan2(float y, float x) => (float)Math.Atan2(y, x);
        public static float Cos(float v) => (float)Math.Cos(v);
        public static float Sin(float v) => (float)Math.Sin(v);
        public static float Pow(float a, float b) => (float)Math.Pow(a, b);
    }

    public static class Debug
    {
        public static void Log(object m) { }
        public static void LogWarning(object m) { }
        public static void LogError(object m) { }
    }
}
