using System.Collections.Generic;
using UnityEngine;

namespace AdvancedRendering.CustomCollisionBenchmark
{
    public sealed class CustomCollisionWorld
    {
        private readonly List<CustomCollisionBody> bodies = new List<CustomCollisionBody>(8192);
        private readonly Dictionary<Vector3Int, List<int>> spatialGrid = new Dictionary<Vector3Int, List<int>>(8192);
        private readonly HashSet<long> testedPairs = new HashSet<long>();

        public int Count => bodies.Count;

        public void Generate(CustomCollisionBenchmarkCase benchmarkCase)
        {
            bodies.Clear();
            Random.InitState(benchmarkCase.randomSeed);

            CustomColliderShape shape = GetShape(benchmarkCase.scenario);
            bool triggerOnly = benchmarkCase.scenario == CustomCollisionScenario.TriggerOnlySpatialHash;

            for (int i = 0; i < benchmarkCase.objectCount; i++)
            {
                Vector3 position = new Vector3(
                    Random.Range(-benchmarkCase.worldSize, benchmarkCase.worldSize),
                    Random.Range(-benchmarkCase.worldSize, benchmarkCase.worldSize),
                    Random.Range(-benchmarkCase.worldSize, benchmarkCase.worldSize));

                Vector3 velocity = Random.insideUnitSphere * benchmarkCase.maxSpeed;

                bodies.Add(new CustomCollisionBody
                {
                    Position = position,
                    PreviousPosition = position,
                    Velocity = velocity,
                    HalfExtents = Vector3.one * Random.Range(0.35f, 0.65f),
                    Radius = Random.Range(0.35f, 0.65f),
                    Layer = i % 2,
                    IsTrigger = triggerOnly,
                    Shape = shape
                });
            }
        }

        public StepResult Step(CustomCollisionBenchmarkCase benchmarkCase)
        {
            MoveBodies(benchmarkCase.deltaTime, benchmarkCase.worldSize);

            CustomBroadPhaseMode broadPhase = GetBroadPhase(benchmarkCase.scenario);
            return broadPhase == CustomBroadPhaseMode.SpatialHash
                ? StepSpatialHash(benchmarkCase)
                : StepBruteForce(benchmarkCase);
        }

        private StepResult StepBruteForce(CustomCollisionBenchmarkCase benchmarkCase)
        {
            int pairChecks = 0;
            int collisions = 0;
            int triggerOverlaps = 0;

            for (int a = 0; a < bodies.Count - 1; a++)
            {
                for (int b = a + 1; b < bodies.Count; b++)
                {
                    TestPair(a, b, benchmarkCase.scenario, ref pairChecks, ref collisions, ref triggerOverlaps);
                }
            }

            return new StepResult(pairChecks, collisions, triggerOverlaps, 0);
        }

        private StepResult StepSpatialHash(CustomCollisionBenchmarkCase benchmarkCase)
        {
            BuildSpatialGrid(benchmarkCase.spatialCellSize);
            testedPairs.Clear();

            int pairChecks = 0;
            int collisions = 0;
            int triggerOverlaps = 0;

            foreach (List<int> cellBodies in spatialGrid.Values)
            {
                for (int i = 0; i < cellBodies.Count - 1; i++)
                {
                    for (int j = i + 1; j < cellBodies.Count; j++)
                    {
                        int a = cellBodies[i];
                        int b = cellBodies[j];
                        long key = PairKey(a, b);

                        if (!testedPairs.Add(key))
                        {
                            continue;
                        }

                        TestPair(a, b, benchmarkCase.scenario, ref pairChecks, ref collisions, ref triggerOverlaps);
                    }
                }
            }

            return new StepResult(pairChecks, collisions, triggerOverlaps, spatialGrid.Count);
        }

        private void TestPair(
            int a,
            int b,
            CustomCollisionScenario scenario,
            ref int pairChecks,
            ref int collisions,
            ref int triggerOverlaps)
        {
            CustomCollisionBody bodyA = bodies[a];
            CustomCollisionBody bodyB = bodies[b];

            if (scenario == CustomCollisionScenario.LayerFilteredSpatialHash && bodyA.Layer != bodyB.Layer)
            {
                return;
            }

            pairChecks++;

            bool overlaps = bodyA.Shape == CustomColliderShape.Sphere
                ? SpheresOverlap(bodyA, bodyB)
                : AabbsOverlap(bodyA.Bounds, bodyB.Bounds);

            if (!overlaps)
            {
                return;
            }

            if (bodyA.IsTrigger || bodyB.IsTrigger)
            {
                triggerOverlaps++;
                return;
            }

            collisions++;
            ResolveSimpleCollision(a, b);
        }

