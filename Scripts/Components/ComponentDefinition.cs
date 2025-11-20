using UnityEngine;

namespace CircuitSimulator.Components
{
    /// <summary>
    /// ScriptableObject that defines component properties for themed components.
    /// Supports custom prefabs with predefined connection points for different scenarios.
    /// </summary>
    [CreateAssetMenu(fileName = "ThemedComponentDefinition", menuName = "Circuit Simulator/Themed Component Definition")]
    public class ThemedComponentDefinition : ScriptableObject
    {
        [Header("Basic Properties")]
        public string displayName = "Custom Component";
        public ThemedBaseComponentType baseType = ThemedBaseComponentType.Resistor;

        [TextArea(3, 5)]
        public string description = "A custom circuit component";

        [Header("Electrical Properties")]
        public ElectricalProperties electricalProperties = new ElectricalProperties();

        [Header("Visual Properties")]
        public VisualProperties visualProperties = new VisualProperties();

        [Header("Connection Points")]
        public ConnectionPointDefinition[] connectionPoints = new ConnectionPointDefinition[2];

        [Header("Educational Properties")]
        public bool showInPalette = true;
        public bool allowPropertyEditing = true;
        public string[] educationalTags = new string[0];

        [Header("Challenge Integration")]
        public bool fixedInChallenges = false;
        public float difficultyLevel = 1f;
    }

    [System.Serializable]
    public class ElectricalProperties
    {
        [Header("Default Values")]
        public float defaultVoltage = 0f;
        public float defaultResistance = 100f;
        public float defaultCurrent = 0f;

        [Header("Value Ranges")]
        public Vector2 voltageRange = new Vector2(0f, 24f);
        public Vector2 resistanceRange = new Vector2(0.1f, 10000f);
        public Vector2 currentRange = new Vector2(0f, 10f);

        [Header("Behavior")]
        public bool isVoltageSource = false;
        public bool isCurrentSource = false;
        public bool isSwitch = false;
        public bool defaultSwitchState = false;

        [Header("Switch Properties")]
        public float openResistance = float.PositiveInfinity;
        public float closedResistance = 0.001f;
    }

    [System.Serializable]
    public class VisualProperties
    {
        [Header("Prefab or Primitive")]
        public GameObject customPrefab;
        public PrimitiveType primitiveType = PrimitiveType.Cube;

        [Header("Appearance")]
        public Vector3 scale = Vector3.one;
        public Color defaultColor = Color.white;
        public Material defaultMaterial;

        [Header("Effects")]
        public bool hasGlowEffect = false;
        public Color glowColor = Color.cyan;
        public bool showsVoltageIndicator = true;
        public bool showsCurrentFlow = true;

        [Header("Animation")]
        public bool animateOnStateChange = false;
        public AnimationCurve stateChangeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    }

    [System.Serializable]
    public class ConnectionPointDefinition
    {
        [Header("Position")]
        public Vector3 localPosition = Vector3.zero;
        public Vector3 localRotation = Vector3.zero;

        [Header("Visual")]
        public string label = "";
        public Color terminalColor = Color.white;
        public PrimitiveType terminalShape = PrimitiveType.Sphere;
        public Vector3 terminalScale = Vector3.one * 0.15f;

        [Header("Electrical")]
        public TerminalType terminalType = TerminalType.Bidirectional;
        public bool isPositiveTerminal = false;

        [Header("Interaction")]
        public float connectionRadius = 0.5f;
        public bool showLabel = true;
        public bool hasGlowEffect = true;
    }

    public enum ThemedBaseComponentType
    {
        Battery,
        Resistor,
        Bulb,
        Switch,
        Junction,
        Capacitor,
        Inductor,
        Diode,
        Transistor,
        Custom
    }

    public enum TerminalType
    {
        Input,
        Output,
        Bidirectional,
        Ground,
        Power
    }

    /// <summary>
    /// Component that applies a ThemedComponentDefinition to a GameObject
    /// </summary>
    public class ThemedComponentDefinitionApplier : MonoBehaviour
    {
        [Header("Component Definition")]
        public ThemedComponentDefinition definition;

        [Header("Runtime References")]
        public Transform[] connectionPoints;

        void Start()
        {
            if (definition != null)
            {
                ApplyDefinition();
            }
        }

