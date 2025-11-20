using System.Collections.Generic;

namespace CircuitSimulator.Services
{
    /// <summary>
    /// Interface for circuit validation and challenge checking.
    /// NEW - supports both general circuit validation and challenge-specific validation.
    /// </summary>
    public interface IValidationService
    {
        // General Circuit Validation
        bool IsCircuitValid();
        List<string> GetCircuitErrors();
        bool HasCompleteCircuit();
        bool HasFloatingComponents();

        // Challenge Validation (NEW)
        bool ValidateChallenge(ChallengeScenario challenge);
        ChallengeResult CheckChallengeCompletion(ChallengeScenario challenge);
        List<string> GetMisconceptionFeedback(ChallengeScenario challenge);

        // Specific Validations
        bool HasBattery();
        bool HasClosedLoop();
        bool AreAllComponentsConnected();

        // Misconception Detection (NEW)
        bool DetectSinkModelMisconception();
        bool DetectCurrentAttenuationMisconception();
        bool DetectConstantCurrentMisconception();

        // Events
        System.Action<bool> OnValidationChanged { get; set; }
        System.Action<ChallengeResult> OnChallengeCompleted { get; set; }
        System.Action<List<string>> OnMisconceptionDetected { get; set; }
    }

    /// <summary>
    /// Result of challenge validation
    /// </summary>
    public class ChallengeResult
    {
        public bool completed;
        public float completionPercentage;
        public List<string> completedGoals;
        public List<string> remainingGoals;
        public List<string> addressedMisconceptions;
        public List<string> detectedMisconceptions;
        public string feedbackMessage;
    }
}