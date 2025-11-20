using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using CircuitSimulator.Services;

/// <summary>
/// Handles all circuit solving logic and solver integration
/// Implements ICircuitSolver interface and integrates with ServiceLocator
/// </summary>
public class CircuitSolverManager : MonoBehaviour, ICircuitSolver
{
    [Header("Solver Settings")]
    public bool manualSolveMode = false;
    public bool debugSolver = true;
    public float solveDelay = 0.5f;

    // ICircuitSolver Events
    public System.Action OnCircuitSolved { get; set; }
    public System.Action<string> OnSolveError { get; set; }
    public System.Action OnSolveStarted { get; set; }

    // ICircuitSolver Properties
    public bool IsAutoSolveEnabled => !manualSolveMode;
    public float SolveInterval => solveDelay;
    public bool IsDebugEnabled { get; set; }
    public bool IsCircuitSolved { get; private set; }
    public float LastSolveTime => lastSolveTime;
    public string LastSolveResult { get; private set; } = "";

    private CircuitSolver circuitSolver;
    private float lastSolveTime = 0f;
    private bool circuitNeedsSolving = false;
    
    private CircuitManager circuitManager;
    private CircuitNodeManager nodeManager;
    private CircuitDebugManager debugManager;
    private ComponentTerminalManager terminalManager;

    // Visual flow graph for animation (separate from electrical solver)
    private VisualFlowGraph visualFlowGraph;
    
    public void Initialize()
    {
        // Register with ServiceLocator
        ServiceLocator.Instance.Register<ICircuitSolver>(this);

        // Initialize interface properties
        IsDebugEnabled = debugSolver;

        // Initialize visual flow graph (separate from electrical solver)
        visualFlowGraph = new VisualFlowGraph();

        // Get manager references with null checks
        circuitManager = CircuitManager.Instance;
        if (circuitManager == null)
        {
            Debug.LogError("CircuitManager.Instance is null in CircuitSolverManager.Initialize()");
            enabled = false;
            return;
        }
        
        nodeManager = GetComponent<CircuitNodeManager>();
        if (nodeManager == null)
        {
            Debug.LogWarning("CircuitNodeManager not found on same GameObject");
        }
        
        debugManager = GetComponent<CircuitDebugManager>();
        if (debugManager == null)
        {
            Debug.LogWarning("CircuitDebugManager not found on same GameObject");
        }
        
        terminalManager = GetComponent<ComponentTerminalManager>();
        if (terminalManager == null)
        {
            Debug.LogWarning("ComponentTerminalManager not found on same GameObject");
        }
        
        // Initialize solver
        circuitSolver = new CircuitSolver();
        CircuitSolver.EnableDebugLog = debugSolver;
        
        Debug.Log("CircuitSolverManager initialized");
    }
    
