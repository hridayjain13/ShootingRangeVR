namespace IVRAR.Group02.Core
{
    /// <summary>
    /// Lifecycle states for the IVRAR Group 02 Sensory-Stress Defensive Security Training Simulation.
    /// </summary>
    public enum SimulationState
    {
        Idle,
        Configuring,
        ParticipantReady,
        RunningTrial,
        SensoryStressActive,
        TargetPresented,
        TargetResolved,
        PostTrialAssessment,
        TrialCompleted,
        SessionCompleted,
        Paused
    }
}



