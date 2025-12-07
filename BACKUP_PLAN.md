# Backup Plan: Topology System Restoration

**Created:** December 2024
**Purpose:** Instructions to restore stable version before path-centric traversal implementation

---

## Current Stable Version

| Field | Value |
|-------|-------|
| **Branch** | `feature/challenge-system` |
| **Commit Hash** | `d118540b66b34c467d293981a2221c510933a3ba` |
| **Commit Message** | "Add PowerPlant prefab, effect test scene, and circuit diagrams" |
| **Commit Date** | December 2024 |

---

## What Works in This Version

- Junction-centric topology (`MergeJunctionTerminalNodes()`)
- Series circuits with direct wire connections
- Parallel circuits when wires connect directly to component terminals
- All solver math (nodal analysis) is validated
- Visual flow animation
- Terminal snapping and wire creation

---

## Known Limitation (Why We're Upgrading)

Wire-to-wire junction chains fail to merge terminals properly. Example:

```
Battery[+] ──Wire1──●──Wire2──●──Wire3── BulbA[left]
                   J1        J2
```

J1 and J2 are wire-to-wire junctions (no component terminals), so `MergeJunctionTerminalNodes()` finds 0 terminals at these junctions and no merging happens.

---

## Restoration Instructions

### Option 1: Reset to Stable Commit (Discard All Changes)

```bash
# WARNING: This discards ALL uncommitted changes!
git reset --hard d118540b66b34c467d293981a2221c510933a3ba
```

### Option 2: Create Restore Branch (Safe)

```bash
# Create a branch at the stable commit for reference
git branch stable-before-path-traversal d118540b66b34c467d293981a2221c510933a3ba

# Later, to switch to it:
git checkout stable-before-path-traversal
```

### Option 3: Checkout Specific File (Partial Restore)

If only `JunctionTopologyManager.cs` needs to be restored:

```bash
git checkout d118540b66b34c467d293981a2221c510933a3ba -- Assets/CircuitSimulator/Scripts/Managers/JunctionTopologyManager.cs
```

### Option 4: Revert a Commit (After Path-Traversal is Committed)

If path-centric changes have been committed and need to be undone:

```bash
# Find the commit hash of the path-centric changes
git log --oneline

# Revert that specific commit (creates a new commit that undoes changes)
git revert <path-traversal-commit-hash>
```

---

## Files That Will Be Modified

The path-centric traversal implementation will modify:

| File | Change Type |
|------|-------------|
| `JunctionTopologyManager.cs` | **MAJOR** - Replace `MergeJunctionTerminalNodes()` with `TraceTerminalPaths()` |

These files remain **unchanged**:
- `CircuitSolver.cs` - Solver math stays the same
- `CircuitSolverManager.cs` - Still calls `BuildTopology()`
- `ComponentTerminalManager.cs` - Terminal creation unchanged
- `VisualFlowGraph.cs` - Animation is separate concern
- `CircuitWire.cs` - Wire visuals unchanged
- `WireEndpoint.cs` - Endpoint logic unchanged

---

## Verification After Restore

1. **Open Unity** and load `CircuitSimulator_v2.unity` scene
2. **Enter Play Mode**
3. **Create a simple series circuit**: Battery → Wire → Bulb → Wire → Battery
4. **Verify**: Bulb should glow, current should flow
5. **Check Console**: No errors about topology or null references

---

## New Design Document

The new path-centric traversal design is documented in:

**`TOPOLOGY_PATH_TRAVERSAL.md`**

This document supersedes:
- `WIRE_JUNCTION_DESIGN.md`
- `FIX_IMPLEMENTATION.md`
- `IMPLEMENTATION_SUMMARY.md`

---

## Emergency Contact

If restoration fails or you encounter issues:

1. Check `git reflog` to find previous HEAD positions
2. Use `git stash` to save current work before switching
3. Review `TOPOLOGY_PATH_TRAVERSAL.md` for design context
