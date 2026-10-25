using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using IVRAR.Group02.TargetManagement;

namespace IVRAR.Group02.Tests
{
    [TestFixture]
    public class SignalDetectionPrioritizationTests
    {
        private GameObject _managerGo;
        private VisualSignalDetectionTrialManager _trialManager;

        [SetUp]
        public void SetUp()
        {
            _managerGo = new GameObject("TestSignalDetectionTrialManager");
            _trialManager = _managerGo.AddComponent<VisualSignalDetectionTrialManager>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_managerGo != null)
            {
                Object.DestroyImmediate(_managerGo);
            }
        }

        [Test]
        public void AbstractVisualStimulus_InitializesWithCorrectPriority()
        {
            var go = new GameObject("TestStimulus");
            var stimulus = go.AddComponent<AbstractVisualStimulus>();

            stimulus.Configure(StimulusCategory.HighThreat_Tier1, 1.0f);
            Assert.AreEqual(StimulusCategory.HighThreat_Tier1, stimulus.Category);
            Assert.AreEqual(1, stimulus.ThreatPriority);
            Assert.IsTrue(stimulus.IsTargetThreat);

            stimulus.Configure(StimulusCategory.MediumThreat_Tier2, 0.65f);
            Assert.AreEqual(StimulusCategory.MediumThreat_Tier2, stimulus.Category);
            Assert.AreEqual(2, stimulus.ThreatPriority);
            Assert.IsTrue(stimulus.IsTargetThreat);

            stimulus.Configure(StimulusCategory.Neutral_Civilian, 0.5f);
            Assert.AreEqual(StimulusCategory.Neutral_Civilian, stimulus.Category);
            Assert.AreEqual(0, stimulus.ThreatPriority);
            Assert.IsFalse(stimulus.IsTargetThreat);

            stimulus.Configure(StimulusCategory.SignalAbsent_CatchTrial, 0.2f);
            Assert.AreEqual(StimulusCategory.SignalAbsent_CatchTrial, stimulus.Category);
            Assert.IsTrue(stimulus.IsCatchTrial);

            Object.DestroyImmediate(go);
        }

        [Test]
        public void PrioritizationRubric_EvaluatesPerfectHierarchicalOrder()
        {
            // Simulate participant engaging Priority 1 then Priority 2
            var historyField = typeof(VisualSignalDetectionTrialManager).GetField("_prioritizationEngagementHistory",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.IsNotNull(historyField);

            var list = (List<int>)historyField.GetValue(_trialManager);
            list.Clear();
            list.Add(1); // Priority 1 first
            list.Add(2); // Priority 2 second

            float score = _trialManager.EvaluatePrioritizationRubric(out DecisionOutcome outcome, out bool isOrderCorrect, isTimeout: false);

            Assert.AreEqual(200f, score, "Perfect order: +100 (Tier 1) + 50 (Tier 2) + 50 (Civilian withheld) = 200 pts.");
            Assert.IsTrue(isOrderCorrect, "Order must be marked as correct.");
            Assert.AreEqual(DecisionOutcome.Hit, outcome);
        }

        [Test]
        public void PrioritizationRubric_AppliesInvertedOrderPenalty()
        {
            var historyField = typeof(VisualSignalDetectionTrialManager).GetField("_prioritizationEngagementHistory",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var list = (List<int>)historyField.GetValue(_trialManager);
            list.Clear();
            list.Add(2); // Priority 2 first (inverted)
            list.Add(1); // Priority 1 second

            float score = _trialManager.EvaluatePrioritizationRubric(out DecisionOutcome outcome, out bool isOrderCorrect, isTimeout: false);

            // Inverted order has -30 penalty: (150 - 30) + 50 = 170 pts
            Assert.AreEqual(170f, score);
            Assert.IsFalse(isOrderCorrect, "Inverted order must be marked as false.");
            Assert.AreEqual(DecisionOutcome.Hit, outcome);
        }

        [Test]
        public void PrioritizationRubric_PenalizesCivilianEngagement()
        {
            var historyField = typeof(VisualSignalDetectionTrialManager).GetField("_prioritizationEngagementHistory",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var list = (List<int>)historyField.GetValue(_trialManager);
            list.Clear();
            list.Add(0); // Engaged civilian / friendly non-combatant

            float score = _trialManager.EvaluatePrioritizationRubric(out DecisionOutcome outcome, out bool isOrderCorrect, isTimeout: false);

            Assert.AreEqual(-100f, score, "Civilian engagement must incur -100 penalty.");
            Assert.AreEqual(DecisionOutcome.FalseAlarm, outcome);
            Assert.IsFalse(isOrderCorrect);
        }

        [Test]
        public void CounterbalancedLatinSquare_GeneratesShiftedSequences()
        {
            var managerGo2 = new GameObject("Manager2");
            var manager2 = managerGo2.AddComponent<VisualSignalDetectionTrialManager>();

            var modeField = typeof(VisualSignalDetectionTrialManager).GetField("_conditionOrderMode",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var latinField = typeof(VisualSignalDetectionTrialManager).GetField("_latinSquareGroupIndex",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            modeField.SetValue(_trialManager, ConditionOrderMode.CounterbalancedLatinSquare);
            latinField.SetValue(_trialManager, 0);
            _trialManager.BuildConditionSequence();

            modeField.SetValue(manager2, ConditionOrderMode.CounterbalancedLatinSquare);
            latinField.SetValue(manager2, 1);
            manager2.BuildConditionSequence();

            var seqField = typeof(VisualSignalDetectionTrialManager).GetField("_blockSequence",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            var seq0 = (List<string>)seqField.GetValue(_trialManager);
            var seq1 = (List<string>)seqField.GetValue(manager2);

            Assert.AreEqual(4, seq0.Count);
            Assert.AreEqual(4, seq1.Count);
            Assert.AreNotEqual(seq0[0], seq1[0], "Latin square group 0 and group 1 must start with different conditions.");

            Object.DestroyImmediate(managerGo2);
        }
    }
}
