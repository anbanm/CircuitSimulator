# Circuit Save/Load - MVP Design (Core Mechanics Only)

**Date**: 2025-01-15
**Goal**: Minimal viable save/load - just components and wires
**Philosophy**: Get it working first, add features later

---

## 1. Minimal Data Model - What MUST Be Saved

### Core Requirements
```json
{
  "version": "1.0",
  "components": [
    {
      "id": "Battery_0",
      "type": "Battery",
      "position": [1.0, 0.5, 1.0],
      "voltage": 12.0
    },
    {
      "id": "Bulb_1",
      "type": "Bulb",
      "position": [-4.0, 0.5, 2.0],
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

### What We're Saving
- ✅ **Component ID** (name)
- ✅ **Component Type** (Battery, Bulb, Resistor, Switch)
- ✅ **Position** (Vector3 as array)
- ✅ **Electrical Properties** (voltage for Battery, resistance for others)
- ✅ **Wire Connections** (which component terminals are connected)

### What We're NOT Saving (Yet)
- ❌ Metadata (name, description, author)
- ❌ Rotation (all components face forward)
- ❌ Visual state (colors, selection)
- ❌ Solved values (current, voltage drop)
- ❌ Camera position
- ❌ Terminal positions (reconstructed from component)

---

## 2. Data Classes (Minimal)

```csharp
// CircuitSaveData.cs
using System;
using UnityEngine;

[Serializable]
public class CircuitSaveData
{
    public string version = "1.0";
    public ComponentData[] components;
    public WireData[] wires;
}

[Serializable]
public class ComponentData
{
    public string id;           // "Battery_0", "Bulb_1", etc.
    public string type;         // "Battery", "Bulb", "Resistor", "Switch"
    public float[] position;    // [x, y, z]

    // Electrical properties (only one will be used based on type)
    public float voltage;       // For Battery
    public float resistance;    // For Resistor, Bulb
    public bool switchClosed;   // For Switch
}

[Serializable]
public class WireData
{
    public string startComponent;  // Component ID
    public string startTerminal;   // "PositiveTerminal", "TerminalA", etc.
    public string endComponent;    // Component ID
    public string endTerminal;     // Terminal name
}
```

**Why so simple?**
- Unity's `JsonUtility` handles these easily
- Easy to debug (human-readable)
- Easy to extend later (just add fields)

---

## 3. Serialization (Save Circuit)

```csharp
// CircuitSerializer.cs
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class CircuitSerializer
{
    public static string SerializeCircuit(
        List<CircuitComponent3D> components,
        List<GameObject> wires)
    {
        var saveData = new CircuitSaveData
        {
            version = "1.0",
            components = SerializeComponents(components),
            wires = SerializeWires(wires)
        };

        return JsonUtility.ToJson(saveData, prettyPrint: true);
    }

    private static ComponentData[] SerializeComponents(List<CircuitComponent3D> components)
    {
        return components.Select(c => new ComponentData
        {
            id = c.name,
            type = c.ComponentType.ToString(),
            position = new float[] { c.transform.position.x, c.transform.position.y, c.transform.position.z },
            voltage = c.voltage,
            resistance = c.resistance,
            switchClosed = c.switchState  // Assuming we add this field
        }).ToArray();
    }

    private static WireData[] SerializeWires(List<GameObject> wires)
    {
        var wireDataList = new List<WireData>();

        foreach (var wireObj in wires)
        {
            var wire = wireObj.GetComponent<CircuitWire>();
            if (wire == null || wire.startComponent == null || wire.endComponent == null)
                continue;

            wireDataList.Add(new WireData
            {
                startComponent = wire.startComponent.name,
                startTerminal = wire.startTerminal?.name ?? "TerminalA",
                endComponent = wire.endComponent.name,
                endTerminal = wire.endTerminal?.name ?? "TerminalB"
            });
        }

        return wireDataList.ToArray();
    }

    public static CircuitSaveData DeserializeCircuit(string json)
    {
        return JsonUtility.FromJson<CircuitSaveData>(json);
    }
}
```

**Key Points**:
- Uses existing CircuitManager.Components and Wires
- Handles null references gracefully
- Pretty-print JSON for debugging

---

## 4. Deserialization (Load Circuit)

```csharp
// CircuitLoader.cs
using System.Collections.Generic;
using UnityEngine;

