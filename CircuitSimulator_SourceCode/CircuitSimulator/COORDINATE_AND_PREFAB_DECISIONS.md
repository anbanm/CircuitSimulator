# Save/Load Design Decisions - Coordinates & Prefabs

**Date**: 2025-01-15

---

## 1. Coordinate System - What to Save?

### Current System Analysis

**How Components Are Currently Positioned**:
```csharp
// ComponentFactoryManager.cs:208
componentObject.transform.position = position;  // World coordinates
componentObject.transform.SetParent(canvasPlane);  // Parented to "Components" GameObject
```

Components are stored with:
- **World coordinates** in `transform.position`
- **Parented** to a "Components" GameObject (canvasPlane)

### Option A: World Coordinates (RECOMMENDED for MVP)
```json
{
  "id": "Battery_0",
  "position": [1.0, 0.5, 1.0]  // Absolute world position
}
```

**Pros**:
- ✅ Simple - just save `transform.position` directly
- ✅ Works immediately, no coordinate conversion
- ✅ Matches Unity Inspector values (easier debugging)
- ✅ Independent of parent GameObject position

**Cons**:
- ❌ If user moves "Components" parent, loaded circuits won't align
- ❌ Harder to share between users (different workspace setups)

**Code**:
```csharp
// Save:
position = new float[] { c.transform.position.x, c.transform.position.y, c.transform.position.z }

// Load:
component.transform.position = new Vector3(data.position[0], data.position[1], data.position[2])
```

### Option B: Local Coordinates (Relative to Parent)
```json
{
  "id": "Battery_0",
  "position": [1.0, 0.5, 1.0],  // Relative to "Components" parent
  "parentOffset": [0, 0, 0]     // For reference
}
```

**Pros**:
- ✅ Independent of workspace position
- ✅ More portable between different setups
- ✅ Survives parent GameObject movement

**Cons**:
- ❌ More complex - need to track parent position
- ❌ What if parent doesn't exist on load?
- ❌ Extra fields to save/load

**Code**:
```csharp
// Save:
position = new float[] { c.transform.localPosition.x, c.transform.localPosition.y, c.transform.localPosition.z }

// Load:
component.transform.SetParent(componentsParent);
component.transform.localPosition = new Vector3(data.position[0], data.position[1], data.position[2])
```

### **RECOMMENDATION: Option A (World Coordinates)**

**Why**:
- MVP needs to be simple and work immediately
- Current circuits use world coordinates already
- Easier to debug (matches Unity Inspector)
- Can always migrate to local coordinates in v2.0 if needed

**Migration Strategy** (Later):
```json
{
  "version": "2.0",
  "components": [{
    "position": [1.0, 0.5, 1.0],
    "coordinateSystem": "world"  // or "local"
  }]
}
```

---

## 2. Prefab References - How to Handle Custom Visuals?

### Current System Analysis

**Component Creation Flow** (`ComponentFactoryManager.cs:245-258`):
```csharp
private GameObject CreateComponentObject(string componentName, Vector3 position)
{
    GameObject prefab = GetPrefabForComponent(componentName);  // Check for prefab first

    if (prefab != null)
    {
        Debug.Log($"Using custom prefab for {componentName}");
        return Instantiate(prefab, position, Quaternion.identity);  // Use prefab
    }
    else
    {
        Debug.Log($"Using default primitive for {componentName}");
        return CreatePrimitiveForComponent(componentName, position);  // Fallback to primitive
    }
}
```

**Current Primitive Mapping**:
- Battery: Cube (red)
- Resistor: Cylinder (yellow)
- Bulb: Sphere (white)
- Switch: Capsule (gray)

### Option A: Save Type Only - Let Factory Decide (RECOMMENDED for MVP)
```json
{
  "id": "Battery_0",
  "type": "Battery"  // Just the type, no prefab reference
}
```

**How it works on load**:
1. Read `type: "Battery"`
2. Call `factory.CreateBattery(position)`
3. Factory checks for prefab, uses it if available, otherwise creates primitive
4. Either way, we get a working battery

