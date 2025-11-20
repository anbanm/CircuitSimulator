using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// ScriptableObject that defines a pre-built circuit scenario for educational use
/// Contains all component data and expected learning outcomes
/// </summary>
[CreateAssetMenu(fileName = "New Circuit Scenario", menuName = "Circuit Simulator/Circuit Scenario")]
public class CircuitScenario : ScriptableObject
{
    [Header("Scenario Info")]
    [Tooltip("Display name for the scenario")]
    public string scenarioName = "Basic Circuit";

    [Tooltip("Grade level this scenario targets")]
    public GradeLevel targetGrade = GradeLevel.Grade9;

    [Tooltip("Description of what students will learn")]
    [TextArea(3, 5)]
    public string learningDescription = "Students will learn basic series circuit principles and Ohm's Law.";

    [Tooltip("Expected time to complete (minutes)")]
    public int estimatedTimeMinutes = 15;

    [Header("Circuit Configuration")]
    [Tooltip("Components that make up this scenario")]
    public List<ComponentData> components = new List<ComponentData>();

    [Tooltip("Wire connections between components")]
    public List<WireConnection> connections = new List<WireConnection>();

    [Tooltip("Should values be displayed initially?")]
    public bool showValuesAtStart = false;

    [Tooltip("Which measurements should be highlighted")]
    public DisplaySettings displaySettings = new DisplaySettings();

    [Header("Learning Objectives")]
    [Tooltip("Key concepts this scenario teaches")]
    public List<LearningObjective> objectives = new List<LearningObjective>();

    [Tooltip("Common misconceptions this addresses")]
    public List<MisconceptionType> addressedMisconceptions = new List<MisconceptionType>();

    [Header("Expected Results")]
    [Tooltip("Expected circuit behavior for validation")]
    public CircuitResults expectedResults = new CircuitResults();

    [Tooltip("Interactive questions for students")]
    public List<ScenarioQuestion> questions = new List<ScenarioQuestion>();
}

[System.Serializable]
public class ComponentData
{
    [Tooltip("Type of component")]
    public ComponentType type = ComponentType.Battery;

    [Tooltip("Unique identifier for connections")]
    public string componentId = "Battery1";

    [Tooltip("Display name for students")]
    public string displayName = "12V Battery";

    [Tooltip("Position in the scene")]
    public Vector3 position = Vector3.zero;

    [Tooltip("Component properties")]
    public ComponentProperties properties = new ComponentProperties();

    [Tooltip("Should this component be interactive?")]
    public bool isInteractive = true;

    [Tooltip("Custom label text override")]
    public string customLabel = "";
}

[System.Serializable]
public class ComponentProperties
{
    [Tooltip("Voltage for batteries (V)")]
    public float voltage = 12f;

    [Tooltip("Resistance for resistors/bulbs (Ω)")]
    public float resistance = 10f;

    [Tooltip("Switch state (true = closed)")]
    public bool switchClosed = true;

    [Tooltip("Color tint for visual distinction")]
    public Color componentColor = Color.white;
}

[System.Serializable]
public class WireConnection
{
    [Tooltip("Starting component ID")]
    public string fromComponent = "Battery1";

    [Tooltip("Ending component ID")]
    public string toComponent = "Resistor1";

    [Tooltip("Connection type for validation")]
    public ConnectionType connectionType = ConnectionType.SeriesConnection;

    [Tooltip("Should this wire show current flow animation?")]
    public bool showCurrentFlow = true;
}

[System.Serializable]
public class DisplaySettings
{
    [Tooltip("Show voltage readings on components")]
    public bool showVoltage = true;

    [Tooltip("Show current readings on components")]
    public bool showCurrent = true;

    [Tooltip("Show resistance values")]
    public bool showResistance = true;

    [Tooltip("Show power calculations")]
    public bool showPower = false;

    [Tooltip("Label display style")]
    public LabelStyle labelStyle = LabelStyle.CleanMinimal;

    [Tooltip("Update frequency for real-time values")]
    public float updateFrequency = 0.5f;
}

[System.Serializable]
public class CircuitResults
{
    [Tooltip("Expected total current (A)")]
    public float expectedCurrent = 1.2f;

    [Tooltip("Expected total resistance (Ω)")]
    public float expectedResistance = 10f;

    [Tooltip("Expected power consumption (W)")]
    public float expectedPower = 14.4f;

    [Tooltip("Tolerance for validation (+/- %)")]
    public float tolerance = 5f;

    [Tooltip("Key voltage points to validate")]
    public List<VoltagePoint> keyVoltages = new List<VoltagePoint>();

    [Header("Runtime Values")]
    public float totalCurrent = 0f;
    public float totalResistance = 0f;
    public float totalPower = 0f;
}

[System.Serializable]
public class VoltagePoint
{
    [Tooltip("Component to measure")]
    public string componentId = "Resistor1";

    [Tooltip("Expected voltage drop (V)")]
    public float expectedVoltage = 12f;

    [Tooltip("What this measurement demonstrates")]
    public string explanation = "Voltage drop across resistor equals battery voltage in series circuit";
}

[System.Serializable]
public class ScenarioQuestion
{
    [Tooltip("Question text for students")]
    [TextArea(2, 4)]
    public string questionText = "What happens to the current if you double the resistance?";

    [Tooltip("Correct answer")]
    public string correctAnswer = "Current is halved (I = V/R)";

    [Tooltip("Common wrong answers with explanations")]
    public List<string> commonMistakes = new List<string>
    {
        "Current doubles - No, current is inversely proportional to resistance",
        "Current stays the same - No, Ohm's Law shows I = V/R"
    };

    [Tooltip("When to ask this question")]
    public QuestionTiming timing = QuestionTiming.AfterSolving;
}

// Supporting Enums
public enum GradeLevel
{
    Grade7, Grade8, Grade9, Grade10, Grade11, Grade12
}


public enum ConnectionType
{
    SeriesConnection, ParallelConnection, BranchConnection
}

public enum LabelStyle
{
    CleanMinimal,     // "12V, 1.2A"
    Detailed,         // "Voltage: 12V, Current: 1.2A"
    Educational,      // "V=12V, I=1.2A, R=10Ω"
    ColorCoded        // Color-coded by value ranges
}

public enum LearningObjective
{
    OhmsLaw,
    SeriesCircuits,
    ParallelCircuits,
    PowerCalculation,
    VoltageDistribution,
    CurrentConservation,
    ResistanceCalculation
}

public enum MisconceptionType
{
    M1_SinkModel,           // One wire circuits
    M2_CurrentAttenuation,  // Current "used up"
    M8_ConstantCurrentSource, // Battery behavior
    M11_VoltageConsumption, // Voltage "used up"
    M15_PowerConfusion      // Power vs energy
}

public enum QuestionTiming
{
    BeforeBuilding, DuringBuilding, AfterSolving, OnCompletion
}