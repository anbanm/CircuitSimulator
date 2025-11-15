# Circuit Save/Load System - Implementation Plan

**Date**: 2025-01-15
**Status**: Planning Phase
**Priority**: High - Core feature for educational tool

## Overview

Allow users to save their circuit designs and load them later for continued experimentation, sharing with classmates/teachers, or assignment submission.

---

## 1. Requirements & Goals

### Educational Use Cases
1. **Student Workflow**:
   - Save incomplete circuits for homework
   - Load teacher-provided circuit templates
   - Export circuits for submission/grading

2. **Teacher Workflow**:
   - Create circuit challenges/exercises
   - Share example circuits with students
   - Grade student-submitted circuits

3. **General Use**:
   - Experiment with complex circuits over multiple sessions
   - Share interesting circuits with others
   - Build circuit library/portfolio

### Technical Requirements
- ✅ Save/Load complete circuit state (components, wires, positions, properties)
- ✅ Preserve electrical properties (voltage, resistance, switch states)
- ✅ Maintain wire connections correctly
- ✅ Human-readable format for debugging/editing
- ✅ Version compatibility (handle old saves in new versions)
- ✅ Cross-platform support (Windows, Mac, WebGL)
- ✅ Fast load times (<1 second for typical circuits)

---

## 2. Data Model - What to Save

### Circuit Data Structure
```json
{
  "version": "2.0",
  "metadata": {
    "name": "Series Circuit Example",
    "description": "Three bulbs in series with 12V battery",
    "author": "Student Name",
    "created": "2025-01-15T10:30:00Z",
    "modified": "2025-01-15T11:45:00Z",
    "tags": ["series", "homework", "grade7"]
  },
  "components": [
    {
      "id": "Battery_0",
      "type": "Battery",
      "position": {"x": 1.0, "y": 0.5, "z": 1.0},
      "rotation": {"x": 0, "y": 0, "z": 0},
      "properties": {
        "voltage": 12.0
      },
      "terminals": [
        {"name": "NegativeTerminal", "localPosition": {"x": -0.4, "y": 0, "z": 0}},
        {"name": "PositiveTerminal", "localPosition": {"x": 0.4, "y": 0, "z": 0}}
      ]
    },
    {
      "id": "Bulb_1",
      "type": "Bulb",
      "position": {"x": -4.0, "y": 0.5, "z": 2.0},
      "rotation": {"x": 0, "y": 0, "z": 0},
      "properties": {
        "resistance": 2.0
      },
      "terminals": [
        {"name": "TerminalA", "localPosition": {"x": -0.4, "y": 0, "z": 0}},
        {"name": "TerminalB", "localPosition": {"x": 0.4, "y": 0, "z": 0}}
      ]
    }
  ],
  "wires": [
    {
      "id": "Wire_Battery_0_to_Bulb_1",
      "startComponent": "Battery_0",
      "startTerminal": "PositiveTerminal",
      "endComponent": "Bulb_1",
      "endTerminal": "TerminalB",
      "endpoints": {
        "start": {"x": 1.4, "y": 0.5, "z": 1.0},
        "end": {"x": -3.6, "y": 0.5, "z": 2.0}
      }
    }
  ],
  "solverResults": {
    "isSolved": true,
    "totalCurrent": 1.5,
    "timestamp": "2025-01-15T11:45:00Z"
  }
}
```

### Data Hierarchy
```
CircuitSaveData
├── Version (string)
├── Metadata
│   ├── Name, Description, Author
│   ├── Timestamps (created, modified)
│   └── Tags/Categories
├── Components[]
│   ├── ID, Type, Position, Rotation
│   ├── Electrical Properties (voltage, resistance, etc.)
│   └── Terminals[] (name, local position)
├── Wires[]
│   ├── ID, Start/End Component IDs
│   ├── Start/End Terminal Names
│   └── Endpoint Positions
└── Solver Results (optional)
    └── Cached electrical values
```

---

## 3. Architecture Design

### New Classes/Components

#### 3.1 `CircuitSerializer.cs`
**Purpose**: Convert circuit state to/from saveable format

