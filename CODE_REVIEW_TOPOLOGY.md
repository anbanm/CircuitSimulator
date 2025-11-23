# Wire-to-Wire Junction System - Comprehensive Code Review

## Overview

This document traces through the complete data flow for various scenarios to verify the 3-layer architecture is working correctly.

### Important Note on Wire Endpoints

Throughout this document, I use labels like "Wire1.EndpointA" and "Wire1.EndpointB" for clarity in tracing scenarios. However, **wires are non-directional** - there is no electrical significance to which endpoint is "A" vs "B". Students can connect either endpoint to either terminal.

The code internally tracks endpoints as `wire.startEndpoint` and `wire.endEndpoint`, but these are just references for visual positioning. **Electrical direction is determined by the circuit solver based on component polarity (battery), not by wire endpoint labels.**

---

## Scenario 1: Series Circuit with Wire-to-Wire Junction

**Goal**: Battery+ → Wire1 → **Junction** → Wire2 → Bulb → Battery-

**Visual Circuit**:
- Wire1: One endpoint at Battery+, other endpoint snapped to Wire2
- Wire2: One endpoint snapped to Wire1, other endpoint at Bulb

### Step-by-Step Trace

#### 1.1 User Creates Wire1
**File**: `ConnectTool.cs` → `CreateDraggableWire()`
```
Action: User presses W key
→ ConnectTool.CreateDraggableWire()
→ Creates GameObject "Draggable_Wire_1"
→ CircuitWire.InitializeWithEndpoints(startPos, endPos)
→ Creates Wire1.Start and Wire1.End endpoints
```

**State After**:
- Wire1.Start: `connectedTerminal = null`, `snappedToEndpoint = null`
- Wire1.End: `connectedTerminal = null`, `snappedToEndpoint = null`
- Both endpoints are gray (disconnected)

#### 1.2 User Drags Wire1.Start to Battery+
**File**: `WireEndpoint.cs` → `OnMouseDown()` → `StartDragging()`

```csharp
// Line 179-201
void StartDragging()
{
    isDragging = true;

    // Disconnect from current terminal if connected
    if (connectedTerminal != null)  // NULL - skip
        DetachFromTerminal();

    // Disconnect from snapped endpoint if snapped
    if (snappedToEndpoint != null)  // NULL - skip
        DetachFromEndpoint();

    // Calculate drag offset
    dragOffset = transform.position - mouseWorldPos;

    // Visual feedback
    UpdateColor(draggingColor);  // Wire1.Start turns CYAN
}
```

**During Drag**: `OnMouseDrag()` → `UpdateDragPosition()` → `FindNearestTerminal()`

```csharp
// Line 312-411
ComponentTerminal FindNearestTerminal()
{
    // Check wire endpoints FIRST (priority)
    foreach (var endpoint in allEndpoints)
    {
        if (endpoint == this) continue;  // Skip Wire1.Start itself
        if (endpoint.parentWire == this.parentWire) continue;  // Skip Wire1.End

        float distance = Vector3.Distance(transform.position, endpoint.transform.position);

        if (distance < minDistance)
        {
            nearestWireEndpointWhileDragging = endpoint;
            if (endpoint.IsConnected)
                nearest = endpoint.ConnectedTerminal;  // Use its terminal
        }
    }

    // Then check component terminals
    foreach (var terminal in allTerminals)
    {
        if (!IsValidTerminalForConnection(terminal))
            continue;  // Skip if invalid

        if (distance < minWireEndpointDistance && distance < snapRadius)
        {
            nearest = terminal;  // Battery+ terminal
            nearestWireEndpointWhileDragging = null;
        }
    }

    return nearest;  // Returns Battery+ terminal
}
```

**On Mouse Up**: `OnMouseUp()` → `StopDragging()` → `SnapToTerminal()`

```csharp
// Line 248-284
void StopDragging()
{
    isDragging = false;

    ComponentTerminal nearestTerminal = FindNearestTerminal();  // Battery+

    if (nearestTerminal != null)
    {
        SnapToTerminal(nearestTerminal);  // SNAP!
    }
}

// Line 461-500
public void SnapToTerminal(ComponentTerminal terminal)
{
    // Validate connection
    if (!IsValidTerminalForConnection(terminal))  // PASSES - Wire1.End not connected
        return;

    // Detach from old connections
    if (connectedTerminal != null && connectedTerminal != terminal)  // NULL - skip
        DetachFromTerminal();

    if (snappedToEndpoint != null)  // NULL - skip
        DetachFromEndpoint();

    // PHYSICAL CONNECTION ONLY
    connectedTerminal = terminal;  // Wire1.Start.connectedTerminal = Battery+

    // Move to terminal position
    transform.position = terminal.transform.position;  // Move Wire1.Start to Battery+

    // Visual feedback
    UpdateColor(connectedColor);  // Wire1.Start turns BLUE
    UpdateJunctionColors(terminal);  // Check if multiple wires on this terminal

    // Notify parent wire
    parentWire.OnEndpointConnected(this);  // Wire visual update
}
```

**State After**:
- Wire1.Start: `connectedTerminal = Battery+`, `snappedToEndpoint = null`, color = BLUE
- Wire1.End: `connectedTerminal = null`, `snappedToEndpoint = null`, color = GRAY

✅ **Visual Layer Complete**: Wire1.Start is physically at Battery+, no electrical propagation

