using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Manages the creation and positioning of input/output terminals on circuit components
/// Replaces spatial node sharing with explicit terminal connections
/// </summary>
public class ComponentTerminalManager : MonoBehaviour
{
    [Header("Terminal Settings")]
    public float terminalDistance = 0.4f;
    public Color inputColor = Color.green;
    public Color outputColor = Color.red;
    public Color terminalHighlightColor = Color.yellow;
    
    private CircuitManager circuitManager;
    private Dictionary<CircuitComponent3D, List<ComponentTerminal>> componentTerminals = new Dictionary<CircuitComponent3D, List<ComponentTerminal>>();
    
    public void Initialize()
    {
        circuitManager = CircuitManager.Instance;
    }
    
    public void SetupComponentTerminals(CircuitComponent3D component)
    {
        if (component == null) return;

        // CRITICAL FIX: Check if terminals already exist - don't recreate them unnecessarily!
        // This prevents destroying terminals when a wire is deleted, which was causing
        // all components to lose their terminals.
        if (componentTerminals.ContainsKey(component) && componentTerminals[component].Count > 0)
        {
            // Terminals already exist and are valid, skip recreation
            return;
        }

        // NEW FIX: Check if terminal GameObjects already exist as children (loaded from scene)
        // If so, register them instead of destroying and recreating
        var existingTerminals = component.GetComponentsInChildren<ComponentTerminal>();
        if (existingTerminals != null && existingTerminals.Length >= 2)
        {
            componentTerminals[component] = new List<ComponentTerminal>(existingTerminals);
            return;
        }

        // CHECK FOR PRE-PLACED TERMINAL GAMEOBJECTS (empty GameObjects with terminal names)
        // This allows prefabs to define custom terminal positions without ComponentTerminal scripts
        var prePlacedTerminals = TrySetupPrePlacedTerminals(component);
        if (prePlacedTerminals != null && prePlacedTerminals.Count >= 2)
        {
            componentTerminals[component] = prePlacedTerminals;
            return;
        }

        RemoveComponentTerminals(component);
        var terminals = new List<ComponentTerminal>();

        switch (component.ComponentType)
        {
            case ComponentType.Battery:
                // Battery is POLARIZED - has specific negative/positive terminals
                terminals.Add(CreateTerminal(component, Vector3.left * terminalDistance, true, "NegativeTerminal"));
                terminals.Add(CreateTerminal(component, Vector3.right * terminalDistance, false, "PositiveTerminal"));
                break;

            case ComponentType.Resistor:
            case ComponentType.Bulb:
            case ComponentType.Switch:
                // Non-polarized components - just generic TerminalA and TerminalB
                // NO INPUT/OUTPUT - direction is determined by connection order!
                terminals.Add(CreateTerminal(component, Vector3.left * terminalDistance, false, "TerminalA"));
                terminals.Add(CreateTerminal(component, Vector3.right * terminalDistance, false, "TerminalB"));
                break;

            case ComponentType.Junction:
                // Junctions need 4 neutral terminals for multi-way connections (parallel circuits)
                // All terminals are "neutral" - neither strictly input nor output
                // Position terminals in cardinal directions for intuitive wire connections
                terminals.Add(CreateTerminal(component, Vector3.left * terminalDistance, false, "LeftTerminal"));
                terminals.Add(CreateTerminal(component, Vector3.right * terminalDistance, false, "RightTerminal"));
                terminals.Add(CreateTerminal(component, Vector3.forward * terminalDistance, false, "FrontTerminal"));
                terminals.Add(CreateTerminal(component, Vector3.back * terminalDistance, false, "BackTerminal"));

                // Make junction terminals visually distinct (purple/neutral color)
                foreach (var terminal in terminals)
                {
                    terminal.terminalColor = new Color(0.7f, 0.5f, 0.9f, 1f); // Purple for neutral
                }
                break;
        }

        componentTerminals[component] = terminals;
    }
    
    ComponentTerminal CreateTerminal(CircuitComponent3D component, Vector3 localPosition, bool isInput, string terminalName)
    {
        GameObject terminalObj = new GameObject(terminalName);
        terminalObj.transform.SetParent(component.transform);
        terminalObj.transform.localPosition = localPosition;

        var terminal = terminalObj.AddComponent<ComponentTerminal>();
        terminal.isInput = isInput;
        terminal.terminalColor = isInput ? inputColor : outputColor;
        terminal.highlightColor = terminalHighlightColor;
        terminal.terminalSize = 0.5f;  // Increased to 0.5f for maximum visibility

        // CRITICAL FIX: Initialize terminal IMMEDIATELY, don't wait for Start()
        // This ensures electricalNode is created before BuildLogicalCircuit() runs
        terminal.InitializeTerminal(component);

        var collider = terminalObj.AddComponent<SphereCollider>();
        collider.radius = 0.5f;  // Match terminal visual size

        Vector3 worldPos = terminalObj.transform.position;

        return terminal;
    }

