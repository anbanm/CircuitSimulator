# Bulb_3 Short Circuit Fix Summary

**Date**: 2025-01-15
**Issue**: Bulb_3 showing 0A current and 0V voltage drop despite being wired into series circuit

## Problem Diagnosis

### Symptoms
- Bulb_3 displays no voltage/current labels
- Console shows: `Bulb_3: NodeA=Bulb_1_Output_-474982, NodeB=Bulb_1_Output_-474982`
- Both terminals assigned to **same electrical node** → creates 0Ω short circuit bypass
- Solver correctly treats this as short circuit: 0A current, 0V voltage drop

### Root Cause
The NODE MERGE log revealed:
```
🔗 NODE MERGE: TerminalA (was Bulb_1_Output_-474982) + TerminalB (was Bulb_1_Output_-474982) → SHARED NODE: Bulb_1_Output_-474982
```

**Both terminals already had the same node BEFORE merging!** This indicates either:
1. Terminals not properly initialized with unique nodes, OR
2. Self-connection bug (component connecting to itself)

## Fix Applied

### File: ComponentTerminal.cs (Lines 165-171)

**Added self-connection prevention**:
```csharp
void ConnectElectricalNodes(ComponentTerminal otherTerminal)
{
    string myOriginalNodeId = electricalNode?.Id ?? "NULL";
    string otherOriginalNodeId = otherTerminal.electricalNode?.Id ?? "NULL";

    // PREVENT SELF-CONNECTION BUG
    if (this.parentComponent == otherTerminal.parentComponent)
    {
        Debug.LogError($"❌ PREVENTED SELF-CONNECTION: {parentComponent.name}.{name} ({myOriginalNodeId}) to {otherTerminal.name} ({otherOriginalNodeId})");
        Debug.LogError($"   Stack trace: {System.Environment.StackTrace}");
        return;  // ← EXIT WITHOUT MERGING
    }

    var sharedNode = electricalNode ?? otherTerminal.electricalNode ?? new CircuitNode($"Shared_{GetInstanceID()}");
    electricalNode = sharedNode;
    otherTerminal.electricalNode = sharedNode;

    // IMPROVED LOGGING (now shows parent component names)
    Debug.Log($"🔗 NODE MERGE: {parentComponent.name}.{name} (was {myOriginalNodeId}) + {otherTerminal.parentComponent.name}.{otherTerminal.name} (was {otherOriginalNodeId}) → SHARED NODE: {sharedNode.Id}");
    // ...
}
```

### What This Fix Does

1. **Prevents Invalid Connections**:
   - Blocks wires from connecting a component's terminal to another terminal on the same component
   - Example: Cannot connect Bulb_3.TerminalA to Bulb_3.TerminalB

2. **Better Diagnostics**:
   - Logs now show: `"Bulb_3.TerminalA"` instead of just `"TerminalA"`
   - Stack traces reveal where invalid connections are attempted
   - Easier to trace which component has the issue

3. **Error Detection**:
   - If self-connection bug exists, we'll now see the error immediately
   - Logs will show exact stack trace of the faulty connection attempt

## Expected Behavior After Fix

### If Self-Connection Was the Bug:
- Console will show: `❌ PREVENTED SELF-CONNECTION: Bulb_3.TerminalA to TerminalB`
- Stack trace will reveal the source of the buggy connection
- Bulb_3's terminals will remain on separate nodes
- Circuit will solve correctly with proper current flow

### If Terminals Weren't Initialized:
- No error logs (because prevention check won't trigger)
- Need to investigate terminal initialization in ComponentTerminalManager
- May need to explicitly call `InitializeTerminal()` for scene-loaded terminals

## Testing Instructions

1. **In Unity**: Return to Play Mode (or re-enter if already playing)
2. **Press Space** or click "Solve Circuit" to re-solve
3. **Check Console** for one of two outcomes:

   **Outcome A - Self-Connection Detected**:
   ```
   ❌ PREVENTED SELF-CONNECTION: Bulb_3.TerminalA (Bulb_1_Output_-474982) to TerminalB (Bulb_1_Output_-474982)
      Stack trace: ...
   ```
   → This confirms the bug and shows where it's coming from

   **Outcome B - No Error, But Still Short Circuit**:
   ```
   🔗 NODE MERGE: Bulb_2.TerminalA + Bulb_3.TerminalB → ...
   🔗 NODE MERGE: Bulb_3.TerminalA + Bulb_1.TerminalB → ...
   📦 Component Bulb_3: NodeA=Node2, NodeB=Node3
   ```
   → Terminals initialized correctly, issue is elsewhere

4. **Check Bulb_3 Labels**: Should now display voltage/current values
5. **Expected Circuit Values** (series: 12V, 4Ω+2Ω+2Ω = 8Ω total):
   - Battery: 12V, 1.5A
   - Bulb_1 (2Ω): 3V, 1.5A
   - Bulb_2 (2Ω): 3V, 1.5A
   - Bulb_3 (2Ω): 3V, 1.5A (← should NOT be 0V, 0A)

## Next Steps If Fix Doesn't Work

If Bulb_3 still shows short circuit after this fix:

1. **Check wire endpoints in scene**:
   - Inspect Wire_Bulb_2_to_Bulb_3 startEndpoint/endEndpoint
   - Inspect Wire_Bulb_3_to_Bulb_1 startEndpoint/endEndpoint
   - Verify they connect to different Bulb_3 terminals

2. **Add terminal initialization logging**:
   - Log when ComponentTerminal.Start() runs
   - Log when CreateElectricalNode() assigns initial nodes

3. **Check ComponentTerminalManager**:
   - Verify scene-loaded terminals get InitializeTerminal() called
   - Or verify Start() properly initializes them

## Files Modified

- `Assets/Scripts/Components/ComponentTerminal.cs` (Lines 165-171, 178)
- `BULB3_DEBUG_PLAN.md` (Created - detailed investigation notes)
- `BULB3_FIX_SUMMARY.md` (This file)
