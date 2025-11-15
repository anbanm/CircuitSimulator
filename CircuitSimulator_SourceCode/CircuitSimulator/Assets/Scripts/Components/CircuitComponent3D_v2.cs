using UnityEngine;
using System.Collections.Generic;
using CircuitSimulator.Services;
using CircuitSimulator.Components;
using CircuitSimulator;
using ComponentDefinitionLegacy = CircuitSimulator.ComponentDefinition;

/// <summary>
/// v2.0 Circuit Component coordinator that manages aspect-based components.
/// Acts as a facade coordinating ElectricalComponent, VisualComponent,
/// InteractionComponent, and LabelComponent.
/// </summary>
public class CircuitComponent3D_v2 : CircuitComponent3D
{
    [Header("Component Configuration")]
    [SerializeField] private ComponentType componentType = ComponentType.Resistor;
    [SerializeField] private ComponentDefinitionLegacy componentDefinition;

    // Aspect Components
    private ElectricalComponent electricalComponent;
    private VisualComponent visualComponent;
    private InteractionComponent interactionComponent;
    private LabelComponent labelComponent;

    // Service Dependencies
    private ICircuitManager circuitManager;

    // Legacy compatibility properties
    public new ComponentType ComponentType
    {
        get => electricalComponent?.ComponentType ?? componentType;
        set
        {
            componentType = value;
            if (electricalComponent != null)
                electricalComponent.ComponentType = value;
        }
    }

    public new float voltage
    {
        get => electricalComponent?.Voltage ?? 0f;
        set
        {
            if (electricalComponent != null)
                electricalComponent.Voltage = value;
        }
    }

    public new float resistance
    {
        get => electricalComponent?.Resistance ?? 0f;
        set
        {
            if (electricalComponent != null)
                electricalComponent.Resistance = value;
        }
    }

    public new float current
    {
        get => electricalComponent?.Current ?? 0f;
        set
        {
            if (electricalComponent != null)
                electricalComponent.Current = value;
        }
    }

    public new float voltageDrop
    {
        get => electricalComponent?.VoltageDrop ?? 0f;
        set
        {
            if (electricalComponent != null)
                electricalComponent.VoltageDrop = value;
        }
    }

    // Wire connections (for compatibility)
    [System.NonSerialized]
    private new List<GameObject> connectedWires = new List<GameObject>();

    // Legacy logical component reference
    [System.NonSerialized]
    public new CircuitComponent logicalComponent;

    void Awake()
    {
        InitializeAspectComponents();
    }

    void Start()
    {
        InitializeServices();
        SetupComponentConfiguration();
        RegisterWithCircuit();
    }

    void OnDestroy()
    {
        UnregisterFromCircuit();
    }

    #region Initialization

    private void InitializeAspectComponents()
    {
        // Get or create aspect components
        electricalComponent = GetComponent<ElectricalComponent>();
        if (electricalComponent == null)
            electricalComponent = gameObject.AddComponent<ElectricalComponent>();

        visualComponent = GetComponent<VisualComponent>();
        if (visualComponent == null)
            visualComponent = gameObject.AddComponent<VisualComponent>();

        interactionComponent = GetComponent<InteractionComponent>();
        if (interactionComponent == null)
            interactionComponent = gameObject.AddComponent<InteractionComponent>();

        labelComponent = GetComponent<LabelComponent>();
        if (labelComponent == null)
            labelComponent = gameObject.AddComponent<LabelComponent>();

        // Subscribe to aspect events
        SubscribeToAspectEvents();
    }

    private void InitializeServices()
    {
        // Get circuit manager service
        circuitManager = ServiceLocator.Instance.TryGet<ICircuitManager>();
        if (circuitManager == null)
        {
            Debug.LogWarning($"[CircuitComponent3D_v2] ICircuitManager service not found for {name}");
        }
    }

    private void SetupComponentConfiguration()
    {
        // Apply component definition if available
        if (componentDefinition != null)
        {
            ApplyComponentDefinition(componentDefinition);
        }
        else
        {
            // Set default configuration based on component type
            ConfigureDefaultProperties();
        }

        // Setup connection terminals for educational clarity
        SetupConnectionTerminals();
    }

    private void SubscribeToAspectEvents()
    {
        if (electricalComponent != null)
        {
            electricalComponent.OnVoltageChanged += OnElectricalChanged;
            electricalComponent.OnCurrentChanged += OnElectricalChanged;
            electricalComponent.OnResistanceChanged += OnElectricalChanged;
        }

        if (interactionComponent != null)
        {
            interactionComponent.OnSelected += OnComponentSelected;
            interactionComponent.OnMoved += OnComponentMoved;
            interactionComponent.OnDeleted += OnComponentDeleted;
        }
    }

