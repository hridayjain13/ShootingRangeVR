using System;

namespace IVRAR.Group02.HumanFactors
{
    /// <summary>
    /// Kennedy Simulator Sickness Questionnaire (SSQ) model.
    /// Evaluates 16 symptoms across Nausea (N), Oculomotor (O), and Disorientation (D) subscales.
    /// Reference: Kennedy et al. (1993), The International Journal of Aviation Psychology.
    /// </summary>
    [Serializable]
    public class KennedySsqAssessment
    {
        // 0 = None, 1 = Slight, 2 = Moderate, 3 = Severe
        public int GeneralDiscomfort;
        public int Fatigue;
        public int Headache;
        public int EyeStrain;
        public int DifficultyFocusing;
        public int SalivationIncreasing;
        public int Sweating;
        public int Nausea;
        public int DifficultyConcentrating;
        public int FullnessOfHead;
        public int BlurredVision;
        public int DizzinessEyesOpen;
        public int DizzinessEyesClosed;
        public int Vertigo;
        public int StomachAwareness;
        public int Burping;

        public struct SsqResult
        {
            public float NauseaScore;
            public float OculomotorScore;
            public float DisorientationScore;
            public float TotalSeverity;
        }

        public SsqResult ComputeScores()
        {
            // Standard Kennedy weighting factors
            int nauseaRaw = GeneralDiscomfort + SalivationIncreasing + Sweating + Nausea + DifficultyConcentrating + StomachAwareness + Burping;
            int oculomotorRaw = GeneralDiscomfort + Fatigue + Headache + EyeStrain + DifficultyFocusing + DifficultyConcentrating + BlurredVision;
            int disorientationRaw = DifficultyFocusing + Nausea + FullnessOfHead + BlurredVision + DizzinessEyesOpen + DizzinessEyesClosed + Vertigo;

            var res = new SsqResult
            {
                NauseaScore = nauseaRaw * 9.54f,
                OculomotorScore = oculomotorRaw * 7.58f,
                DisorientationScore = disorientationRaw * 13.92f,
                TotalSeverity = (nauseaRaw + oculomotorRaw + disorientationRaw) * 3.74f
            };

            return res;
        }
    }
}
