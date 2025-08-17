# Circuit Simulator - Unity 3D Project

## 📋 Overview

A comprehensive 3D circuit simulator built in Unity for educational purposes. Features real-time circuit solving, visual feedback, and support for batteries, resistors, bulbs, switches, and wires.

## ⚡ Core Features

- **Real-time Circuit Solving**: Uses proper electrical circuit analysis algorithms
- **3D Interactive Components**: Drag-and-drop circuit building
- **Visual Feedback**: Components light up, show current flow, voltage displays
- **Educational Focus**: Designed for Grade 7-12 physics education
- **Debug Tools**: Comprehensive logging and testing capabilities

## 🗂️ Project Structure

```
CircuitSimulator_SourceCode/
├── Scripts/
│   ├── CircuitCore.cs          # Core circuit solving algorithms
│   ├── Circuit3DManager.cs     # Main 3D integration manager
│   ├── CircuitComponent3D.cs   # 3D component representation
│   ├── CircuitWire.cs          # Wire connection handling
│   └── CircuitTestRunner.cs    # Testing and validation
└── README.md                   # This file
```

## 🚀 Unity Setup Instructions

### 1. Create New Unity Project
```
1. Open Unity Hub
2. Click "New Project"
3. Select "3D (URP)" template
4. Name: "CircuitSimulator"
5. Click "Create"
```

### 2. Import Scripts
```
1. Copy all files from Scripts/ folder
2. Paste into Unity Assets/ folder
3. Wait for Unity to compile
```

### 3. Scene Setup

#### Create Circuit3DManager GameObject:
```
1. Right-click in Hierarchy
2. Create Empty GameObject
3. Rename to "Circuit3DManager"
4. Add Component → Circuit3DManager script
5. Configure inspector settings:
   - Auto Solve: ✓
   - Solve Interval: 0.5
   - Event Based Solving: ✓
   - Debug Solver: ✓
```

#### Create Component Prefabs:

**Battery Prefab:**
```
1. Create → 3D Object → Cube
2. Rename to "Battery"
3. Scale: (2, 0.5, 1)
4. Add Component → CircuitComponent3D
5. Set Component Type: Battery
6. Set Voltage: 6
7. Create Material: "BatteryMaterial" (Red color)
8. Drag to Project to create prefab
```

**Bulb Prefab:**
```
1. Create → 3D Object → Sphere
2. Rename to "Bulb" 
3. Scale: (0.8, 0.8, 0.8)
4. Add Component → CircuitComponent3D
5. Set Component Type: Bulb
6. Set Resistance: 50
7. Create Material: "BulbMaterial" (Yellow color)
8. Drag to Project to create prefab
```

**Resistor Prefab:**
```
1. Create → 3D Object → Cylinder
2. Rename to "Resistor"
3. Scale: (0.3, 1, 0.3)
4. Add Component → CircuitComponent3D
5. Set Component Type: Resistor
6. Set Resistance: 100
7. Create Material: "ResistorMaterial" (Brown color)
8. Drag to Project to create prefab
```

### 4. Wire System Setup

**Create Wire Prefab:**
```
1. Create → 3D Object → Cylinder
2. Rename to "Wire"
3. Scale: (0.1, 1, 0.1)
4. Add Component → CircuitWire
5. Create Material: "WireMaterial" (Gray color)
6. Drag to Project to create prefab
```

### 5. UI Setup (Optional)

**Create UI Canvas:**
```
1. Right-click Hierarchy → UI → Canvas
2. Add UI → Button for "SOLVE" button
3. Connect button OnClick to Circuit3DManager.ForceRegisterAllComponents()
4. Add UI → Text for displaying current/voltage values
```

### 6. Testing Setup

**Add Test Runner:**
```
1. Create Empty GameObject
2. Rename to "TestRunner"
3. Add Component → CircuitTestRunner
4. This will automatically run tests on play
```

## 🎯 Usage Instructions

