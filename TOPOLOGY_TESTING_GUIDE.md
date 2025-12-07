# Wire-to-Wire Junction System - Testing Guide

> **NOTE**: This guide describes the junction discovery and debug visualization system.
> For the topology algorithm that merges terminals, see **[TOPOLOGY_PATH_TRAVERSAL.md](TOPOLOGY_PATH_TRAVERSAL.md)**.

## Overview

The wire-to-wire junction system uses a **3-layer architecture** that separates visual behavior from electrical topology. This guide explains how to test the system using the **GameObject-based debug visualization**.

## Architecture Summary

```
Visual Layer (WireEndpoint)
    ↓ stores snappedToEndpoint references
Topology Layer (JunctionTopologyManager)
    ↓ discovers junctions (visual debugging)
    ↓ traces terminal paths (electrical connectivity) ← PATH-CENTRIC
Solver Layer (CircuitSolver)
    ↓ nodal analysis with correctly merged nodes
```

**Key Insight**: Junction discovery is for **visual debugging**. Terminal merging uses **path-centric traversal** (BFS from each terminal through wire chains) to handle wire-to-wire junction chains correctly.

---

## GameObject Debug Visualization

### What to Look For

When you build a circuit and trigger a solve, the **JunctionTopologyManager** creates a GameObject hierarchy in the Unity Hierarchy window:

```
_CircuitTopology_Debug (root)
├── Junction_0
│   ├── JunctionMarker (green sphere in Scene View)
│   ├── Endpoint_WireEndpoint_0 (line connecting to actual endpoint)
│   └── Endpoint_WireEndpoint_1
├── Junction_1
│   ├── JunctionMarker
│   ├── Endpoint_WireEndpoint_2
│   └── Endpoint_WireEndpoint_3
└── ...
```

### Inspector Information

**Select `_CircuitTopology_Debug`** to see summary:
- `Total Junctions`: Number of junctions discovered
- `Floating Junctions`: Junctions not connected to any component terminal
- `Connected Junctions`: Junctions connected to component terminals

**Select individual `Junction_X`** to see details:
- `Junction Id`: Unique identifier
- `Endpoint Count`: How many wire endpoints are at this junction
- `Is Floating`: Whether junction connects to a terminal
- `Connected Terminal`: Name of terminal this junction connects to (or "None (Floating)")
- `Junction Position`: World position of junction center

### Scene View Markers

- **Green spheres**: Junction markers showing where junctions are detected
- **Cyan lines**: Connect junction markers to actual wire endpoints
- **Larger green endpoints**: Wire endpoints that are part of a junction

---

## Test Cases

### Test 1: Series Circuit with Wire-to-Wire Junction

**Goal**: Verify that wire-to-wire junctions work for series circuits.

**Setup**:
1. Create Battery (B key)
2. Create Light Bulb (L key)
3. Create Wire1 (W key)
4. Create Wire2 (W key)

**Steps**:
1. Drag Wire1.Start to Battery+ terminal
2. Drag Wire2.End to Bulb terminal
3. **Drag Wire1.End to Wire2.Start** (snap them together - should turn green)
4. Press Space to solve circuit

**Expected Behavior**:

**Visual**:
- Wire1.End and Wire2.Start should both be **green and larger** (junction indicator)
- Circuit should show current flowing (animations)

**Hierarchy** (`_CircuitTopology_Debug`):
```
Junction_0  ← Wire1.End and Wire2.Start
  - Endpoint Count: 2
  - Is Floating: True (not at a terminal)
  - Connected Terminal: "None (Floating)"
Junction_1  ← Wire1.Start at Battery+
  - Endpoint Count: 1
  - Is Floating: False
  - Connected Terminal: "Battery_Terminal_Positive"
Junction_2  ← Wire2.End at Bulb
  - Endpoint Count: 1
  - Is Floating: False
  - Connected Terminal: "LightBulb_Terminal_B"
```

**Scene View**:
- Green sphere at the junction between Wire1 and Wire2
- Green spheres at Battery+ and Bulb terminals
- Cyan lines connecting junction markers to endpoints

