# Circuit Topology: Path-Centric Traversal Design

**Version:** 2.0
**Date:** December 2024
**Status:** CURRENT DESIGN
**Supersedes:** `WIRE_JUNCTION_DESIGN.md`, `FIX_IMPLEMENTATION.md`, `IMPLEMENTATION_SUMMARY.md`

---

## Overview

This document describes the **path-centric traversal** algorithm for discovering electrical connectivity in the Circuit Simulator. This approach replaces the previous junction-centric merging algorithm.

### Why Path-Centric?

> "Wires are life and will move and twist and join across all physical obstructions and bounds... wires truly connect."

Wires in real circuits (and in our simulator) can chain through multiple junction points before reaching a component terminal. The path-centric approach honors this reality.

---

## The Problem with Junction-Centric Merging

### Previous Approach (DEPRECATED)

The old `MergeJunctionTerminalNodes()` method:
1. Discovered junctions (where wire endpoints meet)
2. For each junction, found component terminals
3. Merged terminals at the same junction

### Where It Failed

**Wire Chain Scenario:**
```
Battery[+] ──Wire1──●──Wire2──●──Wire3──● BulbA[left]
                   J1        J2
```

- J1 is a wire-to-wire junction (no component terminal)
- J2 is a wire-to-wire junction (no component terminal)
- Old algorithm found 0 terminals at J1 and J2
- **Result:** No merging happened, circuit broken!

### Parallel Circuit Scenario:
```
                        bulbA
                       ●─────●
                      /       \
    Battery[+] ●────●           ●────● Battery[-]
                     \         /
                      ●─────●
                        bulbB
```

With multiple wire segments between Y-junctions and battery terminals, the junction-centric approach couldn't trace the full path.

---

## The Solution: Path-Centric Traversal

### Core Concept

Instead of asking "what terminals are at this junction?", we ask:

> "Starting from this terminal, what other terminals can I reach by following wires?"

### Algorithm: TraceTerminalPaths()

```csharp
/// <summary>
/// Traces all wire paths from each component terminal using BFS.
/// Terminals connected via wire paths (regardless of junction count) share the same electrical node.
/// </summary>
public void TraceTerminalPaths()
{
    // Get all component terminals in the scene
    var allTerminals = FindAllComponentTerminals();

    // Track which terminals have been merged into groups
    var terminalGroups = new Dictionary<ComponentTerminal, CircuitNode>();

    foreach (var startTerminal in allTerminals)
    {
        // Skip if already assigned to a group
        if (terminalGroups.ContainsKey(startTerminal))
            continue;

        // Create new electrical node for this group
        var sharedNode = new CircuitNode($"Node_{startTerminal.name}");

        // BFS to find all terminals connected via wire paths
        var connectedTerminals = TraceFromTerminal(startTerminal);

        // Merge all connected terminals to share the same node
        foreach (var terminal in connectedTerminals)
        {
            terminal.electricalNode = sharedNode;
            terminalGroups[terminal] = sharedNode;

            // Register component with the shared node
            if (terminal.ParentComponent?.logicalComponent != null)
            {
                sharedNode.ConnectedComponents.Add(terminal.ParentComponent.logicalComponent);
            }
        }
    }
}

/// <summary>
/// BFS traversal from a terminal through all connected wire paths.
/// Returns all terminals reachable via wire connections.
/// </summary>
private List<ComponentTerminal> TraceFromTerminal(ComponentTerminal startTerminal)
{
    var result = new List<ComponentTerminal> { startTerminal };
    var visitedEndpoints = new HashSet<WireEndpoint>();
    var queue = new Queue<WireEndpoint>();

    // Find wire endpoints at this terminal's position
    var startEndpoints = FindEndpointsAtTerminal(startTerminal);
    foreach (var ep in startEndpoints)
    {
        queue.Enqueue(ep);
        visitedEndpoints.Add(ep);
    }

    while (queue.Count > 0)
    {
        var currentEndpoint = queue.Dequeue();

        // Get the wire this endpoint belongs to
        var wire = currentEndpoint.ParentWire;
        if (wire == null) continue;

        // Get the OTHER endpoint of the same wire
        var otherEndpoint = (wire.startEndpoint == currentEndpoint)
            ? wire.endEndpoint
            : wire.startEndpoint;

        if (otherEndpoint == null || visitedEndpoints.Contains(otherEndpoint))
            continue;

        visitedEndpoints.Add(otherEndpoint);

        // Case 1: Other endpoint is at a component terminal
        if (otherEndpoint.ConnectedTerminal != null)
        {
            if (!result.Contains(otherEndpoint.ConnectedTerminal))
            {
                result.Add(otherEndpoint.ConnectedTerminal);
            }
            // Continue searching from this terminal (there may be more wires)
            var moreEndpoints = FindEndpointsAtTerminal(otherEndpoint.ConnectedTerminal);
            foreach (var ep in moreEndpoints)
            {
                if (!visitedEndpoints.Contains(ep))
                {
                    queue.Enqueue(ep);
                    visitedEndpoints.Add(ep);
                }
            }
        }
        // Case 2: Other endpoint is snapped to another wire endpoint (junction)
        else if (otherEndpoint.SnappedToEndpoint != null)
        {
            if (!visitedEndpoints.Contains(otherEndpoint.SnappedToEndpoint))
            {
                queue.Enqueue(otherEndpoint.SnappedToEndpoint);
                visitedEndpoints.Add(otherEndpoint.SnappedToEndpoint);
            }

            // Also check for other endpoints at this junction position
            var junctionEndpoints = FindEndpointsAtPosition(otherEndpoint.GetPosition());
            foreach (var ep in junctionEndpoints)
            {
                if (!visitedEndpoints.Contains(ep))
                {
                    queue.Enqueue(ep);
                    visitedEndpoints.Add(ep);
                }
            }
        }
        // Case 3: Other endpoint is near another endpoint (position-based junction)
        else
        {
            var nearbyEndpoints = FindEndpointsAtPosition(otherEndpoint.GetPosition());
            foreach (var ep in nearbyEndpoints)
            {
                if (!visitedEndpoints.Contains(ep))
                {
                    queue.Enqueue(ep);
                    visitedEndpoints.Add(ep);
                }
            }
        }
    }

    return result;
}
```