        public void ApplyDefinition()
        {
            if (definition == null)
            {
                Debug.LogWarning($"[ComponentDefinitionApplier] No definition assigned to {gameObject.name}");
                return;
            }

            // Apply electrical properties
            var electricalComponent = GetComponent<ElectricalComponent>();
            if (electricalComponent != null)
            {
                electricalComponent.Voltage = definition.electricalProperties.defaultVoltage;
                electricalComponent.Resistance = definition.electricalProperties.defaultResistance;
                electricalComponent.Current = definition.electricalProperties.defaultCurrent;

                electricalComponent.IsVoltageSource = definition.electricalProperties.isVoltageSource;
                electricalComponent.IsSwitch = definition.electricalProperties.isSwitch;
                if (definition.electricalProperties.isSwitch)
                {
                    electricalComponent.SwitchState = definition.electricalProperties.defaultSwitchState;
                }
            }

            // Apply visual properties
            var visualComponent = GetComponent<VisualComponent>();
            if (visualComponent != null)
            {
                visualComponent.ApplyDefinition(definition);
            }

            // Apply connection points
            SetupCustomConnectionPoints();

            // Update display name
            gameObject.name = definition.displayName;

            Debug.Log($"[ComponentDefinitionApplier] Applied definition '{definition.displayName}' to {gameObject.name}");
        }

        private void SetupCustomConnectionPoints()
        {
            if (definition.connectionPoints == null || definition.connectionPoints.Length == 0)
            {
                Debug.LogWarning($"[ComponentDefinitionApplier] No connection points defined for {definition.displayName}");
                return;
            }

            connectionPoints = new Transform[definition.connectionPoints.Length];

            for (int i = 0; i < definition.connectionPoints.Length; i++)
            {
                var pointDef = definition.connectionPoints[i];
                connectionPoints[i] = CreateCustomConnectionPoint(pointDef, i);
            }
        }

        private Transform CreateCustomConnectionPoint(ConnectionPointDefinition pointDef, int index)
        {
            // Create connection point GameObject
            GameObject connectionPoint = new GameObject($"ConnectionPoint_{index}");
            connectionPoint.transform.SetParent(transform);
            connectionPoint.transform.localPosition = pointDef.localPosition;
            connectionPoint.transform.localRotation = Quaternion.Euler(pointDef.localRotation);

            // Create visual terminal
            GameObject terminal = GameObject.CreatePrimitive(pointDef.terminalShape);
            terminal.name = "TerminalIndicator";
            terminal.transform.SetParent(connectionPoint.transform);
            terminal.transform.localPosition = Vector3.zero;
            terminal.transform.localScale = pointDef.terminalScale;

            // Set terminal appearance
            var renderer = terminal.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material terminalMaterial = new Material(Shader.Find("Standard"));
                terminalMaterial.color = pointDef.terminalColor;
                terminalMaterial.SetFloat("_Metallic", 0.8f);
                terminalMaterial.SetFloat("_Smoothness", 0.9f);
                renderer.material = terminalMaterial;
            }

            // Remove collider (parent handles interaction)
            var collider = terminal.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }

            // Add glow effect if enabled
            if (pointDef.hasGlowEffect)
            {
                Light glowLight = terminal.AddComponent<Light>();
                glowLight.type = LightType.Point;
                glowLight.color = pointDef.terminalColor;
                glowLight.range = 0.5f;
                glowLight.intensity = 0.3f;
                glowLight.shadows = LightShadows.None;
            }

            // Add label if specified
            if (pointDef.showLabel && !string.IsNullOrEmpty(pointDef.label))
            {
                CreateConnectionPointLabel(connectionPoint, pointDef.label);
            }

            Debug.Log($"[ComponentDefinitionApplier] Created custom connection point '{pointDef.label}' at {pointDef.localPosition}");
            return connectionPoint.transform;
        }

        private void CreateConnectionPointLabel(GameObject connectionPoint, string labelText)
        {
            GameObject labelObj = new GameObject("ConnectionPointLabel");
            labelObj.transform.SetParent(connectionPoint.transform);
            labelObj.transform.localPosition = new Vector3(0, 0.3f, 0);

            TextMesh textMesh = labelObj.AddComponent<TextMesh>();
            textMesh.text = labelText;
            textMesh.fontSize = 16;
            textMesh.color = Color.white;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.characterSize = 0.08f;

            // Make text face camera
            var camera = Camera.main ?? Camera.current;
            if (camera != null)
            {
                labelObj.transform.LookAt(camera.transform);
                labelObj.transform.Rotate(0, 180, 0);
            }
        }

        public Transform GetConnectionPoint(int index)
        {
            if (connectionPoints != null && index >= 0 && index < connectionPoints.Length)
            {
                return connectionPoints[index];
            }
            return null;
        }

        public Vector3 GetConnectionPosition(int index)
        {
            var connectionPoint = GetConnectionPoint(index);
            return connectionPoint != null ? connectionPoint.position : transform.position;
        }

        [ContextMenu("Apply Definition Now")]
        public void ApplyDefinitionInEditor()
        {
            ApplyDefinition();
        }
    }
}