**Electrical**:
- Circuit solves successfully
- Current flows: Battery+ → Wire1 → Junction → Wire2 → Bulb
- Bulb lights up

**Console Logs**:
```
[TOPOLOGY] Discovering junctions from 4 endpoints
[TOPOLOGY] Created Junction_0 with 2 endpoints at (x, y, z)
[TOPOLOGY] ✅ Discovered 3 junctions
[CircuitNodeManager] Created Junction_0 at (x, y, z) with 2 endpoints, terminal: Floating
```

---

### Test 2: Parallel Circuit with Terminal Junction

**Goal**: Verify that multiple wires connecting to the same terminal create a junction.

**Setup**:
1. Create Battery (B key)
2. Create Resistor1 (R key)
3. Create Resistor2 (R key)
4. Create Wire1, Wire2, Wire3 (W key × 3)

**Steps**:
1. Drag Wire1.Start to Battery+ terminal
2. Drag Wire2.Start to Battery+ terminal (same terminal as Wire1!)
3. Drag Wire3.Start to Battery+ terminal (same terminal!)
4. Drag Wire1.End to Resistor1 terminal
5. Drag Wire2.End to Resistor2 terminal
6. Press Space to solve circuit

**Expected Behavior**:

**Visual**:
- All three wire endpoints at Battery+ should be **green and larger** (junction at terminal)
- Both resistors should show current flowing

**Hierarchy** (`_CircuitTopology_Debug`):
```
Junction_0  ← Wire1.Start, Wire2.Start, Wire3.Start at Battery+
  - Endpoint Count: 3
  - Is Floating: False
  - Connected Terminal: "Battery_Terminal_Positive"
Junction_1  ← Wire1.End at Resistor1
  - Endpoint Count: 1
  - Is Floating: False
  - Connected Terminal: "Resistor1_Terminal_Input"
Junction_2  ← Wire2.End at Resistor2
  - Endpoint Count: 1
  - Is Floating: False
  - Connected Terminal: "Resistor2_Terminal_Input"
```

**Scene View**:
- Large green sphere at Battery+ (3 endpoints meeting)
- Smaller green spheres at Resistor1 and Resistor2 terminals

**Electrical**:
- Circuit solves with parallel branches
- Current splits: Battery+ → (Wire1 || Wire2 || Wire3)
- Both resistors receive current

---

### Test 3: Complex Mixed Circuit

**Goal**: Test both wire-to-wire junctions AND terminal junctions in same circuit.

**Setup**:
1. Create Battery
2. Create Resistor1, Resistor2
3. Create Wire1, Wire2, Wire3, Wire4

**Steps**:
1. Wire1: Battery+ to Wire2.Start (terminal junction)
2. Wire2: Junction to Resistor1
3. Wire3: Battery+ to Wire4.Start (same terminal junction)
4. Wire4: Junction to Resistor2
5. **Snap Wire2.End to Wire3.Start** (wire-to-wire junction!)
6. Connect remaining endpoints to complete circuit

**Expected Behavior**:

**Hierarchy**:
```
Junction_0  ← Wire1.Start, Wire3.Start at Battery+ (terminal junction)
  - Endpoint Count: 2
  - Is Floating: False
  - Connected Terminal: "Battery_Terminal_Positive"
Junction_1  ← Wire2.End, Wire3.Start (wire-to-wire junction)
  - Endpoint Count: 2
  - Is Floating: True
  - Connected Terminal: "None (Floating)"
Junction_2  ← Wire2.End at Resistor1
  - Endpoint Count: 1
  - Is Floating: False
```

**Circuit solves**: Both resistors receive current through parallel branches with wire-to-wire junction.

---

## Common Issues and Debugging

### Issue: Junction not appearing in hierarchy

**Possible Causes**:
1. Wire endpoints not close enough (need to snap together)
2. `enableDebugVisualization = false` in JunctionTopologyManager Inspector
3. Circuit hasn't been solved yet (topology is built on solve)

**Solution**:
- Check that wire endpoints turn **green and larger** when snapped
- Select CircuitManager in Hierarchy → CircuitNodeManager → JunctionTopologyManager → Enable Debug Visualization
- Press Space to trigger circuit solve