---

#### 1.3 User Creates Wire2 and Drags Wire2.End to Bulb
**Same process as 1.2**

**State After**:
- Wire2.End: `connectedTerminal = Bulb_Terminal`, `snappedToEndpoint = null`, color = BLUE
- Wire2.Start: `connectedTerminal = null`, `snappedToEndpoint = null`, color = GRAY

---

#### 1.4 User Drags Wire1.End to Wire2.Start (JUNCTION!)
**File**: `WireEndpoint.cs` → `OnMouseUp()` → `StopDragging()`

```csharp
// Line 254-264
ComponentTerminal nearestTerminal = FindNearestTerminal();  // NULL (no terminal nearby)

if (nearestTerminal != null)
{
    // Skip - nearestTerminal is null
}
else if (nearestWireEndpointWhileDragging != null)  // Wire2.Start!
{
    // Snap to unconnected wire endpoint - connect both to same position
    SnapToWireEndpoint(nearestWireEndpointWhileDragging);  // JUNCTION!
}
```

```csharp
// Line 506-540
public void SnapToWireEndpoint(WireEndpoint otherEndpoint)  // otherEndpoint = Wire2.Start
{
    if (otherEndpoint == null) return;  // PASSES

    // Detach from terminal if any
    if (connectedTerminal != null)  // NULL - skip
        DetachFromTerminal();

    // Store the snap reference (bidirectional)
    snappedToEndpoint = otherEndpoint;  // Wire1.End.snappedToEndpoint = Wire2.Start
    if (otherEndpoint.snappedToEndpoint != this)  // Wire2.Start.snappedToEndpoint is NULL
    {
        otherEndpoint.snappedToEndpoint = this;  // Wire2.Start.snappedToEndpoint = Wire1.End
    }

    // Move to the other endpoint's position
    Vector3 junctionPosition = otherEndpoint.transform.position;  // Wire2.Start position
    transform.position = junctionPosition;  // Move Wire1.End to Wire2.Start position

    // Visual feedback - green for junction
    UpdateColor(junctionColor);  // Wire1.End turns GREEN
    otherEndpoint.UpdateColor(junctionColor);  // Wire2.Start turns GREEN

    // Make junction endpoints larger for visibility
    transform.localScale = Vector3.one * endpointSize * 1.5f;  // Wire1.End enlarges
    otherEndpoint.transform.localScale = Vector3.one * endpointSize * 1.5f;  // Wire2.Start enlarges

    // Notify parent wire
    parentWire.OnEndpointConnected(this);  // Wire1 visual update
}
```

**State After**:
- Wire1.End: `connectedTerminal = null`, `snappedToEndpoint = Wire2.Start`, color = GREEN, scale = 1.5x
- Wire2.Start: `connectedTerminal = null`, `snappedToEndpoint = Wire1.End`, color = GREEN, scale = 1.5x
- **Bidirectional snap reference established!**

✅ **Visual Layer Complete**: Junction created, both endpoints green and enlarged, no electrical connection yet

---

#### 1.5 User Presses Space → Circuit Solve
**File**: `CircuitNodeManager.cs` → `CreateSpatialNodeSystem()`

```csharp
// Line 31-50
public Dictionary<Vector3, CircuitNode> CreateSpatialNodeSystem()
{
    var spatialNodes = new Dictionary<Vector3, CircuitNode>();

    // 1. Build topology by discovering junctions
    var topology = topologyManager.BuildTopology();  // TOPOLOGY LAYER CALLED!
}
```

**File**: `JunctionTopologyManager.cs` → `BuildTopology()`

```csharp
// Line 156-224
public CircuitTopology BuildTopology()
{
    var topology = new CircuitTopology();

    // 1. Find all wire endpoints in the scene
    WireEndpoint[] allEndpoints = FindObjectsByType<WireEndpoint>();
    // Returns: [Wire1.Start, Wire1.End, Wire2.Start, Wire2.End]

    Debug.Log($"[TOPOLOGY] Discovering junctions from {allEndpoints.Length} endpoints");
    // Output: "[TOPOLOGY] Discovering junctions from 4 endpoints"

    // 2. Group endpoints into junctions
    var processedEndpoints = new HashSet<WireEndpoint>();
    int junctionCounter = 0;

    foreach (var endpoint in allEndpoints)  // Loop 1: Wire1.Start
    {
        if (processedEndpoints.Contains(endpoint))  // FALSE
            continue;

        // Find all endpoints that form a junction with this one
        var junctionEndpoints = FindJunctionEndpoints(endpoint, allEndpoints, processedEndpoints);
        // Returns: [Wire1.Start] (only itself, not snapped to anything)

        if (junctionEndpoints.Count > 0)
        {
            var junction = new Junction
            {
                id = $"Junction_{junctionCounter++}",  // "Junction_0"
                endpoints = junctionEndpoints,  // [Wire1.Start]
                position = CalculateJunctionCenter(junctionEndpoints)  // Battery+ position
            };

            topology.junctions.Add(junction);  // Add Junction_0

            foreach (var ep in junctionEndpoints)
                processedEndpoints.Add(ep);  // Mark Wire1.Start as processed

            Debug.Log($"[TOPOLOGY] Created {junction.id} with {junction.endpoints.Count} endpoints");
            // Output: "[TOPOLOGY] Created Junction_0 with 1 endpoints at (Battery+ position)"
        }
    }

    // Loop 2: Wire1.End (already processed? NO, only Wire1.Start was processed)
    // Find junctions for Wire1.End
    var junctionEndpoints = FindJunctionEndpoints(Wire1.End, allEndpoints, processedEndpoints);
    // THIS IS CRITICAL - what does FindJunctionEndpoints return?
}
```