```csharp
public class CircuitSerializer
{
    // Serialize current circuit to JSON
    public static string SerializeCircuit(
        List<CircuitComponent3D> components,
        List<GameObject> wires,
        CircuitMetadata metadata = null)
    {
        var saveData = new CircuitSaveData
        {
            version = "2.0",
            metadata = metadata ?? CreateDefaultMetadata(),
            components = SerializeComponents(components),
            wires = SerializeWires(wires)
        };

        return JsonUtility.ToJson(saveData, prettyPrint: true);
    }

    // Deserialize JSON to circuit data
    public static CircuitSaveData DeserializeCircuit(string json)
    {
        var saveData = JsonUtility.FromJson<CircuitSaveData>(json);
        ValidateVersion(saveData.version);
        return saveData;
    }

    // Component serialization
    private static ComponentSaveData[] SerializeComponents(List<CircuitComponent3D> components)
    {
        return components.Select(c => new ComponentSaveData
        {
            id = c.name,
            type = c.ComponentType.ToString(),
            position = new Vector3Data(c.transform.position),
            rotation = new Vector3Data(c.transform.eulerAngles),
            properties = SerializeComponentProperties(c)
        }).ToArray();
    }

    // Wire serialization
    private static WireSaveData[] SerializeWires(List<GameObject> wires)
    {
        return wires.Select(w => {
            var wire = w.GetComponent<CircuitWire>();
            return new WireSaveData
            {
                id = wire.name,
                startComponent = wire.startComponent.name,
                startTerminal = wire.startTerminal.name,
                endComponent = wire.endComponent.name,
                endTerminal = wire.endTerminal.name
            };
        }).ToArray();
    }
}
```

#### 3.2 `CircuitSaveData.cs` (Data Classes)
```csharp
[System.Serializable]
public class CircuitSaveData
{
    public string version;
    public CircuitMetadata metadata;
    public ComponentSaveData[] components;
    public WireSaveData[] wires;
    public SolverResults solverResults;
}

[System.Serializable]
public class CircuitMetadata
{
    public string name;
    public string description;
    public string author;
    public string created;  // ISO 8601 timestamp
    public string modified;
    public string[] tags;
}

[System.Serializable]
public class ComponentSaveData
{
    public string id;
    public string type;  // "Battery", "Bulb", "Resistor", etc.
    public Vector3Data position;
    public Vector3Data rotation;
    public ComponentProperties properties;
    public TerminalData[] terminals;
}

[System.Serializable]
public class ComponentProperties
{
    public float voltage;     // For Battery
    public float resistance;  // For Resistor, Bulb
    public bool isOpen;       // For Switch
}

[System.Serializable]
public class WireSaveData
{
    public string id;
    public string startComponent;
    public string startTerminal;
    public string endComponent;
    public string endTerminal;
    public Vector3Data startPosition;
    public Vector3Data endPosition;
}

[System.Serializable]
public class Vector3Data
{
    public float x, y, z;

    public Vector3Data(Vector3 v)
    {
        x = v.x; y = v.y; z = v.z;
    }

    public Vector3 ToVector3() => new Vector3(x, y, z);
}
```

#### 3.3 `CircuitLoader.cs`
**Purpose**: Instantiate circuit from save data

