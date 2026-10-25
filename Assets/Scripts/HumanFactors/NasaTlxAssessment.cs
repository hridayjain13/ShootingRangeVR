using System;

namespace IVRAR.Group02.HumanFactors
{
    /// <summary>
    /// Multi-dimensional NASA Task Load Index (NASA-TLX) assessment data structure.
    /// Captures the 6 standardized subscales (Mental Demand, Physical Demand, Temporal Demand,
    /// Performance, Effort, Frustration) and provides Raw TLX (RTLX) scoring.
    /// Reference: Hart & Staveland (1988).
    /// </summary>
    [Serializable]
    public class NasaTlxAssessment
    {
        public float MentalDemand;     // 0 - 100
        public float PhysicalDemand;   // 0 - 100
        public float TemporalDemand;   // 0 - 100
        public float Performance;      // 0 - 100 (0=Good, 100=Poor)
        public float Effort;           // 0 - 100
        public float Frustration;      // 0 - 100

        public float ComputeRawTlxScore()
        {
            return (MentalDemand + PhysicalDemand + TemporalDemand + Performance + Effort + Frustration) / 6.0f;
        }

        public float[] ToArray()
        {
            return new float[] { MentalDemand, PhysicalDemand, TemporalDemand, Performance, Effort, Frustration };
        }

        public void FromArray(float[] values)
        {
            if (values == null || values.Length < 6) return;
            MentalDemand = values[0];
            PhysicalDemand = values[1];
            TemporalDemand = values[2];
            Performance = values[3];
            Effort = values[4];
            Frustration = values[5];
        }
    }
}
