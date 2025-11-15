using System.Collections.Generic;
using UnityEngine;

namespace CircuitSimulator
{
    /// <summary>
    /// ScriptableObject definition for educational challenge scenarios.
    /// Each challenge focuses on a specific misconception and provides guided learning.
    /// </summary>
    [CreateAssetMenu(fileName = "New Challenge Scenario", menuName = "Circuit Simulator/Challenge Scenario")]
    public class ChallengeScenario : ScriptableObject
    {
        [Header("Challenge Identity")]
        public string challengeTitle = "Power the House";
        public string challengeId = "M1_PowerTheHouse";

        [TextArea(3, 5)]
        public string description = "Help the family get electricity to their house by building a complete circuit.";

        [Header("Educational Focus")]
        public MisconceptionType primaryMisconception = MisconceptionType.M1_SinkModel;
        public MisconceptionType[] secondaryMisconceptions;

        [Header("Difficulty")]
        public DifficultyLevel difficulty = DifficultyLevel.Beginner;
        public int recommendedGradeLevel = 7;

        [Header("Available Components")]
        public ComponentDefinition[] availableComponents;
        public int maxComponentsAllowed = 10;

        [Header("Pre-placed Objects")]
        public PreplacedObject[] preplacedObjects;

        [Header("Challenge Goals")]
        public ChallengeGoal[] goals;

        [Header("Misconception Detection")]
        public MisconceptionCheck[] misconceptionChecks;

        [Header("Feedback System")]
        public FeedbackMessage[] feedbackMessages;

        [Header("Success Criteria")]
        [Range(0f, 1f)]
        public float requiredCompletionPercentage = 1f;
        public bool requireAllGoalsComplete = true;
        public bool allowPartialCredit = false;

        [Header("Hints and Help")]
        public HintSystem hintSystem;

        [Header("Additional Properties")]
        [TextArea(2, 4)]
        public string successMessage = "Excellent work! You've successfully completed the challenge!";

        [TextArea(3, 6)]
        public string[] learningPoints = { "Complete circuits require a closed loop", "Current flows from positive to negative" };

        public string[] hints = { "Try connecting the battery positive terminal", "Remember electricity needs a complete path" };

        [Range(1f, 20f)]
        public float tolerancePercent = 5f;

        /// <summary>
        /// Validate if this challenge can be completed with available components
        /// </summary>
        public bool ValidateChallenge()
        {
            // Check if we have necessary components for basic circuit
            bool hasBattery = false;
            bool hasLoad = false;

            foreach (var component in availableComponents)
            {
                if (component.electricalProperties.isVoltageSource)
                    hasBattery = true;
                if (component.baseType == BaseComponentType.Bulb ||
                    component.baseType == BaseComponentType.Resistor)
                    hasLoad = true;
            }

            return hasBattery && hasLoad;
        }
    }

    [System.Serializable]
    public class PreplacedObject
    {
        public ComponentDefinition componentDefinition;
        public Vector3 position;
        public Vector3 rotation;
        public bool isFixed = true; // Can't be moved or deleted
        public bool isTarget = false; // Is this what student needs to connect to?
        public string label = "";
    }

    [System.Serializable]
    public class ChallengeGoal
    {
        public string goalId;
        public string description = "Connect the battery to the house";
        public GoalType type = GoalType.ComponentConnection;
        public GoalType goalType = GoalType.ComponentConnection;
        public GoalValidation validation;
        public int pointValue = 10;
        public bool isRequired = true;
        public bool isMandatory = true;
        public string targetComponentName = "";
        public float expectedValue = 0f;
    }

    [System.Serializable]
    public class GoalValidation
    {
        public ValidationType type = ValidationType.CircuitComplete;
        public string targetComponentId;
        public float expectedVoltage;
        public float expectedCurrent;
        public float tolerance = 0.1f;
        public ComponentConnection[] requiredConnections;
    }

    [System.Serializable]
    public class ComponentConnection
    {
        public string componentA;
        public string componentB;
        public bool mustBeDirectConnection = false;
    }

    [System.Serializable]
    public class MisconceptionCheck
    {
        public MisconceptionType type;
        public string detectionMethod;
        public string feedback = "Remember that electricity needs a complete loop to flow!";
        public bool showImmediateFeedback = true;
    }

    [System.Serializable]
    public class FeedbackMessage
    {
        public FeedbackType type = FeedbackType.Success;
        public string message = "Great job! You've powered the house!";
        public AudioClip soundEffect;
        public bool showVisualEffect = true;
    }

    [System.Serializable]
    public class HintSystem
    {
        public bool hintsEnabled = true;
        public float hintDelay = 30f; // Seconds before first hint
        public Hint[] hints;
    }

    [System.Serializable]
    public class Hint
    {
        public string text = "Try connecting the battery to create a complete circuit";
        public bool highlightComponents = true;
        public string[] componentIdsToHighlight;
        public HintType type = HintType.Text;
    }

    public enum DifficultyLevel
    {
        Beginner,
        Intermediate,
        Advanced,
        Expert
    }

    public enum GoalType
    {
        ComponentConnection,
        VoltageLevel,
        CurrentFlow,
        PowerCalculation,
        CircuitCompletion,
        MisconceptionCorrection,
        ComponentPowered,
        SpecificVoltage,
        SpecificCurrent,
        CircuitComplete
    }

    public enum ValidationType
    {
        CircuitComplete,
        SpecificVoltage,
        SpecificCurrent,
        ComponentsConnected,
        NoFloatingComponents
    }

    public enum FeedbackType
    {
        Success,
        Failure,
        Hint,
        Misconception,
        Progress
    }

    public enum HintType
    {
        Text,
        Visual,
        Animation,
        Audio
    }
}