using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using CircuitSimulator.Services;

namespace CircuitSimulator.Services
{
    /// <summary>
    /// Validation service that provides both general circuit validation
    /// and challenge-specific validation with misconception detection.
    /// </summary>
    public class ValidationService : MonoBehaviour, IValidationService
    {
        [Header("Validation Settings")]
        [SerializeField] private bool enableMisconceptionDetection = true;
        [SerializeField] private bool enableDetailedLogging = false;

        // Service Dependencies
        private ICircuitManager circuitManager;
        private ICircuitSolver circuitSolver;

        // IValidationService Events
        public System.Action<bool> OnValidationChanged { get; set; }
        public System.Action<ChallengeResult> OnChallengeCompleted { get; set; }
        public System.Action<List<string>> OnMisconceptionDetected { get; set; }

        // State
        private bool lastValidationResult = false;
        private List<string> currentErrors = new List<string>();

        void Start()
        {
            InitializeService();
        }

        void InitializeService()
        {
            // Register with ServiceLocator
            ServiceLocator.Instance.Register<IValidationService>(this);

            // Get service dependencies safely
            circuitManager = ServiceLocator.Instance.TryGet<ICircuitManager>();
            circuitSolver = ServiceLocator.Instance.TryGet<ICircuitSolver>();

            // Subscribe to circuit changes
            if (circuitManager != null)
            {
                circuitManager.OnCircuitChanged += OnCircuitChanged;
            }
            else
            {
                Debug.LogWarning("[ValidationService] ICircuitManager not found, some validation features may not work");
            }

            if (enableDetailedLogging)
                Debug.Log("[ValidationService] Initialized and registered with ServiceLocator");
        }

        void OnDestroy()
        {
            // Unsubscribe from events
            if (circuitManager != null)
                circuitManager.OnCircuitChanged -= OnCircuitChanged;
        }

        private void OnCircuitChanged()
        {
            // Validate circuit whenever it changes
            bool currentValidation = IsCircuitValid();

            if (currentValidation != lastValidationResult)
            {
                lastValidationResult = currentValidation;
                OnValidationChanged?.Invoke(currentValidation);
            }
        }

        #region General Circuit Validation

        public bool IsCircuitValid()
        {
            currentErrors.Clear();

            if (!HasBattery())
                currentErrors.Add("Circuit needs at least one power source (battery)");

            if (!HasClosedLoop())
                currentErrors.Add("Circuit needs a complete closed loop");

            if (HasFloatingComponents())
                currentErrors.Add("Some components are not connected to the circuit");

            bool isValid = currentErrors.Count == 0;

            if (enableDetailedLogging)
                Debug.Log($"[ValidationService] Circuit validation: {(isValid ? "VALID" : "INVALID")} ({currentErrors.Count} errors)");

            return isValid;
        }

        public List<string> GetCircuitErrors()
        {
            IsCircuitValid(); // Refresh error list
            return new List<string>(currentErrors);
        }

        public bool HasCompleteCircuit()
        {
            return HasBattery() && HasClosedLoop() && !HasFloatingComponents();
        }

        public bool HasBattery()
        {
            if (circuitManager == null) return false;

            var components = circuitManager.GetAllComponents();
            return components.Any(c => c.ComponentType == ComponentType.Battery);
        }

        public bool HasClosedLoop()
        {
            if (circuitManager == null) return false;

            var components = circuitManager.GetAllComponents();
            var wires = circuitManager.GetAllWires();

            // Check if we have components and wires
            if (components.Count == 0 || wires.Count == 0) return false;

            // Simple check: ensure we have at least 2 wires for a basic loop
            return wires.Count >= 2;
        }

        public bool AreAllComponentsConnected()
        {
            return !HasFloatingComponents();
        }

        public bool HasFloatingComponents()
        {
            if (circuitManager == null) return false;

            var components = circuitManager.GetAllComponents();
            var wires = circuitManager.GetAllWires();

            foreach (var component in components)
            {
                bool hasConnection = false;

                foreach (var wire in wires)
                {
                    if (wire.IsConnectedToComponent(component.gameObject))
                    {
                        hasConnection = true;
                        break;
                    }
                }

                if (!hasConnection)
                {
                    if (enableDetailedLogging)
                        Debug.Log($"[ValidationService] Floating component detected: {component.name}");
                    return true;
                }
            }

            return false;
        }

        #endregion

        #region Challenge Validation

