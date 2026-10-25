using System;
using System.Globalization;

namespace IVRAR.Group02.HumanFactors
{
    /// <summary>
    /// Kennedy Simulator Sickness Questionnaire (SSQ) model.
    /// Evaluates 16 symptoms across Nausea (N), Oculomotor (O), and Disorientation (D) subscales.
    /// Supports pre-session and post-session administration, delta calculation, missing-item tracking,
    /// and completion status.
    /// Reference: Kennedy, R. S., Lane, N. E., Berbaum, K. S., & Lilienthal, M. G. (1993).
    /// Simulator Sickness Questionnaire: An enhanced method for quantifying simulator sickness.
    /// The International Journal of Aviation Psychology, 3(3), 203-220.
    /// </summary>
    [Serializable]
    public class KennedySsqAssessment
    {
        // 0 = None, 1 = Slight, 2 = Moderate, 3 = Severe. Default -1 indicates unanswered/missing.
        public int GeneralDiscomfort = -1;
        public int Fatigue = -1;
        public int Headache = -1;
        public int EyeStrain = -1;
        public int DifficultyFocusing = -1;
        public int SalivationIncreasing = -1;
        public int Sweating = -1;
        public int Nausea = -1;
        public int DifficultyConcentrating = -1;
        public int FullnessOfHead = -1;
        public int BlurredVision = -1;
        public int DizzinessEyesOpen = -1;
        public int DizzinessEyesClosed = -1;
        public int Vertigo = -1;
        public int StomachAwareness = -1;
        public int Burping = -1;

        public AssessmentTiming Timing = AssessmentTiming.PostSession;
        public CompletionStatus Status = CompletionStatus.Completed;
        public string ParticipantCode = "P000";
        public string SessionId = "SESS00";
        public string TimestampUtc = "";

        // Standard Kennedy et al. (1993) subscale weighting coefficients
        public const float NauseaWeight = 9.54f;
        public const float OculomotorWeight = 7.58f;
        public const float DisorientationWeight = 13.92f;
        public const float TotalSeverityWeight = 3.74f;

        public struct SsqResult
        {
            public float NauseaScore;
            public float OculomotorScore;
            public float DisorientationScore;
            public float TotalSeverity;
            public int RawNausea;
            public int RawOculomotor;
            public int RawDisorientation;
            public int AnsweredItemCount;
            public int MissingItemCount;
            public bool IsValid;
        }

        public struct SsqDeltaResult
        {
            public float DeltaNausea;
            public float DeltaOculomotor;
            public float DeltaDisorientation;
            public float DeltaTotalSeverity;
            public bool IsValid;
        }

        public int[] GetAllSymptoms()
        {
            return new int[]
            {
                GeneralDiscomfort, Fatigue, Headache, EyeStrain, DifficultyFocusing,
                SalivationIncreasing, Sweating, Nausea, DifficultyConcentrating,
                FullnessOfHead, BlurredVision, DizzinessEyesOpen, DizzinessEyesClosed,
                Vertigo, StomachAwareness, Burping
            };
        }

        public void SetAllSymptoms(int[] values)
        {
            if (values == null || values.Length < 16) return;
            GeneralDiscomfort = values[0];
            Fatigue = values[1];
            Headache = values[2];
            EyeStrain = values[3];
            DifficultyFocusing = values[4];
            SalivationIncreasing = values[5];
            Sweating = values[6];
            Nausea = values[7];
            DifficultyConcentrating = values[8];
            FullnessOfHead = values[9];
            BlurredVision = values[10];
            DizzinessEyesOpen = values[11];
            DizzinessEyesClosed = values[12];
            Vertigo = values[13];
            StomachAwareness = values[14];
            Burping = values[15];
        }

