using UnityEngine;

// Component types supported by the circuit simulator
public enum ComponentType
{
    Battery,
    Resistor,
    Bulb,
    Switch,
    Wire
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
    private List<GameObject> connectedWires = new List<GameObject>();
    
    void Start()
    {
        // Register with Circuit3DManager when component starts
        if (Circuit3DManager.Instance != null)
        {
            Circuit3DManager.Instance.RegisterComponent(this);
        }
        else
        {
            Debug.LogWarning($"Circuit3DManager not found! {name} will not be included in circuit solving.");
        }
    }
    
    void OnDestroy()
    {
        // Unregister when component is destroyed
        if (Circuit3DManager.Instance != null)
        {
            Circuit3DManager.Instance.UnregisterComponent(this);
        }
    }
    
    // Wire connection methods (used by CircuitWire system)
    public void AddConnectedWire(GameObject wire)
    {
        if (wire != null && !connectedWires.Contains(wire))
        {
            connectedWires.Add(wire);
            Debug.Log($"{name}: Added wire connection to {wire.name}");
        }
    }
    
    public void RemoveConnectedWire(GameObject wire)
    {
        if (wire != null && connectedWires.Contains(wire))
        {
            connectedWires.Remove(wire);
            Debug.Log($"{name}: Removed wire connection to {wire.name}");
        }
    }
    
    public List<GameObject> GetConnectedWires()
    {
        return new List<GameObject>(connectedWires);
    }
    
    public int GetWireCount()
    {
        return connectedWires.Count;
    }
    
    // Visual feedback methods (to be implemented in UI)
    public void UpdateVisualFeedback()
    {
        // Update 3D visual representation based on current/voltage
        // This would control things like:
        // - Bulb brightness based on current
        // - Wire glow based on current
        // - Battery charge indicators
        // - Voltage/current displays
        
        // For AR future: could control holographic effects, particle systems, etc.
    }
}