---

## Example Trace: Parallel Circuit

### Setup
```
Components:
- Battery (12V)
- BulbA (10Ω)
- BulbB (10Ω)

Wires (Y-junction pattern):
- Wire1: Battery[+] → Junction_Top
- Wire2: Junction_Top → BulbA[left]
- Wire3: Junction_Top → BulbB[left]
- Wire4: BulbA[right] → Junction_Bottom
- Wire5: BulbB[right] → Junction_Bottom
- Wire6: Junction_Bottom → Battery[-]
```

### Path Traversal

**Starting from Battery.PositiveTerminal:**
```
BFS Queue: [Wire1.start]
Visited: {Wire1.start}

Step 1: Process Wire1.start
  → Wire1.end is at Junction_Top (snapped to Wire2.start, Wire3.start)
  → Add Wire2.start, Wire3.start to queue

Step 2: Process Wire2.start
  → Wire2.end is at BulbA.TerminalA (component terminal!)
  → Add BulbA.TerminalA to result

Step 3: Process Wire3.start
  → Wire3.end is at BulbB.TerminalA (component terminal!)
  → Add BulbB.TerminalA to result

Result: [Battery.PositiveTerminal, BulbA.TerminalA, BulbB.TerminalA]
→ All three share the same electrical node!
```

**Starting from Battery.NegativeTerminal:**
```
BFS traces through Wire6 → Junction_Bottom → Wire4/Wire5 → BulbA.TerminalB, BulbB.TerminalB

Result: [Battery.NegativeTerminal, BulbA.TerminalB, BulbB.TerminalB]
→ All three share the same electrical node!
```

### Final Node Structure
```
Node_0 (shared): Battery[+], BulbA[left], BulbB[left]
Node_1 (shared): Battery[-], BulbA[right], BulbB[right]
```

**This is correct for parallel!** Both bulbs have the same voltage across them.

---

## Architecture Integration

### Layer Separation

| Layer | Responsibility | File |
|-------|---------------|------|
| **Visual** | Wire GameObjects, endpoints, snapping | `CircuitWire.cs`, `WireEndpoint.cs` |
| **Topology** | Path discovery, terminal grouping | `JunctionTopologyManager.cs` |
| **Solver** | Nodal analysis, current/voltage | `CircuitSolver.cs` |
| **Animation** | Current flow visualization | `VisualFlowGraph.cs` |

### Data Flow

```
User creates wires
       ↓
WireEndpoint snaps to terminals or other endpoints
       ↓
CircuitSolverManager.SolveCircuit()
       ↓
JunctionTopologyManager.BuildTopology()
  └── TraceTerminalPaths()  ← NEW: Path-centric traversal
       ↓
Terminals sharing wire paths get same CircuitNode
       ↓
CircuitSolver.Solve(logicalComponents)
       ↓
Nodal analysis with correct node sharing
       ↓
UpdateComponentsFromSolver()
```