**File**: `JunctionTopologyManager.cs` → `FindJunctionEndpoints()`

```csharp
// Line 231-272
List<WireEndpoint> FindJunctionEndpoints(WireEndpoint startEndpoint, ...)  // startEndpoint = Wire1.End
{
    var junctionEndpoints = new List<WireEndpoint> { startEndpoint };  // [Wire1.End]
    var toProcess = new Queue<WireEndpoint>();
    toProcess.Enqueue(startEndpoint);  // Queue: [Wire1.End]

    while (toProcess.Count > 0)  // Loop 1
    {
        var current = toProcess.Dequeue();  // current = Wire1.End

        // Check explicit snap reference
        if (current.SnappedToEndpoint != null && !junctionEndpoints.Contains(current.SnappedToEndpoint))
        {
            // Wire1.End.SnappedToEndpoint = Wire2.Start
            // Wire2.Start NOT in junctionEndpoints yet
            junctionEndpoints.Add(current.SnappedToEndpoint);  // Add Wire2.Start
            toProcess.Enqueue(current.SnappedToEndpoint);  // Queue: [Wire2.Start]
        }

        // Check position-based proximity
        foreach (var endpoint in allEndpoints)  // Check all 4 endpoints
        {
            if (junctionEndpoints.Contains(endpoint))  // Skip Wire1.End, Wire2.Start
                continue;

            if (processedEndpoints.Contains(endpoint))  // Skip Wire1.Start (already processed)
                continue;

            // Check if this endpoint is close to any endpoint in current junction
            foreach (var junctionEndpoint in junctionEndpoints)  // [Wire1.End, Wire2.Start]
            {
                float distance = Vector3.Distance(endpoint.GetPosition(), junctionEndpoint.GetPosition());
                if (distance <= positionTolerance)  // 0.2f
                {
                    // Wire2.End is NOT close (it's at Bulb terminal)
                    // Wire1.Start is already processed
                    // Nothing added here
                }
            }
        }
    }

    // Loop 2: Process Wire2.Start
    while (toProcess.Count > 0)  // Queue has Wire2.Start
    {
        var current = toProcess.Dequeue();  // current = Wire2.Start

        if (current.SnappedToEndpoint != null && !junctionEndpoints.Contains(current.SnappedToEndpoint))
        {
            // Wire2.Start.SnappedToEndpoint = Wire1.End
            // Wire1.End ALREADY in junctionEndpoints - skip
        }

        // Position-based check - nothing new added
    }

    return junctionEndpoints;  // Returns: [Wire1.End, Wire2.Start]
}
```

**Back to BuildTopology()**:
```csharp
// Create junction for Wire1.End + Wire2.Start
var junction = new Junction
{
    id = "Junction_1",
    endpoints = [Wire1.End, Wire2.Start],
    position = CalculateJunctionCenter([Wire1.End, Wire2.Start])  // Average of both positions
};

topology.junctions.Add(junction);
processedEndpoints.Add(Wire1.End);
processedEndpoints.Add(Wire2.Start);

Debug.Log("[TOPOLOGY] Created Junction_1 with 2 endpoints at (junction position)");
```

**Continue BuildTopology() - Loop 3: Wire2.End**
```csharp
// Wire2.End is not processed yet
var junctionEndpoints = FindJunctionEndpoints(Wire2.End, ...);
// Returns: [Wire2.End] (only itself)

var junction = new Junction
{
    id = "Junction_2",
    endpoints = [Wire2.End],
    position = Bulb_Terminal position
};

topology.junctions.Add(junction);
```

**Final Topology**:
```
topology.junctions = [
    Junction_0: [Wire1.Start] at Battery+ position
    Junction_1: [Wire1.End, Wire2.Start] at junction position (WIRE-TO-WIRE!)
    Junction_2: [Wire2.End] at Bulb position
]
```

✅ **Topology Layer Complete**: Discovered 3 junctions, correctly identified wire-to-wire junction!

---

#### 1.6 Check Junction Terminal Connections
**File**: `JunctionTopologyManager.cs` → `Junction.GetConnectedTerminal()`

```csharp
// Line 37-48
public ComponentTerminal GetConnectedTerminal()
{
    // Check if any endpoint in this junction is physically at a terminal
    foreach (var endpoint in endpoints)
    {
        if (endpoint.IsConnected && endpoint.ConnectedTerminal != null)
            return endpoint.ConnectedTerminal;
    }
    return null;
}
```

**For Junction_0 (Wire1.Start)**:
- Wire1.Start.IsConnected = true (connectedTerminal = Battery+)
- Returns: Battery+ terminal ✅

**For Junction_1 (Wire1.End, Wire2.Start)**:
- Wire1.End.IsConnected = false (connectedTerminal = null)
- Wire2.Start.IsConnected = false (connectedTerminal = null)
- Returns: null (FLOATING JUNCTION) ✅

**For Junction_2 (Wire2.End)**:
- Wire2.End.IsConnected = true (connectedTerminal = Bulb_Terminal)
- Returns: Bulb_Terminal ✅