```csharp
public class CircuitLoader : MonoBehaviour
{
    private ComponentFactoryManager componentFactory;
    private ComponentTerminalManager terminalManager;
    private CircuitManager circuitManager;

    public void LoadCircuit(CircuitSaveData saveData)
    {
        Debug.Log($"Loading circuit: {saveData.metadata.name}");

        // Clear existing circuit
        ClearCurrentCircuit();

        // Create components
        var componentMap = new Dictionary<string, CircuitComponent3D>();
        foreach (var compData in saveData.components)
        {
            var component = CreateComponentFromData(compData);
            componentMap[compData.id] = component;
        }

        // Create wires (after all components exist)
        foreach (var wireData in saveData.wires)
        {
            CreateWireFromData(wireData, componentMap);
        }

        // Solve circuit
        circuitManager.MarkCircuitChanged();

        Debug.Log($"✅ Circuit loaded: {saveData.components.Length} components, {saveData.wires.Length} wires");
    }

    private CircuitComponent3D CreateComponentFromData(ComponentSaveData data)
    {
        // Use ComponentFactoryManager to create component
        CircuitComponent3D component = null;

        switch (data.type)
        {
            case "Battery":
                component = componentFactory.CreateBattery(data.position.ToVector3());
                component.voltage = data.properties.voltage;
                break;
            case "Bulb":
                component = componentFactory.CreateBulb(data.position.ToVector3());
                component.resistance = data.properties.resistance;
                break;
            case "Resistor":
                component = componentFactory.CreateResistor(data.position.ToVector3());
                component.resistance = data.properties.resistance;
                break;
            // ... other types
        }

        // Set rotation
        component.transform.eulerAngles = data.rotation.ToVector3();

        // Override name to match saved ID
        component.name = data.id;

        return component;
    }

    private void CreateWireFromData(WireSaveData data, Dictionary<string, CircuitComponent3D> componentMap)
    {
        var startComp = componentMap[data.startComponent];
        var endComp = componentMap[data.endComponent];

        var startTerminal = terminalManager.GetComponentTerminals(startComp)
            .Find(t => t.name == data.startTerminal);
        var endTerminal = terminalManager.GetComponentTerminals(endComp)
            .Find(t => t.name == data.endTerminal);

        // Use ConnectTool or create wire directly
        terminalManager.CreateWireBetweenComponents(startComp, endComp);
        // TODO: Ensure correct terminals are connected
    }

    private void ClearCurrentCircuit()
    {
        // Use existing CircuitControlManager.ResetCircuit()
        var controlManager = FindFirstObjectByType<CircuitControlManager>();
        controlManager?.ResetCircuit();
    }
}
```

#### 3.4 `SaveLoadManager.cs`
**Purpose**: High-level save/load operations with file I/O

```csharp
public class SaveLoadManager : MonoBehaviour
{
    [Header("File Settings")]
    public string saveDirectory = "CircuitSaves";
    public string fileExtension = ".circuit";

    private CircuitManager circuitManager;
    private CircuitSerializer serializer;
    private CircuitLoader loader;

    public void SaveCircuit(string filename, CircuitMetadata metadata = null)
    {
        // Get current circuit state
        var components = circuitManager.Components;
        var wires = circuitManager.Wires;

        // Serialize to JSON
        string json = CircuitSerializer.SerializeCircuit(components, wires, metadata);

        // Write to file
        string filepath = GetFilePath(filename);
        File.WriteAllText(filepath, json);

        Debug.Log($"✅ Circuit saved: {filepath}");
    }

    public void LoadCircuit(string filename)
    {
        string filepath = GetFilePath(filename);

        if (!File.Exists(filepath))
        {
            Debug.LogError($"❌ Circuit file not found: {filepath}");
            return;
        }

        // Read JSON
        string json = File.ReadAllText(filepath);

        // Deserialize
        var saveData = CircuitSerializer.DeserializeCircuit(json);

        // Load into scene
        loader.LoadCircuit(saveData);

        Debug.Log($"✅ Circuit loaded: {saveData.metadata.name}");
    }

    public string[] GetSavedCircuits()
    {
        string dir = GetSaveDirectory();
        if (!Directory.Exists(dir))
            return new string[0];

        return Directory.GetFiles(dir, $"*{fileExtension}")
            .Select(Path.GetFileNameWithoutExtension)
            .ToArray();
    }

    private string GetFilePath(string filename)
    {
        string dir = GetSaveDirectory();
        Directory.CreateDirectory(dir); // Ensure exists
        return Path.Combine(dir, filename + fileExtension);
    }

    private string GetSaveDirectory()
    {
        return Path.Combine(Application.persistentDataPath, saveDirectory);
    }
}
```

---

## 4. UI/UX Design

### 4.1 Save Dialog
**Location**: New menu or button in PaletteUIManager

```
┌─────────────────────────────────┐
│  Save Circuit                   │
├─────────────────────────────────┤
│  Circuit Name: [____________]   │
│  Description:  [____________]   │
│                [____________]   │
│  Author:       [Your Name___]   │
│  Tags:         [homework____]   │
│                                 │
│  [ Save ]  [ Cancel ]           │
└─────────────────────────────────┘
```

### 4.2 Load Dialog
**Location**: New menu button

