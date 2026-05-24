namespace AdvancedRendering.CustomCollisionBenchmark
{
    public readonly struct CustomCollisionSample
    {
        public readonly string CaseName;
        public readonly CustomCollisionScenario Scenario;
        public readonly int ObjectCount;
        public readonly int Repetition;
        public readonly int Step;
        public readonly double StepMs;
        public readonly int PairChecks;
        public readonly int Collisions;
        public readonly int TriggerOverlaps;
        public readonly int SpatialCellsUsed;

        public CustomCollisionSample(
            string caseName,
            CustomCollisionScenario scenario,
            int objectCount,
            int repetition,
            int step,
            double stepMs,
            int pairChecks,
            int collisions,
            int triggerOverlaps,
            int spatialCellsUsed)
        {
            CaseName = caseName;
            Scenario = scenario;
            ObjectCount = objectCount;
            Repetition = repetition;
            Step = step;
            StepMs = stepMs;
            PairChecks = pairChecks;
            Collisions = collisions;
            TriggerOverlaps = triggerOverlaps;
            SpatialCellsUsed = spatialCellsUsed;
        }
    }
}
