using System.Collections.Generic;
using Verse;

namespace RimCoach
{
    public class ProfiledMethod
    {
        public string Key;
        public PatchDef Def;

        public double AverageTimeMs;
        public double StdDevMs;
        public int CallCount;

        private readonly Queue<double> recentSamples = new Queue<double>();
        private const int MaxSamples = 50;

        private double sumSamples = 0.0;
        private double sumSquares = 0.0;

        public int TotalSamples => recentSamples.Count;

        public void AddSample(double ms)
        {
            CallCount++;

            if (recentSamples.Count >= MaxSamples)
            {
                double removed = recentSamples.Dequeue();
                sumSamples -= removed;
                sumSquares -= removed * removed;
            }

            recentSamples.Enqueue(ms);
            sumSamples += ms;
            sumSquares += ms * ms;

            UpdateRollingStats();
        }
        public void UpdateRollingStats()
        {
            if (recentSamples.Count == 0)
            {
                AverageTimeMs = 0.0;
                StdDevMs = 0.0;
                return;
            }

            AverageTimeMs = sumSamples / recentSamples.Count;

            double varianceSum = 0.0;
            foreach (var sample in recentSamples)
            {
                varianceSum += (sample - AverageTimeMs) * (sample - AverageTimeMs);
            }

            StdDevMs = System.Math.Sqrt(varianceSum / recentSamples.Count);
        }

        public ProfiledMethod Clone()
        {
            return new ProfiledMethod
            {
                Key = this.Key,
                Def = this.Def,
                AverageTimeMs = this.AverageTimeMs,
                StdDevMs = this.StdDevMs,
                CallCount = this.CallCount
            };
        }
    }
}