---

#### 1.7 Create Circuit Solver Nodes
**File**: `CircuitNodeManager.cs` → `CreateSpatialNodeSystem()`

```csharp
// Line 57-78
// 2. Create CircuitNode for each junction
foreach (var junction in topology.junctions)
{
    var node = new CircuitNode(junction.id);

    // Map this node to the junction position
    spatialNodes[junction.position] = node;

    // Also map each endpoint position to this node
    foreach (var endpoint in junction.endpoints)
    {
        spatialNodes[endpoint.GetPosition()] = node;
    }

    var terminal = junction.GetConnectedTerminal();
    string terminalInfo = terminal != null ? terminal.name : "Floating";
    Debug.Log($"Created {node.Id} at {junction.position} with {junction.endpoints.Count} endpoints, terminal: {terminalInfo}");
}
```

**Nodes Created**:
```
Node: Junction_0
- Position: Battery+ position
- Terminal: Battery+
- Maps: { Battery+ position -> Junction_0, Wire1.Start position -> Junction_0 }

Node: Junction_1
- Position: Junction average position
- Terminal: Floating
- Maps: { Junction position -> Junction_1, Wire1.End position -> Junction_1, Wire2.Start position -> Junction_1 }

Node: Junction_2
- Position: Bulb position
- Terminal: Bulb_Terminal
- Maps: { Bulb position -> Junction_2, Wire2.End position -> Junction_2 }
```

✅ **Solver Layer Complete**: Created 3 nodes, mapped to positions

---

#### 1.8 Debug Visualization Created
**File**: `JunctionTopologyManager.cs` → `CreateDebugVisualization()`

```csharp
// Line 333-397
void CreateDebugVisualization(CircuitTopology topology)
{
    foreach (var junction in topology.junctions)
    {
        // Create junction root GameObject
        GameObject junctionObj = new GameObject(junction.id);  // "Junction_0", "Junction_1", "Junction_2"
        junctionObj.transform.SetParent(topologyDebugRoot.transform);

        // Add visual sphere at junction position
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = "JunctionMarker";
        sphere.transform.position = junction.position;
        sphere.transform.localScale = Vector3.one * 0.3f;

        // Color the sphere GREEN
        renderer.material.color = Color.green;

        // Add info component
        var info = junctionObj.AddComponent<JunctionDebugInfo>();
        info.junction = junction;

        // Create child GameObjects for each endpoint
        foreach (var endpoint in junction.endpoints)
        {
            GameObject endpointDebug = new GameObject($"Endpoint_{endpoint.name}");

            // Add CYAN line connecting junction marker to endpoint
            LineRenderer line = endpointDebug.AddComponent<LineRenderer>();
            line.SetPosition(0, junction.position);  // From junction center
            line.SetPosition(1, endpoint.GetPosition());  // To endpoint
        }
    }
}
```

**Hierarchy Created**:
```
_CircuitTopology_Debug
├── Junction_0
│   ├── JunctionMarker (green sphere at Battery+)
│   └── Endpoint_Wire1.Start (cyan line)
├── Junction_1 ⭐ WIRE-TO-WIRE JUNCTION
│   ├── JunctionMarker (green sphere at junction)
│   ├── Endpoint_Wire1.End (cyan line)
│   └── Endpoint_Wire2.Start (cyan line)
└── Junction_2
    ├── JunctionMarker (green sphere at Bulb)
    └── Endpoint_Wire2.End (cyan line)
```

**Inspector for Junction_1**:
- Junction Id: "Junction_1"
- Endpoint Count: 2
- Is Floating: true
- Connected Terminal: "None (Floating)"
- Junction Position: (junction position)

✅ **Debug Visualization Complete**: Green sphere and cyan lines visible in Scene View!

---

### ⚠️ CRITICAL ISSUE FOUND: Floating Junction Won't Solve!

**Problem**: Junction_1 is marked as "Floating" because neither Wire1.End nor Wire2.Start has a `connectedTerminal`.

**Why This Is a Problem**:
The circuit solver needs to know that Junction_1 is electrically connected to both Battery+ (via Wire1) and Bulb (via Wire2). But with the current implementation:
- Junction_0 connects to Battery+ ✅
- Junction_1 is floating (no terminal) ❌
- Junction_2 connects to Bulb ✅

**Expected Electrical Path**:
Battery+ → Wire1 → **Junction_1** → Wire2 → Bulb

**Actual Electrical Path**:
Battery+ → Wire1 → **??? (floating)** → Wire2 → Bulb

**Root Cause Analysis**:

The issue is that we're only checking `endpoint.ConnectedTerminal` to determine if a junction connects to a terminal. But in a wire-to-wire junction, the endpoints DON'T have `connectedTerminal` set - they only have `snappedToEndpoint`.

**Where the electrical connection should be made**:

The circuit solver needs to traverse the connection graph:
1. Battery+ terminal → Junction_0 (via Wire1.Start)
2. Junction_0 → Junction_1 (via Wire1: Wire1.Start and Wire1.End are on same wire)
3. Junction_1 → Junction_2 (via Wire2: Wire2.Start and Wire2.End are on same wire)
4. Junction_2 → Bulb terminal (via Wire2.End)

**Solution Required**:

The `CircuitTopology` needs a method to discover electrical connectivity THROUGH WIRES:

