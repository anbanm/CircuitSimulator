# Circuit Simulator v2.1 - Comprehensive Test Plan

**Date**: 2025-01-26
**Unity Version**: 6000.0.32f1
**Scene**: CircuitSimulator_v2.unity
**Target**: Grade 7-12 Physics Education

---

## Test Environment Setup

### Prerequisites
1. Unity Editor open with CircuitSimulator_v2 scene loaded
2. Enter Play Mode
3. Camera positioned to view workspace (Ground plane visible)
4. All managers initialized (check Console for "✅" messages)

### Keyboard Controls Reference
- **B** = Battery (12V, red cube)
- **R** = Resistor (10Ω, orange cylinder)
- **L** = Bulb (5Ω, yellow sphere)
- **C** = Connect Mode (wire creation)
- **V** = Select Mode (component selection)
- **Space** = Solve Circuit
- **Delete/X** = Delete selected item
- **Right-click** = Edit properties
- **E** = Edit properties (when selected)
- **Escape** = Deselect

---

## SECTION 1: Bug Fix Verification Tests

### Test 1.1: Component Placement (Camera-Relative Spawning)

**What We're Testing**: Components spawn in front of camera, not at origin

**Test Steps**:
1. Enter Play Mode
2. Press **B** key (Battery)
3. Observe battery spawn location
4. Press **R** key (Resistor)
5. Press **L** key (Bulb)

**Expected Results**:
- ✅ Battery spawns ~8 units in front of camera
- ✅ Resistor spawns to the left of battery
- ✅ Bulb spawns to the right of battery
- ✅ All components at Y=0.5 (workspace height)
- ✅ Components arranged in 3-column grid facing camera
- ❌ Components do NOT spawn at origin (0, 0, 0)

**Pass Criteria**: All components visible in camera view without moving camera

---

### Test 1.2: Cascade Deletion Prevention

**What We're Testing**: Deleting one component doesn't delete others

**Test Steps**:
1. Create Battery (B key)
2. Create Resistor (R key)
3. Create Bulb (L key)
4. Click to select Battery only
5. Press **Delete** or **X** key
6. Observe what gets deleted

**Expected Results**:
- ✅ Only Battery is deleted
- ✅ Resistor remains in scene
- ✅ Bulb remains in scene
- ❌ No cascade deletion of multiple components

**Pass Criteria**: Exactly 1 component deleted, others remain

---

### Test 1.3: Wire Preservation on Component Deletion

**What We're Testing**: Wires stay when component deleted (become dangling)

**Test Steps**:
1. Create Battery (B key)
2. Create Resistor (R key)
3. Press **C** key (Connect Mode)
4. Click Battery → Click Resistor (creates wire)
5. Press **V** key (Select Mode)
6. Click Battery to select
7. Press **Delete**

**Expected Results**:
- ✅ Battery is deleted
- ✅ Wire remains in scene (becomes dangling/inactive)
- ✅ Resistor remains in scene
- ✅ Wire shows 0A current (not part of active circuit)

**Pass Criteria**: Wire preserved, can be reconnected to new battery

---

### Test 1.4: Property Popup (Right-Click Editing)

**What We're Testing**: Right-click opens property editor without pre-selection

**Test Steps**:
1. Create Battery (B key)
2. WITHOUT clicking to select, **Right-click** directly on Battery
3. Observe popup appearance
4. Close popup (Cancel button)
5. Create Resistor (R key)
6. Right-click on Resistor

**Expected Results**:
- ✅ Popup appears immediately on right-click
- ✅ Battery popup shows: Title "Edit Battery", Voltage field (12V)
- ✅ Resistor popup shows: Title "Edit Resistor", Resistance field (10Ω)
- ✅ Popup positioned near component, facing camera
- ✅ Apply and Cancel buttons visible

**Pass Criteria**: Popup appears on first right-click, shows correct fields

---

## SECTION 2: Grade 7 Physics - Series Circuit Tests

### Test 2.1: Simple Series Circuit (Battery → Resistor)

**What We're Testing**: Basic Ohm's Law (V = IR)

**Circuit Setup**:
1. Battery: 12V (default)
2. Resistor: 10Ω (default)
3. Connect Battery → Resistor → Battery (complete loop)
4. Press **Space** to solve

