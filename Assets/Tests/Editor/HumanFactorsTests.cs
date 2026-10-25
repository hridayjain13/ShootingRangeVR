using System;
using NUnit.Framework;
using UnityEngine;
using IVRAR.Group02.HumanFactors;

namespace IVRAR.Group02.Tests
{
    [TestFixture]
    public class HumanFactorsTests
    {
        [Test]
        public void KennedySsq_StandardScoring_CalculatesAccurateSubscales()
        {
            var ssq = new KennedySsqAssessment();
            // Assign 1 (Slight) to all 16 symptoms
            int[] allSlight = new int[16];
            for (int i = 0; i < 16; i++) allSlight[i] = 1;
            ssq.SetAllSymptoms(allSlight);

            var res = ssq.ComputeScores();

            Assert.IsTrue(res.IsValid);
            Assert.AreEqual(16, res.AnsweredItemCount);
            Assert.AreEqual(0, res.MissingItemCount);

            // Nausea: 7 items * 1 * 9.54 = 66.78
            Assert.AreEqual(7, res.RawNausea);
            Assert.AreEqual(66.78f, res.NauseaScore, 0.01f);

            // Oculomotor: 7 items * 1 * 7.58 = 53.06
            Assert.AreEqual(7, res.RawOculomotor);
            Assert.AreEqual(53.06f, res.OculomotorScore, 0.01f);

            // Disorientation: 7 items * 1 * 13.92 = 97.44
            Assert.AreEqual(7, res.RawDisorientation);
            Assert.AreEqual(97.44f, res.DisorientationScore, 0.01f);

            // Total Severity: (7 + 7 + 7) * 3.74 = 78.54
            Assert.AreEqual(78.54f, res.TotalSeverity, 0.01f);
        }

        [Test]
        public void KennedySsq_DeltaCalculation_PrePostDistinction()
        {
            var pre = new KennedySsqAssessment { Timing = AssessmentTiming.PreSession };
            int[] preZeros = new int[16]; // All 0 (None)
            pre.SetAllSymptoms(preZeros);

            var post = new KennedySsqAssessment { Timing = AssessmentTiming.PostSession };
            int[] postSlight = new int[16]; // All 1 (Slight)
            for (int i = 0; i < 16; i++) postSlight[i] = 1;
            post.SetAllSymptoms(postSlight);

            var delta = KennedySsqAssessment.ComputeDelta(pre, post);

            Assert.IsTrue(delta.IsValid);
            Assert.AreEqual(66.78f, delta.DeltaNausea, 0.01f);
            Assert.AreEqual(53.06f, delta.DeltaOculomotor, 0.01f);
            Assert.AreEqual(97.44f, delta.DeltaDisorientation, 0.01f);
            Assert.AreEqual(78.54f, delta.DeltaTotalSeverity, 0.01f);
        }

        [Test]
        public void KennedySsq_MissingItems_DetectedAndHandled()
        {
            var ssq = new KennedySsqAssessment();
            // Default constructor has -1 for all 16 items
            var res = ssq.ComputeScores();

            Assert.IsFalse(res.IsValid);
            Assert.AreEqual(0, res.AnsweredItemCount);
            Assert.AreEqual(16, res.MissingItemCount);
            Assert.AreEqual(0f, res.TotalSeverity);
        }

        [Test]
        public void NasaTlx_RawTlxScore_UnweightedMeanOfSixDimensions()
        {
            var tlx = new NasaTlxAssessment();
            tlx.MentalDemand = 60f;
            tlx.PhysicalDemand = 40f;
            tlx.TemporalDemand = 50f;
            tlx.Performance = 30f;
            tlx.Effort = 70f;
            tlx.Frustration = 20f;

            var detailed = tlx.ComputeDetailedScores();

            Assert.IsTrue(detailed.IsValid);
            Assert.AreEqual(6, detailed.AnsweredDimensions);
            Assert.AreEqual(0, detailed.MissingDimensions);

            // Sum = 60 + 40 + 50 + 30 + 70 + 20 = 270. Mean = 270 / 6 = 45.0
            Assert.AreEqual(45.0f, detailed.RawTlxScore, 0.001f);
            Assert.AreEqual(45.0f, tlx.ComputeRawTlxScore(), 0.001f);
        }