public class CircuitLoader : MonoBehaviour
{
    private ComponentFactoryManager factory;
    private ComponentTerminalManager terminalManager;
    private CircuitManager circuitManager;

    void Start()
    {
        factory = FindFirstObjectByType<ComponentFactoryManager>();
        terminalManager = FindFirstObjectByType<ComponentTerminalManager>();
        circuitManager = CircuitManager.Instance;
    }

    public void LoadCircuit(CircuitSaveData saveData)
    {
        Debug.Log($"Loading circuit v{saveData.version}...");

        // Step 1: Clear existing circuit
        ClearCircuit();

        // Step 2: Create components
        var componentMap = new Dictionary<string, CircuitComponent3D>();
        foreach (var compData in saveData.components)
        {
            var component = CreateComponent(compData);
            if (component != null)
            {
                componentMap[compData.id] = component;
            }
        }

        // Step 3: Create wires (after all components exist)
        foreach (var wireData in saveData.wires)
        {
            CreateWire(wireData, componentMap);
        }

        // Step 4: Solve circuit
        circuitManager.MarkCircuitChanged();

        Debug.Log($"✅ Loaded {saveData.components.Length} components, {saveData.wires.Length} wires");
    }

    private CircuitComponent3D CreateComponent(ComponentData data)
    {
        Vector3 position = new Vector3(data.position[0], data.position[1], data.position[2]);
        CircuitComponent3D component = null;

        switch (data.type)
        {
            case "Battery":
                component = factory.CreateBattery(position);
                component.voltage = data.voltage;
                break;

            case "Bulb":
                component = factory.CreateBulb(position);
                component.resistance = data.resistance;
                break;

            case "Resistor":
                component = factory.CreateResistor(position);
                component.resistance = data.resistance;
                break;

            case "Switch":
                component = factory.CreateSwitch(position);
                component.switchState = data.switchClosed;
                break;

            default:
                Debug.LogWarning($"Unknown component type: {data.type}");
                return null;
        }

        // Override name to match saved ID
        component.name = data.id;
        return component;
    }

    private void CreateWire(WireData wireData, Dictionary<string, CircuitComponent3D> componentMap)
    {
        // Get components
        if (!componentMap.TryGetValue(wireData.startComponent, out var startComp))
        {
            Debug.LogWarning($"Wire start component not found: {wireData.startComponent}");
            return;
        }

        if (!componentMap.TryGetValue(wireData.endComponent, out var endComp))
        {
            Debug.LogWarning($"Wire end component not found: {wireData.endComponent}");
            return;
        }

        // Get terminals
        var startTerminals = terminalManager.GetComponentTerminals(startComp);
        var endTerminals = terminalManager.GetComponentTerminals(endComp);

        var startTerminal = startTerminals.Find(t => t.name == wireData.startTerminal);
        var endTerminal = endTerminals.Find(t => t.name == wireData.endTerminal);

        if (startTerminal == null || endTerminal == null)
        {
            Debug.LogWarning($"Terminals not found for wire {wireData.startComponent} -> {wireData.endComponent}");
            return;
        }

        // Create wire using existing system
        terminalManager.CreateWireBetweenComponents(startComp, endComp);

        // TODO: Ensure correct terminals are used (current implementation might not respect specific terminals)
    }

    private void ClearCircuit()
    {
        var controlManager = FindFirstObjectByType<CircuitControlManager>();
        controlManager?.ResetCircuit();
    }
}
```

**Challenge**: `CreateWireBetweenComponents()` might not connect to the specific terminals we want. We may need to:
- Modify that method to accept terminal parameters, OR
- Directly create the wire and set terminals

---

## 5. File I/O (Save/Load Manager)

```csharp
// SaveLoadManager.cs
using System.IO;
using UnityEngine;