### Issue: "Floating junction" but circuit not solving

**Diagnosis**:
- Select junction in hierarchy
- Check `Connected Terminal` field
- If "None (Floating)", the junction is not electrically connected to any component

**Solution**:
- One endpoint of the junction must be at a component terminal
- Drag one endpoint to a terminal, or create another wire from junction to component

### Issue: Endpoints at same position but not forming junction

**Possible Cause**:
- Endpoints are within `positionTolerance` (default 0.2f) but not explicitly snapped

**Solution**:
- Explicitly snap endpoints together (drag one onto the other until both turn green)
- Or adjust `positionTolerance` in JunctionTopologyManager Inspector

### Issue: Circuit solves but no junction visualization

**Possible Cause**:
- JunctionTopologyManager not attached to CircuitManager GameObject

**Solution**:
- Select CircuitManager in Hierarchy
- Add JunctionTopologyManager component if missing
- The CircuitNodeManager will auto-add it if missing

---

## Performance Notes

### Debug Visualization Overhead

The GameObject debug visualization creates GameObjects and LineRenderers for each junction. This is **intended for development only**.

**To Disable for Production**:
1. Select CircuitManager → JunctionTopologyManager
2. Uncheck `Enable Debug Visualization`

This will disable GameObject creation but topology discovery will still work.

### When Topology is Built

Topology is rebuilt every time `CircuitNodeManager.CreateSpatialNodeSystem()` is called, which happens:
- When circuit solve is triggered (Space key, auto-solve)
- When components are added/removed
- When wire endpoints are connected/disconnected

---

## Code References

### Key Files

- **WireEndpoint.cs** (684 lines) - Visual layer, stores `snappedToEndpoint` reference
- **JunctionTopologyManager.cs** (485 lines) - Topology layer, discovers junctions
- **CircuitNodeManager.cs** (173 lines) - Solver layer, creates nodes from topology

### Key Methods

**Visual Layer**:
- `WireEndpoint.SnapToWireEndpoint()` - Snap two endpoints together (sets `snappedToEndpoint`)
- `WireEndpoint.SnapToTerminal()` - Snap endpoint to component terminal
- `WireEndpoint.DetachFromEndpoint()` - Unsnap from wire endpoint

**Topology Layer**:
- `JunctionTopologyManager.BuildTopology()` - Main entry point, discovers all junctions
- `JunctionTopologyManager.FindJunctionEndpoints()` - BFS to find all endpoints in a junction
- `Junction.GetConnectedTerminal()` - Find which terminal (if any) this junction connects to

**Solver Layer**:
- `CircuitNodeManager.CreateSpatialNodeSystem()` - Create solver nodes from topology
- Calls `topologyManager.BuildTopology()` to get junctions
- Creates one `CircuitNode` per junction

---

## Next Steps

After verifying the test cases:

1. **Test edge cases**:
   - Disconnecting endpoints mid-circuit
   - Moving junctions by dragging endpoints
   - Multiple junctions in series (Wire1 → Junction1 → Wire2 → Junction2 → Wire3)

2. **Performance testing**:
   - Large circuits (10+ components, 20+ wires)
   - Rapid connection/disconnection
   - Check debug visualization performance

3. **User experience**:
   - Are junctions obvious to students?
   - Is the green color / size increase sufficient?
   - Should we add UI hints (e.g., "Junction created!")?

4. **Feature enhancements**:
   - 3-way junctions (Wire1, Wire2, Wire3 all snapped together)
   - Junction components (junction nodes that are their own component)
   - Visual current flow through junctions

---

## Success Criteria

✅ **Series circuit with wire-to-wire junction solves correctly**
✅ **Parallel circuit with terminal junction solves correctly**
✅ **Junction visualization appears in hierarchy with correct info**
✅ **Scene view shows green junction markers at correct positions**
✅ **No endpoints move when they shouldn't** (visual vs electrical separation working)
✅ **WireEndpoint.cs is simpler** (removed 79 lines of junction buddy code)
✅ **Architecture is maintainable** (3 layers with clear responsibilities)

If all criteria are met, the refactor is **SUCCESS** and ready for production! 🎉
