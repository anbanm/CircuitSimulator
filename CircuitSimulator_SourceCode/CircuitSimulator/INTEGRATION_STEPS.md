# Save/Load System - Integration Steps

**Quick Reference Guide for Fixing Critical Issues**

---

## Fix #1: Add Switch State Property (30 min)

**File**: `Assets/Scripts/Components/CircuitComponent3D.cs`

**Step 1**: Add property after line 21
```csharp
[Header("Component Properties")]
public ComponentType ComponentType = ComponentType.Bulb;
public float voltage = 6f;
public float resistance = 50f;
public bool switchState = true;  // ✅ ADD THIS LINE (true = closed, false = open)
```

**Step 2**: Sync with electrical component in Start() method (after line 58)
```csharp
// Register with label manager
if (LabelManager.Instance != null)
{
    LabelManager.Instance.RegisterComponent(this);
}

// ✅ ADD THIS BLOCK
// Sync switch state with electrical component
if (ComponentType == ComponentType.Switch)
{
    var electrical = GetComponent<ElectricalComponent>();
    if (electrical != null && electrical.logicalComponent is Switch sw)
    {
        sw.SetState(switchState);
    }
}
```

**Step 3**: Uncomment switch restoration in CircuitLoader.cs (line 114)
```csharp
case "Switch":
    component = factory.CreateSwitch(position);
    if (component != null)
    {
        component.switchState = data.switchClosed;  // ✅ UNCOMMENT THIS LINE
    }
    break;
```

**Test**: Toggle switch → F5 save → Clear → F6 load → Verify switch state

---

## Fix #2: Add Terminal Validation (15 min)

**File**: `Assets/Scripts/CircuitLoader.cs` (line 176)

**Replace**:
```csharp
if (startTerminals == null || endTerminals == null)
{
    Debug.LogWarning($"⚠️ Could not get terminals...");
    return;
}
```

**With**:
```csharp
if (startTerminals == null || startTerminals.Count == 0 ||
    endTerminals == null || endTerminals.Count == 0)
{
    Debug.LogError($"❌ Could not get terminals for wire {wireData.startComponent} → {wireData.endComponent}");
    Debug.LogError($"   Start terminals: {startTerminals?.Count ?? 0}, End terminals: {endTerminals?.Count ?? 0}");
    return;
}
```

**Test**: Corrupt save file with wrong component ID → Load → Should see error message

---

## Fix #3: Fix Terminal Setup Race Condition (30 min)

**File**: `Assets/Scripts/CircuitLoader.cs` (line 52)

**Replace**:
```csharp
foreach (var compData in saveData.components)
{
    var component = CreateComponent(compData);
    if (component != null)
    {
        componentMap[compData.id] = component;
    }
}
```

**With**:
```csharp
foreach (var compData in saveData.components)
{
    var component = CreateComponent(compData);
    if (component != null)
    {
        componentMap[compData.id] = component;

        // ✅ FORCE terminal setup immediately (don't wait for Start())
        var terminalMgr = component.GetComponent<ComponentTerminalManager>();
        if (terminalMgr != null)
        {
            terminalMgr.SetupTerminals();
        }
    }
}
```

**Test**: Save circuit with wires → Load → All wires should connect

---

## Fix #4: Add Terminal Cache Cleanup (30 min)

**File**: `Assets/Scripts/Managers/ComponentTerminalManager.cs`

**Step 1**: Add method at end of class
```csharp
/// <summary>
/// Clears all cached terminal references.
/// Call this before destroying all components to prevent memory leaks.
/// </summary>
public void ClearAllTerminals()
{
    if (componentTerminals != null)
    {
        componentTerminals.Clear();
        Debug.Log("Terminal cache cleared");
    }
}
```

**Step 2**: Call it in CircuitLoader.cs (line 217, in ClearCircuit method)

**Before**:
```csharp
private void ClearCircuit()
{
    Debug.Log("Clearing circuit before load...");

    if (circuitManager != null)
    {
        circuitManager.ClearAllComponents();
    }
```

**After**:
```csharp
private void ClearCircuit()
{
    Debug.Log("Clearing circuit before load...");

    if (circuitManager != null)
    {
        circuitManager.ClearAllComponents();
    }

    // ✅ ADD THIS: Clear terminal cache to prevent memory leaks
    if (terminalManager != null)
    {
        terminalManager.ClearAllTerminals();
    }
```