        private void MoveBodies(float deltaTime, float worldSize)
        {
            for (int i = 0; i < bodies.Count; i++)
            {
                CustomCollisionBody body = bodies[i];
                body.PreviousPosition = body.Position;
                body.Position += body.Velocity * deltaTime;

                BounceAxis(ref body.Position.x, ref body.Velocity.x, worldSize);
                BounceAxis(ref body.Position.y, ref body.Velocity.y, worldSize);
                BounceAxis(ref body.Position.z, ref body.Velocity.z, worldSize);

                bodies[i] = body;
            }
        }

        private void BuildSpatialGrid(float cellSize)
        {
            spatialGrid.Clear();

            for (int i = 0; i < bodies.Count; i++)
            {
                Bounds bounds = bodies[i].Bounds;
                Vector3Int min = Cell(bounds.min, cellSize);
                Vector3Int max = Cell(bounds.max, cellSize);

                for (int x = min.x; x <= max.x; x++)
                {
                    for (int y = min.y; y <= max.y; y++)
                    {
                        for (int z = min.z; z <= max.z; z++)
                        {
                            var key = new Vector3Int(x, y, z);
                            if (!spatialGrid.TryGetValue(key, out List<int> cellBodies))
                            {
                                cellBodies = new List<int>(8);
                                spatialGrid.Add(key, cellBodies);
                            }

                            cellBodies.Add(i);
                        }
                    }
                }
            }
        }

        private void ResolveSimpleCollision(int a, int b)
        {
            CustomCollisionBody bodyA = bodies[a];
            CustomCollisionBody bodyB = bodies[b];

            Vector3 delta = bodyA.Position - bodyB.Position;
            if (delta.sqrMagnitude < 0.0001f)
            {
                delta = Vector3.up;
            }

            Vector3 normal = delta.normalized;
            bodyA.Velocity = Vector3.Reflect(bodyA.Velocity, normal);
            bodyB.Velocity = Vector3.Reflect(bodyB.Velocity, -normal);

            bodies[a] = bodyA;
            bodies[b] = bodyB;
        }

        private static bool AabbsOverlap(Bounds a, Bounds b)
        {
            return a.min.x <= b.max.x && a.max.x >= b.min.x
                && a.min.y <= b.max.y && a.max.y >= b.min.y
                && a.min.z <= b.max.z && a.max.z >= b.min.z;
        }

        private static bool SpheresOverlap(CustomCollisionBody a, CustomCollisionBody b)
        {
            float radius = a.Radius + b.Radius;
            return (a.Position - b.Position).sqrMagnitude <= radius * radius;
        }

        private static void BounceAxis(ref float position, ref float velocity, float limit)
        {
            if (position > limit)
            {
                position = limit;
                velocity = -Mathf.Abs(velocity);
            }
            else if (position < -limit)
            {
                position = -limit;
                velocity = Mathf.Abs(velocity);
            }
        }

        private static Vector3Int Cell(Vector3 position, float cellSize)
        {
            return new Vector3Int(
                Mathf.FloorToInt(position.x / cellSize),
                Mathf.FloorToInt(position.y / cellSize),
                Mathf.FloorToInt(position.z / cellSize));
        }

        private static long PairKey(int a, int b)
        {
            int min = Mathf.Min(a, b);
            int max = Mathf.Max(a, b);
            return ((long)min << 32) | (uint)max;
        }

        private static CustomColliderShape GetShape(CustomCollisionScenario scenario)
        {
            return scenario == CustomCollisionScenario.BruteForceSphere
                || scenario == CustomCollisionScenario.SpatialHashSphere
                    ? CustomColliderShape.Sphere
                    : CustomColliderShape.Aabb;
        }

        private static CustomBroadPhaseMode GetBroadPhase(CustomCollisionScenario scenario)
        {
            return scenario == CustomCollisionScenario.BruteForceAabb
                || scenario == CustomCollisionScenario.BruteForceSphere
                    ? CustomBroadPhaseMode.BruteForce
                    : CustomBroadPhaseMode.SpatialHash;
        }

        public readonly struct StepResult
        {
            public readonly int PairChecks;
            public readonly int Collisions;
            public readonly int TriggerOverlaps;
            public readonly int SpatialCellsUsed;

            public StepResult(int pairChecks, int collisions, int triggerOverlaps, int spatialCellsUsed)
            {
                PairChecks = pairChecks;
                Collisions = collisions;
                TriggerOverlaps = triggerOverlaps;
                SpatialCellsUsed = spatialCellsUsed;
            }
        }
    }
}
