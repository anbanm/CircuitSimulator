using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System;

// Enhanced circuit manager with real solver integration and event-based updates
public class Circuit3DManager : MonoBehaviour
{
    [Header("Circuit Solving")]
    public bool autoSolve = true;
    public float solveInterval = 0.5f;
    public bool eventBasedSolving = true; // NEW: Solve immediately when circuit changes
    public bool debugSolver = true;
    
    [Header("Manual Controls")]
    public bool manualSolveMode = false;
    
    [Header("Status")]
    [SerializeField] private int componentCount = 0;
    [SerializeField] private int wireCount = 0;
    
    private List<CircuitComponent3D> components = new List<CircuitComponent3D>();
    private List<GameObject> wires = new List<GameObject>();
    private CircuitSolver circuitSolver;
    private float lastSolveTime = 0f;
    private bool circuitNeedsSolving = false;
    
    // Singleton pattern for easy access
    private static Circuit3DManager instance;
    public static Circuit3DManager Instance => instance;
    
    // Debug logging
    private StringBuilder debugLog = new StringBuilder();
    private string debugFilePath;
    
    void Awake()
    {
        // Singleton setup
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Debug.LogWarning("Multiple Circuit3DManager instances found! Destroying duplicate.");
            Destroy(gameObject);
            return;
        }
        
