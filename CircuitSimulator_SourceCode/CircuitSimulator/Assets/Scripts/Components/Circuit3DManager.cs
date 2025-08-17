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
    
    [ContextMenu("Solve Circuit Manually")]
    public void SolveCircuitManually()
    {
        string header = "=== MANUAL SOLVE TRIGGERED ===";
        LogToFile(header);
        Debug.Log(header);
        
        string compInfo = $"Components registered: {components.Count}";
        LogToFile(compInfo);
        Debug.Log(compInfo);
        
        string wireInfo = $"Wires registered: {wires.Count}";
        LogToFile(wireInfo);
        Debug.Log(wireInfo);
        
        if (components.Count == 0)
        {
            string error = "No components registered! Check component registration.";
            LogToFile(error);
            Debug.LogError(error);
            return;
        }
        
        foreach (var comp in components)
        {
            if (comp != null)
            {
                string details = $"  Component: {comp.name} ({comp.ComponentType}) - V:{comp.voltage}V R:{comp.resistance}Ω";
                LogToFile(details);
                Debug.Log(details);
            }
        }
        
        foreach (var wire in wires)
        {
            if (wire != null)
            {
                var cw = wire.GetComponent<CircuitWire>();
                if (cw != null)
                {
                    string wireDetails = $"  Wire: {wire.name} connects {cw.Component1?.name} to {cw.Component2?.name}";
                    LogToFile(wireDetails);
                    Debug.Log(wireDetails);
                }
            }
        }
        
        SolveCircuit();
        string footer = "=== MANUAL SOLVE COMPLETE ===";
        LogToFile(footer);
        Debug.Log(footer);
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
            
            // Validate circuit before solving (important for AR and desktop!)
            var validator = new CircuitValidator();
            var validationResult = validator.ValidateCircuit(logicalComponents);
            
            if (!validationResult.IsValid)
            {
                string validationError = $"Circuit validation failed:\n{validationResult.GetSummary()}";
                LogToFile(validationError);
                Debug.LogError(validationError);
                return; // Don't solve invalid circuits
            }
            
            if (validationResult.HasWarnings)
            {
                string validationWarnings = $"Circuit validation warnings:\n{validationResult.GetSummary()}";
                LogToFile(validationWarnings);
                Debug.LogWarning(validationWarnings);
            }
            
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
    
    // Spatial node system - works great for AR placement on planes!
    Dictionary<Vector3, CircuitNode> CreateSpatialNodeSystem()
    {
        var spatialNodes = new Dictionary<Vector3, CircuitNode>();
        float connectionTolerance = 0.5f; // Unity units - adjust for AR scale
        
        if (debugSolver)
        {
            LogToFile("=== CREATING SPATIAL NODE SYSTEM ===");
        }
        
        // Create component terminal positions
        var componentTerminals = new Dictionary<CircuitComponent3D, (Vector3 inputPos, Vector3 outputPos)>();
        
        foreach (var comp3D in components)
        {
            if (comp3D == null) continue;
            
            // For desktop/AR: components placed on XZ plane, terminals are left/right of center
            Vector3 componentPos = comp3D.transform.position;
            Vector3 inputPos = componentPos + Vector3.left * 0.3f;   // Left terminal
            Vector3 outputPos = componentPos + Vector3.right * 0.3f; // Right terminal
            
            componentTerminals[comp3D] = (inputPos, outputPos);
            
            if (debugSolver)
            {
                LogToFile($"Component {comp3D.name}: Input at {inputPos}, Output at {outputPos}");
            }
        }
        
        // Create nodes based on spatial proximity (perfect for AR!)
        var allTerminalPositions = new List<Vector3>();
        foreach (var terminal in componentTerminals.Values)
        {
            allTerminalPositions.Add(terminal.inputPos);
            allTerminalPositions.Add(terminal.outputPos);
        }
        
        // Group nearby terminal positions into shared nodes
        foreach (var pos in allTerminalPositions)
        {
            bool foundExistingNode = false;
            
            foreach (var existingPos in spatialNodes.Keys)
            {
                if (Vector3.Distance(pos, existingPos) <= connectionTolerance)
                {
                    // This position is close enough to an existing node
                    foundExistingNode = true;
                    if (debugSolver)
                    {
                        LogToFile($"Position {pos} shares node with {existingPos} (distance: {Vector3.Distance(pos, existingPos):F3})");
                    }
                    break;
                }
            }
            
            if (!foundExistingNode)
            {
                // Create new node for this position
                var nodeId = $"Node_{pos.x:F1}_{pos.z:F1}";
                spatialNodes[pos] = new CircuitNode(nodeId);
                
                if (debugSolver)
                {
                    LogToFile($"Created new node {nodeId} at position {pos}");
                }
            }
        }
        
        // Process wires to create additional connections
        foreach (var wireObj in wires)
        {
            if (wireObj == null) continue;
            
            var circuitWire = wireObj.GetComponent<CircuitWire>();
            if (circuitWire != null && circuitWire.Component1 != null && circuitWire.Component2 != null)
            {
                // Wire connects component terminals - merge their nodes
                var comp1Terminals = componentTerminals[circuitWire.Component1];
                var comp2Terminals = componentTerminals[circuitWire.Component2];
                
                // Find the closest terminal pairs and connect them
                var wirePairs = new[]
                {
                    (comp1Terminals.outputPos, comp2Terminals.inputPos),
                    (comp1Terminals.inputPos, comp2Terminals.outputPos),
                    (comp1Terminals.outputPos, comp2Terminals.outputPos),
                    (comp1Terminals.inputPos, comp2Terminals.inputPos)
                };
                
                foreach (var (pos1, pos2) in wirePairs)
                {
                    var node1 = GetSpatialNode(spatialNodes, pos1, connectionTolerance);
                    var node2 = GetSpatialNode(spatialNodes, pos2, connectionTolerance);
                    
                    if (node1 != null && node2 != null && node1 != node2)
                    {
                        // Merge nodes by making all references point to node1
                        MergeSpatialNodes(spatialNodes, node1, node2);
                        
                        if (debugSolver)
                        {
                            LogToFile($"Wire connects {pos1} to {pos2} (merged nodes)");
                        }
                        break; // Only connect the closest pair
                    }
                }
            }
        }
        
        if (debugSolver)
        {
            LogToFile($"Created {spatialNodes.Count} spatial nodes with {connectionTolerance} tolerance");
        }
        
        return spatialNodes;
    }
    
    CircuitNode GetSpatialNode(Dictionary<Vector3, CircuitNode> spatialNodes, Vector3 position, float tolerance)
    {
        foreach (var kvp in spatialNodes)
        {
            if (Vector3.Distance(kvp.Key, position) <= tolerance)
            {
                return kvp.Value;
            }
        }
        return null;
    }
    
    void MergeSpatialNodes(Dictionary<Vector3, CircuitNode> spatialNodes, CircuitNode keepNode, CircuitNode mergeNode)
    {
        // Replace all references to mergeNode with keepNode
        var keysToUpdate = new List<Vector3>();
        foreach (var kvp in spatialNodes)
        {
            if (kvp.Value == mergeNode)
            {
                keysToUpdate.Add(kvp.Key);
            }
        }
        
        foreach (var key in keysToUpdate)
        {
            spatialNodes[key] = keepNode;
        }
    }
    
    List<CircuitComponent> BuildLogicalCircuit()
    {
        var logicalComponents = new List<CircuitComponent>();
        
        if (debugSolver)
        {
            LogToFile("=== BUILDING LOGICAL CIRCUIT ===");
            LogToFile($"Components: {components.Count}, Wires: {wires.Count}");
        }
        
        // Create spatial-based node system (good for both desktop and AR)
        var spatialNodes = CreateSpatialNodeSystem();
        
        // Create logical components using spatial nodes
        foreach (var comp3D in components)
        {
            if (comp3D == null) continue;
            
            // Get component terminal positions
            Vector3 componentPos = comp3D.transform.position;
            Vector3 inputPos = componentPos + Vector3.left * 0.3f;
            Vector3 outputPos = componentPos + Vector3.right * 0.3f;
            
            // Find the spatial nodes for these positions
            var nodeA = GetSpatialNode(spatialNodes, inputPos, 0.5f);
            var nodeB = GetSpatialNode(spatialNodes, outputPos, 0.5f);
            
            if (nodeA == null || nodeB == null)
            {
                Debug.LogError($"Could not find spatial nodes for component {comp3D.name}");
                continue;
            }
            
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
    
    // Missing methods for ComponentPalette buttons
    public void SaveDebugReport()
    {
        string reportPath = Path.Combine(Application.persistentDataPath, "CircuitReport.txt");
        try
        {
            using (StreamWriter writer = new StreamWriter(reportPath))
            {
                writer.WriteLine($"Circuit Report - {System.DateTime.Now}");
                writer.WriteLine("=====================================");
                writer.WriteLine($"Components: {componentCount}");
                writer.WriteLine($"Wires: {wireCount}");
                writer.WriteLine($"Circuit Valid: {!circuitNeedsSolving}");
                writer.WriteLine();
                
                writer.WriteLine("Component Details:");
                foreach (var comp in components3D)
                {
                    if (comp != null)
                    {
                        writer.WriteLine($"- {comp.name}: Type={comp.ComponentType}, R={comp.resistance}Ω, V={comp.voltageDrop:F2}V, I={comp.current:F3}A");
                    }
                }
                
                writer.WriteLine();
                writer.WriteLine("Wire Details:");
                foreach (var wireObj in wires)
                {
                    var wire = wireObj.GetComponent<CircuitWire>();
                    if (wire != null)
                    {
                        writer.WriteLine($"- {wire.name}: R={wire.resistance}Ω, I={wire.current:F3}A");
                    }
                }
            }
            
            Debug.Log($"Debug report saved to: {reportPath}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to save debug report: {e.Message}");
        }
    }
    
    public void DebugComponentRegistration()
    {
        Debug.Log("=== Component Registration Debug ===");
        Debug.Log($"Total registered components: {componentCount}");
        Debug.Log($"Total registered wires: {wireCount}");
        
        Debug.Log("3D Components:");
        for (int i = 0; i < components3D.Count; i++)
        {
            var comp = components3D[i];
            if (comp != null)
            {
                Debug.Log($"  [{i}] {comp.name} - Type: {comp.ComponentType}, Position: {comp.transform.position}");
            }
            else
            {
                Debug.LogWarning($"  [{i}] NULL COMPONENT FOUND!");
            }
        }
        
        Debug.Log("Logical Components:");
        for (int i = 0; i < logicalComponents.Count; i++)
        {
            var comp = logicalComponents[i];
            if (comp != null)
            {
                Debug.Log($"  [{i}] {comp.Id} - Type: {comp.ComponentType}, Resistance: {comp.Resistance}Ω");
            }
            else
            {
                Debug.LogWarning($"  [{i}] NULL LOGICAL COMPONENT FOUND!");
            }
        }
        
        Debug.Log("Wires:");
        for (int i = 0; i < wires.Count; i++)
        {
            var wire = wires[i];
            if (wire != null)
            {
                Debug.Log($"  [{i}] {wire.name}");
            }
            else
            {
                Debug.LogWarning($"  [{i}] NULL WIRE FOUND!");
            }
        }
    }
    
    public void ValidateAndTestCircuit()
    {
        Debug.Log("=== Circuit Validation and Test ===");
        
        // Count components by type
        var batteryComps = components3D.FindAll(c => c != null && c.ComponentType == ComponentType.Battery);
        var resistorComps = components3D.FindAll(c => c != null && c.ComponentType == ComponentType.Resistor);
        var bulbComps = components3D.FindAll(c => c != null && c.ComponentType == ComponentType.Bulb);
        var switchComps = components3D.FindAll(c => c != null && c.ComponentType == ComponentType.Switch);
        
        Debug.Log($"Components found: {batteryComps.Count} batteries, {resistorComps.Count} resistors, {bulbComps.Count} bulbs, {switchComps.Count} switches");
        Debug.Log($"Wires: {wireCount}");
        
        // Basic validation
        if (batteryComps.Count == 0)
        {
            Debug.LogError("❌ No battery found - circuit cannot function");
            return;
        }
        
        if (resistorComps.Count + bulbComps.Count == 0)
        {
            Debug.LogError("❌ No resistive components found - circuit would have infinite current");
            return;
        }
        
        if (wireCount == 0)
        {
            Debug.LogWarning("⚠️ No wires found - components may not be connected");
        }
        
        // Test the current circuit
        Debug.Log("🔧 Testing current circuit configuration...");
        SolveCircuit();
        
        // Report results
        Debug.Log("📊 Circuit Test Results:");
        foreach (var comp in components3D)
        {
            if (comp != null && comp.logicalComponent != null)
            {
                Debug.Log($"  {comp.name}: V={comp.voltageDrop:F2}V, I={comp.current:F3}A, R={comp.resistance:F1}Ω");
            }
        }
        
        Debug.Log("✅ Circuit test completed");
    }
}
