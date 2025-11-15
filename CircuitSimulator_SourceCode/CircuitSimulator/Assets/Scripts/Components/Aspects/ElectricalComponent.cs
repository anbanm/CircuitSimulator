using UnityEngine;
using CircuitSimulator.Services;

namespace CircuitSimulator.Components
{
    /// <summary>
    /// Aspect component responsible for electrical properties and behavior.
    /// Separates electrical concerns from visual and interaction aspects.
    /// </summary>
    public class ElectricalComponent : MonoBehaviour
    {
        [Header("Electrical Properties")]
        [SerializeField] private float voltage = 0f;
        [SerializeField] private float resistance = 10f;
        [SerializeField] private float current = 0f;
        [SerializeField] private float voltageDrop = 0f;

        [Header("Component Type")]
        [SerializeField] private ComponentType componentType = ComponentType.Resistor;
        [SerializeField] private bool isVoltageSource = false;
        [SerializeField] private bool isCurrentSource = false;

        [Header("Switch Properties")]
        [SerializeField] private bool isSwitch = false;
        [SerializeField] private bool switchState = false; // false = open, true = closed
        [SerializeField] private float openResistance = float.PositiveInfinity;
        [SerializeField] private float closedResistance = 0.001f;

        // Events for electrical state changes
        public System.Action<float> OnVoltageChanged;
        public System.Action<float> OnCurrentChanged;
        public System.Action<float> OnResistanceChanged;
        public System.Action<bool> OnSwitchStateChanged;

        // Properties with event firing
        public float Voltage
        {
            get => voltage;
            set
            {
                if (Mathf.Abs(voltage - value) > 0.001f)
                {
                    voltage = value;
                    OnVoltageChanged?.Invoke(voltage);
                }
            }
        }

        public float Resistance
        {
            get => isSwitch ? (switchState ? closedResistance : openResistance) : resistance;
            set
            {
                if (Mathf.Abs(resistance - value) > 0.001f)
                {
                    resistance = value;
                    OnResistanceChanged?.Invoke(Resistance);
                }
            }
        }

        public float Current
        {
            get => current;
            set
            {
                if (Mathf.Abs(current - value) > 0.001f)
                {
                    current = value;
                    OnCurrentChanged?.Invoke(current);
                }
            }
        }

        public float VoltageDrop
        {
            get => voltageDrop;
            set => voltageDrop = value;
        }

        public ComponentType ComponentType
        {
            get => componentType;
            set => componentType = value;
        }

        public bool IsVoltageSource
        {
            get => isVoltageSource;
            set => isVoltageSource = value;
        }

        public bool IsCurrentSource
        {
            get => isCurrentSource;
            set => isCurrentSource = value;
        }

        public bool IsSwitch
        {
            get => isSwitch;
            set => isSwitch = value;
        }

        public bool SwitchState
        {
            get => switchState;
            set
            {
                if (switchState != value)
                {
                    switchState = value;
                    OnSwitchStateChanged?.Invoke(switchState);
                    OnResistanceChanged?.Invoke(Resistance); // Resistance changes with switch state
                }
            }
        }

        void Start()
        {
            InitializeElectricalProperties();
        }

        private void InitializeElectricalProperties()
        {
            // Set default values based on component type
            switch (componentType)
            {
                case ComponentType.Battery:
                    voltage = 12f;
                    resistance = 0.1f;
                    isVoltageSource = true;
                    break;

                case ComponentType.Resistor:
                    voltage = 0f;
                    resistance = 10f;
                    isVoltageSource = false;
                    break;

                case ComponentType.Bulb:
                    voltage = 0f;
                    resistance = 5f;
                    isVoltageSource = false;
                    break;

                case ComponentType.Switch:
                    voltage = 0f;
                    resistance = closedResistance;
                    isSwitch = true;
                    isVoltageSource = false;
                    break;

                case ComponentType.Junction:
                    voltage = 0f;
                    resistance = 0.001f; // Very low resistance for junctions
                    isVoltageSource = false;
                    break;
            }
        }

