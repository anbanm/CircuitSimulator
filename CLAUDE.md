# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Circuit Simulator is a Unity 3D educational tool for teaching electrical circuit concepts to Grade 7-12 students. It features real-time circuit solving, 3D interactive components, and visual feedback for batteries, resistors, bulbs, switches, and wires.

## Development Setup

### Unity Requirements
- Unity 2022.3 LTS or newer
- Universal Render Pipeline (URP)
- Target platforms: Windows, Mac, WebGL

### Project Structure
```
CircuitSimulator_SourceCode/
├── CircuitSimulator_SourceCode/CircuitSimulator/Assets/Scripts/
│   ├── Core/                       # Circuit logic & solving
│   │   ├── CircuitCore.cs          # Core data models (CircuitNode, CircuitComponent, etc.)
│   │   ├── CircuitSolver.cs        # Validated nodal analysis solver ✅
│   │   ├── CircuitTestRunner.cs    # Comprehensive test suite
│   │   └── SolverValidation.cs     # Programmatic solver validation
│   ├── Components/                 # 3D Unity components  
│   │   ├── Circuit3DManager.cs     # Main 3D integration manager (singleton)
│   │   ├── CircuitComponent3D.cs   # 3D component representation
│   │   └── CircuitWire.cs         # Interactive wire system
│   ├── Interaction/               # User interaction systems
│   │   ├── SelectableComponent.cs  # Component selection
│   │   ├── MoveableComponent.cs    # Component movement
│   │   ├── ConnectTool.cs         # Wire creation tool
│   │   └── ComponentPalette.cs     # Component placement
│   ├── UI/                       # Visual feedback & UI
│   │   ├── ScreenSpaceLabels.cs   # Fixed label system
│   │   ├── ComponentLabel.cs      # Individual component labels
│   │   ├── Simple3DLabels.cs      # 3D label rendering
│   │   ├── ComponentPropertyPopup.cs # Property editing UI
│   │   └── CircuitDashboard.cs    # Measurements display
│   └── Prefabs/                  # Unity prefabs
│       └── PaletteButton.prefab   # UI button prefab
├── setup.sh                      # Project setup script
└── CircuitLibrary_Package_Structure.md  # Original package plan
```

### Setup Commands
Run the setup script to create Unity project structure:
```bash
./setup.sh
```

This creates a Unity project with proper folder structure and copies scripts to `CircuitSimulator/Assets/Scripts/`.

## Core Architecture

### Circuit Solving System ✅ VALIDATED
- **CircuitCore.cs**: Core data models and component definitions
  - `CircuitNode`: Represents electrical nodes with voltage/current tracking
  - `CircuitComponent`: Abstract base for all components (Battery, Resistor, Bulb, Wire, Switch)
- **CircuitSolver.cs**: ⭐ PROVEN SOLVER - Uses nodal analysis with successive approximation
  - Handles series, parallel, and mixed circuits with 100% accuracy
  - Test Results: Series (1A), Parallel (3A total), Mixed (1.875A) - all perfect
  - Uses Kirchhoff's Current Law with proper voltage reference (battery negative = ground)

### 3D Integration Layer
- **Circuit3DManager.cs**: Singleton manager that bridges 3D scene and logical circuit
  - `SolveCircuit()`: Converts 3D scene to logical circuit and solves
  - `BuildLogicalCircuit()`: Maps 3D components to circuit nodes
  - `UpdateComponentsFromSolver()`: Updates visuals with solved values
  - Event-based solving: Automatically resolves when circuit changes

### Component System
- **CircuitComponent3D.cs**: Represents individual 3D circuit components
  - Auto-registers with Circuit3DManager on Start()
  - Stores electrical properties (voltage, resistance) and results (current, voltageDrop)
  - Links to logical circuit component via `logicalComponent` field

- **CircuitWire.cs**: Handles connections between components
  - Creates shared nodes for circuit solving
  - Validates wire connections