        [Test]
        public void NasaTlx_MissingDimensions_HandledGracefully()
        {
            var tlx = new NasaTlxAssessment();
            tlx.MentalDemand = 60f;
            tlx.PhysicalDemand = 40f;
            // Remaining 4 are -1 (unanswered)

            var detailed = tlx.ComputeDetailedScores();

            Assert.IsFalse(detailed.IsValid);
            Assert.AreEqual(2, detailed.AnsweredDimensions);
            Assert.AreEqual(4, detailed.MissingDimensions);

            // Mean of the 2 answered: (60 + 40) / 2 = 50.0
            Assert.AreEqual(50.0f, detailed.RawTlxScore, 0.001f);
        }

        [Test]
        public void SagatAssessment_EndsleyRubric_ComputesLevelScoresAccurately()
        {
            var sagat = new SagatAssessment
            {
                GroundTruthIsHostile = true,
                GroundTruthSideIndex = 0,         // Left
                GroundTruthIlluminationIndex = 1, // Low-Lux
                GroundTruthDistanceTier = 0,      // Near
                GroundTruthDistractionType = 1,   // Auditory
                GroundTruthRequiredAction = 0     // Engage Hostile
            };

            sagat.PopulateStandardQuestionBank();
            Assert.AreEqual(6, sagat.Questions.Count);

            // Level 1: Answer all 3 correctly
            sagat.Questions[0].SelectedOptionIndex = 0; // Left
            sagat.Questions[1].SelectedOptionIndex = 1; // Low-Lux
            sagat.Questions[2].SelectedOptionIndex = 0; // Near

            // Level 2: Answer threat correctly, distraction incorrectly
            sagat.Questions[3].SelectedOptionIndex = 0; // Hostile (Correct)
            sagat.Questions[4].SelectedOptionIndex = 0; // None (Incorrect, ground truth is 1)

            // Level 3: Answer action correctly
            sagat.Questions[5].SelectedOptionIndex = 0; // Engage (Correct)

            var rubric = sagat.ComputeRubricScores();

            Assert.IsTrue(rubric.IsValid);
            Assert.AreEqual(6, rubric.TotalProbesAdministered);
            Assert.AreEqual(6, rubric.TotalAnswered);
            Assert.AreEqual(0, rubric.TotalMissing);
            Assert.AreEqual(5, rubric.TotalCorrect);

            // Overall = 5 / 6 * 100 = 83.33%
            Assert.AreEqual(83.33f, rubric.OverallSaPercentage, 0.1f);

            // L1: 3/3 = 100%
            Assert.AreEqual(3, rubric.Level1Correct);
            Assert.AreEqual(3, rubric.Level1Total);
            Assert.AreEqual(100.0f, rubric.Level1Percentage, 0.01f);

            // L2: 1/2 = 50%
            Assert.AreEqual(1, rubric.Level2Correct);
            Assert.AreEqual(2, rubric.Level2Total);
            Assert.AreEqual(50.0f, rubric.Level2Percentage, 0.01f);

            // L3: 1/1 = 100%
            Assert.AreEqual(1, rubric.Level3Correct);
            Assert.AreEqual(1, rubric.Level3Total);
            Assert.AreEqual(100.0f, rubric.Level3Percentage, 0.01f);
        }

        [Test]
        public void SagatAssessment_MissingAnswers_TrackedInRubric()
        {
            var sagat = new SagatAssessment();
            sagat.PopulateStandardQuestionBank();

            // Answer only Question 0 (L1), leave other 5 as -1
            sagat.Questions[0].SelectedOptionIndex = sagat.Questions[0].CorrectOptionIndex;

            var rubric = sagat.ComputeRubricScores();

            Assert.IsFalse(rubric.IsValid);
            Assert.AreEqual(1, rubric.TotalAnswered);
            Assert.AreEqual(5, rubric.TotalMissing);
            Assert.AreEqual(1, rubric.TotalCorrect);
            Assert.AreEqual(100.0f, rubric.Level1Percentage, 0.01f);
            Assert.AreEqual(0.0f, rubric.Level2Percentage, 0.01f);
            Assert.AreEqual(0.0f, rubric.Level3Percentage, 0.01f);
        }
    }
}
