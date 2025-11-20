using UnityEngine;

/// <summary>
/// Handles circuit control operations and manager coordination
/// Manages solving, validation, testing, and debugging operations
/// </summary>
public class CircuitControlManager : MonoBehaviour
{
    private CircuitManager circuitManager;

    void Start()
    {
        Initialize();
    }

    public void Initialize()
    {
        // Get the main circuit manager
        circuitManager = CircuitManager.Instance;
        if (circuitManager == null && ComponentRegistry.Instance != null)
        {
            circuitManager = ComponentRegistry.Instance.GetManager<CircuitManager>();
        }

        Debug.Log("CircuitControlManager initialized");

        // TEST: Check if circuitManager was found
        if (circuitManager != null)
        {
            Debug.Log("✅ CircuitControlManager has valid CircuitManager reference");
        }
        else
        {
            Debug.LogError("❌ CircuitControlManager could not find CircuitManager!");
        }
    }
    
    void Update()
    {
        // TEST: Manual solver trigger with Space key
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("🚀 SPACE KEY PRESSED - Testing manual solve");
            SolveCircuit();
        }
        
        // Removed excessive per-frame debug logging that was causing crashes
    }
    
    #region Circuit Operations
    
    public void SolveCircuit()
    {
        // Refresh reference if null (defensive programming)
        if (circuitManager == null)
        {
            RefreshManagerReference();
        }

        if (circuitManager != null)
        {
            Debug.Log("Manual solve triggered from control manager");
            circuitManager.SolveCircuit();
        }
        else
        {
            Debug.LogWarning("No CircuitManager found!");
        }
    }
    
    public void ValidateCircuit()
    {
        if (circuitManager != null)
        {
            Debug.Log("Manual validation triggered from control manager");
            circuitManager.ValidateAndTestCircuit();
        }
        else
        {
            Debug.LogWarning("No CircuitManager found!");
        }
    }
    
    public void TestCircuit()
    {
        if (circuitManager != null)
        {
            Debug.Log("Test circuit triggered from control manager");
            circuitManager.ValidateAndTestCircuit();
        }
        else
        {
            Debug.LogWarning("No CircuitManager found!");
        }
    }
    
    public void SaveReport()
    {
        if (circuitManager != null)
        {
            Debug.Log("Save debug report triggered from control manager");
            circuitManager.SaveDebugReport();
        }
        else
        {
            Debug.LogWarning("No CircuitManager found!");
        }
    }
    
    public void DebugRegistration()
    {
        if (circuitManager != null)
        {
            Debug.Log("Debug registration triggered from control manager");
            circuitManager.DebugComponentRegistration();
        }
        else
        {
            Debug.LogWarning("No CircuitManager found!");
        }
    }
    
    #endregion
    
    // Legacy support removed - all references should use CircuitManager
    
    #region Public API
    
    public bool HasCircuitManager()
    {
        return circuitManager != null;
    }
    
    public CircuitManager GetCircuitManager()
    {
        return circuitManager;
    }
    
    public void RefreshManagerReference()
    {
        circuitManager = CircuitManager.Instance;
        if (circuitManager == null)
        {
            circuitManager = ComponentRegistry.Instance.GetManager<CircuitManager>();
        }
    }
    
    #endregion
}