# Current Flow Animation Status

**Date**: 2025-01-11
**Status**: ⚠️ IN PROGRESS - Direction logic implemented but not yet working correctly

## Problem Statement

Current flow animation dots need to flow in the correct direction based on:
1. **Physical current flow**: Conventional current flows from positive (+) to negative (-)
2. **Wire drawing order**: User clicks terminals in specific order (start→end)
3. **Dynamic updates**: Animation should adapt when wire endpoints are moved to different terminals

## Work Completed Today

### ✅ Fixed Issues

1. **Negative Current Display** (WireValueDisplay.cs:75)
   - Changed: Display `Mathf.Abs(circuitWire.current)` instead of raw value
   - Result: Wire labels now show "0.12A" instead of "-0.12A"

2. **Sign Preservation in CurrentFlowVisualizer** (CurrentFlowVisualizer.cs:217-228)
   - Removed: `Mathf.Abs()` from `GetWireCurrentMagnitude()`
   - Kept sign for direction logic while using absolute value for magnitude checks

3. **Sign Preservation in CircuitWire** (CircuitWire.cs:480-481)
   - Removed: `Mathf.Abs()` from component current reading
   - Preserved direction information from solver

### 🔧 Attempted Fixes (Not Yet Working)

#### Approach 1: Voltage-Based Direction (Attempted)
**Logic**: Current flows from high voltage to low voltage
```csharp
// Check terminal voltages
if (startVoltage < endVoltage)
    wireCurrent = -wireCurrent; // Reverse direction
```
**Issue**: Requires solver to run first; voltages may not be updated yet

#### Approach 2: Terminal Polarity-Based Direction (Current Implementation)
**Logic**: Current flows OUTPUT terminal → INPUT terminal
```csharp
// CircuitWire.cs:488-523
bool startIsOutput = !startTerminal.isInput;
bool endIsInput = endTerminal.isInput;

if (startIsOutput && endIsInput)
    // Forward: OUTPUT→INPUT
else if (!startIsOutput && !endIsInput)
    // Reversed: INPUT→OUTPUT (apply negative sign)
```

**Terminal Definitions**:
- **Battery Red** (+): OUTPUT terminal
- **Battery Green** (-): INPUT terminal (ground)
- **Bulb/Resistor**: Both INPUT and OUTPUT terminals

**Expected Behavior**:
- Battery Red → Bulb: Forward animation (red to bulb)
- Bulb → Battery Green: Forward animation (bulb to green)
- If wire drawn backwards: Automatic reversal

**Current Status**: Logic implemented but animation still flows incorrectly

## Debug Logging Added

### ConnectTool.cs:254-267
Logs terminal assignment when wire is created:
```
🔌 WIRE CREATED: Start=LeftTerminal (Input:true), End=RightTerminal (Input:false)
   Start Component: Battery, End Component: Bulb
✅ Terminal wire created - animation should flow from START to END based on click order
```

### CircuitWire.cs:154-173
Logs terminal updates when endpoints are moved:
```
🔌 Endpoint StartEndpoint connected to terminal
  → Start endpoint connected to Battery, Terminal: LeftTerminal (IsInput: false)
  🎬 Animation direction updated: OUTPUT→INPUT (forward)
```

### CircuitWire.cs:492-517
Logs direction decision during current update:
```
🔌 Wire Wire_Battery_to_Bulb: Start=LeftTerminal (IsInput:false), End=RightTerminal (IsInput:true), Magnitude=0.120A
   ➡️ Direction: FORWARD (output→input), Final=0.120A
```

## Root Cause Analysis

### Potential Issues:

1. **Terminal Type Definitions May Be Incorrect**
   - Need to verify: Is Battery Red really marked as `isInput=false` (OUTPUT)?
   - Need to verify: Is Battery Green really marked as `isInput=true` (INPUT)?

2. **Wire Orientation vs Physical Layout**
   - Wire's start/end may not match visual left/right positions
   - LineRenderer position[0] vs position[1] may not align with terminal assignment

3. **Solver Node Ordering**
   - Solver calculates current based on NodeA→NodeB direction
   - This may not match wire's start→end orientation
   - Component current sign may already be "pre-reversed" by solver

4. **CurrentFlowVisualizer Spawn Logic**
   - Spawns dots at position[0] for positive, position[1] for negative
   - May need to spawn based on terminal type, not wire position index

## Next Steps for Tomorrow

### Investigation Needed:

1. **Verify Terminal Types**
   ```csharp
   // Check in ComponentTerminalManager or ComponentTerminal creation
   // For Battery: Red should be OUTPUT (isInput=false), Green should be INPUT (isInput=true)
   ```

2. **Check LineRenderer Position Order**
   ```csharp
   // Verify: Does position[0] correspond to startTerminal?
   // Or is there a mismatch between wire endpoints and LineRenderer positions?
   ```

3. **Test Solver Current Sign**
   ```csharp
   // Create simple circuit: Battery → Bulb → Battery
   // Log: component1.current (should be positive if flowing out)
   // Log: component2.current (should match in magnitude)
   ```

### Potential Solutions:

#### Option A: Use Actual Terminal Voltage (Post-Solve)
- Wait until solver finishes
- Compare `startTerminal.electricalNode.Voltage` vs `endTerminal.electricalNode.Voltage`
- Flow from higher voltage to lower voltage

#### Option B: Use Battery Detection
- Detect if component is Battery
- Battery output (Red/+) emits current
- All other components receive current
- Use battery position to determine "source" direction

#### Option C: Ignore Wire Orientation Entirely
- Always spawn dots at HIGH voltage terminal
- Always flow toward LOW voltage terminal
- Completely ignore wire drawing order (may confuse users)

#### Option D: Use CircuitNode Voltage Gradient
- Get voltage at both wire endpoints from solver
- Current magnitude from component
- Direction from voltage difference: `sign(V_start - V_end)`

## Files Modified Today

1. **CurrentFlowVisualizer.cs**
   - Lines 106-129: Updated flow activation logic
   - Lines 131-158: Spawn position based on direction
   - Lines 217-238: Removed Mathf.Abs() from current reading

2. **CircuitWire.cs**
   - Lines 145-173: Added terminal logging in OnEndpointConnected()
   - Lines 483-523: Terminal polarity-based direction logic
   - Lines 480-481: Preserved sign from component current

3. **WireValueDisplay.cs**
   - Line 75: Display absolute value only (no negative signs)

4. **ConnectTool.cs**
   - Lines 254-267: Added wire creation logging with terminal types

## Testing Checklist for Tomorrow

- [ ] Create Battery → Bulb → Battery circuit
- [ ] Log all terminal types (isInput values)
- [ ] Log solver current signs for both components
- [ ] Log terminal voltages after solving
- [ ] Compare wire orientation (start/end) with physical positions
- [ ] Test animation direction with different click orders
- [ ] Test endpoint dragging to new terminals
- [ ] Verify CurrentFlowVisualizer receives correct signed current

## References

- **CurrentFlowVisualizer.cs**: Animation dot spawning and movement
- **CircuitWire.cs**: Wire current calculation and direction logic
- **ComponentTerminal.cs**: Terminal definitions (isInput property)
- **ComponentTerminalManager.cs**: Terminal creation and electrical node management
- **CircuitSolver.cs**: Current calculation (proven accurate, don't modify)

---

**Conclusion**: The terminal polarity-based approach is theoretically sound, but something in the implementation chain is causing incorrect animation direction. Tomorrow's debugging should focus on verifying terminal type assignments and tracing the current sign through the entire data flow.
