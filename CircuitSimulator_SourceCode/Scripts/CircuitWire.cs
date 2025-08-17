using UnityEngine;

// Represents a wire connection between two circuit components
public class CircuitWire : MonoBehaviour
{
    [Header("Wire Connection")]
    public CircuitComponent3D Component1;
    public CircuitComponent3D Component2;
    
    [Header("Wire Properties")]
    public float current = 0f;
    public float resistance = 0f; // Wires have 0 resistance
    
    void Start()
    {
        // Register with Circuit3DManager when wire starts
        if (Circuit3DManager.Instance != null)
        {
            Circuit3DManager.Instance.RegisterWire(gameObject);
        }
        else
        {
            Debug.LogWarning($"Circuit3DManager not found! Wire {name} will not be included in circuit solving.");
        }
        
        // Validate wire connections
        ValidateConnections();
    }
    
    void OnDestroy()
    {
        // Unregister when wire is destroyed
        if (Circuit3DManager.Instance != null)
        {
            Circuit3DManager.Instance.UnregisterWire(gameObject);
        }
    }
    
    void ValidateConnections()
    {
        if (Component1 == null || Component2 == null)
        {
            Debug.LogWarning($"Wire {name} has missing component connections!");
        }
        
        if (Component1 == Component2)
        {
            Debug.LogWarning($"Wire {name} connects component to itself!");
        }
    }
    
    // Update visual representation based on current
    public void UpdateWireVisual()
    {
        // Update wire appearance based on current flow
        // This could control:
        // - Wire color/glow intensity
        // - Animation effects for current flow
        // - Current direction indicators
    }
    
    // Helper method to create wire connections programmatically
    public static GameObject CreateWire(CircuitComponent3D comp1, CircuitComponent3D comp2, string wireName = null)
    {
        if (wireName == null)
            wireName = $"Wire_{comp1.name}_to_{comp2.name}";
            
        GameObject wireObject = new GameObject(wireName);
        CircuitWire wire = wireObject.AddComponent<CircuitWire>();
        
        wire.Component1 = comp1;
        wire.Component2 = comp2;
        
        return wireObject;
    }
}
