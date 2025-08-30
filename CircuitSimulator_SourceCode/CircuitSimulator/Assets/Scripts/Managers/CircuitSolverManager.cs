using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Handles all circuit solving logic and solver integration
/// Manages when and how circuits are solved
/// </summary>
public class CircuitSolverManager : MonoBehaviour
{
    [Header("Solver Settings")]
    public bool manualSolveMode = false;
    public bool debugSolver = true;
    
    private CircuitSolver circuitSolver;
    private float lastSolveTime = 0f;
    private bool circuitNeedsSolving = false;
    
    private CircuitManager circuitManager;
    private CircuitNodeManager nodeManager;
    private CircuitDebugManager debugManager;
    private ComponentTerminalManager terminalManager;
    
    public void Initialize()
    {
        // Get manager references
        circuitManager = CircuitManager.Instance;
        nodeManager = GetComponent<CircuitNodeManager>();
        debugManager = GetComponent<CircuitDebugManager>();
        terminalManager = GetComponent<ComponentTerminalManager>();
        
        // Initialize solver
        circuitSolver = new CircuitSolver();
        CircuitSolver.EnableDebugLog = debugSolver;
        
        Debug.Log("CircuitSolverManager initialized");
    }
    
    public void Update()
    {
        // DEBUGGING: Force solve if we have components but haven't solved yet
        if (circuitManager.ComponentCount > 0 && !circuitNeedsSolving && lastSolveTime == 0f)
        {
            Debug.Log("🔧 FORCED SOLVE: Circuit has components but circuitNeedsSolving=false, forcing solve...");
            circuitNeedsSolving = true;
        }
        
        // Handle automatic solving
        if (circuitManager.autoSolve && !manualSolveMode && circuitNeedsSolving)
        {
            if (Time.time - lastSolveTime >= circuitManager.solveInterval)
            {
                SolveCircuit();
            }
        }
        
        // Handle manual solve shortcut
        if (Input.GetKeyDown(KeyCode.T) && (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)))
        {
            Debug.Log("Test circuit triggered (Ctrl+T)");
            debugManager?.TestCircuitComponents();
            return;
        }
        
