# Bulb_3 Short Circuit Debug Plan

**Problem**: Bulb_3 has both terminals assigned to the same electrical node (Bulb_1_Output_-474982), causing a short circuit.

## Evidence

### Console Logs
```
📦 Component Bulb_3: NodeA=Bulb_1_Output_-474982, NodeB=Bulb_1_Output_-474982
✅ Updated Bulb_3: Current=0,0000A, VoltageDrop=0,0000V
🔗 NODE MERGE: TerminalA (was Bulb_1_Output_-474982) + TerminalB (was Bulb_1_Output_-474982) → SHARED NODE: Bulb_1_Output_-474982
```

### Physical Wiring (CORRECT)
- Wire_Bulb_2_to_Bulb_3: Bulb_2.TerminalA → Bulb_3.TerminalB ✓
- Wire_Bulb_3_to_Bulb_1: Bulb_3.TerminalA → Bulb_1.TerminalB ✓

### Visual Flow Graph (CORRECT)
```
Battery_0.PositiveTerminal → Bulb_2.TerminalB
Bulb_2.TerminalA → Bulb_3.TerminalB
Bulb_3.TerminalA → Bulb_1.TerminalB
Bulb_1.TerminalA → Battery_0.NegativeTerminal
```

## Root Cause Analysis

### Terminal Initialization Flow (Scene-Loaded Components)
1. **Scene loads** → Terminal GameObjects exist but `electricalNode` is null (not serialized)
2. **ComponentTerminalManager.SetupComponentTerminals()** runs:
   - Finds existing terminal children via `GetComponentsInChildren<ComponentTerminal>()`
   - Registers them in `componentTerminals` dictionary
   - **EARLY RETURN** without calling `InitializeTerminal()` → `parentComponent` remains null
3. **ComponentTerminal.Start()** runs:
   - Checks `if (parentComponent == null)` → TRUE
   - Calls `parentComponent = GetComponentInParent<CircuitComponent3D>()`
   - Calls `CreateElectricalNode()` → Creates unique node (e.g., "Bulb_3_Output_-478492")
4. **Wire connection** (`OnEndpointConnected()`):
   - Calls `startTerminal.ConnectToTerminal(endTerminal, this)`
   - `ConnectElectricalNodes()` merges nodes

### Expected Node Assignments (Series Circuit)
- **Node1**: Battery_0.NegativeTerminal + Bulb_1.TerminalA
- **Node2**: Bulb_1.TerminalB + Bulb_3.TerminalA
- **Node3**: Bulb_3.TerminalB + Bulb_2.TerminalA
- **Node4**: Bulb_2.TerminalB + Battery_0.PositiveTerminal

### Actual Node Assignments (BUGGY)
- Bulb_3.TerminalA: **Bulb_1_Output_-474982** (Node2) ✓ CORRECT
- Bulb_3.TerminalB: **Bulb_1_Output_-474982** (Node2) ✗ WRONG! Should be Node3

## Hypothesis: OnEndpointConnected() Called Multiple Times

The NODE MERGE log shows **both terminals already had the same node BEFORE merging**. This suggests:

1. Wire_Bulb_3_to_Bulb_1 connects first:
   - Merges Bulb_3.TerminalA with Bulb_1.TerminalB → Both get node "Bulb_1_Output_-474982"
2. Wire_Bulb_2_to_Bulb_3 connects:
   - Should merge Bulb_2.TerminalA with Bulb_3.TerminalB
   - But Bulb_3.TerminalB somehow ALSO gets node "Bulb_1_Output_-474982"

**Possible causes**:
- Scene-loaded wires call `OnEndpointConnected()` during initialization
- Wire reconnection during scene setup
- Incorrect terminal reference in wire endpoint

## Investigation Steps

1. **Add debug logging to ComponentTerminal.CreateElectricalNode()**:
   - Log when each terminal creates its initial node
   - Log the unique node ID and terminal name

2. **Add debug logging to ComponentTerminal.ConnectElectricalNodes()**:
   - Log the parent component names, not just terminal names
   - Log BEFORE merge: `"Connecting {this.parentComponent.name}.{this.name} (node: {this.electricalNode.Id}) to {otherTerminal.parentComponent.name}.{otherTerminal.name} (node: {otherTerminal.electricalNode.Id})"`

3. **Add call stack tracking**:
   - Log stack trace when ConnectElectricalNodes() is called with same-component terminals

4. **Check wire endpoint assignments**:
   - Verify Wire_Bulb_2_to_Bulb_3.startEndpoint connects to correct terminal
   - Verify Wire_Bulb_2_to_Bulb_3.endEndpoint connects to correct terminal

## Fix Strategy

### Option A: Prevent Self-Connection
Add validation in `ComponentTerminal.ConnectToTerminal()`:
```csharp
public void ConnectToTerminal(ComponentTerminal otherTerminal, CircuitWire wire)
{
    if (otherTerminal == null || wire == null) return;

    // PREVENT SELF-CONNECTION BUG
    if (this.parentComponent == otherTerminal.parentComponent)
    {
        Debug.LogError($"❌ PREVENTED SELF-CONNECTION: {parentComponent.name}.{name} to {otherTerminal.name}");
        return;
    }

    // Connect the electrical nodes
    ConnectElectricalNodes(otherTerminal);
    // ...
}
```

### Option B: Clear Nodes Before Reconnection
Add node clearing in `BuildLogicalCircuit()`:
```csharp
// Before building, reset all terminal nodes to their original states
foreach (var comp3D in circuitManager.Components)
{
    var terminals = terminalManager?.GetComponentTerminals(comp3D);
    foreach (var terminal in terminals)
    {
        // Reset to unique node
        terminal.ResetElectricalNode();
    }
}
```

### Option C: Fix Wire Endpoint References
Check if wire endpoints are connecting to wrong terminals during scene load.

## Next Action

**Immediate**: Add detailed logging to ComponentTerminal.ConnectElectricalNodes() to show parent component names, then trigger a circuit solve to see the merge sequence.