        // Setup debug file
        debugFilePath = Path.Combine(Application.persistentDataPath, "CircuitDebug.log");
        LogToFile($"=== Circuit Debug Session Started at {System.DateTime.Now} ===");
        LogToFile($"Debug file location: {debugFilePath}");
    }
    
    void Start()
    {
        circuitSolver = new CircuitSolver();
        CircuitSolver.EnableDebugLog = debugSolver;
        LogToFile("Circuit3DManager initialized with real solver integration");
        Debug.Log("Circuit3DManager initialized with real solver integration");
        
        // Find and register any existing components
        RegisterExistingComponents();
    }
    
    void LogToFile(string message)
    {
        string timestampedMessage = $"[{System.DateTime.Now:HH:mm:ss.fff}] {message}";
        debugLog.AppendLine(timestampedMessage);
        
        try
        {
            File.AppendAllText(debugFilePath, timestampedMessage + "\n");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to write to debug file: {e.Message}");
        }
    }
    
    void RegisterExistingComponents()
    {
        CircuitComponent3D[] existingComponents = FindObjectsOfType<CircuitComponent3D>();
        foreach (var comp in existingComponents)
        {
            RegisterComponent(comp);
        }
        string msg = $"Found and registered {existingComponents.Length} existing components";
        LogToFile(msg);
        Debug.Log(msg);
    }
    
    void Update()
    {
        // Manual solving shortcut
        if (Input.GetKeyDown(KeyCode.S) && (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)))
        {
            Debug.Log("Manual solve triggered (Ctrl+S)");
            SolveCircuitManually();
            return;
        }
        
        // Auto test shortcut
        if (Input.GetKeyDown(KeyCode.T) && (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)))
        {
            Debug.Log("Test circuit triggered (Ctrl+T)");
            TestWithoutWires();
            return;
        }
        
        // Debug key to toggle debug logging
        if (Input.GetKeyDown(KeyCode.D) && (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)))
        {
            debugSolver = !debugSolver;
            CircuitSolver.EnableDebugLog = debugSolver;
            Debug.Log($"Debug solver: {(debugSolver ? "ON" : "OFF")}");
        }
        
        if (manualSolveMode) return; // Skip auto-solving in manual mode
        
        // Event-based solving (immediate when changes detected)
        if (eventBasedSolving && circuitNeedsSolving)
        {
            if (debugSolver) Debug.Log("Event-based solve triggered");
            SolveCircuit();
            circuitNeedsSolving = false;
        }
        // Or interval-based solving
        else if (autoSolve && Time.time - lastSolveTime > solveInterval)
        {
            if (debugSolver) Debug.Log("Interval-based solve triggered");
            SolveCircuit();
            lastSolveTime = Time.time;
        }
    }
    
    public void RegisterComponent(CircuitComponent3D component)
    {
        if (!components.Contains(component))
        {
            components.Add(component);
            componentCount = components.Count;
            string msg = $"Registered component: {component.name} (Total: {componentCount})";
            LogToFile(msg);
            Debug.Log(msg);
            MarkCircuitChanged(); // Trigger re-solve
        }
    }
    
    public void UnregisterComponent(CircuitComponent3D component)
    {
        if (components.Remove(component))
        {
            componentCount = components.Count;
            Debug.Log($"Unregistered component: {component.name} (Total: {componentCount})");
            MarkCircuitChanged(); // Trigger re-solve
        }
    }
    
    public void RegisterWire(GameObject wire)
    {
        if (!wires.Contains(wire))
        {
            wires.Add(wire);
            wireCount = wires.Count;
            Debug.Log($"Registered wire: {wire.name} (Total: {wireCount})");
            MarkCircuitChanged(); // Trigger re-solve
        }
    }
    
    public void UnregisterWire(GameObject wire)
    {
        if (wires.Remove(wire))
        {
            wireCount = wires.Count;
            Debug.Log($"Unregistered wire: {wire.name} (Total: {wireCount})");
            MarkCircuitChanged(); // Trigger re-solve
        }
    }
    
    public void MarkCircuitChanged()
    {
        circuitNeedsSolving = true;
        if (debugSolver)
        {
            Debug.Log("Circuit marked for re-solving");
        }
    }
    
    [ContextMenu("Force Register All Components")]
    public void ForceRegisterAllComponents()
    {
        Debug.Log("=== FORCE REGISTERING ALL COMPONENTS ===");
        
        // Clear existing registration
        components.Clear();
        wires.Clear();
        
        // Find all CircuitComponent3D objects in scene
        CircuitComponent3D[] allComponents = FindObjectsOfType<CircuitComponent3D>();
        Debug.Log($"Found {allComponents.Length} CircuitComponent3D objects in scene");
        
        foreach (var comp in allComponents)
        {
            RegisterComponent(comp);
            Debug.Log($"Registered: {comp.name} ({comp.ComponentType}) - V:{comp.voltage}V R:{comp.resistance}Ω");
        }
        
        // Find all wire objects
        GameObject[] allGameObjects = FindObjectsOfType<GameObject>();
        int wireCount = 0;
        foreach (var go in allGameObjects)
        {
            if (go.name.StartsWith("Wire_") || go.GetComponent<CircuitWire>() != null)
            {
                RegisterWire(go);
                wireCount++;
                var cw = go.GetComponent<CircuitWire>();
                if (cw != null)
                {
                    Debug.Log($"Registered wire: {go.name} ({cw.Component1?.name} <-> {cw.Component2?.name})");
                }
            }
        }
        
        Debug.Log($"SUMMARY: Registered {components.Count} components and {wires.Count} wires");
        
        // Force solve circuit immediately
        MarkCircuitChanged();
        SolveCircuit();
        
        Debug.Log("=====================================");
    }
    
    [ContextMenu("Test Known Working Circuit")]
    public void TestKnownWorkingCircuit()
    {
        Debug.Log("=== TESTING KNOWN WORKING CIRCUIT ===");
        
        // Make sure solver is initialized
        if (circuitSolver == null)
        {
            circuitSolver = new CircuitSolver();
            Debug.Log("Initialized circuitSolver for test");
        }
        
        try
        {
            // This is the EXACT same test that worked in CircuitTestRunner
            var node1 = new CircuitNode("S_Node1");
            var node2 = new CircuitNode("S_Node2");
            var node3 = new CircuitNode("S_Node3");
            
            var battery = new Battery("Battery", node1, node3, 6f);
            var r1 = new Resistor("R1", node1, node2, 3f);
            var r2 = new Resistor("R2", node2, node3, 3f);
            
            var components = new List<CircuitComponent> { battery, r1, r2 };
            
            Debug.Log("Testing original working series circuit: 6V battery + 3Ω + 3Ω resistors");
            Debug.Log("Expected: Total R = 6Ω, I = 1A, V across each resistor = 3V");
            
            circuitSolver.Solve(components);
            
            Debug.Log($"Results:");
            Debug.Log($"  Battery: V={battery.VoltageDrop:F3}V, I={battery.Current:F3}A");
            Debug.Log($"  R1: V={r1.VoltageDrop:F3}V, I={r1.Current:F3}A");
            Debug.Log($"  R2: V={r2.VoltageDrop:F3}V, I={r2.Current:F3}A");
            
            bool works = Math.Abs(battery.Current - 1.0f) < 0.01f;
            Debug.Log($"Original solver works correctly: {works}");
            
            if (!works)
            {
                Debug.LogError("❌ CircuitCore.cs has been broken!");
            }
            else
            {
                Debug.Log("✅ CircuitCore.cs still works - issue is in 3D conversion");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error: {e.Message}\n{e.StackTrace}");
        }
        
        Debug.Log("==============================");
    }
    
    public void SolveCircuit()
    {
        string header = "=== SOLVING CIRCUIT ===";
        LogToFile(header);
        Debug.Log(header);
        
        string info = $"Components: {components.Count}, Wires: {wires.Count}";
        LogToFile(info);
        Debug.Log(info);
        
        if (components.Count == 0) 
        {
            string warning = "No components to solve";
            LogToFile(warning);
            Debug.LogWarning(warning);
            return;
        }
        
        try
        {
            // Convert 3D components to logical circuit components
            string building = "Building logical circuit...";
            LogToFile(building);
            Debug.Log(building);
            
            var logicalComponents = BuildLogicalCircuit();
            
            if (logicalComponents.Count == 0)
            {
                string noLogical = "No logical components created";
                LogToFile(noLogical);
                Debug.LogWarning(noLogical);
                return;
            }
            
            string built = $"Built {logicalComponents.Count} logical components";
            LogToFile(built);
            Debug.Log(built);
            
            // Check if we have at least one battery
            var battery = logicalComponents.OfType<Battery>().FirstOrDefault();
            if (battery == null)
            {
                string noBattery = "No battery found - cannot solve circuit";
                LogToFile(noBattery);
                Debug.LogWarning(noBattery);
                ResetComponentValues();
                return;
            }
            
            string foundBattery = $"Found battery: {battery.Id} ({battery.Voltage}V)";
            LogToFile(foundBattery);
            Debug.Log(foundBattery);
            
            // Solve using your CircuitCore solver
            string calling = "Calling CircuitSolver.Solve()...";
            LogToFile(calling);
            Debug.Log(calling);
            
            circuitSolver.Solve(logicalComponents);
            
            string completed = "CircuitSolver.Solve() completed";
            LogToFile(completed);
            Debug.Log(completed);
            
            // Update 3D component visuals with real results
            string updating = "Updating 3D components with results...";
            LogToFile(updating);
            Debug.Log(updating);
            
            UpdateComponentsFromSolver(logicalComponents);
            
            string success = "Circuit solved successfully!";
            LogToFile(success);
            Debug.Log(success);
            
            if (debugSolver)
            {
                LogCircuitResults();
            }
        }
        catch (System.Exception e)
        {
            string error = $"Circuit solving failed: {e.Message}\n{e.StackTrace}";
            LogToFile(error);
            Debug.LogError(error);
        }
        
        lastSolveTime = Time.time;
        string footer = "=== SOLVE COMPLETE ===";
        LogToFile(footer);
        Debug.Log(footer);
    }
    
    void ResetComponentValues()
    {
        foreach (var comp3D in components)
        {
            if (comp3D != null)
            {
                comp3D.current = 0f;
                comp3D.voltageDrop = 0f;
            }
        }
    }
    
    void LogCircuitResults()
    {
        Debug.Log("=== CIRCUIT RESULTS ===");
        foreach (var comp3D in components)
        {
            if (comp3D != null)
            {
                Debug.Log($"{comp3D.name} ({comp3D.ComponentType}): V={comp3D.voltageDrop:F3}V, I={comp3D.current:F3}A");
            }
        }
        Debug.Log("=====================");
    }
    
    List<CircuitComponent> BuildLogicalCircuit()
    {
        var logicalComponents = new List<CircuitComponent>();
        var nodeMap = new Dictionary<string, CircuitNode>();
        
        if (debugSolver)
        {
            LogToFile("=== BUILDING LOGICAL CIRCUIT ===");
            LogToFile($"Components: {components.Count}, Wires: {wires.Count}");
        }
        
        // Create nodes for each component
        foreach (var comp3D in components)
        {
            if (comp3D == null) continue;
            
            string nodeAId = $"{comp3D.name}_A";
            string nodeBId = $"{comp3D.name}_B";
            
            if (!nodeMap.ContainsKey(nodeAId))
                nodeMap[nodeAId] = new CircuitNode(nodeAId);
            if (!nodeMap.ContainsKey(nodeBId))
                nodeMap[nodeBId] = new CircuitNode(nodeBId);
        }
        
        // Process wires to share nodes (simple approach)
        foreach (var wireObj in wires)
        {
            if (wireObj == null) continue;
            
            var circuitWire = wireObj.GetComponent<CircuitWire>();
            if (circuitWire != null && circuitWire.Component1 != null && circuitWire.Component2 != null)
            {
                string comp1BId = $"{circuitWire.Component1.name}_B";
                string comp2AId = $"{circuitWire.Component2.name}_A";
                
                if (nodeMap.ContainsKey(comp1BId) && nodeMap.ContainsKey(comp2AId))
                {
                    // Simple: make comp2A use the same node as comp1B
                    var sharedNode = nodeMap[comp1BId];
                    nodeMap[comp2AId] = sharedNode;
                    
                    if (debugSolver)
                    {
                        LogToFile($"Wire: {circuitWire.Component1.name}_B <-> {circuitWire.Component2.name}_A (shared node {sharedNode.Id})");
                    }
                }
            }
        }
        
        // Create logical components
        foreach (var comp3D in components)
        {
            if (comp3D == null) continue;
            
            string nodeAId = $"{comp3D.name}_A";
            string nodeBId = $"{comp3D.name}_B";
            
            var nodeA = nodeMap[nodeAId];
            var nodeB = nodeMap[nodeBId];
            
            CircuitComponent logicalComp = null;
            switch (comp3D.ComponentType)
            {
                case ComponentType.Battery:
                    logicalComp = new Battery(comp3D.name, nodeA, nodeB, comp3D.voltage);
                    if (debugSolver) LogToFile($"Created Battery: {comp3D.name} = {comp3D.voltage}V ({nodeA.Id} -> {nodeB.Id})");
                    break;
                case ComponentType.Resistor:
                    logicalComp = new Resistor(comp3D.name, nodeA, nodeB, comp3D.resistance);
                    if (debugSolver) LogToFile($"Created Resistor: {comp3D.name} = {comp3D.resistance}Ω ({nodeA.Id} -> {nodeB.Id})");
                    break;
                case ComponentType.Bulb:
                    logicalComp = new Bulb(comp3D.name, nodeA, nodeB, comp3D.resistance);
                    if (debugSolver) LogToFile($"Created Bulb: {comp3D.name} = {comp3D.resistance}Ω ({nodeA.Id} -> {nodeB.Id})");
                    break;
                case ComponentType.Switch:
                    bool isClosed = comp3D.resistance < 1f;
                    logicalComp = new Switch(comp3D.name, nodeA, nodeB, isClosed);
                    if (debugSolver) LogToFile($"Created Switch: {comp3D.name} = {(isClosed ? "CLOSED" : "OPEN")} ({nodeA.Id} -> {nodeB.Id})");
                    break;
            }
            
            if (logicalComp != null)
            {
                logicalComponents.Add(logicalComp);
                comp3D.logicalComponent = logicalComp;
            }
        }
        
        if (debugSolver)
        {
            LogToFile($"Final circuit: {logicalComponents.Count} components");
            var uniqueNodes = nodeMap.Values.Distinct().ToList();
            LogToFile($"Unique nodes: {uniqueNodes.Count}");
            foreach (var node in uniqueNodes)
            {
                LogToFile($"  Node {node.Id}: {node.ConnectedComponents.Count} components");
            }
            LogToFile($"=================================");
        }
        
        return logicalComponents;
    }
    
    void UpdateComponentsFromSolver(List<CircuitComponent> solvedComponents)
    {
        // Update 3D components with solved values
        foreach (var comp3D in components)
        {
            if (comp3D?.logicalComponent != null)
            {
                var logical = comp3D.logicalComponent;
                comp3D.current = logical.Current;
                comp3D.voltageDrop = logical.VoltageDrop;
                
                if (debugSolver && logical.Current > 0.001f)
                {
                    Debug.Log($"{comp3D.name}: V={logical.VoltageDrop:F2}V, I={logical.Current:F3}A");
                }
            }
        }
        
        // Update wire currents (simplified)
        foreach (var wireObj in wires)
        {
            var circuitWire = wireObj.GetComponent<CircuitWire>();
            if (circuitWire != null)
            {
                // For now, use average current of connected components
                float avgCurrent = 0f;
                int count = 0;
                if (circuitWire.Component1 != null)
                {
                    avgCurrent += circuitWire.Component1.current;
                    count++;
                }
                if (circuitWire.Component2 != null)
                {
                    avgCurrent += circuitWire.Component2.current;
                    count++;
                }
                if (count > 0)
                {
                    circuitWire.current = avgCurrent / count;
                }
            }
        }
    }
}

// Extension to CircuitComponent3D to store logical component reference
public partial class CircuitComponent3D
{
    [System.NonSerialized]
    public CircuitComponent logicalComponent; // Reference to logical component
}