```
┌─────────────────────────────────┐
│  Load Circuit                   │
├─────────────────────────────────┤
│  ┌─────────────────────────┐   │
│  │ Series_Circuit_Example  │   │
│  │ Parallel_Practice       │   │
│  │ Homework_Assignment_3   │   │
│  │ Complex_Mixed_Circuit   │   │
│  └─────────────────────────┘   │
│                                 │
│  Preview:                       │
│  ┌───────────────────────┐     │
│  │ Components: 4         │     │
│  │ Wires: 4              │     │
│  │ Author: Student Name  │     │
│  │ Modified: 2025-01-15  │     │
│  └───────────────────────┘     │
│                                 │
│  [ Load ]  [ Delete ]  [ Cancel ]
└─────────────────────────────────┘
```

### 4.3 Keyboard Shortcuts
- **Ctrl+S**: Quick save (updates last saved file)
- **Ctrl+Shift+S**: Save As (new name)
- **Ctrl+O**: Open/Load circuit
- **Ctrl+N**: New circuit (clear with confirmation)

---

## 5. Implementation Phases

### Phase 1: Core Serialization (Week 1)
**Goal**: Basic save/load functionality without UI

**Tasks**:
1. Create data model classes (CircuitSaveData, ComponentSaveData, etc.)
2. Implement CircuitSerializer.SerializeCircuit()
3. Implement CircuitSerializer.DeserializeCircuit()
4. Add unit tests for serialization/deserialization
5. Test with simple circuits (battery + 1 bulb)

**Success Criteria**:
- ✅ Can serialize circuit to JSON string
- ✅ Can deserialize JSON back to data objects
- ✅ JSON is human-readable and properly formatted

### Phase 2: File I/O (Week 1-2)
**Goal**: Save/load to actual files

**Tasks**:
1. Implement SaveLoadManager.SaveCircuit()
2. Implement SaveLoadManager.LoadCircuit()
3. Handle file system paths (Application.persistentDataPath)
4. Add error handling (file not found, invalid JSON, etc.)
5. Test on Windows and Mac

**Success Criteria**:
- ✅ Files saved to correct directory
- ✅ Files can be loaded back successfully
- ✅ Cross-platform compatibility

### Phase 3: Circuit Reconstruction (Week 2)
**Goal**: Recreate circuit in Unity from save data

**Tasks**:
1. Implement CircuitLoader.LoadCircuit()
2. Create components at correct positions
3. Restore component properties (voltage, resistance)
4. Recreate wire connections
5. Handle terminal connections correctly

**Success Criteria**:
- ✅ Loaded circuit visually matches saved circuit
- ✅ All electrical properties preserved
- ✅ Circuit solves correctly after loading

### Phase 4: UI Integration (Week 2-3)
**Goal**: Add user-facing save/load dialogs

**Tasks**:
1. Design and implement Save Dialog
2. Design and implement Load Dialog
3. Add file browser/selector UI
4. Implement metadata editing (name, description, tags)
5. Add keyboard shortcuts (Ctrl+S, Ctrl+O)

**Success Criteria**:
- ✅ Users can save circuits with metadata
- ✅ Users can browse and load saved circuits
- ✅ Intuitive workflow (minimal clicks)

### Phase 5: Advanced Features (Week 3-4)
**Goal**: Polish and additional functionality

**Tasks**:
1. Auto-save (every 5 minutes)
2. Undo/Redo using save states
3. Export to shareable format (compressed JSON)
4. Import from clipboard (paste JSON)
5. Version migration (handle old saves)
6. Circuit thumbnails/previews

**Success Criteria**:
- ✅ No data loss from crashes
- ✅ Easy sharing between users
- ✅ Old saves still load in new versions

---

## 6. Edge Cases & Validation

### Data Integrity
- **Missing components**: Skip wires referencing non-existent components
- **Invalid terminal names**: Fallback to positional terminals (first/second)
- **Corrupt JSON**: Show error, don't crash
- **Circular references**: Validate graph structure