    #endregion

    #region Component Configuration

    private void ConfigureDefaultProperties()
    {
        if (electricalComponent != null)
        {
            electricalComponent.ComponentType = componentType;
        }

        // Set component-specific defaults
        switch (ComponentType)
        {
            case ComponentType.Battery:
                if (electricalComponent != null)
                {
                    electricalComponent.Voltage = 12f;
                    electricalComponent.Resistance = 0.1f;
                    electricalComponent.IsVoltageSource = true;
                }
                break;

            case ComponentType.Resistor:
                if (electricalComponent != null)
                {
                    electricalComponent.Resistance = 10f;
                }
                break;

            case ComponentType.Bulb:
                if (electricalComponent != null)
                {
                    electricalComponent.Resistance = 5f;
                }
                break;

            case ComponentType.Switch:
                if (electricalComponent != null)
                {
                    electricalComponent.IsSwitch = true;
                    electricalComponent.Resistance = 0.001f; // Closed resistance
                }
                break;
        }
    }

    public void ApplyComponentDefinition(ComponentDefinitionLegacy definition)
    {
        if (definition == null) return;

        componentDefinition = definition;
        ComponentType = ConvertBaseType(definition.baseType);

        // Apply to aspect components
        electricalComponent?.ApplyDefinition(definition);
        visualComponent?.ApplyDefinition(definition);
        labelComponent?.ApplyDefinition(definition);

        // Update name
        gameObject.name = definition.displayName;

        Debug.Log($"[CircuitComponent3D_v2] Applied ComponentDefinition '{definition.displayName}'");
    }

    private ComponentType ConvertBaseType(CircuitSimulator.BaseComponentType baseType)
    {
        switch (baseType)
        {
            case CircuitSimulator.BaseComponentType.Battery: return ComponentType.Battery;
            case CircuitSimulator.BaseComponentType.Resistor: return ComponentType.Resistor;
            case CircuitSimulator.BaseComponentType.Bulb: return ComponentType.Bulb;
            case CircuitSimulator.BaseComponentType.Switch: return ComponentType.Switch;
            case CircuitSimulator.BaseComponentType.Junction: return ComponentType.Junction;
            default: return ComponentType.Resistor;
        }
    }

    #endregion

    #region Circuit Integration

    private void RegisterWithCircuit()
    {
        if (circuitManager != null)
        {
            circuitManager.RegisterComponent(this);
        }
        else
        {
            // Fallback to legacy singleton
            if (CircuitManager.Instance != null)
            {
                CircuitManager.Instance.RegisterComponent(this);
            }
            else
            {
                Debug.LogWarning($"[CircuitComponent3D_v2] No circuit manager found for {name}");
            }
        }
    }

    private void UnregisterFromCircuit()
    {
        if (circuitManager != null)
        {
            circuitManager.UnregisterComponent(this);
        }
        else if (CircuitManager.Instance != null)
        {
            CircuitManager.Instance.UnregisterComponent(this);
        }
    }

    #endregion

    #region Connection System

    private void SetupConnectionTerminals()
    {
        // Connection terminals are now handled by InteractionComponent
        // This method exists for legacy compatibility
        if (interactionComponent != null)
        {
            // Terminals are automatically created by InteractionComponent
            Debug.Log($"[CircuitComponent3D_v2] Connection terminals setup for {name}");
        }
    }

    public void AddWire(GameObject wire)
    {
        if (wire != null && !connectedWires.Contains(wire))
        {
            connectedWires.Add(wire);
            Debug.Log($"[CircuitComponent3D_v2] Wire added to {name}: {wire.name}");
        }
    }

    public void RemoveWire(GameObject wire)
    {
        if (wire != null && connectedWires.Contains(wire))
        {
            connectedWires.Remove(wire);
            Debug.Log($"[CircuitComponent3D_v2] Wire removed from {name}: {wire.name}");
        }
    }

    public new List<GameObject> GetConnectedWires()
    {
        return new List<GameObject>(connectedWires);
    }

    public bool IsConnectedToWire(GameObject wire)
    {
        return connectedWires.Contains(wire);
    }

    public Vector3 GetConnectionPoint(bool useLeftTerminal = true)
    {
        if (interactionComponent != null)
        {
            return interactionComponent.GetConnectionPoint(useLeftTerminal);
        }
        return transform.position;
    }

    #endregion

