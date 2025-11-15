# Switch Logic & Bulb Brightness - Design & Implementation

**Date**: 2025-01-15
**Status**: Design Phase
**Priority**: High - Core educational features needed before Save/Load

---

## 1. Switch Logic - Current Implementation

### ✅ What Already Works

**Solver Logic** (`CircuitCore.cs:99-114`):
```csharp
public class Switch : CircuitComponent
{
    private bool isClosed;
    private const float OPEN_RESISTANCE = 1e12f; // 1 trillion ohms
    public override float Resistance => isClosed ? 0f : OPEN_RESISTANCE;

    public void Toggle() => isClosed = !isClosed;
    public bool IsClosed() => isClosed;
}
```

**How it works**:
- **Closed (ON)**: 0Ω resistance → current flows freely
- **Open (OFF)**: 1 trillion Ω resistance → essentially infinite, no current
- Uses 1e12Ω instead of `float.MaxValue` to avoid numerical instability

**Visual Feedback** (`VisualComponent.cs:213-227`):
```csharp
private void OnSwitchStateChanged(bool switchState)
{
    if (switchState) // Closed
        SetVisualState(VisualState.Active);  // Green
    else // Open
        SetVisualState(VisualState.Default); // Gray
}
```

### ❌ What's Missing

1. **No UI to toggle switch** - User can't click to open/close
2. **No visual indicator of state** - Can't tell if switch is on/off
3. **No switch creation in ComponentFactoryManager** - Can't place switches
4. **Switch state not connected to 3D component** - CircuitComponent3D doesn't track switch state

---

## 2. Switch Implementation Plan

### Phase 1: Basic Toggle Functionality

**A. Add Switch State to CircuitComponent3D.cs**:
```csharp
public class CircuitComponent3D : MonoBehaviour
{
    // Existing properties...
    public bool switchState = true; // Default: closed (on)

    // Add to UpdateVisualFeedback()
    public void UpdateVisualFeedback()
    {
        if (ComponentType == ComponentType.Switch)
        {
            // Update visual based on switch state
            var visualComp = GetComponent<VisualComponent>();
            if (visualComp != null)
            {
                visualComp.OnSwitchStateChanged(switchState);
            }
        }

        LabelManager.Instance?.UpdateLabelsForComponent(this);
    }

    // New: Toggle switch
    public void ToggleSwitch()
    {
        if (ComponentType != ComponentType.Switch) return;

        switchState = !switchState;

        // Update logical component
        if (logicalComponent is Switch logicalSwitch)
        {
            logicalSwitch.Toggle();
        }

        // Re-solve circuit
        CircuitManager.Instance?.MarkCircuitChanged();

        Debug.Log($"Switch {name} toggled to {(switchState ? "CLOSED" : "OPEN")}");
    }
}
```

**B. Add Click Handler for Switches**:
```csharp
// In SelectableComponent.cs or new SwitchInteraction.cs
void OnMouseDown()
{
    var component = GetComponent<CircuitComponent3D>();
    if (component != null && component.ComponentType == ComponentType.Switch)
    {
        // Right-click or double-click to toggle
        if (Input.GetMouseButtonDown(1)) // Right-click
        {
            component.ToggleSwitch();
        }
    }
}
```

**C. Create Switch in ComponentFactoryManager**:
```csharp
public CircuitComponent3D CreateSwitch(Vector3 position)
{
    GameObject switchObj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
    switchObj.name = $"Switch_{switchCounter++}";
    switchObj.transform.position = position;
    switchObj.transform.localScale = new Vector3(0.3f, 1.0f, 0.3f);

    var component = switchObj.AddComponent<CircuitComponent3D>();
    component.ComponentType = ComponentType.Switch;
    component.resistance = 0f; // Closed by default
    component.switchState = true; // Closed

    // Set color (gray for switches)
    var renderer = switchObj.GetComponent<Renderer>();
    renderer.material.color = Color.gray;

    return component;
}
```

### Phase 2: Visual Indicators