        public bool ValidateChallenge(ChallengeScenario challenge)
        {
            if (challenge == null) return false;

            var result = CheckChallengeCompletion(challenge);
            return result.completed;
        }

        public ChallengeResult CheckChallengeCompletion(ChallengeScenario challenge)
        {
            if (challenge == null)
            {
                return new ChallengeResult
                {
                    completed = false,
                    completionPercentage = 0f,
                    feedbackMessage = "No challenge scenario provided"
                };
            }

            var result = new ChallengeResult
            {
                completedGoals = new List<string>(),
                remainingGoals = new List<string>(),
                addressedMisconceptions = new List<string>(),
                detectedMisconceptions = new List<string>()
            };

            // Check each goal
            foreach (var goal in challenge.goals)
            {
                if (ValidateGoal(goal))
                {
                    result.completedGoals.Add(goal.goalId);
                }
                else
                {
                    result.remainingGoals.Add(goal.goalId);
                }
            }

            // Calculate completion percentage
            float totalGoals = challenge.goals.Length;
            float completedGoals = result.completedGoals.Count;
            result.completionPercentage = totalGoals > 0 ? completedGoals / totalGoals : 0f;

            // Check if challenge is complete
            result.completed = result.completionPercentage >= challenge.requiredCompletionPercentage;

            if (challenge.requireAllGoalsComplete)
            {
                result.completed = result.completionPercentage >= 1f;
            }

            // Detect misconceptions
            if (enableMisconceptionDetection)
            {
                DetectMisconceptions(challenge, result);
            }

            // Generate feedback message
            result.feedbackMessage = GenerateFeedbackMessage(challenge, result);

            if (result.completed)
            {
                OnChallengeCompleted?.Invoke(result);
            }

            if (enableDetailedLogging)
            {
                Debug.Log($"[ValidationService] Challenge '{challenge.challengeTitle}' completion: {result.completionPercentage:P} " +
                         $"({result.completedGoals.Count}/{challenge.goals.Length} goals)");
            }

            return result;
        }

        public List<string> GetMisconceptionFeedback(ChallengeScenario challenge)
        {
            var feedback = new List<string>();

            if (challenge == null) return feedback;

            // Check for each misconception type
            foreach (var misconceptionCheck in challenge.misconceptionChecks)
            {
                bool detected = false;

                switch (misconceptionCheck.type)
                {
                    case MisconceptionType.M1_SinkModel:
                        detected = DetectSinkModelMisconception();
                        break;
                    case MisconceptionType.M2_CurrentAttenuation:
                        detected = DetectCurrentAttenuationMisconception();
                        break;
                    case MisconceptionType.M8_ConstantCurrentSource:
                        detected = DetectConstantCurrentMisconception();
                        break;
                }

                if (detected)
                {
                    feedback.Add(misconceptionCheck.feedback);
                }
            }

            return feedback;
        }

        private bool ValidateGoal(ChallengeGoal goal)
        {
            switch (goal.validation.type)
            {
                case ValidationType.CircuitComplete:
                    return HasCompleteCircuit();

                case ValidationType.SpecificVoltage:
                    return ValidateVoltageGoal(goal);

                case ValidationType.SpecificCurrent:
                    return ValidateCurrentGoal(goal);

                case ValidationType.ComponentsConnected:
                    return ValidateConnectionGoal(goal);

                case ValidationType.NoFloatingComponents:
                    return !HasFloatingComponents();

                default:
                    return false;
            }
        }

        private bool ValidateVoltageGoal(ChallengeGoal goal)
        {
            if (circuitManager == null) return false;

            var targetComponent = circuitManager.GetAllComponents()
                .FirstOrDefault(c => c.name.Contains(goal.validation.targetComponentId) ||
                                   c.gameObject.name.Contains(goal.validation.targetComponentId));

            if (targetComponent == null) return false;

            float actualVoltage = targetComponent.voltageDrop;
            float expectedVoltage = goal.validation.expectedVoltage;
            float tolerance = goal.validation.tolerance;

            return Mathf.Abs(actualVoltage - expectedVoltage) <= tolerance;
        }

        private bool ValidateCurrentGoal(ChallengeGoal goal)
        {
            if (circuitManager == null) return false;

            var targetComponent = circuitManager.GetAllComponents()
                .FirstOrDefault(c => c.name.Contains(goal.validation.targetComponentId) ||
                                   c.gameObject.name.Contains(goal.validation.targetComponentId));

            if (targetComponent == null) return false;

            float actualCurrent = targetComponent.current;
            float expectedCurrent = goal.validation.expectedCurrent;
            float tolerance = goal.validation.tolerance;

            return Mathf.Abs(actualCurrent - expectedCurrent) <= tolerance;
        }

