> **DEPRECATED** - This document describes the old junction-centric merging approach.
> See **[TOPOLOGY_PATH_TRAVERSAL.md](TOPOLOGY_PATH_TRAVERSAL.md)** for the current path-centric design.
> This document is retained for historical reference only.

---

# Wire-to-Wire Junction Solving Issue - Root Cause Analysis

## Executive Summary

**Problem**: Wire-to-wire junctions appear visually but circuit doesn't solve electrically.

**Root Cause**: Two separate, disconnected node systems:
1. **Terminal Node System** (used by solver) - terminal.electricalNode
2. **Spatial Node System** (unused) - CircuitNodeManager.CreateSpatialNodeSystem()

**Solution**: Merge terminal.electricalNode references when junctions are discovered.

---

## The Two Node Systems

### System 1: Terminal Electrical Nodes (USED BY SOLVER)

**Creation**:
```csharp
// ComponentTerminal.cs line 106-112
void CreateElectricalNode()
{
    string nodeId = $"{parentComponent.name}_{(isInput ? "Input" : "Output")}_{GetInstanceID()}";
    electricalNode = new CircuitNode(nodeId);  // Each terminal gets UNIQUE node
}
```

**Connection** (Terminal-to-Terminal Wires):
```csharp
// ComponentTerminal.cs line 145-173
void ConnectElectricalNodes(ComponentTerminal otherTerminal)
{
    // Merge nodes by making both terminals reference same node
    var sharedNode = electricalNode ?? otherTerminal.electricalNode;

    electricalNode = sharedNode;
    otherTerminal.electricalNode = sharedNode;  // NODE MERGING!
}
```

**Called by**:
```csharp
// ConnectTool.cs line 254
terminal1.ConnectToTerminal(terminal2, circuitWire);
```

**Usage**:
```csharp
// ComponentTerminalManager.cs line 220-245
public void UpdateLogicalConnections()
{
    foreach (var componentPair in componentTerminals)
    {
        var terminals = componentPair.Value;

        // Use terminal.electricalNode for solver!
        component.logicalComponent.NodeA = terminals[0].electricalNode;
        component.logicalComponent.NodeB = terminals[1].electricalNode;
    }
}
```

### System 2: Spatial Nodes from Topology (COMPLETELY UNUSED)

**Creation**:
```csharp
// CircuitNodeManager.cs line 31-122
public Dictionary<Vector3, CircuitNode> CreateSpatialNodeSystem()
{
    var spatialNodes = new Dictionary<Vector3, CircuitNode>();

    // Build topology
    var topology = topologyManager.BuildTopology();

    // Create NEW CircuitNode for each junction
    foreach (var junction in topology.junctions)
    {
        var node = new CircuitNode(junction.id);  // DIFFERENT from terminal.electricalNode!
        spatialNodes[junction.position] = node;
    }

    return spatialNodes;
}
```

**Usage**:
```csharp
// CircuitSolverManager.cs - NEVER CALLED!
// Grep result: No matches found
```

---

## The Disconnect

### Terminal-to-Terminal Wire Connection (WORKS ✅)

**User Action**: Click Terminal1 → Click Terminal2

**Flow**:
1. ConnectTool.HandleTerminalClick(terminal1)
2. ConnectTool.HandleTerminalClick(terminal2)
3. ConnectTool creates CircuitWire
4. **terminal1.ConnectToTerminal(terminal2, wire)**
5. **terminal1.electricalNode = terminal2.electricalNode** ← NODE MERGE!
6. Solver uses merged nodes ✅

### Wire-to-Wire Junction (BROKEN ❌)

**User Action**: Drag Wire1.Endpoint → Snap to Wire2.Endpoint

**Flow**:
1. WireEndpoint.OnMouseDrag()
2. WireEndpoint.SnapToOtherEndpoint(otherEndpoint)
3. **snappedToEndpoint = otherEndpoint** ← Only visual!
4. JunctionTopologyManager discovers junction
5. CircuitNodeManager creates spatial nodes (unused)
6. **NO terminal.electricalNode merging happens!** ❌
7. Solver uses disconnected terminal nodes ❌

---

## Detailed Trace: Battery → Wire1 → Junction → Wire2 → Bulb