**Option A: Color-Based (Simple)**
- **Closed (ON)**: Green or bright color
- **Open (OFF)**: Red or dark gray

**Option B: Model-Based (Better)**
- Create simple "lever" visual that rotates
- **Closed**: Lever horizontal
- **Open**: Lever vertical

**Option C: Text Label (Clearest)**
- Display "ON" or "OFF" text label above switch
- Update on toggle

**Recommendation**: Start with Option A (color), add Option C (label) for clarity.

```csharp
// In VisualComponent.cs
private void OnSwitchStateChanged(bool switchState)
{
    if (switchState) // Closed/ON
    {
        currentColor = Color.green;
        SetGlowIntensity(0.3f);
    }
    else // Open/OFF
    {
        currentColor = new Color(0.3f, 0.3f, 0.3f); // Dark gray
        SetGlowIntensity(0f);
    }

    SetRendererColor(currentColor);
}
```

### Phase 3: UI Integration

**Add to PaletteUIManager**:
- S key: Create Switch
- Switch button in component palette
- Tooltip: "Add on/off switch (Right-click to toggle)"

**Add to ControlPanel**:
- "Toggle Switch" button (when switch selected)
- Shows switch state: "Switch: ON" or "Switch: OFF"

---

## 3. Bulb Brightness - Current Implementation

### ✅ What Already Works

**Glow Effect System** (`VisualComponent.cs:258-342`):
```csharp
// Creates glow effect using scaled mesh renderer
private void CreateGlowEffect() { ... }

// Sets glow intensity (0.0 to 1.0)
private void SetGlowIntensity(float intensity) { ... }

// Responds to voltage changes
private void OnVoltageChanged(float newVoltage)
{
    if (hasGlowEffect && currentState == VisualState.Active)
    {
        float intensity = Mathf.Clamp01(newVoltage / 12f); // Normalize to 12V
        SetGlowIntensity(intensity * 0.5f);
    }
}
```

**How it works**:
- Creates slightly larger duplicate mesh with transparent material
- Alpha value = glow intensity
- Intensity calculated from voltage (normalized to 12V)

### ❌ What's Missing

1. **Brightness should be based on CURRENT, not voltage**
   - Physics: Bulb brightness ∝ Power = I²R
   - Current implementation uses voltage (incorrect!)

2. **Glow effect not enabled by default for bulbs**
   - Need to set `hasGlowEffect = true` when creating bulbs

3. **Emission shader not used**
   - Standard shader emission would look better than alpha transparency

4. **No realistic brightness curve**
   - Linear mapping doesn't match real bulb behavior
   - Should have threshold (dim below certain current)

---

## 4. Bulb Brightness Implementation Plan

### Physics Background

**Bulb Power**: `P = I²R`
- Power dissipated as heat and light
- Brightness proportional to power

**Example** (2Ω bulb):
- 0.5A → P = (0.5)² × 2 = 0.5W (dim)
- 1.0A → P = (1.0)² × 2 = 2.0W (medium)
- 1.5A → P = (1.5)² × 2 = 4.5W (bright)
- 2.0A → P = (2.0)² × 2 = 8.0W (very bright)

**Brightness Curve**:
```
Power (W)   Brightness
0.0 - 0.5   Off/Very Dim
0.5 - 1.5   Dim Glow
1.5 - 3.0   Medium
3.0 - 5.0   Bright
5.0+        Very Bright (max)
```

### Phase 1: Power-Based Brightness