        /// <summary>
        /// Apply properties from ComponentDefinition (ScriptableObjects)
        /// </summary>
        public void ApplyDefinition(CircuitSimulator.ComponentDefinition definition)
        {
            if (definition == null) return;

            var electrical = definition.electricalProperties;

            voltage = electrical.defaultVoltage;
            resistance = electrical.defaultResistance;
            current = electrical.defaultCurrent;
            isVoltageSource = electrical.isVoltageSource;
            isCurrentSource = electrical.isCurrentSource;

            if (electrical.isSwitch)
            {
                isSwitch = true;
                switchState = electrical.defaultSwitchState;
                openResistance = electrical.openResistance;
                closedResistance = electrical.closedResistance;
            }

            Debug.Log($"[ElectricalComponent] Applied definition: V={voltage}V, R={Resistance}Ω, Type={componentType}");
        }

        /// <summary>
        /// Apply properties from ThemedComponentDefinition
        /// </summary>
        public void ApplyDefinition(CircuitSimulator.Components.ThemedComponentDefinition definition)
        {
            if (definition == null) return;

            var electricalProps = definition.electricalProperties;

            voltage = electricalProps.defaultVoltage;
            resistance = electricalProps.defaultResistance;
            current = electricalProps.defaultCurrent;
            isVoltageSource = electricalProps.isVoltageSource;
            isCurrentSource = electricalProps.isCurrentSource;

            if (electricalProps.isSwitch)
            {
                isSwitch = true;
                switchState = electricalProps.defaultSwitchState;
                openResistance = electricalProps.openResistance;
                closedResistance = electricalProps.closedResistance;
            }

            Debug.Log($"[ElectricalComponent] Applied themed definition: V={voltage}V, R={Resistance}Ω, Type={componentType}");
        }

        /// <summary>
        /// Update electrical values from circuit solver
        /// </summary>
        public void UpdateFromSolver(float newCurrent, float newVoltageDrop)
        {
            Current = newCurrent;
            VoltageDrop = newVoltageDrop;
        }

        /// <summary>
        /// Get electrical properties for circuit solver
        /// </summary>
        public CircuitComponent ToLogicalComponent()
        {
            string id = $"{componentType}_{GetInstanceID()}";
            var nodeA = new CircuitNode($"{id}_nodeA");
            var nodeB = new CircuitNode($"{id}_nodeB");

            switch (componentType)
            {
                case ComponentType.Battery:
                    return new Battery(id, nodeA, nodeB, voltage);

                case ComponentType.Resistor:
                    return new Resistor(id, nodeA, nodeB, Resistance);

                case ComponentType.Bulb:
                    return new Bulb(id, nodeA, nodeB, Resistance);

                case ComponentType.Switch:
                    // Switch acts as variable resistor
                    return new Resistor(id, nodeA, nodeB, Resistance);

                case ComponentType.Junction:
                    // Junction acts as very low resistance
                    return new Resistor(id, nodeA, nodeB, 0.001f);

                default:
                    return new Resistor(id, nodeA, nodeB, Resistance);
            }
        }

        /// <summary>
        /// Toggle switch state (if this is a switch)
        /// </summary>
        public void ToggleSwitch()
        {
            if (isSwitch)
            {
                SwitchState = !SwitchState;
                Debug.Log($"[ElectricalComponent] Switch toggled: {(SwitchState ? "CLOSED" : "OPEN")}");
            }
        }

        /// <summary>
        /// Get current power consumption/generation
        /// </summary>
        public float GetPower()
        {
            return Voltage * Current; // P = V * I
        }

        /// <summary>
        /// Check if component is consuming power
        /// </summary>
        public bool IsConsumingPower()
        {
            return !isVoltageSource && current > 0.001f;
        }

        /// <summary>
        /// Check if component is generating power
        /// </summary>
        public bool IsGeneratingPower()
        {
            return isVoltageSource && current > 0.001f;
        }

        // Debug visualization
        void OnValidate()
        {
            // Update resistance based on switch state in editor
            if (isSwitch)
            {
                // This will trigger the resistance calculation
                var currentResistance = Resistance;
            }
        }

        [ContextMenu("Debug Electrical State")]
        public void DebugElectricalState()
        {
            Debug.Log($"[ElectricalComponent] {gameObject.name}:\n" +
                     $"  Type: {componentType}\n" +
                     $"  Voltage: {voltage:F2}V\n" +
                     $"  Current: {current:F3}A\n" +
                     $"  Resistance: {Resistance:F2}Ω\n" +
                     $"  Power: {GetPower():F2}W\n" +
                     $"  Is Switch: {isSwitch} (State: {(switchState ? "CLOSED" : "OPEN")})");
        }
    }
}