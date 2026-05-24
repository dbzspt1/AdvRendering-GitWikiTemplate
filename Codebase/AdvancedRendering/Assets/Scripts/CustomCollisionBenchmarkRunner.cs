using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace AdvancedRendering.CustomCollisionBenchmark
{
    public sealed class CustomCollisionBenchmarkRunner : MonoBehaviour
    {
        [SerializeField] private bool runOnStart = true;
        [SerializeField] private bool rebuildDefaultCasesOnRun = true;
        [SerializeField] private string outputFolderName = "CustomCollisionBenchmark";
        [SerializeField] private List<CustomCollisionBenchmarkCase> benchmarkCases = new List<CustomCollisionBenchmarkCase>();

        private readonly List<CustomCollisionSample> samples = new List<CustomCollisionSample>(32768);
        private readonly CustomCollisionWorld world = new CustomCollisionWorld();

        private void Reset()
        {
            benchmarkCases = CreateDefaultCases();
        }

        private void Start()
        {
            if (runOnStart)
            {
                StartCoroutine(RunSuite());
            }
        }

        [ContextMenu("Run Custom Collision Benchmark Suite")]
        public void RunFromContextMenu()
        {
            StopAllCoroutines();
            StartCoroutine(RunSuite());
        }

        private IEnumerator RunSuite()
        {
            if (rebuildDefaultCasesOnRun || benchmarkCases == null || benchmarkCases.Count == 0)
            {
                benchmarkCases = CreateDefaultCases();
            }

            samples.Clear();

            for (int caseIndex = 0; caseIndex < benchmarkCases.Count; caseIndex++)
            {
                CustomCollisionBenchmarkCase benchmarkCase = benchmarkCases[caseIndex];
                Debug.Log($"Running custom collision benchmark: {benchmarkCase.name}");

                for (int repetition = 0; repetition < benchmarkCase.repetitions; repetition++)
                {
                    world.Generate(benchmarkCase);
                    yield return null;

                    for (int step = 0; step < benchmarkCase.measuredSteps; step++)
                    {
                        Stopwatch stopwatch = Stopwatch.StartNew();
                        CustomCollisionWorld.StepResult result = world.Step(benchmarkCase);
                        stopwatch.Stop();

                        samples.Add(new CustomCollisionSample(
                            benchmarkCase.name,
                            benchmarkCase.scenario,
                            benchmarkCase.objectCount,
                            repetition,
                            step,
                            stopwatch.Elapsed.TotalMilliseconds,
                            result.PairChecks,
                            result.Collisions,
                            result.TriggerOverlaps,
                            result.SpatialCellsUsed));
                    }

                    yield return null;
                }
            }

            string outputPath = CreateOutputPath();
            CustomCollisionCsvWriter.Write(outputPath, samples);
            Debug.Log($"Custom collision benchmark completed. Samples: {samples.Count}. CSV: {outputPath}");
        }

        private string CreateOutputPath()
        {
            string folder = Path.Combine(Application.persistentDataPath, outputFolderName);
            string fileName = "custom_collision_benchmark_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".csv";
            return Path.Combine(folder, fileName);
        }

        public static List<CustomCollisionBenchmarkCase> CreateDefaultCases()
        {
            int[] objectCounts = { 500, 1000, 2000, 4000, 5000 };
            var cases = new List<CustomCollisionBenchmarkCase>();

            foreach (int objectCount in objectCounts)
            {
                cases.Add(CreateCase($"Brute force AABB {objectCount}", CustomCollisionScenario.BruteForceAabb, objectCount));
                cases.Add(CreateCase($"Spatial hash AABB {objectCount}", CustomCollisionScenario.SpatialHashAabb, objectCount));
                cases.Add(CreateCase($"Brute force sphere {objectCount}", CustomCollisionScenario.BruteForceSphere, objectCount));
                cases.Add(CreateCase($"Spatial hash sphere {objectCount}", CustomCollisionScenario.SpatialHashSphere, objectCount));
                cases.Add(CreateCase($"Layer filtered spatial hash {objectCount}", CustomCollisionScenario.LayerFilteredSpatialHash, objectCount));
                cases.Add(CreateCase($"Trigger only spatial hash {objectCount}", CustomCollisionScenario.TriggerOnlySpatialHash, objectCount));
            }

            return cases;
        }

        private static CustomCollisionBenchmarkCase CreateCase(
            string name,
            CustomCollisionScenario scenario,
            int objectCount)
        {
            return new CustomCollisionBenchmarkCase
            {
                name = name,
                scenario = scenario,
                objectCount = objectCount,
                repetitions = 3,
                measuredSteps = 240,
                randomSeed = 12345,
                deltaTime = 0.0166667f,
                worldSize = 28f,
                maxSpeed = 4f,
                spatialCellSize = 2f
            };
        }
    }
}