```csharp
// MISSING METHOD:
public List<Junction> GetElectricallyConnectedJunctions(Junction junction)
{
    var connected = new List<Junction>();

    // For each endpoint in this junction
    foreach (var endpoint in junction.endpoints)
    {
        // Find the wire this endpoint belongs to
        var wire = GetWireForEndpoint(endpoint);

        // Find the OTHER endpoint of this wire
        var otherEndpoint = wire.GetOtherEndpoint(endpoint);

        // Find the junction that contains the other endpoint
        var otherJunction = FindJunctionForEndpoint(otherEndpoint);

        if (otherJunction != null && otherJunction != junction)
            connected.Add(otherJunction);
    }

    return connected;
}
```

This would allow the solver to traverse:
- Junction_0 → Junction_1 (via Wire1)
- Junction_1 → Junction_2 (via Wire2)

**VERDICT**: 🔴 **CRITICAL BUG - Circuit won't solve with floating junctions**

---

## Issue Summary

### ✅ What Works:
1. **Visual Layer** (WireEndpoint.cs):
   - ✅ Snapping to terminals works
   - ✅ Snapping to wire endpoints works
   - ✅ Bidirectional snap references established
   - ✅ Green color and size increase for junctions
   - ✅ No electrical propagation (clean separation)

2. **Topology Layer** (JunctionTopologyManager.cs):
   - ✅ Discovers junctions correctly
   - ✅ Finds wire-to-wire junctions via `snappedToEndpoint`
   - ✅ Finds position-based junctions via proximity
   - ✅ Debug visualization creates GameObject hierarchy
   - ✅ BFS traversal works for finding junction buddies

3. **Solver Layer** (CircuitNodeManager.cs):
   - ✅ Creates nodes from topology
   - ✅ Maps positions to nodes

### ❌ What Doesn't Work:

1. **CRITICAL: Floating junctions have no electrical path**
   - Wire-to-wire junctions are marked as "Floating"
   - Solver can't traverse from junction to junction
   - Circuit won't solve electrically

2. **Missing: Wire-based junction connectivity**
   - Need method to find junctions connected via wires
   - CircuitTopology needs `GetElectricallyConnectedJunctions()`

---

## Recommendations

### Fix 1: Add Wire-Based Traversal to CircuitTopology

Add this method to `CircuitTopology` class:

```csharp
/// <summary>
/// Get junctions that are electrically connected to this junction via wires
/// </summary>
public List<Junction> GetConnectedJunctionsViaWires(Junction junction)
{
    var connectedJunctions = new HashSet<Junction>();

    // For each wire that touches this junction
    var wires = junction.GetConnectedWires();

    foreach (var wire in wires)
    {
        // Find both endpoints of this wire
        var endpoints = new List<WireEndpoint> { wire.startEndpoint, wire.endEndpoint };

        // For each endpoint
        foreach (var endpoint in endpoints)
        {
            // Find the junction containing this endpoint
            var otherJunction = FindJunctionForEndpoint(endpoint);

            // If it's a different junction, it's electrically connected
            if (otherJunction != null && otherJunction != junction)
            {
                connectedJunctions.Add(otherJunction);
            }
        }
    }

    return connectedJunctions.ToList();
}
```

### Fix 2: Update Circuit Solver to Use Junction Graph

The circuit solver should:
1. Start from component terminals
2. Find junction at each terminal
3. Traverse to connected junctions via wires
4. Build complete electrical graph

---

## Next Test: Parallel Circuit

I should trace through a parallel circuit scenario to verify terminal junctions work correctly.

**Setup**: Battery+ → Wire1, Wire2, Wire3 (all at same terminal)

**Expected**:
- Junction_0: [Wire1.Start, Wire2.Start, Wire3.Start] at Battery+
- All three endpoints should have `connectedTerminal = Battery+`
- Junction should NOT be floating

Let me trace through this scenario next...

---

## Scenario 2: Parallel Circuit with Terminal Junction

**Goal**: Battery+ → (Wire1 || Wire2 || Wire3) all at same terminal

### Step-by-Step Trace

#### 2.1 User Drags Wire1.Start to Battery+
**Same as Scenario 1.2**

**State**: Wire1.Start.connectedTerminal = Battery+

#### 2.2 User Drags Wire2.Start to Battery+ (SAME TERMINAL!)
**File**: `WireEndpoint.cs` → `SnapToTerminal()`

```csharp
public void SnapToTerminal(ComponentTerminal terminal)  // terminal = Battery+
{
    // Wire1.Start is already at Battery+
    // Now Wire2.Start is being snapped to the SAME terminal

    if (!IsValidTerminalForConnection(terminal))  // Check if Wire2.End at Battery+ component
        return;  // PASSES - Wire2.End is not connected yet

    // Connect to terminal
    connectedTerminal = terminal;  // Wire2.Start.connectedTerminal = Battery+

    // Move to terminal position
    transform.position = terminal.transform.position;  // Same position as Wire1.Start

    // Visual feedback
    UpdateColor(connectedColor);  // BLUE
    UpdateJunctionColors(terminal);  // CHECK FOR JUNCTION!
}
```

**File**: `WireEndpoint.cs` → `UpdateJunctionColors()`

