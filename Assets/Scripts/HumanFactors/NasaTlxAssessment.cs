using System;
using System.Globalization;

namespace IVRAR.Group02.HumanFactors
{
    /// <summary>
    /// Multi-dimensional NASA Task Load Index (NASA-TLX) assessment data structure.
    /// Captures the 6 standardized subscales (Mental Demand, Physical Demand, Temporal Demand,
    /// Performance, Effort, Frustration) and provides Raw TLX (RTLX) scoring.
    /// Supports missing-data tracking, condition tagging, and completion status.
    /// Reference: Hart, S. G., & Staveland, L. E. (1988). Development of NASA-TLX (Task Load Index):
    /// Results of empirical and theoretical research. Advances in Psychology, 52, 139-183.
    /// </summary>
    [Serializable]
    public class NasaTlxAssessment
    {
        // 0 - 100 scale. Default -1 indicates unanswered/missing item.
        public float MentalDemand = -1f;
        public float PhysicalDemand = -1f;
        public float TemporalDemand = -1f;
        public float Performance = -1f;      // 0 = Good / Satisfied, 100 = Poor / Failure
        public float Effort = -1f;
        public float Frustration = -1f;

        public AssessmentTiming Timing = AssessmentTiming.PostBlock;
        public CompletionStatus Status = CompletionStatus.Completed;
        public string ParticipantCode = "P000";
        public string SessionId = "SESS00";
        public string ConditionLabel = "Baseline";
        public string TimestampUtc = "";

        public struct TlxResult
        {
            public float RawTlxScore;
            public int AnsweredDimensions;
            public int MissingDimensions;
            public bool IsValid;
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

        /// <summary>
        /// Computes Raw TLX (RTLX) score.
        /// RTLX is the unweighted arithmetic mean of all answered dimensions:
        /// RTLX = (1 / K) * sum(dimension_i) for all valid dimension_i.
        /// If all 6 dimensions are answered, K = 6.
        /// </summary>
        public float ComputeRawTlxScore()
        {
            return ComputeDetailedScores().RawTlxScore;
        }

        public TlxResult ComputeDetailedScores()
        {
            float[] dims = ToArray();
            float sum = 0f;
            int answered = 0;
            int missing = 0;

            for (int i = 0; i < dims.Length; i++)
            {
                if (dims[i] >= 0f && dims[i] <= 100f)
                {
                    sum += dims[i];
                    answered++;
                }
                else
                {
                    missing++;
                }
            }

            float rawScore = (answered > 0) ? (sum / answered) : 0f;

            return new TlxResult
            {
                RawTlxScore = rawScore,
                AnsweredDimensions = answered,
                MissingDimensions = missing,
                IsValid = (missing == 0)
            };
        }

        /// <summary>
        /// Generates a standardized CSV row string.
        /// </summary>
        public string ToCsvRow()
        {
            var res = ComputeDetailedScores();
            string timestamp = string.IsNullOrEmpty(TimestampUtc) ? DateTime.UtcNow.ToString("o") : TimestampUtc;

            return string.Format(
                CultureInfo.InvariantCulture,
                "{0},{1},{2},{3},{4},{5},{6:F1},{7:F1},{8:F1},{9:F1},{10:F1},{11:F1},{12:F2},{13},{14}",
                timestamp,
                ParticipantCode,
                SessionId,
                ConditionLabel,
                Timing.ToString(),
                Status.ToString(),
                MentalDemand,
                PhysicalDemand,
                TemporalDemand,
                Performance,
                Effort,
                Frustration,
                res.RawTlxScore,
                res.AnsweredDimensions,
                res.MissingDimensions
            );
        }

        public static string GetCsvHeader()
        {
            return "TimestampUtc,ParticipantID,SessionID,Condition,Timing,CompletionStatus," +
                   "MentalDemand,PhysicalDemand,TemporalDemand,Performance,Effort,Frustration," +
                   "RawTlxMean,AnsweredDimensions,MissingDimensions";
        }
    }
}