### Setup:
```
Battery.PositiveTerminal (electricalNode = Node_Battery_Pos)
    ↓ Wire1
Wire1.StartEndpoint (at Battery.PositiveTerminal)
Wire1.EndEndpoint (snapped to Wire2.StartEndpoint) ← JUNCTION
    ↓ Wire2
Wire2.StartEndpoint (snapped to Wire1.EndEndpoint) ← JUNCTION
Wire2.EndEndpoint (at Bulb.TerminalA)
    ↓ Bulb
Bulb.TerminalA (electricalNode = Node_Bulb_A)
```

### What SHOULD Happen:
```
Battery.PositiveTerminal.electricalNode = Node_Junction_0
Wire1.StartTerminal.electricalNode = Node_Junction_0
Wire1.EndTerminal.electricalNode = Node_Junction_1  (junction!)
Wire2.StartTerminal.electricalNode = Node_Junction_1  (junction!)
Wire2.EndTerminal.electricalNode = Node_Junction_2
Bulb.TerminalA.electricalNode = Node_Junction_2
```

Electrical path: Node_Battery → Node_Junction_1 (wire-to-wire junction) → Node_Bulb

### What ACTUALLY Happens:
```
Battery.PositiveTerminal.electricalNode = Node_Battery_Pos
Wire1.StartTerminal.electricalNode = Node_Battery_Pos  (merged when wire created ✅)
Wire1.EndTerminal.electricalNode = Node_Wire1_End_12345  (UNIQUE, never merged! ❌)
Wire2.StartTerminal.electricalNode = Node_Wire2_Start_67890  (UNIQUE, never merged! ❌)
Wire2.EndTerminal.electricalNode = Node_Bulb_A  (merged when wire created ✅)
Bulb.TerminalA.electricalNode = Node_Bulb_A
```

**BROKEN ELECTRICAL PATH**:
- Battery.PositiveTerminal → Wire1.Start (connected ✅)
- Wire1.End → Wire2.Start (NOT CONNECTED ❌)
- Wire2.End → Bulb.TerminalA (connected ✅)

**Result**: Circuit doesn't solve because Wire1.End and Wire2.Start have different electricalNode references!

---

## Why Topology Alone Doesn't Fix This

The JunctionTopologyManager correctly discovers junctions:
```
Junction_1:
  - Endpoints: [Wire1.EndEndpoint, Wire2.StartEndpoint]
  - Position: (1.5, 0, 0)
  - Connected Terminal: null (floating junction)
  - GetConnectedJunctionsViaWires(): [Junction_0 (Battery), Junction_2 (Bulb)]
```

But this information is NEVER used to merge terminal.electricalNode!

The CircuitNodeManager creates spatial nodes:
```csharp
var node = new CircuitNode("Junction_1");
spatialNodes[(1.5, 0, 0)] = node;
```

But CircuitSolverManager doesn't use spatialNodes - it uses terminal.electricalNode!

---

## The Fix

### Option 1: Merge Terminal Nodes in JunctionTopologyManager

After discovering junctions, merge the terminal.electricalNode references:

```csharp
// In JunctionTopologyManager.BuildTopology()
foreach (var junction in junctions)
{
    // Get all wire endpoints at this junction
    var endpoints = junction.endpoints;

    // Get the terminals those endpoints are connected to
    var terminals = new List<ComponentTerminal>();
    foreach (var endpoint in endpoints)
    {
        var wire = GetWireForEndpoint(endpoint);
        if (wire != null)
        {
            // Determine which terminal this endpoint connects to
            var terminal = GetTerminalForWireEndpoint(wire, endpoint);
            if (terminal != null)
                terminals.Add(terminal);
        }
    }

    // Merge all terminal electrical nodes
    if (terminals.Count >= 2)
    {
        var sharedNode = terminals[0].electricalNode;

        foreach (var terminal in terminals)
        {
            terminal.electricalNode = sharedNode;  // NODE MERGE!
        }
    }
}
```

### Option 2: Use Spatial Nodes in Solver

Modify CircuitSolverManager to use spatialNodes instead of terminal.electricalNode:

**NOT RECOMMENDED** - would require massive refactor of terminal system

---

## Implementation Plan

### Step 1: Add Terminal Lookup to CircuitWire

