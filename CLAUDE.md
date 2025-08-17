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
├── Scripts/
│   ├── CircuitCore.cs              # Core circuit solving algorithms & data models
│   ├── Circuit3DManager.cs         # Main 3D integration manager (singleton)
│   ├── CircuitComponent3D.cs       # 3D component representation
│   ├── CircuitWire.cs             # Wire connection handling
│   └── CircuitTestRunner.cs        # Testing and validation framework
├── setup.sh                       # Project setup script
└── CircuitLibrary_Package_Structure.md  # Package architecture documentation
```

### Setup Commands
Run the setup script to create Unity project structure:
```bash
./setup.sh
```

This creates a Unity project with proper folder structure and copies scripts to `CircuitSimulator/Assets/Scripts/`.

## Core Architecture

### Circuit Solving System
- **CircuitCore.cs**: Contains the circuit solver engine with proper electrical analysis algorithms
  - `CircuitNode`: Represents electrical nodes with voltage/current tracking
  - `CircuitComponent`: Abstract base for all components (Battery, Resistor, Bulb, Wire, Switch)
  - `CircuitSolver`: Main solving algorithm supporting series, parallel, and mixed circuits

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