namespace AdvancedRendering.CustomCollisionBenchmark
{
    public enum CustomColliderShape
    {
        Aabb,
        Sphere
    }

    public enum CustomBroadPhaseMode
    {
        BruteForce,
        SpatialHash
    }

    public enum CustomCollisionScenario
    {
        BruteForceAabb,
        BruteForceSphere,
        SpatialHashAabb,
        SpatialHashSphere,
        LayerFilteredSpatialHash,
        TriggerOnlySpatialHash
    }
}