### Version Compatibility
```csharp
private static void ValidateVersion(string version)
{
    var major = int.Parse(version.Split('.')[0]);

    if (major > 2)
    {
        Debug.LogWarning("⚠️ Save file from newer version, may have compatibility issues");
    }
    else if (major < 2)
    {
        Debug.Log("🔄 Migrating save file from v1.x to v2.x");
        MigrateFromV1(saveData);
    }
}
```

### File System Issues
- **Disk full**: Catch IOException, notify user
- **Permission denied**: Fallback to alternate directory
- **WebGL builds**: Use PlayerPrefs instead of File I/O

---

## 7. Testing Strategy

### Unit Tests
```csharp
[Test]
public void SerializeSimpleCircuit_ProducesValidJSON()
{
    // Arrange
    var battery = CreateBattery();
    var bulb = CreateBulb();
    var wire = CreateWire(battery, bulb);

    // Act
    string json = CircuitSerializer.SerializeCircuit(
        new List<CircuitComponent3D> { battery, bulb },
        new List<GameObject> { wire }
    );

    // Assert
    Assert.IsNotNull(json);
    var saveData = JsonUtility.FromJson<CircuitSaveData>(json);
    Assert.AreEqual(2, saveData.components.Length);
    Assert.AreEqual(1, saveData.wires.Length);
}

[Test]
public void LoadCircuit_RestoresComponentPositions()
{
    // Arrange
    var saveData = CreateTestSaveData();

    // Act
    loader.LoadCircuit(saveData);

    // Assert
    var loadedBattery = GameObject.Find("Battery_0");
    Assert.AreEqual(new Vector3(1, 0.5f, 1), loadedBattery.transform.position);
}
```

### Integration Tests
1. **Round-trip test**: Save circuit → Load circuit → Verify match
2. **Complex circuit test**: 10+ components, 15+ wires
3. **Edge case test**: Empty circuit, single component, disconnected components

### Manual Testing Checklist
- [ ] Save simple series circuit
- [ ] Load and verify electrical values match
- [ ] Save complex parallel circuit
- [ ] Test save/load with all component types
- [ ] Test invalid file handling
- [ ] Test cross-platform (Windows ↔ Mac)
- [ ] Test WebGL build (PlayerPrefs)

---

## 8. File Format Considerations

### Why JSON?
**Pros**:
- ✅ Human-readable (debugging, manual editing)
- ✅ Unity's JsonUtility built-in support
- ✅ Easy to extend (new properties)
- ✅ Shareable (copy/paste, version control)

**Cons**:
- ❌ Larger file size vs binary
- ❌ Slower parsing vs binary
- ❌ No built-in encryption

**Decision**: Use JSON for v1, consider binary for v2 if performance becomes issue.

### Alternative: Binary Format
```csharp
// BinaryFormatter (deprecated in Unity)
// Protobuf-net (3rd party)
// MessagePack (3rd party)
```

**When to switch**: If circuits exceed 100+ components and load times > 2 seconds.

---

## 9. Integration with Existing Systems

### Modified Files
1. **PaletteUIManager.cs**: Add Save/Load buttons
2. **CircuitControlManager.cs**: Add save/load methods
3. **CircuitManager.cs**: Expose Components/Wires for serialization

### New Files
1. `CircuitSerializer.cs` (Core serialization logic)
2. `CircuitSaveData.cs` (Data model classes)
3. `CircuitLoader.cs` (Circuit reconstruction)
4. `SaveLoadManager.cs` (File I/O and UI)
5. `SaveDialogUI.cs` (Save dialog UI)
6. `LoadDialogUI.cs` (Load dialog UI)

---

## 10. Success Metrics

### Performance Targets
- Save time: <100ms for typical circuit (10 components)
- Load time: <500ms for typical circuit
- File size: <50KB for typical circuit (JSON)

### User Experience
- Minimal clicks: 2 clicks to save (Ctrl+S → Enter)
- No data loss: Auto-save every 5 minutes
- Intuitive: First-time users understand without tutorial

---

## Next Steps

1. **Review this plan** with user/stakeholders
2. **Prioritize phases** (which features are MVP?)
3. **Start Phase 1** (Core Serialization)
4. **Create unit tests** before implementation
5. **Iterate based on feedback**

---

**Timeline Estimate**: 3-4 weeks for full implementation
**MVP Timeline**: 1-2 weeks for basic save/load without UI