### Basic Circuit Building:
1. Drag prefabs into scene
2. Position components to form circuit
3. Create wire connections between components
4. Press Play to see circuit solve in real-time

### Manual Testing:
- **Ctrl+S**: Force solve circuit
- **Ctrl+T**: Test without wires
- **Ctrl+D**: Toggle debug logging

### Inspector Commands:
Right-click on Circuit3DManager in inspector:
- **Force Register All Components**: Find and register all components
- **Test Known Working Circuit**: Validate solver with known good circuit
- **Debug Component Registration**: Check component registration status
- **Save Debug Report**: Export detailed debug information

## 🔧 Key Script Functions

### CircuitCore.cs
- `CircuitSolver.Solve()`: Main circuit analysis algorithm
- Supports series, parallel, and mixed circuits
- Handles batteries, resistors, bulbs, switches

### Circuit3DManager.cs
- `SolveCircuit()`: Converts 3D scene to logical circuit and solves
- `BuildLogicalCircuit()`: Maps 3D components to circuit nodes
- `UpdateComponentsFromSolver()`: Updates visuals with solved values

### CircuitComponent3D.cs
- Represents individual circuit components
- Auto-registers with Circuit3DManager
- Stores electrical properties and results

### CircuitWire.cs
- Represents connections between components
- Creates shared nodes for circuit solving
- Validates wire connections

## 🐛 Debugging

### Common Issues:

**"Components: 0, Wires: 0"**
- Solution: Use "Force Register All Components" command
- Ensure components have CircuitComponent3D script attached

**Tiny Current Values (e.g., 0.0000006A)**
- Solution: Remove disconnected components from scene
- Ensure proper wire connections between all components

**No Voltage Drop Across Components**
- Solution: Check wire connections
- Verify component registration
- Use debug logging to trace circuit topology

### Debug Logs:
- Enable "Debug Solver" in Circuit3DManager inspector
- Check Console for detailed circuit analysis logs
- Use "Save Debug Report" to export full debug information

## 📚 Educational Applications

### Lesson Plans:
1. **Series Circuits**: Single path for current
2. **Parallel Circuits**: Multiple current paths
3. **Ohm's Law**: V = I × R relationships
4. **Power Calculations**: P = V × I
5. **Circuit Analysis**: Voltage division, current distribution

### Student Activities:
- Build circuits to match given specifications
- Predict vs. measured current/voltage comparisons
- Troubleshooting broken circuits
- Design challenges with constraints

## 🔄 Extending the System

### Adding New Component Types:
1. Add new enum value to `ComponentType`
2. Extend `BuildLogicalCircuit()` switch statement
3. Create corresponding CircuitCore component class
4. Add visual prefab and materials

### Advanced Features:
- AC circuit analysis
- Capacitors and inductors
- Oscilloscope visualization
- Multimeter tool integration
- Circuit schematic view

## 📋 Requirements

- **Unity Version**: 2022.3 LTS or newer
- **Render Pipeline**: URP (Universal Render Pipeline)
- **Platform**: Windows, Mac, WebGL supported
- **.NET**: Framework 4.x equivalent

## 🤝 Development Notes

### Architecture:
- **Separation of Concerns**: 3D visualization separate from circuit logic
- **Event-Driven**: Circuit updates trigger automatic re-solving
- **Extensible**: Easy to add new component types
- **Educational**: Clear, commented code for learning

### Performance:
- **Real-time Solving**: Optimized for 60fps with complex circuits
- **Efficient Node Mapping**: Minimal memory allocation during solving
- **Debug Mode Toggle**: Can disable expensive logging for production

## 🎓 Learning Outcomes

Students will understand:
- Basic electrical circuit principles
- Voltage, current, and resistance relationships
- Series vs. parallel circuit behavior
- Real-world circuit troubleshooting
- Scientific method through prediction/testing

---

**Ready to start building circuits!** 🔌⚡

Create your first circuit:
1. Place a Battery and Bulb in the scene
2. Connect them with a Wire
3. Press Play and watch the circuit solve!
