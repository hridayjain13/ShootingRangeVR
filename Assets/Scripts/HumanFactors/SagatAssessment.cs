using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace IVRAR.Group02.HumanFactors
{
    /// <summary>
    /// Situational Awareness Global Assessment Technique (SAGAT) probe model & freeze manager.
    /// Evaluates Level 1 (Perception), Level 2 (Comprehension), and Level 3 (Projection)
    /// situational awareness under sensory stress and low-visibility conditions.
    /// Manages simulation freezes (Time.timeScale = 0) and target occlusion to probe
    /// the participant's internal mental model without visual aid.
    /// Reference: Endsley, M. R. (1995). Measurement of situation awareness in dynamic systems.
    /// Human Factors, 37(1), 65-84.
    /// </summary>
    [Serializable]
    public class SagatAssessment
    {
        public string ParticipantCode = "P000";
        public string SessionId = "SESS00";
        public string TrialId = "FREEZE-00";
        public string ConditionLabel = "Baseline";
        public AssessmentTiming Timing = AssessmentTiming.DuringFreeze;
        public CompletionStatus Status = CompletionStatus.Completed;
        public string TimestampUtc = "";

        // Probed scenario state captured at the moment of freeze
        public bool GroundTruthIsHostile;
        public int GroundTruthSideIndex;       // 0 = Left, 1 = Center, 2 = Right
        public int GroundTruthIlluminationIndex; // 0 = Baseline, 1 = Low-Lux, 2 = Low-Lux Fog
        public int GroundTruthDistanceTier;     // 0 = Near (<10m), 1 = Mid (10-15m), 2 = Far (>15m)
        public int GroundTruthDistractionType;  // 0 = None, 1 = Auditory, 2 = Peripheral Visual, 3 = Combined
        public int GroundTruthRequiredAction;   // 0 = Engage Hostile, 1 = Withhold Fire Civilian

        // Questions administered during this SAGAT freeze
        public List<SagatQuestion> Questions = new List<SagatQuestion>();

        public struct SagatResult
        {
            public int TotalProbesAdministered;
            public int TotalAnswered;
            public int TotalMissing;
            public int TotalCorrect;
            public float OverallSaPercentage;

            public int Level1Correct;
            public int Level1Total;
            public float Level1Percentage;

            public int Level2Correct;
            public int Level2Total;
            public float Level2Percentage;

            public int Level3Correct;
            public int Level3Total;
            public float Level3Percentage;

            public bool IsValid;
        }

        public void PopulateStandardQuestionBank()
        {
            Questions.Clear();

            // Level 1: Perception
            Questions.Add(new SagatQuestion(
                "SAGAT-L1-SIDE",
                SagatLevel.Level1_Perception,
                "In which sector was the last stimulus located?",
                new string[] { "Left Sector", "Center Sector", "Right Sector" },
                GroundTruthSideIndex
            ));

            Questions.Add(new SagatQuestion(
                "SAGAT-L1-ENV",
                SagatLevel.Level1_Perception,
                "What was the environmental illumination and visibility condition?",
                new string[] { "Normal Baseline", "Reduced Low-Visibility", "Low-Visibility with Fog" },
                GroundTruthIlluminationIndex
            ));

            Questions.Add(new SagatQuestion(
                "SAGAT-L1-DIST",
                SagatLevel.Level1_Perception,
                "What was the approximate range/distance to the stimulus?",
                new string[] { "Near (< 10 m)", "Medium (10 - 15 m)", "Far (> 15 m)" },
                GroundTruthDistanceTier
            ));

            // Level 2: Comprehension
            Questions.Add(new SagatQuestion(
                "SAGAT-L2-THREAT",
                SagatLevel.Level2_Comprehension,
                "Was the target a hostile threat or an unarmed civilian/neutral?",
                new string[] { "Hostile Threat", "Unarmed Civilian / Neutral" },
                GroundTruthIsHostile ? 0 : 1
            ));

            Questions.Add(new SagatQuestion(
                "SAGAT-L2-STRESS",
                SagatLevel.Level2_Comprehension,
                "What sensory distraction was concurrently active during presentation?",
                new string[] { "None (Baseline)", "Auditory Startle", "Peripheral Visual Flash", "Combined Modalities" },
                GroundTruthDistractionType
            ));

            // Level 3: Projection
            Questions.Add(new SagatQuestion(
                "SAGAT-L3-ACTION",
                SagatLevel.Level3_Projection,
                "What was the correct tactical rule-of-engagement decision?",
                new string[] { "Immediate Lethal Engagement", "Withhold Fire / Assess Situation" },
                GroundTruthRequiredAction
            ));
        }

        public SagatResult ComputeRubricScores()
        {
            int l1Tot = 0, l1Corr = 0;
            int l2Tot = 0, l2Corr = 0;
            int l3Tot = 0, l3Corr = 0;
            int answered = 0;
            int missing = 0;

            for (int i = 0; i < Questions.Count; i++)
            {
                var q = Questions[i];
                if (q.IsAnswered)
                {
                    answered++;
                    bool correct = q.IsCorrect;

                    switch (q.Level)
                    {
                        case SagatLevel.Level1_Perception:
                            l1Tot++;
                            if (correct) l1Corr++;
                            break;
                        case SagatLevel.Level2_Comprehension:
                            l2Tot++;
                            if (correct) l2Corr++;
                            break;
                        case SagatLevel.Level3_Projection:
                            l3Tot++;
                            if (correct) l3Corr++;
                            break;
                    }
                }
                else
                {
                    missing++;
                    switch (q.Level)
                    {
                        case SagatLevel.Level1_Perception: l1Tot++; break;
                        case SagatLevel.Level2_Comprehension: l2Tot++; break;
                        case SagatLevel.Level3_Projection: l3Tot++; break;
                    }
                }
            }

            int totalAdministered = Questions.Count;
            int totalCorrect = l1Corr + l2Corr + l3Corr;
            float overallPct = totalAdministered > 0 ? (totalCorrect / (float)totalAdministered) * 100f : 0f;

            float l1Pct = l1Tot > 0 ? (l1Corr / (float)l1Tot) * 100f : 0f;
            float l2Pct = l2Tot > 0 ? (l2Corr / (float)l2Tot) * 100f : 0f;
            float l3Pct = l3Tot > 0 ? (l3Corr / (float)l3Tot) * 100f : 0f;

            return new SagatResult
            {
                TotalProbesAdministered = totalAdministered,
                TotalAnswered = answered,
                TotalMissing = missing,
                TotalCorrect = totalCorrect,
                OverallSaPercentage = overallPct,
                Level1Correct = l1Corr,
                Level1Total = l1Tot,
                Level1Percentage = l1Pct,
                Level2Correct = l2Corr,
                Level2Total = l2Tot,
                Level2Percentage = l2Pct,
                Level3Correct = l3Corr,
                Level3Total = l3Tot,
                Level3Percentage = l3Pct,
                IsValid = (missing == 0)
            };
        }

        public string ToCsvRow()
        {
            var res = ComputeRubricScores();
            string timestamp = string.IsNullOrEmpty(TimestampUtc) ? DateTime.UtcNow.ToString("o") : TimestampUtc;

            return string.Format(
                CultureInfo.InvariantCulture,
                "{0},{1},{2},{3},{4},{5},{6},{7},{8},{9:F1},{10},{11},{12:F1},{13},{14},{15:F1},{16},{17},{18:F1}",
                timestamp,
                ParticipantCode,
                SessionId,
                TrialId,
                ConditionLabel,
                Timing.ToString(),
                Status.ToString(),
                res.TotalProbesAdministered,
                res.TotalCorrect,
                res.OverallSaPercentage,
                res.Level1Correct,
                res.Level1Total,
                res.Level1Percentage,
                res.Level2Correct,
                res.Level2Total,
                res.Level2Percentage,
                res.Level3Correct,
                res.Level3Total,
                res.Level3Percentage
            );
        }

        public static string GetCsvHeader()
        {
            return "TimestampUtc,ParticipantID,SessionID,TrialID,Condition,Timing,CompletionStatus," +
                   "TotalProbes,TotalCorrect,OverallSaScorePct," +
                   "L1Correct,L1Total,L1ScorePct," +
                   "L2Correct,L2Total,L2ScorePct," +
                   "L3Correct,L3Total,L3ScorePct";
        }
    }
}
