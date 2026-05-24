using UnityEngine;

namespace AdvancedRendering.CustomCollisionBenchmark
{
    public struct CustomCollisionBody
    {
        public Vector3 Position;
        public Vector3 PreviousPosition;
        public Vector3 Velocity;
        public Vector3 HalfExtents;
        public float Radius;
        public int Layer;
        public bool IsTrigger;
        public CustomColliderShape Shape;

        public Bounds Bounds
        {
            get
            {
                Vector3 size = Shape == CustomColliderShape.Sphere
                    ? Vector3.one * Radius * 2f
                    : HalfExtents * 2f;

                return new Bounds(Position, size);
            }
        }
    }
}