    /// <summary>
    /// Looks for pre-placed terminal GameObjects in the prefab (by name) and converts them to ComponentTerminals.
    /// This allows prefabs to define custom terminal positions using empty GameObjects.
    /// Supported names: NegativeTerminal, PositiveTerminal, TerminalA, TerminalB
    /// </summary>
    List<ComponentTerminal> TrySetupPrePlacedTerminals(CircuitComponent3D component)
    {
        var terminals = new List<ComponentTerminal>();

        // Define terminal names to look for based on component type
        string[] terminalNames;
        bool[] isInputFlags;

        switch (component.ComponentType)
        {
            case ComponentType.Battery:
                terminalNames = new[] { "NegativeTerminal", "PositiveTerminal" };
                isInputFlags = new[] { true, false }; // Negative is input (ground), Positive is output
                break;
            case ComponentType.Resistor:
            case ComponentType.Bulb:
            case ComponentType.Switch:
                terminalNames = new[] { "TerminalA", "TerminalB", "NegativeTerminal", "PositiveTerminal" };
                isInputFlags = new[] { false, false, false, false }; // Non-polarized
                break;
            default:
                return null; // Junction and other types use dynamic creation
        }

        // Look for pre-placed GameObjects by name
        foreach (Transform child in component.transform)
        {
            for (int i = 0; i < terminalNames.Length; i++)
            {
                if (child.name == terminalNames[i])
                {
                    // Found a pre-placed terminal - add ComponentTerminal script if not already present
                    var terminal = child.GetComponent<ComponentTerminal>();
                    if (terminal == null)
                    {
                        terminal = child.gameObject.AddComponent<ComponentTerminal>();
                        terminal.isInput = isInputFlags[i];
                        terminal.terminalColor = isInputFlags[i] ? inputColor : outputColor;
                        terminal.highlightColor = terminalHighlightColor;
                        terminal.terminalSize = 0.5f;

                        // Initialize the terminal
                        terminal.InitializeTerminal(component);

                        // Add collider if not present
                        if (child.GetComponent<SphereCollider>() == null)
                        {
                            var collider = child.gameObject.AddComponent<SphereCollider>();
                            collider.radius = 0.5f;
                        }

                        Debug.Log($"Setup pre-placed terminal '{child.name}' on {component.name}");
                    }

                    terminals.Add(terminal);
                    break;
                }
            }
        }

        // Need at least 2 terminals
        if (terminals.Count >= 2)
        {
            return terminals;
        }

        return null;
    }

    public void RemoveComponentTerminals(CircuitComponent3D component)
    {
        if (componentTerminals.ContainsKey(component))
        {
            foreach (var terminal in componentTerminals[component])
            {
                if (terminal != null && terminal.gameObject != null)
                {
                    if (Application.isPlaying)
                        Destroy(terminal.gameObject);
                    else
                        DestroyImmediate(terminal.gameObject);
                }
            }
            componentTerminals.Remove(component);
        }
    }
    
    public List<ComponentTerminal> GetComponentTerminals(CircuitComponent3D component)
    {
        if (componentTerminals.ContainsKey(component))
            return componentTerminals[component];
        return new List<ComponentTerminal>();
    }
    
    public ComponentTerminal GetClosestTerminal(Vector3 worldPosition, float maxDistance = 1.0f)
    {
        ComponentTerminal closest = null;
        float closestDistance = maxDistance;
        
        foreach (var terminals in componentTerminals.Values)
        {
            foreach (var terminal in terminals)
            {
                if (terminal == null) continue;
                
                float distance = Vector3.Distance(terminal.transform.position, worldPosition);
                if (distance < closestDistance)
                {
                    closest = terminal;
                    closestDistance = distance;
                }
            }
        }
        
        return closest;
    }
    
