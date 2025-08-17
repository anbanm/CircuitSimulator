using UnityEngine;

/// <summary>
/// Handles circuit control operations and manager coordination
/// Manages solving, validation, testing, and debugging operations
/// </summary>
public class CircuitControlManager : MonoBehaviour
{
    private CircuitManager circuitManager;
    
    public void Initialize()
    {
        // Get the main circuit manager
        circuitManager = CircuitManager.Instance;
        if (circuitManager == null)
        {
            circuitManager = FindFirstObjectByType<CircuitManager>();
        }
        
        Debug.Log("CircuitControlManager initialized");
    }
    
    #region Circuit Operations
    
    public void SolveCircuit()
    {
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
            circuitManager = FindFirstObjectByType<CircuitManager>();
        }
    }
    
    #endregion
}