        // Debug key to toggle debug logging
        if (Input.GetKeyDown(KeyCode.D) && (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)))
        {
            debugSolver = !debugSolver;
            CircuitSolver.EnableDebugLog = debugSolver;
            Debug.Log($"Debug solver: {(debugSolver ? "ON" : "OFF")}");
        }
    }
    
    public void MarkForSolving()
    {
        circuitNeedsSolving = true;
        
        if (circuitManager.eventBasedSolving && !manualSolveMode)
        {
            // Solve immediately when circuit changes
            SolveCircuit();
        }
        else
        {
            Debug.Log("Circuit marked for re-solving");
        }
    }
    
    public void StopAutoSolve()
    {
        // Stop any pending solves by clearing the flag
        circuitNeedsSolving = false;
        Debug.Log("Auto-solve stopped");
    }
    
    public void ClearSolverCache()
    {
        // Clear any cached solver data
        circuitNeedsSolving = false;
        lastSolveTime = 0f;
        Debug.Log("Solver cache cleared");
    }
    
    [ContextMenu("Solve Circuit Manually")]
    public void SolveCircuitManually()
    {
        string header = "=== MANUAL SOLVE TRIGGERED ===";
        debugManager?.LogToFile(header);
        Debug.Log(header);
        
        SolveCircuit();
    }
    
    public void SolveCircuit()
    {
        if (circuitManager == null || circuitManager.Components.Count == 0)
        {
            Debug.LogWarning("No components to solve");
            return;
        }
        
        debugManager?.LogToFile($"=== SOLVING CIRCUIT (Components: {circuitManager.ComponentCount}, Wires: {circuitManager.WireCount}) ===");
        
        try
        {
            // Build logical circuit from 3D components
            var logicalComponents = BuildLogicalCircuit();
            
            if (logicalComponents.Count == 0)
            {
                Debug.LogWarning("No valid circuit components found");
                return;
            }
            
            // Solve the circuit components directly
            circuitSolver.Solve(logicalComponents);
            
            // Update 3D components with solved values
            UpdateComponentsFromSolver(logicalComponents);
            
            // Mark as solved
            circuitNeedsSolving = false;
            lastSolveTime = Time.time;
            
            debugManager?.LogToFile($"Circuit solved successfully: {logicalComponents.Count} components");
            Debug.Log($"✅ Circuit solved: {logicalComponents.Count} components");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Circuit solving failed: {ex.Message}");
            debugManager?.LogToFile($"ERROR: {ex.Message}");
        }
    }
    
    private List<CircuitComponent> BuildLogicalCircuit()
    {
        var logicalComponents = new List<CircuitComponent>();
        
        if (debugSolver)
        {
            debugManager?.LogToFile("=== BUILDING LOGICAL CIRCUIT ===");
            debugManager?.LogToFile($"Components: {circuitManager.Components.Count}, Wires: {circuitManager.Wires.Count}");
        }
        
        // Use terminal manager to update logical connections
        terminalManager?.UpdateLogicalConnections();
        
        // Create logical components using terminal-based nodes
        foreach (var comp3D in circuitManager.Components)
        {
            if (comp3D == null) continue;
            
            // Get component terminals
            var terminals = terminalManager?.GetComponentTerminals(comp3D);
            if (terminals == null || terminals.Count < 2)
            {
                Debug.LogWarning($"Component {comp3D.name} does not have proper terminals");
                continue;
            }
            
            var inputTerminal = terminals.Find(t => t.isInput);
            var outputTerminal = terminals.Find(t => !t.isInput);
            
            if (inputTerminal?.electricalNode == null || outputTerminal?.electricalNode == null)
            {
                Debug.LogWarning($"Component {comp3D.name} terminals do not have electrical nodes");
                continue;
            }
            
            // Create appropriate logical component
            CircuitComponent logicalComp = CreateLogicalComponent(comp3D, inputTerminal.electricalNode, outputTerminal.electricalNode);
            if (logicalComp != null)
            {
                logicalComponents.Add(logicalComp);
                comp3D.logicalComponent = logicalComp;
                
                if (debugSolver)
                {
                    debugManager?.LogToFile($"Created {logicalComp.GetType().Name}: {logicalComp.Id} with terminals");
                }
            }
        }
        
        if (debugSolver)
        {
            debugManager?.LogToFile($"Final circuit: {logicalComponents.Count} components");
            debugManager?.LogToFile("Terminal-based electrical connections established");
        }
        
        return logicalComponents;
    }
    
    private CircuitComponent CreateLogicalComponent(CircuitComponent3D comp3D, CircuitNode nodeA, CircuitNode nodeB)
    {
        string componentId = $"{comp3D.ComponentType}_{comp3D.GetInstanceID()}";
        
        switch (comp3D.ComponentType)
        {
            case ComponentType.Battery:
                return new Battery(componentId, nodeA, nodeB, comp3D.voltage);
                
            case ComponentType.Resistor:
                return new Resistor(componentId, nodeA, nodeB, comp3D.resistance);
                
            case ComponentType.Bulb:
                return new Bulb(componentId, nodeA, nodeB, comp3D.resistance);
                
            case ComponentType.Switch:
                // TODO: Implement proper switch logic (open/closed state)
                return new Resistor(componentId, nodeA, nodeB, comp3D.resistance);
                
            default:
                Debug.LogWarning($"Unknown component type: {comp3D.ComponentType}");
                return null;
        }
    }
    
    private void UpdateComponentsFromSolver(List<CircuitComponent> solvedComponents)
    {
        foreach (var logicalComp in solvedComponents)
        {
            // Find corresponding 3D component
            var comp3D = circuitManager.Components.Find(c => c.logicalComponent == logicalComp);
            if (comp3D != null)
            {
                // Update 3D component with solved values
                comp3D.current = logicalComp.Current;
                comp3D.voltageDrop = logicalComp.VoltageDrop;
                
                // Update visual feedback
                comp3D.UpdateVisualFeedback();
                
                if (debugSolver)
                {
                    debugManager?.LogToFile($"Updated {comp3D.name}: I={comp3D.current:F3}A, V={comp3D.voltageDrop:F2}V");
                }
            }
        }
    }
}