**Expected Results (Ohm's Law)**:
- Current: I = V/R = 12V / 10Ω = **1.2A**
- ✅ Battery shows: V=12V, I=1.2A
- ✅ Resistor shows: R=10Ω, I=1.2A, V=12V
- ✅ Current same throughout (Kirchhoff's Current Law)

**Educational Goal**: M2 Misconception - Current NOT "used up" in resistor

**Pass Criteria**: Current = 1.2A throughout circuit

---

### Test 2.2: Series Circuit with Bulb (Battery → Resistor → Bulb)

**What We're Testing**: Voltage divides, current stays constant

**Circuit Setup**:
1. Battery: 12V
2. Resistor: 20Ω (right-click, edit to 20)
3. Bulb: 50Ω (right-click, edit to 50)
4. Wire: Battery+ → Resistor → Bulb → Battery-
5. Press **Space** to solve

**Expected Results (Kirchhoff's Voltage Law)**:
- Total Resistance: R_total = 20Ω + 50Ω = 70Ω
- Total Current: I = 12V / 70Ω = **0.171A** (0.17A rounded)
- Voltage across Resistor: V_R = 0.171A × 20Ω = **3.43V**
- Voltage across Bulb: V_L = 0.171A × 50Ω = **8.57V**
- Check: 3.43V + 8.57V = 12V ✅

**Expected Display**:
- ✅ Current same everywhere: **~0.17A**
- ✅ Resistor voltage drop: **~3.4V**
- ✅ Bulb voltage drop: **~8.6V**
- ✅ Voltage drops add to 12V (battery voltage)

**Educational Goal**: Voltage divides in series, current constant

**Pass Criteria**: I=0.17A throughout, voltages add to 12V

---

### Test 2.3: Series Circuit Current Flow Animation

**What We're Testing**: Visual current flow dots on wires

**Circuit Setup**: Use same circuit as Test 2.2

**Test Steps**:
1. After circuit solved, observe wires
2. Look for animated cyan dots moving along wires

**Expected Results**:
- ✅ Cyan dots appear on all 3 wires
- ✅ Dots move at same speed (constant current)
- ✅ Speed proportional to 0.17A (slow/medium speed)
- ✅ Dots flow from Battery+ through circuit to Battery-

**Educational Goal**: Make current flow VISIBLE to students

**Pass Criteria**: Animated dots visible, moving continuously

---

## SECTION 3: Grade 7 Physics - Parallel Circuit Tests

### Test 3.1: Simple Parallel Circuit (Battery → R1 || R2)

**What We're Testing**: Current splits, voltage stays same

**Circuit Setup**:
1. Battery: 12V
2. Resistor 1: 10Ω
3. Resistor 2: 10Ω
4. Wire 1: Battery+ → Junction (split point)
5. Wire 2: Junction → Resistor 1 → Battery-
6. Wire 3: Junction → Resistor 2 → Battery-
7. Press **Space** to solve

**Expected Results (Parallel Resistance)**:
- Equivalent Resistance: 1/R_eq = 1/10 + 1/10 = 0.2, so R_eq = 5Ω
- Total Current: I_total = 12V / 5Ω = **2.4A**
- Current through R1: I_R1 = 12V / 10Ω = **1.2A**
- Current through R2: I_R2 = 12V / 10Ω = **1.2A**
- Check: 1.2A + 1.2A = 2.4A ✅

**Expected Display**:
- ✅ Wire from battery: **I = 2.4A**
- ✅ Wire through R1: **I = 1.2A**
- ✅ Wire through R2: **I = 1.2A**
- ✅ Both resistors: **V = 12V** (same as battery)

**Educational Goal**: Current SPLITS in parallel, voltage SAME

**Pass Criteria**: Currents add correctly, voltages equal battery

---

### Test 3.2: Parallel Animation (Current Splitting)

**What We're Testing**: Visual current splitting at junction

**Circuit Setup**: Use same circuit as Test 3.1

**Expected Results**:
- ✅ Fast dots on battery wire (2.4A = high current)
- ✅ Medium dots on R1 wire (1.2A)
- ✅ Medium dots on R2 wire (1.2A)
- ✅ Dots appear to "split" at junction

**Educational Goal**: See current dividing visually

**Pass Criteria**: Different animation speeds match current values

---

## SECTION 4: Edge Cases and Validation

### Test 4.1: Open Circuit (Incomplete Path)

**What We're Testing**: No current flows in incomplete circuit

**Circuit Setup**:
1. Battery: 12V
2. Resistor: 10Ω
3. Wire ONLY: Battery+ → Resistor (no return path)
4. Press **Space**

**Expected Results**:
- ✅ Warning message: "Open circuit" or "Incomplete circuit"
- ✅ All currents = 0A
- ✅ No current flow animation
- ❌ Circuit solver should NOT crash

**Educational Goal**: M1 Misconception - Current needs complete path

**Pass Criteria**: Validation error shown, no crash

---

### Test 4.2: Short Circuit (Wire Only)

**What We're Testing**: Extremely high current warning

**Circuit Setup**:
1. Battery: 12V
2. Wire ONLY: Battery+ → Battery- (no resistor)
3. Press **Space**

**Expected Results**:
- ⚠️ Warning: "Short circuit detected"
- ⚠️ Current very high (thousands of amps)
- ✅ System handles gracefully (no crash)

**Educational Goal**: Safety - short circuits are dangerous

**Pass Criteria**: Warning shown, system stable

---

### Test 4.3: Multiple Property Edits

**What We're Testing**: Property changes update circuit correctly

**Test Steps**:
1. Create Battery → Resistor circuit
2. Right-click Battery, change voltage to **6V**
3. Click Apply
4. Press **Space** to re-solve
5. Right-click Resistor, change resistance to **30Ω**
6. Click Apply
7. Press **Space**

**Expected Results**:
- After Step 4: I = 6V / 10Ω = **0.6A**
- After Step 7: I = 6V / 30Ω = **0.2A**
- ✅ Values update immediately after solve
- ✅ Animations reflect new current

**Pass Criteria**: Circuit responds to property changes

---

## SECTION 5: User Interaction Tests

### Test 5.1: Mode Switching (C and V Keys)

**Test Steps**:
1. Press **C** key
2. Observe UI mode indicator
3. Press **V** key
4. Observe mode change

**Expected Results**:
- ✅ C key → Connect Mode active
- ✅ V key → Select Mode active
- ✅ Mode indicator updates
- ✅ Wire preview appears in Connect Mode

**Pass Criteria**: Mode switches work, visual feedback clear

---

### Test 5.2: Wire Connection Flow

**Test Steps**:
1. Create Battery and Resistor
2. Press **C** (Connect Mode)
3. Click Battery (first endpoint)
4. Move mouse toward Resistor
5. Click Resistor (second endpoint)

**Expected Results**:
- ✅ Wire preview follows cursor after first click
- ✅ Wire snaps to terminal on second click
- ✅ Wire registered with circuit system
- ✅ Can repeat for multiple wires

**Pass Criteria**: Wire creation intuitive, preview visible

---

### Test 5.3: Camera Controls

**Test Steps**:
1. **Mouse Wheel**: Scroll up and down
2. **Right Drag**: Rotate camera
3. **WASD**: Move camera position
4. **F key**: Focus on circuit
5. **R key**: Reset camera

**Expected Results**:
- ✅ Zoom in/out smooth
- ✅ Rotation orbits around circuit
- ✅ WASD moves camera
- ✅ F centers view on components
- ✅ R returns to default position

**Pass Criteria**: All camera controls functional

---

## SECTION 6: Educational Misconception Detection

### Test 6.1: M2 Misconception - Current Attenuation

**Misconception**: Students think current "gets used up" in components

**Test Scenario**: Series circuit Battery → R1 → R2 → Battery

**Validation**:
- Measure current at 3 points: after battery, after R1, after R2
- ✅ All three currents MUST be identical
- ✅ Display should show same current value everywhere

**Expected Message**: "✅ Current is the SAME throughout the series circuit!"

---

### Test 6.2: M1 Misconception - Sink Model

**Misconception**: Students think one wire is enough (current doesn't return)

**Test Scenario**: Battery with only ONE wire connected

**Validation**:
- ✅ Circuit validator detects open circuit
- ✅ Error message: "Incomplete circuit - electricity needs a complete path!"
- ✅ No current flows

**Expected Message**: "❌ Circuit not complete - add return path!"

---

### Test 6.3: M8 Misconception - Constant Current Source

**Misconception**: Students think battery provides constant current (not voltage)

**Test Scenario**: Battery → R1 (10Ω), then add R2 (10Ω) in series

**Validation**:
- First circuit: I = 12V/10Ω = 1.2A
- Second circuit: I = 12V/20Ω = 0.6A
- ✅ Current DECREASES when resistance increases
- ✅ Voltage stays at 12V (battery property)

**Expected Message**: "💡 Battery provides constant VOLTAGE, not current!"

---

## Test Execution Checklist

### Pre-Test
- [ ] Unity Editor open
- [ ] CircuitSimulator_v2 scene loaded
- [ ] Play Mode entered
- [ ] Console cleared (no errors on start)

### Test Execution Order
1. [ ] Section 1: Bug Fix Verification (Tests 1.1-1.4)
2. [ ] Section 2: Series Circuits (Tests 2.1-2.3)
3. [ ] Section 3: Parallel Circuits (Tests 3.1-3.2)
4. [ ] Section 4: Edge Cases (Tests 4.1-4.3)
5. [ ] Section 5: User Interaction (Tests 5.1-5.3)
6. [ ] Section 6: Misconception Detection (Tests 6.1-6.3)

### Post-Test
- [ ] Exit Play Mode
- [ ] Review Console for errors
- [ ] Document any failures
- [ ] Note performance issues

---

## Success Criteria

**Critical (Must Pass)**:
- All Section 1 tests (bug fixes)
- Test 2.1 (basic series circuit)
- Test 3.1 (basic parallel circuit)
- Test 4.1 (open circuit validation)

**Important (Should Pass)**:
- All physics calculations within 5% tolerance
- Current flow animations visible
- Property editor functional

**Nice to Have (Optional)**:
- Camera controls smooth
- Misconception messages clear
- Performance >30 FPS

---

## Failure Investigation

If a test fails:
1. Check Unity Console for error messages
2. Note exact steps that caused failure
3. Try test again to confirm reproducibility
4. Document actual vs expected results
5. Report to development team

---

**Test Document Version**: 1.0
**Last Updated**: 2025-01-26
**Author**: Claude Code AI Assistant