        public SsqResult ComputeScores()
        {
            int[] symptoms = GetAllSymptoms();
            int answered = 0;
            int missing = 0;

            for (int i = 0; i < symptoms.Length; i++)
            {
                if (symptoms[i] >= 0 && symptoms[i] <= 3) answered++;
                else missing++;
            }

            // Treat missing items as 0 for sum purposes only if partial, but flag validity
            int SafeVal(int val) => (val >= 0 && val <= 3) ? val : 0;

            // Published Kennedy (1993) subscale item mappings:
            // Nausea: GeneralDiscomfort, SalivationIncreasing, Sweating, Nausea, DifficultyConcentrating, StomachAwareness, Burping (7 items)
            int nauseaRaw = SafeVal(GeneralDiscomfort) + SafeVal(SalivationIncreasing) + SafeVal(Sweating) +
                            SafeVal(Nausea) + SafeVal(DifficultyConcentrating) + SafeVal(StomachAwareness) + SafeVal(Burping);

            // Oculomotor: GeneralDiscomfort, Fatigue, Headache, EyeStrain, DifficultyFocusing, DifficultyConcentrating, BlurredVision (7 items)
            int oculomotorRaw = SafeVal(GeneralDiscomfort) + SafeVal(Fatigue) + SafeVal(Headache) +
                                SafeVal(EyeStrain) + SafeVal(DifficultyFocusing) + SafeVal(DifficultyConcentrating) + SafeVal(BlurredVision);

            // Disorientation: DifficultyFocusing, Nausea, FullnessOfHead, BlurredVision, DizzinessEyesOpen, DizzinessEyesClosed, Vertigo (7 items)
            int disorientationRaw = SafeVal(DifficultyFocusing) + SafeVal(Nausea) + SafeVal(FullnessOfHead) +
                                    SafeVal(BlurredVision) + SafeVal(DizzinessEyesOpen) + SafeVal(DizzinessEyesClosed) + SafeVal(Vertigo);

            var res = new SsqResult
            {
                RawNausea = nauseaRaw,
                RawOculomotor = oculomotorRaw,
                RawDisorientation = disorientationRaw,
                NauseaScore = nauseaRaw * NauseaWeight,
                OculomotorScore = oculomotorRaw * OculomotorWeight,
                DisorientationScore = disorientationRaw * DisorientationWeight,
                TotalSeverity = (nauseaRaw + oculomotorRaw + disorientationRaw) * TotalSeverityWeight,
                AnsweredItemCount = answered,
                MissingItemCount = missing,
                IsValid = (missing == 0)
            };

            return res;
        }

        /// <summary>
        /// Computes the change in simulator sickness from pre-session baseline to post-session exposure.
        /// Delta = PostSessionScore - PreSessionScore.
        /// Positive values indicate symptom aggravation; negative values indicate reduction.
        /// </summary>
        public static SsqDeltaResult ComputeDelta(KennedySsqAssessment pre, KennedySsqAssessment post)
        {
            if (pre == null || post == null)
            {
                return new SsqDeltaResult { IsValid = false };
            }

            var preScores = pre.ComputeScores();
            var postScores = post.ComputeScores();

            return new SsqDeltaResult
            {
                DeltaNausea = postScores.NauseaScore - preScores.NauseaScore,
                DeltaOculomotor = postScores.OculomotorScore - preScores.OculomotorScore,
                DeltaDisorientation = postScores.DisorientationScore - preScores.DisorientationScore,
                DeltaTotalSeverity = postScores.TotalSeverity - preScores.TotalSeverity,
                IsValid = preScores.IsValid && postScores.IsValid
            };
        }

        /// <summary>
        /// Generates a standardized CSV row string for empirical human factors archiving.
        /// </summary>
        public string ToCsvRow()
        {
            var res = ComputeScores();
            string timestamp = string.IsNullOrEmpty(TimestampUtc) ? DateTime.UtcNow.ToString("o") : TimestampUtc;

            return string.Format(
                CultureInfo.InvariantCulture,
                "{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15},{16},{17},{18},{19},{20},{21:F2},{22:F2},{23:F2},{24:F2},{25},{26}",
                timestamp,
                ParticipantCode,
                SessionId,
                "KennedySSQ",
                Timing.ToString(),
                Status.ToString(),
                GeneralDiscomfort, Fatigue, Headache, EyeStrain, DifficultyFocusing,
                SalivationIncreasing, Sweating, Nausea, DifficultyConcentrating,
                FullnessOfHead, BlurredVision, DizzinessEyesOpen, DizzinessEyesClosed,
                Vertigo, StomachAwareness, Burping,
                res.NauseaScore, res.OculomotorScore, res.DisorientationScore, res.TotalSeverity,
                res.AnsweredItemCount, res.MissingItemCount
            );
        }

        public static string GetCsvHeader()
        {
            return "TimestampUtc,ParticipantID,SessionID,Instrument,Timing,CompletionStatus," +
                   "GeneralDiscomfort,Fatigue,Headache,EyeStrain,DifficultyFocusing," +
                   "SalivationIncreasing,Sweating,Nausea,DifficultyConcentrating," +
                   "FullnessOfHead,BlurredVision,DizzinessEyesOpen,DizzinessEyesClosed," +
                   "Vertigo,StomachAwareness,Burping," +
                   "NauseaScore,OculomotorScore,DisorientationScore,TotalSeverityScore," +
                   "AnsweredItems,MissingItems";
        }
    }
}