        private bool ValidateConnectionGoal(ChallengeGoal goal)
        {
            if (circuitManager == null) return false;

            foreach (var connection in goal.validation.requiredConnections)
            {
                if (!ValidateConnection(connection))
                    return false;
            }

            return true;
        }

        private bool ValidateConnection(ComponentConnection connection)
        {
            var components = circuitManager.GetAllComponents();
            var wires = circuitManager.GetAllWires();

            var compA = components.FirstOrDefault(c => c.name.Contains(connection.componentA));
            var compB = components.FirstOrDefault(c => c.name.Contains(connection.componentB));

            if (compA == null || compB == null) return false;

            // Check if components are connected via wires
            foreach (var wire in wires)
            {
                bool connectsA = wire.IsConnectedToComponent(compA.gameObject);
                bool connectsB = wire.IsConnectedToComponent(compB.gameObject);

                if (connectsA && connectsB)
                    return true;
            }

            return false;
        }

        #endregion

        #region Misconception Detection

        public bool DetectSinkModelMisconception()
        {
            // M1: Student thinks current flows one-way and gets "used up"
            // Detected if: incomplete circuit (missing return path)
            if (circuitManager == null) return false;

            var components = circuitManager.GetAllComponents();
            var wires = circuitManager.GetAllWires();

            // Look for battery with only one connection
            var batteries = components.Where(c => c.ComponentType == ComponentType.Battery);

            foreach (var battery in batteries)
            {
                int connectionCount = 0;
                foreach (var wire in wires)
                {
                    if (wire.IsConnectedToComponent(battery.gameObject))
                        connectionCount++;
                }

                // If battery has only one wire, student may think current only flows out
                if (connectionCount == 1)
                {
                    if (enableDetailedLogging)
                        Debug.Log("[ValidationService] M1 Sink Model misconception detected: battery with single connection");
                    return true;
                }
            }

            return false;
        }

        public bool DetectCurrentAttenuationMisconception()
        {
            // M2: Student thinks current decreases through components
            // This would require analyzing expected vs actual current values
            // For now, return false as it requires more complex analysis
            return false;
        }

        public bool DetectConstantCurrentMisconception()
        {
            // M8: Student thinks batteries provide constant current regardless of load
            // This would require analyzing circuit solver results
            // For now, return false as it requires more complex analysis
            return false;
        }

        private void DetectMisconceptions(ChallengeScenario challenge, ChallengeResult result)
        {
            foreach (var misconceptionCheck in challenge.misconceptionChecks)
            {
                bool detected = false;

                switch (misconceptionCheck.type)
                {
                    case MisconceptionType.M1_SinkModel:
                        detected = DetectSinkModelMisconception();
                        break;
                    case MisconceptionType.M2_CurrentAttenuation:
                        detected = DetectCurrentAttenuationMisconception();
                        break;
                    case MisconceptionType.M8_ConstantCurrentSource:
                        detected = DetectConstantCurrentMisconception();
                        break;
                }

                if (detected)
                {
                    result.detectedMisconceptions.Add(misconceptionCheck.type.ToString());
                }
                else
                {
                    result.addressedMisconceptions.Add(misconceptionCheck.type.ToString());
                }
            }

            if (result.detectedMisconceptions.Count > 0)
            {
                OnMisconceptionDetected?.Invoke(result.detectedMisconceptions);
            }
        }

        #endregion

        #region Helper Methods

        private string GenerateFeedbackMessage(ChallengeScenario challenge, ChallengeResult result)
        {
            if (result.completed)
            {
                return $"Excellent! You've completed '{challenge.challengeTitle}' successfully!";
            }

            if (result.completionPercentage > 0.5f)
            {
                return $"Good progress! You're {result.completionPercentage:P} complete. " +
                       $"You still need to: {string.Join(", ", result.remainingGoals)}";
            }

            if (result.detectedMisconceptions.Count > 0)
            {
                return "Try checking your circuit connections. Remember that electricity needs a complete loop to flow!";
            }

            return $"Keep working on '{challenge.challengeTitle}'. " +
                   $"Goals remaining: {string.Join(", ", result.remainingGoals)}";
        }

        #endregion
    }
}