public class SaveLoadManager : MonoBehaviour
{
    private CircuitManager circuitManager;
    private CircuitLoader loader;

    void Start()
    {
        circuitManager = CircuitManager.Instance;
        loader = GetComponent<CircuitLoader>();
        if (loader == null)
            loader = gameObject.AddComponent<CircuitLoader>();
    }

    public void SaveCircuit(string filename)
    {
        // Get current circuit state
        var components = circuitManager.Components;
        var wires = circuitManager.Wires;

        // Serialize to JSON
        string json = CircuitSerializer.SerializeCircuit(components, wires);

        // Write to file
        string filepath = GetFilePath(filename);
        File.WriteAllText(filepath, json);

        Debug.Log($"✅ Circuit saved: {filepath}");
        Debug.Log($"File size: {json.Length} bytes");
    }

    public void LoadCircuit(string filename)
    {
        string filepath = GetFilePath(filename);

        if (!File.Exists(filepath))
        {
            Debug.LogError($"❌ File not found: {filepath}");
            return;
        }

        // Read JSON
        string json = File.ReadAllText(filepath);

        // Deserialize
        var saveData = CircuitSerializer.DeserializeCircuit(json);

        // Load into scene
        loader.LoadCircuit(saveData);

        Debug.Log($"✅ Circuit loaded: {filename}");
    }

    public string[] GetSavedCircuits()
    {
        string dir = GetSaveDirectory();
        if (!Directory.Exists(dir))
            return new string[0];

        var files = Directory.GetFiles(dir, "*.circuit");
        for (int i = 0; i < files.Length; i++)
        {
            files[i] = Path.GetFileNameWithoutExtension(files[i]);
        }
        return files;
    }

    private string GetFilePath(string filename)
    {
        // Ensure .circuit extension
        if (!filename.EndsWith(".circuit"))
            filename += ".circuit";

        string dir = GetSaveDirectory();
        Directory.CreateDirectory(dir); // Ensure directory exists
        return Path.Combine(dir, filename);
    }

    private string GetSaveDirectory()
    {
        // Use Application.persistentDataPath for cross-platform support
        return Path.Combine(Application.persistentDataPath, "CircuitSaves");
    }

    // Debug: Print save directory path
    [ContextMenu("Show Save Directory")]
    public void ShowSaveDirectory()
    {
        Debug.Log($"Save directory: {GetSaveDirectory()}");
    }
}
```

**File Location** (cross-platform):
- **Windows**: `C:\Users\[User]\AppData\LocalLow\[Company]\CircuitSimulator\CircuitSaves`
- **Mac**: `~/Library/Application Support/[Company]/CircuitSimulator/CircuitSaves`
- **WebGL**: Uses PlayerPrefs instead (we'll handle this later)

---

## 6. UI Integration (Minimal)

For MVP, we can use simple keyboard shortcuts and context menu:

```csharp
// Add to PaletteUIManager or new SaveLoadUI.cs
void Update()
{
    var saveLoadManager = FindFirstObjectByType<SaveLoadManager>();
    if (saveLoadManager == null) return;

    // Ctrl+S: Quick save
    if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.S))
    {
        saveLoadManager.SaveCircuit("quicksave");
        Debug.Log("Quick saved!");
    }

    // Ctrl+L: Quick load
    if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.L))
    {
        saveLoadManager.LoadCircuit("quicksave");
        Debug.Log("Quick loaded!");
    }
}

// Context menu for testing
[ContextMenu("Save Test Circuit")]
void SaveTestCircuit()
{
    FindFirstObjectByType<SaveLoadManager>()?.SaveCircuit("test");
}