```csharp
// Line 602-627
void UpdateJunctionColors(ComponentTerminal terminal)  // terminal = Battery+
{
    // Find all wire endpoints connected to this terminal
    WireEndpoint[] allEndpoints = FindObjectsByType<WireEndpoint>();
    List<WireEndpoint> endpointsOnThisTerminal = new List<WireEndpoint>();

    foreach (var endpoint in allEndpoints)
    {
        if (endpoint.connectedTerminal == terminal)  // Battery+
        {
            endpointsOnThisTerminal.Add(endpoint);
        }
    }
    // Found: [Wire1.Start, Wire2.Start]

    // Determine color based on junction status
    Color colorToUse = endpointsOnThisTerminal.Count >= 2 ? junctionColor : connectedColor;
    // 2 >= 2 → TRUE → junctionColor (GREEN)

    // Update all endpoints on this terminal to the same color
    foreach (var endpoint in endpointsOnThisTerminal)
    {
        endpoint.UpdateColor(colorToUse);  // Both turn GREEN
    }
}
```

**State After Wire2.Start snapped**:
- Wire1.Start: connectedTerminal = Battery+, color = **GREEN** (junction!)
- Wire2.Start: connectedTerminal = Battery+, color = **GREEN** (junction!)

✅ **Visual Layer**: Both endpoints turn green when multiple wires connect to same terminal!

#### 2.3 User Drags Wire3.Start to Battery+
**Same process**

**State After**:
- Wire1.Start, Wire2.Start, Wire3.Start: All at Battery+, all **GREEN**, all have `connectedTerminal = Battery+`

---

#### 2.4 Topology Discovery
**File**: `JunctionTopologyManager.cs` → `BuildTopology()`

```csharp
// Endpoints: [Wire1.Start, Wire1.End, Wire2.Start, Wire2.End, Wire3.Start, Wire3.End]

// Loop 1: Wire1.Start
var junctionEndpoints = FindJunctionEndpoints(Wire1.Start, ...);
```

**File**: `JunctionTopologyManager.cs` → `FindJunctionEndpoints()`

```csharp
List<WireEndpoint> FindJunctionEndpoints(WireEndpoint startEndpoint, ...)  // Wire1.Start
{
    var junctionEndpoints = new List<WireEndpoint> { startEndpoint };  // [Wire1.Start]
    var toProcess = new Queue<WireEndpoint>();
    toProcess.Enqueue(startEndpoint);

    while (toProcess.Count > 0)
    {
        var current = toProcess.Dequeue();  // Wire1.Start

        // Check explicit snap reference
        if (current.SnappedToEndpoint != null)  // NULL - Wire1.Start is at terminal, not snapped
        {
            // Skip
        }

        // Check position-based proximity
        foreach (var endpoint in allEndpoints)
        {
            if (junctionEndpoints.Contains(endpoint)) continue;
            if (processedEndpoints.Contains(endpoint)) continue;

            foreach (var junctionEndpoint in junctionEndpoints)  // [Wire1.Start]
            {
                float distance = Vector3.Distance(endpoint.GetPosition(), junctionEndpoint.GetPosition());
                if (distance <= positionTolerance)  // 0.2f
                {
                    // Wire2.Start is at SAME position (Battery+ terminal)
                    // Distance = 0.0 (exact same position)
                    junctionEndpoints.Add(endpoint);  // Add Wire2.Start
                    toProcess.Enqueue(endpoint);
                    break;
                }
            }
        }
    }

    // Queue now has: Wire2.Start
    // Process Wire2.Start - will find Wire3.Start at same position
    // Queue: [Wire3.Start]
    // Process Wire3.Start - no more endpoints at same position

    return junctionEndpoints;  // [Wire1.Start, Wire2.Start, Wire3.Start]
}
```

**Junction Created**:
```csharp
var junction = new Junction
{
    id = "Junction_0",
    endpoints = [Wire1.Start, Wire2.Start, Wire3.Start],
    position = Battery+ position (average of 3 identical positions)
};

topology.junctions.Add(junction);
```

✅ **Topology Layer**: Discovered junction with 3 endpoints at terminal!

---

#### 2.5 Check Terminal Connection
**File**: `JunctionTopologyManager.cs` → `Junction.GetConnectedTerminal()`

```csharp
public ComponentTerminal GetConnectedTerminal()
{
    foreach (var endpoint in endpoints)  // [Wire1.Start, Wire2.Start, Wire3.Start]
    {
        if (endpoint.IsConnected && endpoint.ConnectedTerminal != null)
        {
            // Wire1.Start.IsConnected = true
            // Wire1.Start.ConnectedTerminal = Battery+
            return endpoint.ConnectedTerminal;  // Returns Battery+ ✅
        }
    }
}
```

**Result**: Junction_0 is **NOT floating** - it connects to Battery+! ✅

---

### Scenario 2 Verdict: ✅ Terminal Junctions Work Correctly!

**What Works**:
1. Multiple wire endpoints can connect to same terminal
2. Visual feedback (green color) indicates junction
3. Topology discovers all endpoints at same position
4. Junction correctly identifies connected terminal
5. Junction is NOT floating

**Key Difference from Scenario 1**:
- Terminal junctions: Endpoints have `connectedTerminal` set → junction is NOT floating ✅
- Wire-to-wire junctions: Endpoints have `snappedToEndpoint` set but NO `connectedTerminal` → junction IS floating ❌

---

## Root Cause Analysis: Why Wire-to-Wire Junctions Fail

### The Problem