    public void Update()
    {
        // Safety check for null circuit manager
        if (circuitManager == null) return;
        
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
    
    public bool SolveCircuit()
    {
        if (circuitManager == null || circuitManager.Components.Count == 0)
        {
            Debug.LogWarning("No components to solve");
            LastSolveResult = "No components to solve";
            ClearAllComponentValues();  // Clear old values when no circuit
            OnSolveError?.Invoke(LastSolveResult);
            return false;
        }

        OnSolveStarted?.Invoke();
        debugManager?.LogToFile($"=== SOLVING CIRCUIT (Components: {circuitManager.ComponentCount}, Wires: {circuitManager.WireCount}) ===");

        try
        {
            // Build logical circuit from 3D components
            var logicalComponents = BuildLogicalCircuit();

            if (logicalComponents.Count == 0)
            {
                Debug.LogWarning("No valid circuit components found");
                LastSolveResult = "No valid circuit components found";
                ClearAllComponentValues();  // Clear old values when circuit is invalid
                OnSolveError?.Invoke(LastSolveResult);
                return false;
            }

            // Solve the circuit components directly
            circuitSolver.Solve(logicalComponents.ToList());

            // Update 3D components with solved values
            UpdateComponentsFromSolver(logicalComponents.ToList());

            // Mark as solved
            circuitNeedsSolving = false;
            lastSolveTime = Time.time;
            IsCircuitSolved = true;

            LastSolveResult = $"Circuit solved successfully: {logicalComponents.Count} components";
            debugManager?.LogToFile(LastSolveResult);
            Debug.Log($"✅ Circuit solved: {logicalComponents.Count} components");

            // Assign wire flow directions based on connection path from Battery+
            AssignWireFlowDirections();

            // Fire success event
            OnCircuitSolved?.Invoke();

            // Fire global circuit solved event for all subscribers (e.g., wires)
            var eventManager = FindFirstObjectByType<CircuitEventManager>();
            eventManager?.OnCircuitSolved();

            return true;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Circuit solving failed: {ex.Message}");
            Debug.LogError($"Stack trace: {ex.StackTrace}"); // Show full stack trace
            LastSolveResult = $"Circuit solving failed: {ex.Message}";
            debugManager?.LogToFile($"ERROR: {ex.Message}\nStack: {ex.StackTrace}");
            ClearAllComponentValues();  // Clear old values when solve fails
            OnSolveError?.Invoke(LastSolveResult);
            IsCircuitSolved = false;
            return false;
        }
    }

    /// <summary>
    /// Clear all current and voltage values when circuit is invalid or disconnected
    /// </summary>
    void ClearAllComponentValues()
    {
        if (circuitManager == null) return;

        Debug.Log("🧹 Clearing all component values (circuit invalid or disconnected)");

        // Clear all component values
        foreach (var component in circuitManager.Components)
        {
            if (component != null)
            {
                component.current = 0f;
                component.voltageDrop = 0f;
            }
        }

        // Clear all wire values
        foreach (var wireObj in circuitManager.Wires)
        {
            var wire = wireObj.GetComponent<CircuitWire>();
            if (wire != null)
            {
                wire.current = 0f;
                wire.voltageDrop = 0f;
            }
        }
    }
    
    public IReadOnlyList<CircuitComponent> BuildLogicalCircuit()
    {
        var logicalComponents = new List<CircuitComponent>();

        if (debugSolver)
        {
            debugManager?.LogToFile("=== BUILDING LOGICAL CIRCUIT ===");
            debugManager?.LogToFile($"Components: {circuitManager.Components.Count}, Wires: {circuitManager.Wires.Count}");
        }

        // CRITICAL FIX: Clear all CircuitNode.ConnectedComponents lists before rebuilding
        // This prevents duplicate component references when logical components are recreated
        ClearAllNodeComponentLists();

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

            // NEW APPROACH: Use positional indexing instead of isInput flag
            // terminals[0] = TerminalA or NegativeTerminal → NodeA
            // terminals[1] = TerminalB or PositiveTerminal → NodeB
            var firstTerminal = terminals[0];
            var secondTerminal = terminals[1];

            if (firstTerminal?.electricalNode == null || secondTerminal?.electricalNode == null)
            {
                Debug.LogWarning($"Component {comp3D.name} terminals do not have electrical nodes");
                continue;
            }

            // Create appropriate logical component
            CircuitComponent logicalComp = CreateLogicalComponent(comp3D, firstTerminal.electricalNode, secondTerminal.electricalNode);
            if (logicalComp != null)
            {
                logicalComponents.Add(logicalComp);
                comp3D.logicalComponent = logicalComp;

                if (debugSolver)
                {
                    debugManager?.LogToFile($"Created {logicalComp.GetType().Name}: {logicalComp.Id}");
                    debugManager?.LogToFile($"  → First Terminal: {firstTerminal.name}, Node: {firstTerminal.electricalNode.Id}");
                    debugManager?.LogToFile($"  → Second Terminal: {secondTerminal.name}, Node: {secondTerminal.electricalNode.Id}");
                }

                Debug.Log($"📦 Component {comp3D.name}: NodeA={firstTerminal.electricalNode.Id} (HashCode={firstTerminal.electricalNode.GetHashCode()}), NodeB={secondTerminal.electricalNode.Id} (HashCode={secondTerminal.electricalNode.GetHashCode()})");
            }
        }
        
        if (debugSolver)
        {
            debugManager?.LogToFile($"Final circuit: {logicalComponents.Count} components");
            debugManager?.LogToFile("Terminal-based electrical connections established");
        }
        
        return logicalComponents.AsReadOnly();
    }

