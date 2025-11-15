using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles loading circuit data from CircuitSaveData and recreating the circuit in Unity.
/// Works with ComponentFactoryManager and ComponentTerminalManager to rebuild circuits.
/// </summary>
public class CircuitLoader : MonoBehaviour
{
    private ComponentFactoryManager factory;
    private ComponentTerminalManager terminalManager;
    private CircuitManager circuitManager;

    void Start()
    {
        factory = FindFirstObjectByType<ComponentFactoryManager>();
        terminalManager = FindFirstObjectByType<ComponentTerminalManager>();
        circuitManager = CircuitManager.Instance;

        if (factory == null)
            Debug.LogError("❌ CircuitLoader: ComponentFactoryManager not found!");
        if (terminalManager == null)
            Debug.LogError("❌ CircuitLoader: ComponentTerminalManager not found!");
        if (circuitManager == null)
            Debug.LogError("❌ CircuitLoader: CircuitManager not found!");
    }

    /// <summary>
    /// Loads a circuit from save data, recreating all components and wires.
    /// </summary>
    /// <param name="saveData">The circuit save data to load</param>
    public void LoadCircuit(CircuitSaveData saveData)
    {
        if (saveData == null)
        {
            Debug.LogError("❌ Cannot load null CircuitSaveData");
            return;
        }

        Debug.Log($"Loading circuit v{saveData.version}...");

        // Step 1: Clear existing circuit
        ClearCircuit();

        // Step 2: Create all components first
        var componentMap = new Dictionary<string, CircuitComponent3D>();
        foreach (var compData in saveData.components)
        {
            var component = CreateComponent(compData);
            if (component != null)
            {
                componentMap[compData.id] = component;
            }
        }

        // Step 3: Create wires (after all components exist)
        foreach (var wireData in saveData.wires)
        {
            CreateWire(wireData, componentMap);
        }

        // Step 4: Trigger circuit solve
        if (circuitManager != null)
        {
            circuitManager.MarkCircuitChanged();
        }

        Debug.Log($"✅ Circuit loaded: {saveData.components.Length} components, {saveData.wires.Length} wires");
    }

    /// <summary>
    /// Creates a single component from save data.
    /// </summary>
    private CircuitComponent3D CreateComponent(ComponentData data)
    {
        if (factory == null)
        {
            Debug.LogError("❌ Cannot create component: ComponentFactoryManager is null");
            return null;
        }

        Vector3 position = new Vector3(data.position[0], data.position[1], data.position[2]);
        CircuitComponent3D component = null;

        // Create component based on type using factory
        switch (data.type)
        {
            case "Battery":
                component = factory.CreateBattery(position);
                if (component != null)
                {
                    component.voltage = data.voltage;
                }
                break;

            case "Bulb":
                component = factory.CreateBulb(position);
                if (component != null)
                {
                    component.resistance = data.resistance;
                }
                break;

            case "Resistor":
                component = factory.CreateResistor(position);
                if (component != null)
                {
                    component.resistance = data.resistance;
                }
                break;

            case "Switch":
                component = factory.CreateSwitch(position);
                if (component != null)
                {
                    // TODO: Set switch state when CircuitComponent3D.switchState property is added
                    // component.switchState = data.switchClosed;
                }
                break;

            case "Junction":
                component = factory.CreateJunction(position);
                break;

            default:
                Debug.LogWarning($"⚠️ Unknown component type: {data.type}");
                return null;
        }

        if (component != null)
        {
            // Override name to match saved ID
            component.name = data.id;
            Debug.Log($"  ✅ Created {data.type}: {data.id} at {position}");
        }
        else
        {
            Debug.LogError($"  ❌ Failed to create {data.type}: {data.id}");
        }

        return component;
    }

    /// <summary>
    /// Creates a wire connection between two components using specific terminals.
    /// </summary>
    private void CreateWire(WireData wireData, Dictionary<string, CircuitComponent3D> componentMap)
    {
        // Get components from map
        if (!componentMap.TryGetValue(wireData.startComponent, out var startComp))
        {
            Debug.LogWarning($"⚠️ Wire start component not found: {wireData.startComponent}");
            return;
        }

        if (!componentMap.TryGetValue(wireData.endComponent, out var endComp))
        {
            Debug.LogWarning($"⚠️ Wire end component not found: {wireData.endComponent}");
            return;
        }

        // Get terminals from components
        if (terminalManager == null)
        {
            Debug.LogError("❌ Cannot create wire: ComponentTerminalManager is null");
            return;
        }

        var startTerminals = terminalManager.GetComponentTerminals(startComp);
        var endTerminals = terminalManager.GetComponentTerminals(endComp);

        if (startTerminals == null || endTerminals == null)
        {
            Debug.LogWarning($"⚠️ Could not get terminals for wire {wireData.startComponent} → {wireData.endComponent}");
            return;
        }

        // Find specific terminals by name
        var startTerminal = startTerminals.Find(t => t.name == wireData.startTerminal);
        var endTerminal = endTerminals.Find(t => t.name == wireData.endTerminal);

        if (startTerminal == null)
        {
            Debug.LogWarning($"⚠️ Start terminal not found: {wireData.startTerminal} on {wireData.startComponent}");
            return;
        }

        if (endTerminal == null)
        {
            Debug.LogWarning($"⚠️ End terminal not found: {wireData.endTerminal} on {wireData.endComponent}");
            return;
        }

        // Create wire GameObject
        GameObject wireObject = new GameObject($"Wire_{wireData.startComponent}_to_{wireData.endComponent}");
        var wire = wireObject.AddComponent<CircuitWire>();

        // Initialize with specific terminals
        wire.InitializeWithTerminals(startTerminal, endTerminal);

        Debug.Log($"  ✅ Created wire: {wireData.startComponent}.{wireData.startTerminal} → {wireData.endComponent}.{wireData.endTerminal}");
    }

    /// <summary>
    /// Clears the current circuit, destroying all components and wires.
    /// Uses the same pattern as PaletteUIManager.ResetCircuit().
    /// </summary>
    private void ClearCircuit()
    {
        Debug.Log("Clearing circuit before load...");

        // Clear CircuitManager's internal tracking
        if (circuitManager != null)
        {
            circuitManager.ClearAllComponents();
        }

        // Reset factory component counters
        if (factory != null)
        {
            factory.ResetComponentTracking();
        }

        // Find and destroy all existing components
        CircuitComponent3D[] components = FindObjectsByType<CircuitComponent3D>(FindObjectsSortMode.None);
        foreach (var comp in components)
        {
            if (comp != null && comp.gameObject != null)
            {
                Destroy(comp.gameObject);
            }
        }

        // Find and destroy all existing wires
        CircuitWire[] wires = FindObjectsByType<CircuitWire>(FindObjectsSortMode.None);
        foreach (var wire in wires)
        {
            if (wire != null && wire.gameObject != null)
            {
                Destroy(wire.gameObject);
            }
        }

        Debug.Log("Circuit cleared");
    }
}
