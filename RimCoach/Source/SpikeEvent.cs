using System;
using Verse;

namespace RimCoach
{
    public class SpikeEvent
    {
        public string MethodKey;
        public PatchDef PatchDef;
        public double SpikeTimeMs;
        public DateTime Timestamp;

        public override string ToString()
        {
            string label = PatchDef?.label ?? MethodKey;
            return $"{label} spiked to {SpikeTimeMs:F2} ms at {Timestamp}";
        }
    }
}
