using System;

namespace IVRAR.Group02.HumanFactors
{
    /// <summary>
    /// Situational Awareness Global Assessment Technique (SAGAT) probe model.
    /// Evaluates Level 1 (Perception), Level 2 (Comprehension), and Level 3 (Projection)
    /// situational awareness under sensory stress and low-visibility conditions.
    /// Reference: Endsley (1995).
    /// </summary>
    [Serializable]
    public class SagatAssessment
    {
        public bool RecalledTargetHostility;     // Was target hostile or civilian?
        public bool ActualTargetHostility;

        public bool RecalledTargetSideLeft;       // Was target located left or right of center?
        public bool ActualTargetSideLeft;

        public bool RecalledWeaponVisible;        // Was weapon clearly visible under illumination?
        public bool ActualWeaponVisible;

        public int CorrectProbes
        {
            get
            {
                int score = 0;
                if (RecalledTargetHostility == ActualTargetHostility) score++;
                if (RecalledTargetSideLeft == ActualTargetSideLeft) score++;
                if (RecalledWeaponVisible == ActualWeaponVisible) score++;
                return score;
            }
        }

        public int TotalProbes => 3;
        public float SituationalAwarenessPercentage => (CorrectProbes / (float)TotalProbes) * 100.0f;
    }
}
