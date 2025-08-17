using UnityEngine;

/// <summary>
/// Legacy compatibility wrapper for the old Circuit3DManager
/// Forwards calls to the new modular architecture
/// TODO: Remove this after all references are updated
/// </summary>
[System.Obsolete("Use CircuitManager instead. This wrapper will be removed in future versions.")]
public class Circuit3DManager : MonoBehaviour
{
    // Singleton pattern for backward compatibility
    private static Circuit3DManager instance;
    public static Circuit3DManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<Circuit3DManager>();
                if (instance == null)
                {
                    Debug.LogWarning("Circuit3DManager not found. Creating new instance with CircuitManager.");
                    var go = new GameObject("CircuitManager");
                    instance = go.AddComponent<Circuit3DManager>();
                    go.AddComponent<CircuitManager>();
                }
            }
            return instance;
        }
    }
    
    private CircuitManager circuitManager;
    
    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        // Get or create CircuitManager
        circuitManager = GetComponent<CircuitManager>();
        if (circuitManager == null)
        {
            circuitManager = gameObject.AddComponent<CircuitManager>();
        }
    }
    
    #region Legacy API Forwarding
    
    public void RegisterComponent(CircuitComponent3D component)
    {
        Debug.LogWarning("[DEPRECATED] Use CircuitManager.Instance.RegisterComponent instead");
        circuitManager?.RegisterComponent(component);
    }
    
    public void UnregisterComponent(CircuitComponent3D component)
    {
        Debug.LogWarning("[DEPRECATED] Use CircuitManager.Instance.UnregisterComponent instead");
        circuitManager?.UnregisterComponent(component);
    }
    
    public void RegisterWire(GameObject wire)
    {
        Debug.LogWarning("[DEPRECATED] Use CircuitManager.Instance.RegisterWire instead");
        circuitManager?.RegisterWire(wire);
    }
    
    public void UnregisterWire(GameObject wire)
    {
        Debug.LogWarning("[DEPRECATED] Use CircuitManager.Instance.UnregisterWire instead");
        circuitManager?.UnregisterWire(wire);
    }
    
    public void SolveCircuit()
    {
        Debug.LogWarning("[DEPRECATED] Use CircuitManager.Instance.SolveCircuit instead");
        circuitManager?.SolveCircuit();
    }
    
    public void SolveCircuitManually()
    {
        Debug.LogWarning("[DEPRECATED] Use CircuitManager.Instance.SolveCircuitManually instead");
        circuitManager?.SolveCircuitManually();
    }
    
    public void SaveDebugReport()
    {
        Debug.LogWarning("[DEPRECATED] Use CircuitManager.Instance.SaveDebugReport instead");
        circuitManager?.SaveDebugReport();
    }
    
    public void DebugComponentRegistration()
    {
        Debug.LogWarning("[DEPRECATED] Use CircuitManager.Instance.DebugComponentRegistration instead");
        circuitManager?.DebugComponentRegistration();
    }
    
    public void ValidateAndTestCircuit()
    {
        Debug.LogWarning("[DEPRECATED] Use CircuitManager.Instance.ValidateAndTestCircuit instead");
        circuitManager?.ValidateAndTestCircuit();
    }
    
    #endregion
}