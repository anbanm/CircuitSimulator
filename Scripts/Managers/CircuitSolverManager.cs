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
    private JunctionTopologyManager topologyManager;

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

        topologyManager = FindFirstObjectByType<JunctionTopologyManager>();
        if (topologyManager == null)
        {
            Debug.LogWarning("JunctionTopologyManager not found in scene");
        }

        // Initialize solver
        circuitSolver = new CircuitSolver();
        CircuitSolver.EnableDebugLog = debugSolver;
        
    }
    
    public void Update()
    {
        // Safety check for null circuit manager
        if (circuitManager == null) return;
        
        // DEBUGGING: Force solve if we have components but haven't solved yet
        if (circuitManager.ComponentCount > 0 && !circuitNeedsSolving && lastSolveTime == 0f)
        {
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
            debugManager?.TestCircuitComponents();
            return;
        }
        
        // Debug key to toggle debug logging
        if (Input.GetKeyDown(KeyCode.D) && (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)))
        {
            debugSolver = !debugSolver;
            CircuitSolver.EnableDebugLog = debugSolver;
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
        }
    }
    
    public void StopAutoSolve()
    {
        // Stop any pending solves by clearing the flag
        circuitNeedsSolving = false;
    }
    
    public void ClearSolverCache()
    {
        // Clear any cached solver data
        circuitNeedsSolving = false;
        lastSolveTime = 0f;
    }
    
    [ContextMenu("Solve Circuit Manually")]
    public void SolveCircuitManually()
    {
        string header = "=== MANUAL SOLVE TRIGGERED ===";
        debugManager?.LogToFile(header);
        
        SolveCircuit();
    }
    
    public bool SolveCircuit()
    {
        if (debugSolver) Debug.Log($"[SOLVER] SolveCircuit() called. CircuitManager: {(circuitManager != null ? "Found" : "NULL")}, Components: {(circuitManager != null ? circuitManager.Components.Count : 0)}");

        if (circuitManager == null || circuitManager.Components.Count == 0)
        {
            // Only log warning once per empty state, not every frame
            LastSolveResult = "No components to solve";
            ClearAllComponentValues();  // Clear old values when no circuit
            OnSolveError?.Invoke(LastSolveResult);
            return false;
        }

        OnSolveStarted?.Invoke();
        if (debugSolver) Debug.Log($"[SOLVER] Building logical circuit with {circuitManager.ComponentCount} components and {circuitManager.WireCount} wires");
        debugManager?.LogToFile($"=== SOLVING CIRCUIT (Components: {circuitManager.ComponentCount}, Wires: {circuitManager.WireCount}) ===");

        try
        {
            // Build logical circuit from 3D components
            var logicalComponents = BuildLogicalCircuit();
            if (debugSolver) Debug.Log($"[SOLVER] BuildLogicalCircuit() returned {logicalComponents.Count} logical components");

            if (logicalComponents.Count == 0)
            {
                // Only warn once - not every solve attempt
                LastSolveResult = "No valid circuit components found";
                ClearAllComponentValues();  // Clear old values when circuit is invalid
                OnSolveError?.Invoke(LastSolveResult);
                return false;
            }

            // Solve the circuit components directly
            if (debugSolver) Debug.Log($"[SOLVER] Calling circuitSolver.Solve() with {logicalComponents.Count} components");
            circuitSolver.Solve(logicalComponents.ToList());
            if (debugSolver) Debug.Log("[SOLVER] circuitSolver.Solve() completed successfully");

            // Update 3D components with solved values
            UpdateComponentsFromSolver(logicalComponents.ToList());

            // Mark as solved
            circuitNeedsSolving = false;
            lastSolveTime = Time.time;
            IsCircuitSolved = true;

            LastSolveResult = $"Circuit solved successfully: {logicalComponents.Count} components";
            debugManager?.LogToFile(LastSolveResult);

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

        // CRITICAL FIX #1: Clear ALL terminal electrical nodes first
        // This ensures we start completely fresh with no stale node references
        ClearAllTerminalNodes();

        // Clear logical component references from 3D components
        // This ensures we don't have stale logical component references
        foreach (var comp3D in circuitManager.Components)
        {
            if (comp3D != null)
            {
                comp3D.logicalComponent = null;
            }
        }

        // Build topology to discover junctions and merge terminal nodes
        // This assigns fresh CircuitNode objects to terminals via TraceTerminalPaths()
        JunctionTopologyManager.CircuitTopology topology = null;
        if (topologyManager != null)
        {
            topology = topologyManager.BuildTopology();
            if (debugSolver)
            {
                debugManager?.LogToFile($"Topology discovered: {(topology != null ? topology.junctions.Count : 0)} junctions");
            }

            // Update wire component references from topology
            if (topology != null)
            {
                UpdateWireComponentReferences(topology);
            }
        }

        // CRITICAL FIX #2: Clear all CircuitNode.ConnectedComponents lists AFTER topology
        // Now that nodes exist, we can clear their component lists before rebuilding
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
            }
        }

        // SINGLE SUMMARY LOG: Show final node connectivity
        var allNodes = new HashSet<CircuitNode>();
        foreach (var comp in logicalComponents)
        {
            allNodes.Add(comp.NodeA);
            allNodes.Add(comp.NodeB);
        }
        var nodeSummary = new System.Text.StringBuilder();
        nodeSummary.Append($"[SOLVER SUMMARY] {logicalComponents.Count} components, {allNodes.Count} nodes: ");
        foreach (var node in allNodes)
        {
            var compNames = string.Join("+", node.ConnectedComponents.ConvertAll(c => c.GetType().Name.Substring(0, 3)));
            nodeSummary.Append($"[{node.Id.Substring(0, Mathf.Min(15, node.Id.Length))}→{compNames}] ");
        }
        Debug.Log(nodeSummary.ToString());

        if (debugSolver)
        {
            debugManager?.LogToFile($"Final circuit: {logicalComponents.Count} components");
            debugManager?.LogToFile("Terminal-based electrical connections established");
        }
        
        return logicalComponents.AsReadOnly();
    }

    /// <summary>
    /// CRITICAL: Clear ALL terminal electrical node references before rebuilding.
    /// This ensures we start with a completely clean slate each solve.
    /// </summary>
    private void ClearAllTerminalNodes()
    {
        int clearedCount = 0;

        // Find ALL terminals in the scene and null their electrical nodes
        var allTerminals = FindObjectsByType<ComponentTerminal>(FindObjectsSortMode.None);
        foreach (var terminal in allTerminals)
        {
            if (terminal != null && terminal.electricalNode != null)
            {
                terminal.electricalNode = null;
                clearedCount++;
            }
        }

        if (debugSolver)
        {
            debugManager?.LogToFile($"Cleared {clearedCount} terminal electrical nodes");
        }
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

    /// <summary>
    /// Update wire component references from topology junctions
    /// This allows junction wires to have startComponent/endComponent for labels and animations
    /// </summary>
    void UpdateWireComponentReferences(JunctionTopologyManager.CircuitTopology topology)
    {
        if (topology == null || topology.junctions == null || topology.junctions.Count == 0)
        {
            return;
        }

        if (debugSolver)
        {
            debugManager?.LogToFile("=== UPDATING WIRE COMPONENT REFERENCES FROM TOPOLOGY ===");
        }

        // Build endpoint→junction map for quick lookup
        var endpointToJunction = new Dictionary<WireEndpoint, JunctionTopologyManager.Junction>();
        foreach (var junction in topology.junctions)
        {
            foreach (var endpoint in junction.endpoints)
            {
                endpointToJunction[endpoint] = junction;
            }
        }

        // For each wire, check if endpoints are in junctions
        foreach (var wireObj in circuitManager.Wires)
        {
            var wire = wireObj.GetComponent<CircuitWire>();
            if (wire == null) continue;

            // Check start endpoint
            if (wire.startEndpoint != null && endpointToJunction.ContainsKey(wire.startEndpoint))
            {
                var junction = endpointToJunction[wire.startEndpoint];
                var connectedTerminal = junction.GetConnectedTerminal();
                if (connectedTerminal != null && wire.startComponent == null)
                {
                    wire.startComponent = connectedTerminal.ParentComponent;
                    if (debugSolver)
                    {
                        debugManager?.LogToFile($"Wire {wire.name} startComponent set to {wire.startComponent.name} via junction {junction.id}");
                    }
                }
            }

            // Check end endpoint
            if (wire.endEndpoint != null && endpointToJunction.ContainsKey(wire.endEndpoint))
            {
                var junction = endpointToJunction[wire.endEndpoint];
                var connectedTerminal = junction.GetConnectedTerminal();
                if (connectedTerminal != null && wire.endComponent == null)
                {
                    wire.endComponent = connectedTerminal.ParentComponent;
                    if (debugSolver)
                    {
                        debugManager?.LogToFile($"Wire {wire.name} endComponent set to {wire.endComponent.name} via junction {junction.id}");
                    }
                }
            }
        }

        if (debugSolver)
        {
            debugManager?.LogToFile("Wire component references updated from topology");
        }
    }

    #region ICircuitSolver Implementation

    // Configuration Methods
    public void EnableAutoSolve(bool enabled)
    {
        manualSolveMode = !enabled;
    }

    public void SetSolveInterval(float interval)
    {
        solveDelay = Mathf.Max(0.1f, interval); // Minimum 0.1 seconds
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