**Terminal Junction** (works):
```
Wire1.Start → connectedTerminal = Battery+
Wire2.Start → connectedTerminal = Battery+
Junction_0.GetConnectedTerminal() → Battery+ ✅
```

**Wire-to-Wire Junction** (broken):
```
Wire1.End → connectedTerminal = null, snappedToEndpoint = Wire2.Start
Wire2.Start → connectedTerminal = null, snappedToEndpoint = Wire1.End
Junction_1.GetConnectedTerminal() → null ❌ FLOATING!
```

### Why This Is Wrong

The circuit should solve like this:
```
Battery+ → Junction_0 → Wire1 → Junction_1 → Wire2 → Junction_2 → Bulb
```

But the solver sees:
```
Battery+ → Junction_0 → ???
??? → Junction_1 (floating) → ???
??? → Junction_2 → Bulb
```

The solver needs to know that:
- Junction_0 connects to Junction_1 (via Wire1)
- Junction_1 connects to Junction_2 (via Wire2)

---

## The Fix: Add Wire-Based Junction Connectivity

### Problem Summary

The `CircuitTopology` can discover junctions, but it can't traverse the electrical path THROUGH wires.

### Solution

Add methods to `CircuitTopology` class to discover junction-to-junction connectivity:

```csharp
/// <summary>
/// Get all junctions electrically connected to this junction via wires
/// </summary>
public List<Junction> GetConnectedJunctionsViaWires(Junction junction)
{
    var connectedJunctions = new HashSet<Junction>();

    // For each endpoint in this junction
    foreach (var endpoint in junction.endpoints)
    {
        // Find the wire this endpoint belongs to
        var wire = GetWireForEndpoint(endpoint);
        if (wire == null) continue;

        // Find the OTHER endpoint of this wire
        WireEndpoint otherEndpoint = null;
        if (wire.startEndpoint == endpoint)
            otherEndpoint = wire.endEndpoint;
        else if (wire.endEndpoint == endpoint)
            otherEndpoint = wire.startEndpoint;

        if (otherEndpoint == null) continue;

        // Find the junction containing the other endpoint
        var otherJunction = FindJunctionForEndpoint(otherEndpoint);

        // If it's a different junction, they're electrically connected
        if (otherJunction != null && otherJunction != junction)
        {
            connectedJunctions.Add(otherJunction);
        }
    }

    return connectedJunctions.ToList();
}

/// <summary>
/// Find the wire that this endpoint belongs to
/// </summary>
CircuitWire GetWireForEndpoint(WireEndpoint endpoint)
{
    // Check component on endpoint GameObject
    var wire = endpoint.GetComponent<CircuitWire>();
    if (wire != null) return wire;

    // Check parent GameObject
    if (endpoint.transform.parent != null)
    {
        wire = endpoint.transform.parent.GetComponent<CircuitWire>();
        if (wire != null) return wire;
    }

    return null;
}

/// <summary>
/// Build complete electrical graph including wire connections
/// </summary>
public void BuildElectricalGraph()
{
    // For each junction, find connected junctions
    foreach (var junction in junctions)
    {
        var connectedJunctions = GetConnectedJunctionsViaWires(junction);
        Debug.Log($"[TOPOLOGY] {junction.id} connects to {connectedJunctions.Count} other junctions via wires");
    }
}
```

### Usage in Circuit Solver

The circuit solver should:

```csharp
// 1. Build topology
var topology = topologyManager.BuildTopology();

// 2. Build electrical graph
topology.BuildElectricalGraph();

// 3. For each component terminal, find its junction
var batteryPlusJunction = topology.FindJunctionForTerminal(batteryPlusTerminal);

// 4. Traverse from this junction via wires
var connectedJunctions = topology.GetConnectedJunctionsViaWires(batteryPlusJunction);
// Returns: [Junction_1] (via Wire1)

// 5. Continue traversal from Junction_1
var nextJunctions = topology.GetConnectedJunctionsViaWires(Junction_1);
// Returns: [Junction_0, Junction_2] (via Wire1 back to Battery, and via Wire2 to Bulb)

// 6. Build complete electrical path
```

---

## Additional Issues Found

### Issue 1: Potential Null Reference in GetWireForEndpoint

**Location**: `JunctionTopologyManager.cs` → `Junction.GetConnectedWires()` (line 58-74)

```csharp
var wire = endpoint.GetComponent<CircuitWire>();
if (wire == null && endpoint.transform.parent != null)
{
    wire = endpoint.transform.parent.GetComponent<CircuitWire>();
}
```

**Problem**: Assumes endpoint has CircuitWire component or parent has it. What if endpoint GameObject structure changes?

**Fix**: Add null check after searching:
```csharp
if (wire == null)
{
    Debug.LogWarning($"[TOPOLOGY] Cannot find CircuitWire for endpoint {endpoint.name}");
    continue;
}
```

---

### Issue 2: Position Tolerance May Group Unrelated Endpoints

**Location**: `JunctionTopologyManager.cs` → `FindJunctionEndpoints()` (line 257-267)

```csharp
float distance = Vector3.Distance(endpoint.GetPosition(), junctionEndpoint.GetPosition());
if (distance <= positionTolerance)  // 0.2f
{
    junctionEndpoints.Add(endpoint);
}
```

**Problem**: If two separate wires happen to be within 0.2 units, they'll be grouped as a junction even if not explicitly snapped.

