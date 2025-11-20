using UnityEngine;

namespace CircuitSimulator
{
    /// <summary>
    /// ScriptableObject definition for custom circuit components.
    /// Enables creating educational components like "House" (special bulb) or "Car Battery" (high voltage battery).
    /// </summary>
    [CreateAssetMenu(fileName = "New Component Definition", menuName = "Circuit Simulator/Component Definition")]
    public class ComponentDefinition : ScriptableObject
    {
        [Header("Component Identity")]
        public string displayName = "House";
        public string description = "A house that needs electrical power";

        [Header("Base Component Type")]
        public BaseComponentType baseType = BaseComponentType.Bulb;

        [Header("Electrical Properties")]
        public ElectricalProperties electricalProperties = new ElectricalProperties();

        [Header("Visual Properties")]
        public VisualProperties visualProperties = new VisualProperties();

        [Header("Educational Properties")]
        public bool isEducationalFocus = false;
        [TextArea(3, 5)]
        public string educationalNote = "This component represents a real-world application of electrical circuits.";

        [Header("Challenge Integration")]
        public bool availableInChallenges = true;
        public MisconceptionType[] addressesMisconceptions;

        [Header("Legacy Compatibility")]
        public Color componentColor = Color.white;
        public float modelScale = 1f;
        public bool allowValueEditing = true;
        public bool allowMovement = true;
    }

    [System.Serializable]
    public class ElectricalProperties
    {
        [Header("Default Values")]
        public float defaultVoltage = 0f;
        public float defaultResistance = 10f;
        public float defaultCurrent = 0f;

        [Header("Legacy Compatibility")]
        public float voltage = 0f;
        public float resistance = 10f;

        [Header("Constraints")]
        public bool isVoltageSource = false;
        public bool isCurrentSource = false;
        public bool hasVariableResistance = false;

        [Header("Switch Properties")]
        public bool isSwitch = false;
        public bool defaultSwitchState = false; // false = open, true = closed
        public float openResistance = float.PositiveInfinity;
        public float closedResistance = 0.001f;
    }

    [System.Serializable]
    public class VisualProperties
    {
        [Header("3D Model")]
        public GameObject customPrefab;
        public PrimitiveType primitiveType = PrimitiveType.Cube;
        public Vector3 scale = Vector3.one;

        [Header("Materials")]
        public Material defaultMaterial;
        public Material activeMaterial;
        public Material errorMaterial;

        [Header("Colors")]
        public Color defaultColor = Color.gray;
        public Color activeColor = Color.green;
        public Color errorColor = Color.red;

        [Header("Effects")]
        public bool hasGlowEffect = false;
        public bool hasCurrentAnimation = false;
        public bool showsVoltageIndicator = true;
    }

    public enum BaseComponentType
    {
        Battery,
        Resistor,
        Bulb,
        Switch,
        Junction,
        Custom
    }

    public enum MisconceptionType
    {
        M1_SinkModel,           // Student thinks current flows from positive to negative and gets "used up"
        M2_CurrentAttenuation,  // Student thinks current decreases through components
        M8_ConstantCurrentSource, // Student thinks batteries provide constant current regardless of circuit
        M3_SequentialReasoning, // Student thinks current flows through one path at a time in parallel circuits
        M4_LocalReasoning,      // Student ignores distant parts of circuit when making predictions
        M5_PowerSupplyAsConstantCurrentSource, // Similar to M8 but specifically about power supplies
        M6_CurrentDivisionConfusion, // Student confused about how current divides in parallel branches
        M7_VoltageDropConfusion // Student confused about voltage drops across components
    }
}