[ContextMenu("Load Test Circuit")]
void LoadTestCircuit()
{
    FindFirstObjectByType<SaveLoadManager>()?.LoadCircuit("test");
}
```

**Later**: Add proper UI dialogs with file browser

---

## 7. Implementation Steps (MVP)

### Step 1: Data Classes (30 min)
1. Create `CircuitSaveData.cs`
2. Add `ComponentData`, `WireData` classes
3. Test JSON serialization with dummy data

### Step 2: Serialization (1 hour)
1. Create `CircuitSerializer.cs`
2. Implement `SerializeCircuit()`
3. Test with current circuit → verify JSON looks correct

### Step 3: File I/O (30 min)
1. Create `SaveLoadManager.cs`
2. Implement `SaveCircuit()` with file writing
3. Test: Build circuit → Save → Check file exists

### Step 4: Deserialization (2 hours) - HARDEST PART
1. Create `CircuitLoader.cs`
2. Implement `CreateComponent()` for each type
3. Implement `CreateWire()` - may need to modify wire creation
4. Test: Load saved file → Verify circuit appears

### Step 5: Round-Trip Testing (1 hour)
1. Create complex circuit (battery + 3 bulbs in series)
2. Save → Clear → Load
3. Verify:
   - ✅ All components present
   - ✅ Correct positions
   - ✅ Wires connected correctly
   - ✅ Circuit solves correctly
   - ✅ Same electrical values

### Step 6: Edge Cases (1 hour)
1. Test empty circuit (no components)
2. Test disconnected components (no wires)
3. Test corrupted JSON (missing fields)
4. Test invalid component types

**Total Estimate**: 6-7 hours for working MVP

---

## 8. Known Challenges & Solutions

### Challenge 1: Wire Terminal Assignment
**Problem**: `CreateWireBetweenComponents()` might connect wrong terminals

**Solution A** (Quick): Accept that terminals might be wrong, re-solve circuit (might work if order doesn't matter)

**Solution B** (Better): Modify `CreateWireBetweenComponents()` to accept terminal parameters:
```csharp
public void CreateWireBetweenComponents(
    CircuitComponent3D fromComp,
    CircuitComponent3D toComp,
    string fromTerminalName = null,
    string toTerminalName = null)
{
    // Find specific terminals if names provided
    // ...
}
```

### Challenge 2: Component Counters
**Problem**: `CreateBattery()` increments `batteryCounter`, might cause duplicate names

**Solution**: Reset counters before loading, or override names after creation (already doing this)

### Challenge 3: Missing CreateSwitch()
**Problem**: Switch creation doesn't exist yet

**Solution**: Either implement it now (5 min) or skip switches in MVP

### Challenge 4: WebGL File System
**Problem**: WebGL can't use `File.WriteAllText()`

**Solution** (Later): Use PlayerPrefs for WebGL builds:
```csharp
#if UNITY_WEBGL
    PlayerPrefs.SetString("circuit_" + filename, json);
#else
    File.WriteAllText(filepath, json);
#endif
```

---

## 9. Testing Plan

### Test Circuit 1: Simple Series
```
Battery(12V) → Bulb(2Ω)
```
**Expected**:
- Save: 2 components, 1 wire
- Load: Circuit solves to 6A

### Test Circuit 2: Three Bulbs
```
Battery(12V) → Bulb1(2Ω) → Bulb2(2Ω) → Bulb3(2Ω)
```
**Expected**:
- Save: 4 components, 4 wires
- Load: Each bulb gets 2A

### Test Circuit 3: Your Current Circuit
```
Battery → Bulb_2 → Bulb_3 → Bulb_1 → Battery
```
**Expected**:
- All 4 components at correct positions
- All 4 wires connected correctly
- Solves to same values as before save

---

## 10. Success Criteria

✅ **MVP Complete When**:
1. Can save current circuit to file
2. Can load file and recreate circuit
3. Loaded circuit solves correctly
4. Round-trip works (save → load → save → load)
5. No crashes on edge cases

❌ **Not Required for MVP**:
- UI dialogs (keyboard shortcuts OK)
- Metadata (name, author, etc.)
- Pretty file browser
- Export/import features
- Version migration
- Error recovery UI

---

## Next Action

**Ready to implement?** I can start with:
1. Create the data classes
2. Implement serialization
3. Test with your current circuit

Or would you like to review/modify the design first?

---

**Estimated Time**: 6-7 hours total
**Priority**: High - Blocking for other features
**Risk**: Medium - Wire terminal assignment might need iteration