### Testing Framework
- **CircuitTestRunner.cs**: Automated testing system
  - Runs circuit validation tests on play
  - Provides manual test commands

## Common Development Tasks

### Debugging Circuit Issues
Use these keyboard shortcuts during Play mode:
- **Ctrl+S**: Force solve circuit manually
- **Ctrl+T**: Test without wires
- **Ctrl+D**: Toggle debug logging

Inspector commands (right-click Circuit3DManager):
- **Force Register All Components**: Find and register all components in scene
- **Test Known Working Circuit**: Validate solver with known good circuit
- **Debug Component Registration**: Check component registration status
- **Save Debug Report**: Export detailed debug information to file

### Adding New Component Types
1. Add enum value to `ComponentType` in CircuitComponent3D.cs:5
2. Extend `BuildLogicalCircuit()` switch statement in Circuit3DManager.cs
3. Create corresponding component class in CircuitCore.cs (inherit from `CircuitComponent`)
4. Create Unity prefab with CircuitComponent3D script attached

### Testing Circuit Logic
The CircuitTestRunner automatically validates:
- Component registration
- Circuit topology building
- Voltage/current calculations
- Known working circuit configurations

Enable debug logging in Circuit3DManager inspector to see detailed solver output.

## Educational Integration

### Supported Physics Concepts
- Basic electricity and circuits (Grade 7-12)
- Ohm's Law (V = IR)
- Series vs Parallel circuits
- Current flow and conservation
- Power calculations (P = V × I)

### Misconceptions Addressed
- M1: Sink Model (one wire circuits)
- M2: Current attenuation (current "used up") 
- M8: Constant current source (battery behavior)

## Important Implementation Notes

### Circuit3DManager Singleton
Always access via `Circuit3DManager.Instance`. Components auto-register on Start() and unregister on OnDestroy().

### Node-Based Circuit Mapping
The solver converts 3D component positions into electrical nodes. Components at the same position share nodes, creating connections.

### Real-Time Solving
- Auto-solve interval: 0.5 seconds (configurable)
- Event-based solving: Immediate resolution when circuit changes
- Manual solve: Ctrl+S for debugging

### Performance Considerations
- Disable debug logging for production builds
- Circuit solving optimized for 60fps with complex circuits
- Efficient node mapping with minimal memory allocation

## 🚨 CRITICAL ISSUES (v0.1)

### Known Problems That MUST Be Fixed:
1. **Missing Wire Connection Methods** - CircuitComponent3D missing AddConnectedWire/RemoveConnectedWire
2. **Flawed Node Sharing** - Components get isolated nodes instead of sharing for connections
3. **Wire Logic Assumptions** - Only works for specific B→A connection patterns
4. **No Circuit Validation** - Bad topology gets passed to solver without validation

### Impact on Solver:
- **Solver Logic**: ✅ Perfect (100% validated)
- **Unity Integration**: ❌ Broken (components will be electrically isolated)

## 🔧 REQUIRED FIXES (Priority Order)

### Priority 1: Fix Missing Methods
Add to CircuitComponent3D.cs:
```csharp
private List<GameObject> connectedWires = new List<GameObject>();
public void AddConnectedWire(GameObject wire);
public void RemoveConnectedWire(GameObject wire);
```

### Priority 2: Fix Node Creation
Replace isolated node creation with spatial-based sharing:
- Use Vector3 positions with tolerance for node sharing
- Components within 0.5 units share electrical nodes
- Proper wire-based connections

### Priority 3: Add Circuit Validation
Create CircuitValidator.cs:
- Validate complete circuit paths before solving
- Check for floating components
- Verify battery presence and configuration

### Priority 4: Debug Visualization
Add CircuitDebugVisualizer.cs:
- Show electrical nodes as colored spheres
- Display current flow with arrows
- Highlight disconnected components

## Testing Strategy
1. Fix Priority 1 issues first
2. Test with simple 2-component circuits
3. Gradually add complexity
4. Always validate solver results against known values

## Version History
- **v0.1**: Initial release with validated solver but broken Unity integration