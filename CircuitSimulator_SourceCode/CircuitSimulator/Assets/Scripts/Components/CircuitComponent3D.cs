using UnityEngine;
using System.Collections.Generic;

// Component types supported by the circuit simulator
public enum ComponentType
{
    Battery,
    Resistor,
    Bulb,
    Switch,
    Wire,
    Junction
}

// 3D representation of circuit components
public class CircuitComponent3D : MonoBehaviour
{
    [Header("Component Properties")]
    public ComponentType ComponentType = ComponentType.Bulb;
    public float voltage = 6f;      // For batteries
    public float resistance = 50f;  // For resistors/bulbs
    
    [Header("Circuit Results")]
    [SerializeField] public float current = 0f;
    [SerializeField] public float voltageDrop = 0f;
    
    // Reference to logical component (set by Circuit3DManager)
    [System.NonSerialized]
    public CircuitComponent logicalComponent;
    
    // Wire connections (used by CircuitWire system)
    [System.NonSerialized]
    public List<CircuitWire> connectedWires = new List<CircuitWire>();
    
    void Start()
    {
        // Set up visual connection terminals for educational clarity
        SetupConnectionTerminals();
        
        // Register with CircuitManager when component starts
        if (CircuitManager.Instance != null)
        {
            CircuitManager.Instance.RegisterComponent(this);
        }
        else
        {
            Debug.LogWarning($"CircuitManager not found! {name} will not be included in circuit solving.");
        }
        
        // Register with label manager for persistent labels
        if (LabelManager.Instance != null)
        {
            LabelManager.Instance.RegisterComponent(this);
        }
        else
        {
            Debug.LogWarning($"LabelManager not found! {name} will not have persistent labels.");
        }
    }
    
    void SetupConnectionTerminals()
    {
        // DISABLED temporarily to avoid compilation errors
        // Will re-enable once core solver is working
        /*
        // Add terminal manager if it doesn't exist
        ComponentTerminalManager terminalManager = GetComponent<ComponentTerminalManager>();
        if (terminalManager == null)
        {
            terminalManager = gameObject.AddComponent<ComponentTerminalManager>();
            terminalManager.SetupTerminals();
            Debug.Log($"✅ Added connection terminals to {name} ({ComponentType})");
        }
        */
    }
    
    void OnDestroy()
    {
        // Unregister when component is destroyed
        if (CircuitManager.Instance != null)
        {
            CircuitManager.Instance.UnregisterComponent(this);
        }
        
        // Unregister from label manager
        if (LabelManager.Instance != null)
        {
            LabelManager.Instance.UnregisterComponent(this);
        }
        
        // Clear references to prevent memory leaks
        logicalComponent = null;
        connectedWires?.Clear();
        connectedWires = null;
    }
    
    // Wire connection methods (used by CircuitWire system)
    public void AddConnectedWire(CircuitWire wire)
    {
        if (wire != null && !connectedWires.Contains(wire))
        {
            connectedWires.Add(wire);
            Debug.Log($"{name}: Added wire connection to {wire.name}");
        }
    }

    public void AddConnectedWire(GameObject wireGO)
    {
        var wire = wireGO?.GetComponent<CircuitWire>();
        if (wire != null)
        {
            AddConnectedWire(wire);
        }
    }

    public void RemoveConnectedWire(CircuitWire wire)
    {
        if (wire != null && connectedWires.Contains(wire))
        {
            connectedWires.Remove(wire);
            Debug.Log($"{name}: Removed wire connection to {wire.name}");
        }
    }

    public void RemoveConnectedWire(GameObject wireGO)
    {
        var wire = wireGO?.GetComponent<CircuitWire>();
        if (wire != null)
        {
            RemoveConnectedWire(wire);
        }
    }

    public List<CircuitWire> GetConnectedWires()
    {
        return new List<CircuitWire>(connectedWires);
    }

    public int GetWireCount()
    {
        return connectedWires.Count;
    }
    
    // Visual feedback methods - now triggers label updates
    public void UpdateVisualFeedback()
    {
        // Update 3D visual representation based on current/voltage
        // This would control things like:
        // - Bulb brightness based on current
        // - Wire glow based on current
        // - Battery charge indicators
        
        // Trigger event-driven label updates (Performance: no more polling)
        LabelManager.Instance?.UpdateLabelsForComponent(this);
        
        // For AR future: could control holographic effects, particle systems, etc.
    }
}