**Test**: Load circuit 10 times → Memory should stay constant

---

## Fix #5: Add Null Checks in Serializer (15 min)

**File**: `Assets/Scripts/CircuitSerializer.cs` (line 103)

**Replace**:
```csharp
wireDataList.Add(new WireData
{
    startComponent = wire.startComponent.name,
    startTerminal = wire.startTerminal.name,
    endComponent = wire.endComponent.name,
    endTerminal = wire.endTerminal.name
});
```

**With**:
```csharp
// ✅ ADD defensive null checks
if (wire.startComponent == null || wire.endComponent == null ||
    wire.startTerminal == null || wire.endTerminal == null)
{
    Debug.LogError($"❌ Wire '{wireObj.name}' has null references, cannot serialize");
    continue;
}

wireDataList.Add(new WireData
{
    startComponent = wire.startComponent.name,
    startTerminal = wire.startTerminal.name,
    endComponent = wire.endComponent.name,
    endTerminal = wire.endTerminal.name
});
```

**Test**: Try to save circuit with broken wire → Should get error instead of crash

---

## Fix #6: Fix Bulb Brightness Physics (45 min)

**File**: `Assets/Scripts/Components/VisualComponent.cs`

**Step 1**: Find OnVoltageChanged method (line 208)
**Step 2**: Replace entire method with correct physics

**Replace**:
```csharp
private void OnVoltageChanged(float newVoltage)
{
    if (hasGlowEffect && currentState == VisualState.Active)
    {
        float intensity = Mathf.Clamp01(newVoltage / 12f);  // ❌ WRONG PHYSICS
        SetGlowIntensity(intensity * 0.5f);
    }
}
```

**With**:
```csharp
private void OnVoltageChanged(float newVoltage)
{
    UpdateBulbBrightness();  // Use power-based calculation
}

private void OnCurrentChanged(float newCurrent)
{
    UpdateBulbBrightness();  // Use power-based calculation
}

/// <summary>
/// Updates bulb brightness based on power dissipation (P = I²R).
/// Educationally correct: brightness depends on power, not just voltage.
/// </summary>
private void UpdateBulbBrightness()
{
    if (!hasGlowEffect || currentState != VisualState.Active)
        return;

    if (electricalComponent == null)
        return;

    // ✅ CORRECT PHYSICS: Power = I²R
    float current = electricalComponent.Current;
    float resistance = electricalComponent.Resistance;
    float power = current * current * resistance;

    // Normalize to typical bulb (5Ω at 1A = 5W)
    float intensity = Mathf.Clamp01(power / 5f);

    SetGlowIntensity(intensity);
}
```

**Test**: Create circuit with bulbs in series → Brightness should decrease for each bulb

---

## Testing Checklist

### After Each Fix
- [ ] Code compiles without errors
- [ ] No new warnings
- [ ] Specific test for that fix passes

### Integration Testing (After All Fixes)
- [ ] Create circuit with Battery + 3 Bulbs + 2 Switches
- [ ] Toggle switches to different states
- [ ] Press F5 to save
- [ ] Clear circuit (Reset button)
- [ ] Press F6 to load
- [ ] Verify all components restored correctly
- [ ] Verify all wires connected correctly
- [ ] Verify switch states preserved
- [ ] Verify bulb brightness looks correct
- [ ] Load circuit 10 times (memory leak test)
- [ ] Check console for errors

### Full Test Suite
See SAVE_LOAD_IMPLEMENTATION_SUMMARY.md for comprehensive test scenarios

---

## Estimated Time

| Fix | Description | Time |
|-----|-------------|------|
| #1 | Switch state property | 30 min |
| #2 | Terminal validation | 15 min |
| #3 | Terminal setup timing | 30 min |
| #4 | Terminal cache cleanup | 30 min |
| #5 | Null checks | 15 min |
| #6 | Bulb brightness physics | 45 min |
| **Total** | **Critical fixes** | **2h 45min** |
| Testing | Integration + regression | 2 hours |
| **Grand Total** | **Production ready** | **~5 hours** |

---

## Next Steps

1. Apply fixes in order (#1-6)
2. Test each fix individually
3. Run integration test suite
4. Commit to git with detailed message
5. Update CLAUDE.md with new version (v2.3)
6. Update IMPROVEMENT_ROADMAP.md marking issues as resolved

---

**Questions?** See CODE_REVIEW_FINDINGS.md for detailed explanations of each issue.