**Scenario**:
- Wire1.End at position (5.0, 0.5, 3.0)
- Wire2.Start at position (5.15, 0.5, 3.0) - distance = 0.15 < 0.2
- They'll be grouped as junction even if user didn't snap them!

**Fix**: Only use position tolerance for endpoints that are at terminals (where exact position match is expected):

```csharp
// Check if this endpoint is at the same position as any endpoint in current junction
foreach (var junctionEndpoint in junctionEndpoints)
{
    // Only use position tolerance if BOTH endpoints are at terminals
    bool bothAtTerminals = endpoint.IsConnected && junctionEndpoint.IsConnected;

    float tolerance = bothAtTerminals ? positionTolerance : 0.01f;  // Very strict for floating endpoints

    float distance = Vector3.Distance(endpoint.GetPosition(), junctionEndpoint.GetPosition());
    if (distance <= tolerance)
    {
        junctionEndpoints.Add(endpoint);
        toProcess.Enqueue(endpoint);
        break;
    }
}
```

---

### Issue 3: DetachFromEndpoint Color Logic Error

**Location**: `WireEndpoint.cs` → `DetachFromEndpoint()` (line 557)

```csharp
other.UpdateColor(other.IsConnected ? connectedColor : disconnectedColor);
```

**Problem**: After detaching, if the endpoint is still connected to a terminal, it should check if there are multiple wires on that terminal (junction at terminal).

**Fix**:
```csharp
if (other.IsConnected)
{
    // Update junction colors at terminal (may still be green if multiple wires)
    UpdateJunctionColors(other.ConnectedTerminal);
}
else
{
    other.UpdateColor(disconnectedColor);
}
```

---

### Issue 4: Memory Leak in Debug Visualization

**Location**: `JunctionTopologyManager.cs` → `CreateDebugVisualization()` (line 389)

```csharp
line.material = new Material(Shader.Find("Sprites/Default"));
```

**Problem**: Creating new Material instances without cleanup. These will leak if topology is rebuilt multiple times.

**Fix**: Store materials and destroy them in ClearDebugVisualization:

```csharp
private List<Material> debugMaterials = new List<Material>();

void CreateDebugVisualization(...)
{
    var lineMat = new Material(Shader.Find("Sprites/Default"));
    debugMaterials.Add(lineMat);
    line.material = lineMat;
}

void ClearDebugVisualization()
{
    // Destroy all children
    for (int i = topologyDebugRoot.transform.childCount - 1; i >= 0; i--)
    {
        var child = topologyDebugRoot.transform.GetChild(i);
        if (Application.isPlaying)
            Destroy(child.gameObject);
        else
            DestroyImmediate(child.gameObject);
    }

    // Cleanup materials
    foreach (var mat in debugMaterials)
    {
        if (mat != null)
        {
            if (Application.isPlaying)
                Destroy(mat);
            else
                DestroyImmediate(mat);
        }
    }
    debugMaterials.Clear();
}
```

---

## Summary of Issues

### 🔴 CRITICAL (Must Fix):
1. **Wire-to-wire junctions are floating** - circuit won't solve
   - Fix: Add `GetConnectedJunctionsViaWires()` method
   - Fix: Update solver to traverse junction graph

### 🟡 MAJOR (Should Fix):
2. **Position tolerance may group unrelated endpoints**
   - Fix: Use strict tolerance for floating endpoints

3. **Missing null checks for wire lookup**
   - Fix: Add null checks in `GetConnectedWires()`

### 🟢 MINOR (Nice to Have):
4. **DetachFromEndpoint doesn't update junction colors correctly**
   - Fix: Call `UpdateJunctionColors()` after detach

5. **Memory leak in debug visualization materials**
   - Fix: Track and destroy materials in cleanup

---

## Testing Checklist

After applying fixes:

- [ ] **Test 1**: Series circuit with wire-to-wire junction
  - Battery+ → Wire1 → Junction → Wire2 → Bulb
  - Expected: Circuit solves, current flows through junction

- [ ] **Test 2**: Parallel circuit with terminal junction
  - Battery+ → Wire1, Wire2, Wire3 (all at same terminal)
  - Expected: All wires green, junction has 3 endpoints, circuit solves

- [ ] **Test 3**: Complex mixed circuit
  - Multiple wire-to-wire junctions AND terminal junctions
  - Expected: All paths traverse correctly

- [ ] **Test 4**: Detach and re-snap
  - Create junction, detach endpoint, verify cleanup
  - Expected: Colors update correctly, no stale references

- [ ] **Test 5**: Multiple solve cycles
  - Create circuit, solve, modify, solve again
  - Expected: No memory leaks, old debug objects cleaned up

---

## Conclusion

The 3-layer architecture is **fundamentally sound**, but requires one critical fix to handle wire-to-wire junctions:

**What Works**:
- ✅ Visual layer correctly handles snapping and visual feedback
- ✅ Topology layer correctly discovers junctions
- ✅ Terminal junctions work perfectly
- ✅ Debug visualization is excellent for debugging

**What Needs Fixing**:
- ❌ Wire-to-wire junctions have no electrical path (critical)
- ❌ Need junction-to-junction connectivity via wires
- ❌ Minor bugs in cleanup and validation

**Estimated Fix Time**: 1-2 hours to implement `GetConnectedJunctionsViaWires()` and update circuit solver.

