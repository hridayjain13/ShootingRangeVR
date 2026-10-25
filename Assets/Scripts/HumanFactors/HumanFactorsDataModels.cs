using System;

namespace IVRAR.Group02.HumanFactors
{
    /// <summary>
    /// Supported human-factors psychometric and situational awareness assessment instruments.
    /// </summary>
    public enum HumanFactorsInstrument
    {
        NasaTlx,
        KennedySsq,
        Sagat
    }

    /// <summary>
    /// Experimental timing of the assessment relative to the training simulation session.
    /// </summary>
    public enum AssessmentTiming
    {
        PreSession,
        DuringFreeze,
        PostBlock,
        PostSession
    }

    /// <summary>
    /// Participant questionnaire completion status.
    /// Supports tracking of full completions, partial completions, skipped questionnaires,
    /// interruptions, and voluntary study withdrawals.
    /// </summary>
    public enum CompletionStatus
    {
        Completed,
        PartiallyCompleted,
        Skipped,
        Interrupted,
        Withdrawn
    }

    /// <summary>
    /// Endsley (1995) three-level situational awareness classification.
    /// </summary>
    public enum SagatLevel
    {
        Level1_Perception,     // Perception of critical cues/elements in the environment
        Level2_Comprehension,  // Comprehension of current operational situation & threat identity
        Level3_Projection      // Projection of future status & tactical decision requirements
    }

    /// <summary>
    /// Discrete situational awareness probe item within the SAGAT question bank.
    /// </summary>
    [Serializable]
    public class SagatQuestion
    {
        public string QuestionId;
        public SagatLevel Level;
        public string Prompt;
        public string[] Options;
        public int CorrectOptionIndex;
        public int SelectedOptionIndex = -1; // -1 represents unanswered / missing

        public bool IsAnswered => SelectedOptionIndex >= 0;
        public bool IsCorrect => SelectedOptionIndex == CorrectOptionIndex;

        public SagatQuestion(string id, SagatLevel level, string prompt, string[] options, int correctIndex)
        {
            QuestionId = id;
            Level = level;
            Prompt = prompt;
            Options = options;
            CorrectOptionIndex = correctIndex;
            SelectedOptionIndex = -1;
        }
    }
}