### Files Modified

**Primary Change:**
- `JunctionTopologyManager.cs` - Replace `MergeJunctionTerminalNodes()` with `TraceTerminalPaths()`

**No Changes Required:**
- `CircuitSolver.cs` - Math unchanged, just receives correct nodes
- `CircuitSolverManager.cs` - Still calls `BuildTopology()`
- `ComponentTerminalManager.cs` - Terminal creation unchanged
- `VisualFlowGraph.cs` - Animation is separate concern

---

## Junction Discovery (Still Useful)

The existing `BuildTopology()` junction discovery is **still valuable** for:

1. **Visual debugging** - See where wires meet in hierarchy
2. **Validation** - Detect floating wire endpoints
3. **Future features** - Junction components, current splitting visualization

Junction discovery runs **before** path traversal:
```csharp
public CircuitTopology BuildTopology()
{
    // 1. Discover junctions (visual/debugging)
    DiscoverJunctions();

    // 2. Trace paths and merge terminals (electrical)
    TraceTerminalPaths();

    return topology;
}
```

---

## Testing Checklist

### Series Circuit
- [ ] Battery → Wire → Bulb → Wire → Battery
- [ ] Current flows, bulb glows
- [ ] Single path, no junctions

### Series with Wire Chain
- [ ] Battery → Wire1 → Junction → Wire2 → Junction → Wire3 → Bulb → Wire4 → Battery
- [ ] Multiple wire-to-wire junctions in path
- [ ] Current still flows correctly

### Parallel Circuit (Y-Junction)
- [ ] Battery → Y-split → BulbA + BulbB → Y-join → Battery
- [ ] Both bulbs glow with equal brightness
- [ ] Current splits correctly (half to each if equal resistance)

### Mixed Circuit
- [ ] Series resistor before parallel bulbs
- [ ] Verify current values match physics

### Complex Wire Paths
- [ ] 5+ wire segments in a single path
- [ ] Multiple Y-junctions
- [ ] Asymmetric parallel branches

---

## Performance Considerations

### Time Complexity
- **TraceFromTerminal:** O(V + E) where V = endpoints, E = connections
- **TraceTerminalPaths:** O(T × (V + E)) where T = terminals
- **Typical circuit:** < 1ms for 20 components, 50 wire segments

### Space Complexity
- **visitedEndpoints:** O(E) - one entry per endpoint
- **terminalGroups:** O(T) - one entry per terminal

### Optimization Opportunities
1. Cache wire endpoint adjacency list
2. Early termination when all terminals found
3. Incremental updates on wire add/remove (future)

---

## Migration from Old Design

### Deprecated Methods
```csharp
// OLD - Remove these:
void MergeJunctionTerminalNodes(List<Junction> junctions)
ComponentTerminal GetTerminalForWireEndpoint(CircuitWire wire, WireEndpoint endpoint)
```

### New Methods
```csharp
// NEW - Add these:
void TraceTerminalPaths()
List<ComponentTerminal> TraceFromTerminal(ComponentTerminal startTerminal)
List<WireEndpoint> FindEndpointsAtTerminal(ComponentTerminal terminal)
List<WireEndpoint> FindEndpointsAtPosition(Vector3 position)
```

### Backward Compatibility
- Junction discovery still works (for debugging)
- All public APIs unchanged
- Solver input format unchanged

---

## Related Documents

### Deprecated (Historical Reference Only)
- `WIRE_JUNCTION_DESIGN.md` - Original junction-centric design
- `FIX_IMPLEMENTATION.md` - Old MergeJunctionTerminalNodes implementation
- `IMPLEMENTATION_SUMMARY.md` - Old junction graph methods
- `CODE_REVIEW_TOPOLOGY.md` - Problem analysis (still useful for context)

### Current
- `TOPOLOGY_PATH_TRAVERSAL.md` - **This document**
- `ARCHITECTURE.md` - System architecture (update with path traversal)
- `DEPENDENCY.md` - Dependency flows (update solving chain)

---

## Changelog

### v2.0 (December 2024)
- Complete redesign from junction-centric to path-centric
- BFS traversal from terminals through wire chains
- Handles arbitrary wire-to-wire junction chains
- Fixes parallel circuit connectivity

### v1.0 (Previous)
- Junction-centric merging via `MergeJunctionTerminalNodes()`
- Limited to junctions with direct component terminals
- Failed on wire-to-wire chains