**A. Calculate Power in CircuitComponent3D**:
```csharp
public class CircuitComponent3D : MonoBehaviour
{
    // Existing...
    public float current;
    public float resistance;

    // NEW: Calculate power
    public float CalculatePower()
    {
        return current * current * resistance; // I²R
    }

    // UPDATE: Use power for brightness
    public void UpdateVisualFeedback()
    {
        if (ComponentType == ComponentType.Bulb)
        {
            float power = CalculatePower();
            UpdateBulbBrightness(power);
        }

        LabelManager.Instance?.UpdateLabelsForComponent(this);
    }

    private void UpdateBulbBrightness(float power)
    {
        var visualComp = GetComponent<VisualComponent>();
        if (visualComp == null) return;

        // Map power to brightness intensity (0.0 to 1.0)
        float brightness = CalculateBrightnessFromPower(power);
        visualComp.SetBulbBrightness(brightness);
    }

    private float CalculateBrightnessFromPower(float power)
    {
        // Brightness curve with threshold
        if (power < 0.2f) return 0f; // Too dim to see

        // Map 0.2W-6W to 0.0-1.0 brightness
        float normalizedPower = Mathf.Clamp01((power - 0.2f) / 5.8f);

        // Apply gamma curve for perceptual brightness
        return Mathf.Pow(normalizedPower, 0.5f); // Square root for perceptual linearity
    }
}
```

**B. Add SetBulbBrightness to VisualComponent**:
```csharp
public class VisualComponent : MonoBehaviour
{
    [Header("Bulb-Specific")]
    [SerializeField] private bool isBulb = false;
    [SerializeField] private float maxGlowIntensity = 1.0f;
    [SerializeField] private Color bulbGlowColor = new Color(1f, 0.9f, 0.6f); // Warm white

    public void SetBulbBrightness(float brightness) // 0.0 to 1.0
    {
        if (!isBulb) return;

        // Update glow intensity
        SetGlowIntensity(brightness * maxGlowIntensity);

        // Update base color (darker when off, brighter when on)
        Color baseColor = Color.Lerp(Color.gray, bulbGlowColor, brightness * 0.5f);
        SetRendererColor(baseColor);

        // Optional: Update emission
        SetEmissionIntensity(brightness);
    }

    private void SetEmissionIntensity(float intensity)
    {
        if (componentRenderer == null) return;

        var material = componentRenderer.material;
        if (material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            Color emissionColor = bulbGlowColor * intensity * 2f; // HDR
            material.SetColor("_EmissionColor", emissionColor);
        }
    }
}
```

### Phase 2: Enhanced Visual Quality

**A. Use Emission Shader** (Better than transparency):
```csharp
private void CreateBulbMaterial()
{
    var material = new Material(Shader.Find("Standard"));
    material.EnableKeyword("_EMISSION");
    material.SetColor("_EmissionColor", Color.black); // Start off
    material.SetFloat("_Metallic", 0.2f);
    material.SetFloat("_Glossiness", 0.8f);
    componentRenderer.material = material;
}
```

**B. Add Bloom Effect** (Post-processing):
- Requires Unity Post Processing Stack
- Makes bright bulbs "glow" into surrounding area
- Optional enhancement for later

**C. Add Light Component** (Optional):
```csharp
private Light bulbLight;

private void CreateBulbLight()
{
    var lightObj = new GameObject("BulbLight");
    lightObj.transform.SetParent(transform);
    lightObj.transform.localPosition = Vector3.zero;

    bulbLight = lightObj.AddComponent<Light>();
    bulbLight.type = LightType.Point;
    bulbLight.color = new Color(1f, 0.9f, 0.6f); // Warm white
    bulbLight.range = 3f;
    bulbLight.intensity = 0f; // Start off
}

public void SetBulbBrightness(float brightness)
{
    // ... existing code ...

    // Update point light
    if (bulbLight != null)
    {
        bulbLight.intensity = brightness * 2f; // Max 2 intensity
    }
}
```

### Phase 3: Educational Features

**A. Display Power Value**:
```csharp
// Add to label system
"Power: 2.5W"
```

**B. Visual Power Scale**:
- Color gradient based on brightness
  - Off: Dark gray
  - Dim (0-33%): Dark orange
  - Medium (33-66%): Orange
  - Bright (66-100%): Yellow-white

**C. Comparison Mode**:
- Place two bulbs side-by-side
- Clearly see brightness difference
- Teaching: "More current = brighter bulb"

---

## 5. Integration & Testing

### Update ComponentFactoryManager