**Pros**:
- ✅ Simplest approach
- ✅ Works with both prefabs AND primitives
- ✅ No prefab path management
- ✅ Forward compatible (adding prefabs later doesn't break old saves)
- ✅ Small file size

**Cons**:
- ❌ Loses visual customization if user had specific prefab
- ❌ Can't preserve "themed" component appearance exactly

### Option B: Save Prefab Path (Preserve Exact Visuals)
```json
{
  "id": "CarBattery_0",
  "type": "Battery",
  "prefabPath": "Assets/Prefabs/ThemedComponents/CarBattery.prefab",  // Optional
  "usePrimitive": false
}
```

**How it works on load**:
1. If `prefabPath` exists, try to load prefab from Resources
2. If prefab not found, fall back to primitive
3. If `usePrimitive: true`, skip prefab lookup

**Pros**:
- ✅ Preserves exact visual appearance
- ✅ Supports themed component system (ScriptableObject definitions)
- ✅ Future-proof for custom assets

**Cons**:
- ❌ More complex loading logic
- ❌ What if prefab doesn't exist on another user's machine?
- ❌ Larger file size
- ❌ Need prefab path resolution (Resources.Load)

### Option C: Hybrid - Save Component Definition Name
```json
{
  "id": "CarBattery_0",
  "type": "Battery",
  "definitionName": "ComponentDef_CarBattery"  // Optional ScriptableObject reference
}
```

**How it works**:
1. If `definitionName` exists, try to load from Resources
2. Use definition's prefab if available
3. Otherwise fall back to default Battery creation

**Pros**:
- ✅ Preserves themed components
- ✅ Works with existing ComponentDefinition system
- ✅ More portable than direct prefab paths

**Cons**:
- ❌ Still requires Resources.Load
- ❌ Extra complexity

### **RECOMMENDATION: Option A (Type Only) for MVP**

**Why**:
- Get save/load working ASAP
- Users aren't using custom prefabs yet (mostly primitives)
- Can add prefab support in v2.0 without breaking old saves
- Forward compatible:
  ```json
  // v1.0 save (MVP):
  {"type": "Battery"}

  // v2.0 save (with prefabs):
  {"type": "Battery", "prefabPath": "..."}  // Added field, old loader ignores it
  ```

**When to Add Prefab Support**:
- After MVP works
- When users start creating custom themed components
- When sharing circuits becomes important

---

## 3. Implementation for MVP

### Save Data Structure (Final)
```json
{
  "version": "1.0",
  "components": [
    {
      "id": "Battery_0",
      "type": "Battery",
      "position": [1.0, 0.5, 1.0],  // World coordinates
      "voltage": 12.0,
      "resistance": 0.0
    },
    {
      "id": "Bulb_1",
      "type": "Bulb",
      "position": [-4.0, 0.5, 2.0],
      "voltage": 0.0,
      "resistance": 2.0
    }
  ],
  "wires": [
    {
      "startComponent": "Battery_0",
      "startTerminal": "PositiveTerminal",
      "endComponent": "Bulb_1",
      "endTerminal": "TerminalB"
    }
  ]
}
```

### Serialization Code
```csharp
// Save:
var componentData = new ComponentData
{
    id = component.name,
    type = component.ComponentType.ToString(),  // "Battery", "Bulb", etc.
    position = new float[] {
        component.transform.position.x,
        component.transform.position.y,
        component.transform.position.z
    },
    voltage = component.voltage,
    resistance = component.resistance
};
```

### Deserialization Code
```csharp
// Load:
private CircuitComponent3D CreateComponent(ComponentData data)
{
    Vector3 worldPos = new Vector3(data.position[0], data.position[1], data.position[2]);
    CircuitComponent3D component = null;

    // Factory automatically handles prefab vs primitive decision
    switch (data.type)
    {
        case "Battery":
            component = factory.CreateBattery();  // Factory decides prefab/primitive
            component.transform.position = worldPos;
            component.voltage = data.voltage;
            break;
        // ... other types
    }

    component.name = data.id;  // Restore original name
    return component;
}
```

---

## 4. Future Enhancements (Post-MVP)

### Version 2.0 - Prefab Support
```json
{
  "version": "2.0",
  "components": [{
    "id": "CarBattery_0",
    "type": "Battery",
    "position": [1.0, 0.5, 1.0],
    "prefabReference": {  // NEW
      "definitionName": "ComponentDef_CarBattery",
      "resourcePath": "ThemedComponents/CarBattery"
    },
    "voltage": 12.0
  }]
}
```

### Version 3.0 - Local Coordinates
```json
{
  "version": "3.0",
  "workspace": {  // NEW
    "origin": [0, 0, 0],
    "coordinateSystem": "local"
  },
  "components": [{
    "position": [1.0, 0.5, 1.0],  // Now relative to workspace origin
    // ...
  }]
}
```

---

## Decision Summary

| Decision | Choice | Rationale |
|----------|--------|-----------|
| **Coordinates** | World (absolute) | Simple, works immediately, matches Inspector |
| **Prefab Handling** | Type only | Factory decides, forward compatible, MVP-ready |
| **Rotation** | Skip for MVP | All components face forward (0,0,0) |
| **Scale** | Skip for MVP | Use component defaults |

**Total Fields Saved Per Component**: 6-7
- id, type, position[3], voltage OR resistance

**File Size Estimate**: ~50-100 bytes per component (JSON)
- 10 components ≈ 1KB file

---

## Next Step: Implement with These Decisions ✅

Ready to create the data classes with:
- World coordinates
- Type-only (no prefab paths)
- Minimal properties

Proceed? 🚀