CircuitWire needs to expose which terminal each endpoint connects to:

```csharp
// CircuitWire.cs
public ComponentTerminal startTerminal;  // Terminal that startEndpoint connects to
public ComponentTerminal endTerminal;    // Terminal that endEndpoint connects to
```

These should be set when wire is created in ConnectTool.

### Step 2: Add Helper Method to Get Terminal for Endpoint

```csharp
// JunctionTopologyManager.cs
ComponentTerminal GetTerminalForWireEndpoint(CircuitWire wire, WireEndpoint endpoint)
{
    if (wire.startEndpoint == endpoint)
        return wire.startTerminal;
    if (wire.endEndpoint == endpoint)
        return wire.endTerminal;
    return null;
}
```

### Step 3: Add Node Merging to BuildTopology()

```csharp
// JunctionTopologyManager.cs
public CircuitTopology BuildTopology()
{
    // ... existing junction discovery code ...

    // NEW: Merge terminal electrical nodes for each junction
    MergeJunctionTerminalNodes(topology.junctions);

    return topology;
}

void MergeJunctionTerminalNodes(List<Junction> junctions)
{
    foreach (var junction in junctions)
    {
        var terminals = new List<ComponentTerminal>();

        // Collect all terminals at this junction
        foreach (var endpoint in junction.endpoints)
        {
            var wire = GetWireForEndpoint(endpoint);
            if (wire != null)
            {
                var terminal = GetTerminalForWireEndpoint(wire, endpoint);
                if (terminal != null)
                    terminals.Add(terminal);
            }
        }

        // Merge all terminal nodes to first terminal's node
        if (terminals.Count >= 2)
        {
            var sharedNode = terminals[0].electricalNode ?? new CircuitNode(junction.id);

            foreach (var terminal in terminals)
            {
                terminal.electricalNode = sharedNode;
            }

            Debug.Log($"[TOPOLOGY] Merged {terminals.Count} terminal nodes at {junction.id}");
        }
    }
}
```

### Step 4: Ensure Wire Terminals Are Set

Verify that ConnectTool sets wire.startTerminal and wire.endTerminal when creating wires.

---

## Testing Checklist

After implementing fix:

1. **Series Circuit with Wire-to-Wire Junction**
   - Setup: Battery+ → Wire1 → [Junction] → Wire2 → Bulb → Battery-
   - Expected: Circuit solves, bulb lights up
   - Verify: Battery.PositiveTerminal.electricalNode == Wire1.EndTerminal.electricalNode == Wire2.StartTerminal.electricalNode

2. **Parallel Circuit with Terminal Junction**
   - Setup: Battery+ → Wire1, Wire2, Wire3 (all at Battery+) → Resistors → Battery-
   - Expected: Circuit solves, parallel current division
   - Verify: All three wire terminals share same electricalNode

3. **Complex Mixed Circuit**
   - Setup: Battery+ → Wire1 → Junction (Wire1↔Wire2, Wire2↔Wire3) → Bulbs → Battery-
   - Expected: Circuit solves correctly
   - Verify: All junctions properly merge terminal nodes

---

## Root Cause Summary

**The fundamental issue**: We built a beautiful topology discovery system that finds junctions, but we forgot to connect it to the actual electrical solving system.

- **Visual Layer** (WireEndpoint): ✅ Creates junction references
- **Topology Layer** (JunctionTopologyManager): ✅ Discovers junctions
- **Node Layer** (CircuitNodeManager): ❌ Creates unused spatial nodes
- **Solver Layer** (ComponentTerminalManager): ❌ Uses disconnected terminal nodes

**The missing link**: Merge terminal.electricalNode references when junctions are discovered!

---

## Files to Modify

1. **CircuitWire.cs**: Add startTerminal and endTerminal fields
2. **ConnectTool.cs**: Set wire.startTerminal and wire.endTerminal when creating wires
3. **JunctionTopologyManager.cs**:
   - Add GetTerminalForWireEndpoint() helper
   - Add MergeJunctionTerminalNodes() method
   - Call MergeJunctionTerminalNodes() in BuildTopology()

**Estimated lines of code**: ~50 lines total
**Risk level**: Low (isolated change, doesn't affect existing terminal-to-terminal wire logic)
**Testing time**: 15 minutes
