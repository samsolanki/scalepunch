using UnityEngine;

namespace ScalePunch.Enemies
{
    public class FakeTransform { public Vector3 position; }

    public class Enemy
    {
        public FakeTransform transform = new FakeTransform();
        public bool IsDead { get; set; }
        public float BodyRadius { get; set; } = 0.5f;
        public string Id = "zombie";
    }
}
