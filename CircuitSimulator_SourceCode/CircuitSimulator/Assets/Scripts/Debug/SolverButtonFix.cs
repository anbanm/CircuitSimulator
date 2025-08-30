using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Quick fix to manually connect the solve button to the circuit solver
/// This addresses the issue where button onClick events aren't being properly connected
/// </summary>
public class SolverButtonFix : MonoBehaviour
{
    void Start()
    {
        // Find the solve button and connect it manually
        FixSolveButton();
    }
    
    void Update()
    {
        // Also allow Space key as manual trigger for testing
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("Space key pressed - triggering manual solve");
            TriggerSolve();
        }
        
        // Also allow Ctrl+S for manual solve (as mentioned in CircuitSolverManager)
        if (Input.GetKeyDown(KeyCode.S) && (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)))
        {
            Debug.Log("Ctrl+S pressed - triggering manual solve");
            TriggerSolve();
        }
    }
    
    void FixSolveButton()
    {
        // Find the solve button by name
        Button solveButton = GameObject.Find("Button_Solve_Calculate circuit")?.GetComponent<Button>();
        if (solveButton != null)
        {
            // Clear any existing listeners
            solveButton.onClick.RemoveAllListeners();
            
            // Add our manual solve trigger
            solveButton.onClick.AddListener(() => {
                Debug.Log("Solve button clicked via SolverButtonFix");
                TriggerSolve();
            });
            
            Debug.Log("✅ Solve button manually connected via SolverButtonFix");
        }
        else
        {
            Debug.LogWarning("Solve button not found for manual fix");
        }
    }
    
    void TriggerSolve()
    {
        // Get the CircuitManager and trigger solve
        CircuitManager circuitManager = CircuitManager.Instance;
        if (circuitManager == null)
        {
            circuitManager = FindObjectOfType<CircuitManager>();
        }
        
        if (circuitManager != null)
        {
            Debug.Log("🔧 Manual solve triggered via SolverButtonFix");
            circuitManager.SolveCircuit();
        }
        else
        {
            Debug.LogError("❌ CircuitManager not found!");
        }
    }
}