    #region Circuit Solving Integration

    public void UpdateFromLogical(CircuitComponent logical)
    {
        if (logical == null || electricalComponent == null) return;

        logicalComponent = logical;

        // Update electrical values
        electricalComponent.UpdateFromSolver(logical.Current, logical.VoltageDrop);

        Debug.Log($"[CircuitComponent3D_v2] Updated {name} from solver: I={logical.Current:F3}A, V={logical.VoltageDrop:F2}V");
    }

    public CircuitComponent ToLogicalComponent()
    {
        if (electricalComponent != null)
        {
            return electricalComponent.ToLogicalComponent();
        }

        // Fallback to basic component
        return new Resistor("fallback_resistor", new CircuitNode("node_a"), new CircuitNode("node_b"), 10f);
    }

    public new void UpdateVisualFeedback()
    {
        // Visual feedback is now automatically handled by VisualComponent
        // through electrical events. This method exists for legacy compatibility.
    }

    #endregion

    #region Event Handlers

    private void OnElectricalChanged(float value)
    {
        // Electrical values changed - circuit manager will be notified through events
        // Labels will be updated automatically by LabelComponent
    }

    private void OnComponentSelected()
    {
        Debug.Log($"[CircuitComponent3D_v2] Component selected: {name}");
    }

    private void OnComponentMoved(Vector3 newPosition)
    {
        // Notify circuit manager of position change
        circuitManager?.MarkCircuitChanged();
        Debug.Log($"[CircuitComponent3D_v2] Component moved: {name} to {newPosition}");
    }

    private void OnComponentDeleted()
    {
        Debug.Log($"[CircuitComponent3D_v2] Component deleted: {name}");
    }

    #endregion

    #region Public Interface

    /// <summary>
    /// Get specific aspect component
    /// </summary>
    public T GetAspect<T>() where T : MonoBehaviour
    {
        return GetComponent<T>();
    }

    /// <summary>
    /// Configure interaction permissions for challenges
    /// </summary>
    public void SetFixed(bool isFixed)
    {
        interactionComponent?.SetFixed(isFixed);
    }

    /// <summary>
    /// Set component highlighting
    /// </summary>
    public void SetHighlighted(bool highlighted)
    {
        visualComponent?.SetHighlighted(highlighted);
    }

    /// <summary>
    /// Toggle switch state (if this is a switch)
    /// </summary>
    public void ToggleSwitch()
    {
        electricalComponent?.ToggleSwitch();
    }

    /// <summary>
    /// Get current power consumption/generation
    /// </summary>
    public float GetPower()
    {
        return electricalComponent?.GetPower() ?? 0f;
    }

    /// <summary>
    /// Check if component is a voltage source
    /// </summary>
    public bool IsVoltageSource()
    {
        return electricalComponent?.IsVoltageSource ?? false;
    }

    /// <summary>
    /// Check if component is a switch
    /// </summary>
    public bool IsSwitch()
    {
        return electricalComponent?.IsSwitch ?? false;
    }

    #endregion

    #region Legacy Compatibility

    // These methods provide compatibility with existing code

    public void SetComponentType(ComponentType type)
    {
        ComponentType = type;
        ConfigureDefaultProperties();
    }

    public ComponentType GetComponentType()
    {
        return ComponentType;
    }

    public void SetResistance(float newResistance)
    {
        resistance = newResistance;
    }

    public void SetVoltage(float newVoltage)
    {
        voltage = newVoltage;
    }

    #endregion

    [ContextMenu("Debug Component State")]
    public void DebugComponentState()
    {
        Debug.Log($"[CircuitComponent3D_v2] {name} State:\n" +
                 $"  Type: {ComponentType}\n" +
                 $"  Voltage: {voltage:F2}V\n" +
                 $"  Current: {current:F3}A\n" +
                 $"  Resistance: {resistance:F2}Ω\n" +
                 $"  Power: {GetPower():F2}W\n" +
                 $"  Connected Wires: {connectedWires.Count}\n" +
                 $"  Has Definition: {(componentDefinition != null)}");
    }

    [ContextMenu("Apply Test Definition")]
    public void ApplyTestDefinition()
    {
        // For testing - create a simple definition
        var testDef = ScriptableObject.CreateInstance<ComponentDefinition>();
        testDef.displayName = "Test House";
        testDef.baseType = BaseComponentType.Bulb;
        testDef.electricalProperties.defaultResistance = 15f;
        testDef.visualProperties.defaultColor = Color.yellow;

        ApplyComponentDefinition(testDef);
    }
}