    public bool CanConnectTerminals(ComponentTerminal terminal1, ComponentTerminal terminal2)
    {
        if (terminal1 == null || terminal2 == null) return false;
        if (terminal1 == terminal2) return false;
        if (terminal1.ParentComponent == terminal2.ParentComponent) return false;

        // NEW PHILOSOPHY: Terminals are non-polarized (except battery)
        // Any terminal can connect to any other terminal on a different component
        // Direction is determined by CONNECTION ORDER, not terminal types!

        // All connections are valid as long as they're on different components
        return true;
    }
    
    /// <summary>
    /// Get the first terminal (TerminalA or NegativeTerminal for battery)
    /// </summary>
    public ComponentTerminal GetFirstTerminal(CircuitComponent3D component)
    {
        var terminals = GetComponentTerminals(component);
        if (terminals.Count > 0) return terminals[0];
        return null;
    }

    /// <summary>
    /// Get the second terminal (TerminalB or PositiveTerminal for battery)
    /// </summary>
    public ComponentTerminal GetSecondTerminal(CircuitComponent3D component)
    {
        var terminals = GetComponentTerminals(component);
        if (terminals.Count > 1) return terminals[1];
        return null;
    }

    /// <summary>
    /// DEPRECATED: Use GetFirstTerminal instead. Kept for backward compatibility.
    /// </summary>
    public ComponentTerminal GetInputTerminal(CircuitComponent3D component)
    {
        var terminals = GetComponentTerminals(component);
        return terminals.Find(t => t.isInput) ?? GetFirstTerminal(component);
    }

    /// <summary>
    /// DEPRECATED: Use GetSecondTerminal instead. Kept for backward compatibility.
    /// </summary>
    public ComponentTerminal GetOutputTerminal(CircuitComponent3D component)
    {
        var terminals = GetComponentTerminals(component);
        return terminals.Find(t => !t.isInput) ?? GetSecondTerminal(component);
    }
    
    public void OnComponentRegistered(CircuitComponent3D component)
    {
        SetupComponentTerminals(component);
    }
    
    public void OnComponentUnregistered(CircuitComponent3D component)
    {
        RemoveComponentTerminals(component);
    }
    
    public void UpdateLogicalConnections()
    {
        foreach (var componentPair in componentTerminals)
        {
            var component = componentPair.Key;
            var terminals = componentPair.Value;

            if (component.logicalComponent == null) continue;

            if (terminals.Count >= 2)
            {
                // NEW APPROACH: Use positional assignment instead of isInput
                // terminals[0] = TerminalA or NegativeTerminal → NodeA
                // terminals[1] = TerminalB or PositiveTerminal → NodeB
                var firstTerminal = terminals[0];
                var secondTerminal = terminals[1];

                if (firstTerminal?.electricalNode != null && secondTerminal?.electricalNode != null)
                {
                    component.logicalComponent.NodeA = firstTerminal.electricalNode;
                    component.logicalComponent.NodeB = secondTerminal.electricalNode;
                }
            }
        }

    }

    /// <summary>
    /// Creates a wire connection between two components automatically
    /// Used by scenario system for automatic circuit creation
    /// </summary>
    public void CreateWireBetweenComponents(CircuitComponent3D fromComponent, CircuitComponent3D toComponent)
    {
        if (fromComponent == null || toComponent == null)
        {
            Debug.LogWarning("Cannot create wire between null components");
            return;
        }

        // Get terminals for both components
        var fromOutputTerminal = GetOutputTerminal(fromComponent);
        var toInputTerminal = GetInputTerminal(toComponent);

        if (fromOutputTerminal == null || toInputTerminal == null)
        {
            Debug.LogWarning($"Cannot create wire: missing terminals on {fromComponent.name} or {toComponent.name}");
            return;
        }

        // Create wire GameObject
        GameObject wireObject = new GameObject($"Wire_{fromComponent.name}_to_{toComponent.name}");
        var wire = wireObject.AddComponent<CircuitWire>();

        // Initialize wire with components
        wire.Initialize(fromComponent, toComponent);

        // Connect to components
        wire.startComponent = fromComponent;
        wire.endComponent = toComponent;

        // Add to component wire lists
        if (fromComponent.connectedWires == null)
            fromComponent.connectedWires = new List<CircuitWire>();
        if (toComponent.connectedWires == null)
            toComponent.connectedWires = new List<CircuitWire>();

        fromComponent.connectedWires.Add(wire);
        toComponent.connectedWires.Add(wire);

        // Register with circuit manager
        if (circuitManager != null)
        {
            circuitManager.RegisterWire(wireObject);
        }

    }
}