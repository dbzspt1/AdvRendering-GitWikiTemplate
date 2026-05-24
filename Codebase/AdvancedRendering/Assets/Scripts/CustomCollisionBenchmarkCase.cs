using System;
using UnityEngine;

namespace AdvancedRendering.CustomCollisionBenchmark
{
    [Serializable]
    public sealed class CustomCollisionBenchmarkCase
    {
        public string name = "Custom collision brute force AABB 1000";
        public CustomCollisionScenario scenario = CustomCollisionScenario.BruteForceAabb;
        [Min(2)] public int objectCount = 1000;
        [Min(1)] public int repetitions = 3;
        [Min(1)] public int measuredSteps = 240;
        [Min(1)] public int randomSeed = 12345;
        [Min(0.001f)] public float deltaTime = 0.0166667f;
        [Min(1f)] public float worldSize = 35f;
        [Min(0.1f)] public float maxSpeed = 4f;
        [Min(0.1f)] public float spatialCellSize = 2f;
    }
}