```csharp
public CircuitComponent3D CreateBulb(Vector3 position)
{
    GameObject bulbObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
    bulbObj.name = $"Bulb_{bulbCounter++}";
    bulbObj.transform.position = position;
    bulbObj.transform.localScale = Vector3.one * 0.8f;

    var component = bulbObj.AddComponent<CircuitComponent3D>();
    component.ComponentType = ComponentType.Bulb;
    component.resistance = 2.0f; // Default: 2 ohms (Grade 7 friendly value)

    // NEW: Add VisualComponent with glow enabled
    var visualComp = bulbObj.AddComponent<VisualComponent>();
    visualComp.isBulb = true;
    visualComp.hasGlowEffect = true;
    visualComp.bulbGlowColor = new Color(1f, 0.9f, 0.6f);

    // Set initial material with emission support
    var renderer = bulbObj.GetComponent<Renderer>();
    var material = new Material(Shader.Find("Standard"));
    material.EnableKeyword("_EMISSION");
    material.color = new Color(0.9f, 0.9f, 0.5f); // Yellowish
    renderer.material = material;

    return component;
}
```

### Testing Scenarios

**Test 1: Switch Control**
- [ ] Place switch in series circuit
- [ ] Toggle switch on/off (right-click)
- [ ] Verify current stops when open
- [ ] Verify visual state changes (green/gray)

**Test 2: Bulb Brightness - Series**
- [ ] Create: Battery(12V) → Bulb(2Ω)
- [ ] Expected: 6A current, 12W power, VERY BRIGHT
- [ ] Verify: Bright glow, warm color

**Test 3: Bulb Brightness - Multiple Bulbs**
- [ ] Create: Battery(12V) → Bulb1(2Ω) → Bulb2(2Ω) → Bulb3(2Ω)
- [ ] Total R = 6Ω, Current = 2A
- [ ] Each bulb: P = (2)² × 2 = 8W
- [ ] Expected: All three bulbs bright and equal

**Test 4: Brightness Comparison**
- [ ] Parallel: Bulb1(2Ω) || Bulb2(10Ω)
- [ ] Bulb1: Higher current → Brighter
- [ ] Bulb2: Lower current → Dimmer
- [ ] Verify visual difference is obvious

**Test 5: Switch + Brightness**
- [ ] Series: Battery → Switch → Bulb
- [ ] Switch closed: Bulb bright
- [ ] Switch open: Bulb off (no glow)
- [ ] Toggle: Smooth transition

---

## 6. Implementation Order

### Week 1: Core Functionality
1. ✅ Switch toggle logic (CircuitComponent3D)
2. ✅ Switch creation (ComponentFactoryManager)
3. ✅ Switch UI (right-click handler, palette button)
4. ✅ Power-based brightness calculation
5. ✅ Update VisualComponent for bulbs

### Week 2: Visual Polish
6. ✅ Emission shader for bulbs
7. ✅ Glow effect improvements
8. ✅ Switch visual indicators (color/label)
9. ✅ Testing and calibration

### Optional Enhancements
- [ ] Point light components for bulbs
- [ ] Bloom post-processing
- [ ] Animated switch lever
- [ ] Power value labels

---

## 7. Questions for User

Before we implement, please clarify:

### Switch Questions:
1. **Toggle method**: Right-click, Double-click, or UI button?
2. **Visual style**: Color-based (green/gray) or more elaborate (lever)?
3. **Default state**: Should switches be open or closed when placed?

### Bulb Brightness Questions:
1. **Brightness range**: What current range should we design for? (0-3A? 0-5A?)
2. **Visual style**: Glow effect only, or add point lights too?
3. **Educational focus**: Should we display power value labels?
4. **Comparison circuits**: Any specific demo circuits you want to showcase brightness?

### General:
5. **Priority**: Which feature is more important? (Switch or Brightness)
6. **Timeline**: Need these before save/load, or can they be parallel?

---

**Next Steps**: Awaiting your design choices, then we implement! 🚀