    /// <summary>
    /// Clear all CircuitNode.ConnectedComponents lists before rebuilding the logical circuit.
    /// This prevents duplicate component references when logical components are recreated.
    /// </summary>
    private void ClearAllNodeComponentLists()
    {
        // Iterate through all components and clear their terminal nodes
        foreach (var comp3D in circuitManager.Components)
        {
            if (comp3D == null) continue;

            var terminals = terminalManager?.GetComponentTerminals(comp3D);
            if (terminals == null) continue;

            foreach (var terminal in terminals)
            {
                if (terminal?.electricalNode != null)
                {
                    int oldCount = terminal.electricalNode.ConnectedComponents.Count;
                    terminal.electricalNode.ConnectedComponents.Clear();

                    if (debugSolver && oldCount > 0)
                    {
                        debugManager?.LogToFile($"Cleared {oldCount} components from node {terminal.electricalNode.Id}");
                    }
                }
            }
        }

        if (debugSolver)
        {
            debugManager?.LogToFile("All CircuitNode.ConnectedComponents lists cleared");
        }
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
    
    public void UpdateComponentsFromSolver(List<CircuitComponent> solvedComponents)
    {
        Debug.Log($"=== UPDATING {solvedComponents.Count} COMPONENTS WITH SOLVED VALUES ===");

        foreach (var logicalComp in solvedComponents)
        {
            // Find corresponding 3D component
            var comp3D = circuitManager.Components.Find(c => c.logicalComponent == logicalComp);
            if (comp3D != null)
            {
                // Update 3D component with solved values
                comp3D.current = logicalComp.Current;
                comp3D.voltageDrop = logicalComp.VoltageDrop;

                Debug.Log($"✅ Updated {comp3D.name}: Current={comp3D.current:F4}A, VoltageDrop={comp3D.voltageDrop:F4}V");

                // Update visual feedback
                comp3D.UpdateVisualFeedback();

                if (debugSolver)
                {
                    debugManager?.LogToFile($"Updated {comp3D.name}: I={comp3D.current:F3}A, V={comp3D.voltageDrop:F2}V");
                }
            }
            else
            {
                Debug.LogWarning($"⚠️ Could not find 3D component for logical component {logicalComp.Id}");
            }
        }
    }

    /// <summary>
    /// Assigns wire flow directions using a clean component-to-component visual graph
    /// COMPLETELY SEPARATE from the electrical solver's merged node graph
    /// Uses BFS from Battery+ to traverse the topological wire connections
    /// </summary>
    void AssignWireFlowDirections()
    {
        Debug.Log("=== ASSIGNING WIRE FLOW DIRECTIONS (Visual Graph) ===");

        // Step 1: Clear all existing flags
        foreach (var wireObj in circuitManager.Wires)
        {
            var wire = wireObj.GetComponent<CircuitWire>();
            if (wire != null)
            {
                if (wire.startEndpoint != null) wire.startEndpoint.isStart = false;
                if (wire.endEndpoint != null) wire.endEndpoint.isStart = false;
            }
        }

        // Step 2: Build the visual flow graph from current wire connections
        visualFlowGraph.BuildFromScene(circuitManager.Wires);

        // Step 3: Find the battery
        CircuitComponent3D battery = circuitManager.Components.Find(c => c.ComponentType == ComponentType.Battery);
        if (battery == null)
        {
            Debug.LogWarning("⚠️ No battery found, cannot assign wire flow directions");
            return;
        }

        // Step 4: Use the visual graph to assign flow directions via BFS
        visualFlowGraph.AssignFlowDirectionsFromBattery(battery, terminalManager);
    }

    #region ICircuitSolver Implementation

    // Configuration Methods
    public void EnableAutoSolve(bool enabled)
    {
        manualSolveMode = !enabled;
        Debug.Log($"Auto-solve {(enabled ? "enabled" : "disabled")}");
    }

    public void SetSolveInterval(float interval)
    {
        solveDelay = Mathf.Max(0.1f, interval); // Minimum 0.1 seconds
        Debug.Log($"Solve interval set to {solveDelay:F1} seconds");
    }

    // Direct Interface Methods (delegating to existing methods)
    public void MarkCircuitChanged()
    {
        MarkForSolving();
    }

    // Interface method implementation - parameterless version
    public void UpdateComponentsFromSolver()
    {
        // Build the logical circuit first to get current component state
        var logicalComponents = BuildLogicalCircuit();

        // Call the existing implementation with the logical components
        UpdateComponentsFromSolver(logicalComponents.ToList());
    }

    #endregion
}