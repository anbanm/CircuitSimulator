# Current Flow Animation Status

**Date**: 2025-01-15
**Status**: ✅ **COMPLETE** - Flow direction deterministic and correct

## Problem Solved

### Original Issue
Wire current flow animation direction was **non-deterministic** - changed randomly on every solve or component click due to two conflicting systems:

1. **Electrical Solver** - Merged connected terminals into shared nodes for calculations
2. **OrientWireTowardSource()** - Tried to guess flow direction from node polarity

**Result**: Wire endpoints would swap unpredictably, causing dots to animate in wrong/changing directions.

### Root Cause
```
User's Circuit: Battery → Bulb1 → Bulb2 → Battery
Physical Wires: Wire_A, Wire_B, Wire_C

Electrical Solver (CORRECT for calculations):
  Merged Nodes: {Node_1, Node_2, Node_3}
  Wire_B endpoints: both → Node_2 (merged!)

Visual Animation (WRONG approach):
  Tried to use merged nodes to determine flow direction
  BFS couldn't traverse because it saw "short circuit" (both endpoints → same node)
  OrientWireTowardSource() guessed based on polarity → fought with BFS
```

## Solution: VisualFlowGraph Architecture

### New File: `VisualFlowGraph.cs`
**Location**: `Assets/Scripts/Managers/VisualFlowGraph.cs`

**Purpose**: Separate visual flow topology from electrical simulation

**Key Components**:
1. **WireConnection Class** - Component-to-component edges
   - Tracks: `wire`, `fromComponent`, `fromTerminal`, `toComponent`, `toTerminal`
   - No reference to electrical nodes

2. **BuildFromScene()** - Constructs visual graph
   - Reads wire hierarchy (not merged nodes)
   - Creates bidirectional connections for each wire
   - Deterministic sorting by wire name

3. **AssignFlowDirectionsFromBattery()** - BFS traversal
   - Starts from Battery PositiveTerminal
   - Traverses component-to-component connections
   - Current flows THROUGH components (enters one terminal, exits another)
   - Sets `isStart` flags on wire endpoints

### Integration Points

**CircuitSolverManager.cs** (Lines 39-40, 50-51, 424-456):
```csharp
// Field
private VisualFlowGraph visualFlowGraph;

// Initialize
visualFlowGraph = new VisualFlowGraph();

// Called after circuit solve
void AssignWireFlowDirections()
{
    visualFlowGraph.BuildFromScene(circuitManager.Wires);
    visualFlowGraph.AssignFlowDirectionsFromBattery(battery, terminalManager);
}
```

**CircuitWire.cs** (Lines 78-81):
```csharp
// DISABLED legacy code that fought with VisualFlowGraph
// OrientWireTowardSource();  // <-- Commented out!
```

**CurrentFlowVisualizer.cs** (Lines 155-184):
```csharp
// Uses isStart flags set by VisualFlowGraph
if (circuitWire.startEndpoint.isStart)
    flowForward = true;  // Flow from start → end
else if (circuitWire.endEndpoint.isStart)
    flowForward = false; // Flow from end → start
```

## Technical Details

### BFS Semantics
Current flows **THROUGH** components, not just between nodes:

```
Battery+ → [enters Bulb1.TerminalB] → Bulb1 → [exits Bulb1.TerminalA] → Wire → ...
```

**Queue State**:
- `(component, exitTerminal)` - Where current will EXIT from
- BFS processes connections FROM the exit terminal
- Next component: enters via `toTerminal`, exits via OTHER terminal

### Deterministic Ordering
**Problem**: Dictionary iteration order is non-deterministic in C#
**Solution**: Sort by name at every decision point

```csharp
// Sort wire connections
connections.Sort((a, b) => string.Compare(a.wire.name, b.wire.name));

// Sort terminal selection
var exitTerminal = destTerminals
    .Where(t => t != connection.toTerminal)
    .OrderBy(t => t.name)
    .FirstOrDefault();
```

## Verification Results

### Test Case: Battery → Bulb1 → Bulb2 → Battery

**BFS Log Output** (consistent every solve):
```
🔋 Starting BFS from Battery+ (PositiveTerminal)
  📍 Visiting Battery_0, exiting from PositiveTerminal
    🔗 Wire_Battery_0_to_Bulb_2: START endpoint is flow start
  📍 Visiting Bulb_2, exiting from TerminalA
    🔗 Wire_Bulb_1_to_Bulb_2: END endpoint is flow start
  📍 Visiting Bulb_1, exiting from TerminalA
    🔗 Wire_Battery_0_to_Bulb_1: END endpoint is flow start
✅ Flow directions assigned: 3 wires processed, 3 components visited
```

**Before Fix**:
- Clicking components → direction changes randomly
- BFS processed 0-4 wires inconsistently
- OrientWireTowardSource logs: "⚠️ SWAPPING! Wire is backwards"

**After Fix**:
- Clicking components → direction stays consistent ✅
- BFS always processes exactly 3 wires in same order ✅
- No more endpoint swapping ✅

## Benefits

✅ **Deterministic** - Same flow direction every solve
✅ **Correct Topology** - Follows actual circuit connections
✅ **Clean Separation** - Visual concerns separate from electrical solver
✅ **Handles Reconnection** - Wires can be reconnected in any order
✅ **No Fighting Systems** - Single source of truth for flow direction

## Files Modified

1. **NEW**: `Assets/Scripts/Managers/VisualFlowGraph.cs` (259 lines)
2. **MODIFIED**: `Assets/Scripts/Managers/CircuitSolverManager.cs`
   - Added VisualFlowGraph integration (~30 lines)
   - Simplified AssignWireFlowDirections from ~160 to ~30 lines
3. **MODIFIED**: `Assets/Scripts/Components/CircuitWire.cs`
   - Disabled OrientWireTowardSource() (line 81)
4. **MODIFIED**: `Assets/Scripts/Managers/ComponentTerminalManager.cs`
   - Fixed scene-loaded terminal registration (lines 39-47)
5. **MODIFIED**: `Assets/Scripts/UI/CurrentFlowVisualizer.cs`
   - Uses isStart flags from VisualFlowGraph (lines 155-184)

## Commit

**Git Hash**: `e942e5b`
**Commit Message**: "✨ FEATURE: Visual Flow Graph - Deterministic Current Animation Direction"
**Date**: 2025-01-15

## Future Improvements

### Optional Enhancements
- [ ] Support for multiple batteries (currently assumes single battery)
- [ ] Handle parallel branches (currently works, could optimize traversal)
- [ ] Visualize BFS traversal order in debug mode
- [ ] Export visual graph as GraphViz DOT file for debugging

### Not Needed
- ~~Node-based flow detection~~ - VisualFlowGraph is topology-based ✅
- ~~Polarity detection~~ - Handled by BFS from Battery+ ✅
- ~~Endpoint swapping~~ - BFS sets flags directly ✅

---

**Status**: Production-ready ✅
**Regression Risk**: Low (legacy code disabled, new system isolated)
**Performance Impact**: Minimal (BFS runs once per solve, O(components + wires))
