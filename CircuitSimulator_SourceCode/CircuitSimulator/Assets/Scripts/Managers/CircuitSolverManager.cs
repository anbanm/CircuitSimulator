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
    
    public void Initialize()
    {
        // Get manager references
        circuitManager = CircuitManager.Instance;
        nodeManager = GetComponent<CircuitNodeManager>();
        debugManager = GetComponent<CircuitDebugManager>();
        
        // Initialize solver
        circuitSolver = new CircuitSolver();
        CircuitSolver.EnableDebugLog = debugSolver;
        
        Debug.Log("CircuitSolverManager initialized");
    }
    
    public void Update()
    {
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
        
        // Use node manager to create spatial node system
        var spatialNodes = nodeManager?.CreateSpatialNodeSystem();
        if (spatialNodes == null)
        {
            Debug.LogError("Failed to create spatial node system");
            return logicalComponents;
        }
        
        // Create logical components using spatial nodes
        foreach (var comp3D in circuitManager.Components)
        {
            if (comp3D == null) continue;
            
            // Get component terminal positions
            Vector3 componentPos = comp3D.transform.position;
            Vector3 inputPos = componentPos + Vector3.left * 0.3f;
            Vector3 outputPos = componentPos + Vector3.right * 0.3f;
            
            // Find the spatial nodes for these positions
            var nodeA = nodeManager?.GetSpatialNode(spatialNodes, inputPos, 0.5f);
            var nodeB = nodeManager?.GetSpatialNode(spatialNodes, outputPos, 0.5f);
            
            if (nodeA == null || nodeB == null)
            {
                Debug.LogError($"Could not find spatial nodes for component {comp3D.name}");
                continue;
            }
            
            // Create appropriate logical component
            CircuitComponent logicalComp = CreateLogicalComponent(comp3D, nodeA, nodeB);
            if (logicalComp != null)
            {
                logicalComponents.Add(logicalComp);
                comp3D.logicalComponent = logicalComp;
                
                if (debugSolver)
                {
                    debugManager?.LogToFile($"Created {logicalComp.GetType().Name}: {logicalComp.Id}");
                }
            }
        }
        
        if (debugSolver)
        {
            debugManager?.LogToFile($"Final circuit: {logicalComponents.Count} components");
            var uniqueNodes = spatialNodes.Values.Distinct().ToList();
            debugManager?.LogToFile($"Unique nodes: {uniqueNodes.Count()}");
            foreach (var node in uniqueNodes)
            {
                debugManager?.LogToFile($"  Node {node.Id}: {node.ConnectedComponents.Count} components");
            }
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