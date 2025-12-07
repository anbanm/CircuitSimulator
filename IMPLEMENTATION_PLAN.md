# Path-Centric Traversal Implementation Plan

**Date:** December 2024
**Status:** EXECUTING

---

## Overview

Replace `MergeJunctionTerminalNodes()` with `TraceTerminalPaths()` in `JunctionTopologyManager.cs`.

---

## Implementation Steps

### Step 1: Add Helper Methods

Add these helper methods to `JunctionTopologyManager`:

```csharp
/// <summary>
/// Find all wire endpoints connected to a terminal's position
/// </summary>
List<WireEndpoint> FindEndpointsAtTerminal(ComponentTerminal terminal)

/// <summary>
/// Find all wire endpoints at a given position (within tolerance)
/// </summary>
List<WireEndpoint> FindEndpointsAtPosition(Vector3 position, float tolerance = 0.2f)

/// <summary>
/// Get all component terminals in the scene
/// </summary>
List<ComponentTerminal> FindAllComponentTerminals()
```

### Step 2: Implement TraceFromTerminal() BFS

```csharp
/// <summary>
/// BFS traversal from a terminal through all connected wire paths.
/// Returns all terminals reachable via wire connections.
/// </summary>
List<ComponentTerminal> TraceFromTerminal(ComponentTerminal startTerminal)
{
    var result = new List<ComponentTerminal> { startTerminal };
    var visitedEndpoints = new HashSet<WireEndpoint>();
    var queue = new Queue<WireEndpoint>();

    // Seed BFS with endpoints at this terminal
    var startEndpoints = FindEndpointsAtTerminal(startTerminal);
    foreach (var ep in startEndpoints)
    {
        queue.Enqueue(ep);
        visitedEndpoints.Add(ep);
    }

    while (queue.Count > 0)
    {
        var currentEndpoint = queue.Dequeue();
        var wire = currentEndpoint.ParentWire;
        if (wire == null) continue;

        // Get OTHER endpoint of same wire
        var otherEndpoint = (wire.startEndpoint == currentEndpoint)
            ? wire.endEndpoint : wire.startEndpoint;

        if (otherEndpoint == null || visitedEndpoints.Contains(otherEndpoint))
            continue;

        visitedEndpoints.Add(otherEndpoint);

        // Case 1: Other endpoint at a terminal
        if (otherEndpoint.ConnectedTerminal != null)
        {
            if (!result.Contains(otherEndpoint.ConnectedTerminal))
                result.Add(otherEndpoint.ConnectedTerminal);

            // Continue from this terminal
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
        // Case 2: Snapped to another endpoint (wire-to-wire junction)
        else if (otherEndpoint.SnappedToEndpoint != null)
        {
            if (!visitedEndpoints.Contains(otherEndpoint.SnappedToEndpoint))
            {
                queue.Enqueue(otherEndpoint.SnappedToEndpoint);
                visitedEndpoints.Add(otherEndpoint.SnappedToEndpoint);
            }

            // Also check position-based
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
        // Case 3: Position-based junction
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

### Step 3: Implement TraceTerminalPaths()

```csharp
/// <summary>
/// Traces all wire paths from each component terminal using BFS.
/// Terminals connected via wire paths share the same electrical node.
/// </summary>
void TraceTerminalPaths()
{
    var allTerminals = FindAllComponentTerminals();
    var terminalGroups = new Dictionary<ComponentTerminal, CircuitNode>();

    Debug.Log($"[TOPOLOGY] TraceTerminalPaths: Processing {allTerminals.Count} terminals");

    foreach (var startTerminal in allTerminals)
    {
        if (terminalGroups.ContainsKey(startTerminal))
            continue;

        // Create new electrical node for this group
        var sharedNode = new CircuitNode($"Node_{startTerminal.name}_{startTerminal.GetInstanceID()}");

        // BFS to find all connected terminals
        var connectedTerminals = TraceFromTerminal(startTerminal);

        Debug.Log($"[TOPOLOGY] Terminal group starting at {startTerminal.name}: {connectedTerminals.Count} terminals connected");

        // Merge all terminals to share same node
        foreach (var terminal in connectedTerminals)
        {
            terminal.electricalNode = sharedNode;
            terminalGroups[terminal] = sharedNode;

            if (terminal.ParentComponent?.logicalComponent != null)
            {
                if (!sharedNode.ConnectedComponents.Contains(terminal.ParentComponent.logicalComponent))
                {
                    sharedNode.ConnectedComponents.Add(terminal.ParentComponent.logicalComponent);
                }
            }
        }
    }

    Debug.Log($"[TOPOLOGY] TraceTerminalPaths complete: {terminalGroups.Count} terminals merged");
}
```

### Step 4: Update BuildTopology()

Replace `MergeJunctionTerminalNodes(topology.junctions)` with `TraceTerminalPaths()`:

```csharp
public CircuitTopology BuildTopology()
{
    // ... existing junction discovery code ...

    // 5. Build electrical graph (wire-based connectivity)
    topology.BuildElectricalGraph();

    // 6. NEW: Path-centric terminal merging (replaces MergeJunctionTerminalNodes)
    TraceTerminalPaths();

    Debug.Log($"[TOPOLOGY] ✅ Discovered {topology.junctions.Count} junctions");

    return topology;
}
```

### Step 5: Keep MergeJunctionTerminalNodes() as Deprecated

Mark the old method as deprecated but keep for reference:

```csharp
/// <summary>
/// DEPRECATED: Use TraceTerminalPaths() instead.
/// This method fails on wire-to-wire junction chains.
/// </summary>
[System.Obsolete("Use TraceTerminalPaths() instead")]
void MergeJunctionTerminalNodes(List<Junction> junctions)
{
    // ... existing code ...
}
```

---

## Validation Checklist

### Unit Tests
- [ ] Series circuit: Battery → Wire → Bulb → Wire → Battery
- [ ] Wire chain: Battery → Wire1 → Junction → Wire2 → Bulb
- [ ] Parallel circuit: Battery → Y-split → BulbA + BulbB → Y-join → Battery
- [ ] Mixed circuit: Series resistor before parallel bulbs

### Integration Tests
- [ ] Circuit solves correctly in Unity
- [ ] Current values match expected physics
- [ ] Visual flow animation works
- [ ] No null reference errors

### Regression Tests
- [ ] Existing simple circuits still work
- [ ] Terminal snapping still works
- [ ] Wire creation still works

---

## Files Modified

| File | Change |
|------|--------|
| `JunctionTopologyManager.cs` | Add TraceTerminalPaths(), TraceFromTerminal(), helper methods |

## Files NOT Modified

- `CircuitSolver.cs` - Solver math unchanged
- `CircuitSolverManager.cs` - Still calls BuildTopology()
- `ComponentTerminalManager.cs` - Terminal creation unchanged
- `WireEndpoint.cs` - Endpoint logic unchanged
- `CircuitWire.cs` - Wire visuals unchanged

---

## Rollback Plan

If issues occur, restore from commit `d118540`:

```bash
git checkout d118540 -- Assets/CircuitSimulator/Scripts/Managers/JunctionTopologyManager.cs
```
