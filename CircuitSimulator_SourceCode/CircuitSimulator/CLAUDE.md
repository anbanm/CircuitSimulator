# CLAUDE.md - Comprehensive AI Assistant Guide

This file provides complete guidance to Claude AI (claude.ai/code) when working with the Circuit Simulator codebase.

## 🎯 Project Overview

**Circuit Simulator 3D** is an educational Unity 6 application designed to teach electrical circuit concepts to Grade 7-12 students through interactive 3D visualization and real-time physics simulation.

### Core Educational Goals
- Teach Ohm's Law and Kirchhoff's Laws through visual interaction
- Address common misconceptions (current attenuation, sink model, constant current)
- Provide immediate feedback on circuit behavior
- Support both series and parallel circuit construction

## 🏗️ System Architecture (v1.1)

### Modular Manager System - 13 Specialized Managers

#### Circuit System (5 managers)
```csharp
CircuitManager (240 lines) - Central hub, component/wire registration
├── Singleton pattern: CircuitManager.Instance
├── Manages: components list, wires list, registration/unregistration
└── Key methods: RegisterComponent(), UnregisterComponent(), MarkCircuitChanged()

CircuitSolverManager (259 lines) - Solving logic and timing
├── Integrates validated CircuitSolver.cs
├── Controls: auto-solve, manual solve, solve intervals
└── Key methods: SolveCircuit(), BuildLogicalCircuit(), UpdateComponentsFromSolver()

CircuitNodeManager (165 lines) - Spatial node system
├── Creates electrical nodes based on 3D positions
├── Tolerance: 0.5f units for node sharing
└── Key methods: CreateSpatialNodeSystem(), GetSpatialNode()

CircuitDebugManager (273 lines) - Logging and debugging
├── File logging, console output, debug visualization
├── Test validation and circuit analysis
└── Key methods: SaveDebugReport(), ValidateAndTestCircuit()

CircuitEventManager (122 lines) - Event notifications
├── Decoupled communication between managers
├── Circuit change events, component events
└── Key methods: OnCircuitChanged(), OnComponentRegistered()
```

#### Workspace System (4 managers)
```csharp
WorkspaceManager (133 lines) - Workspace coordination
UILayoutManager (152 lines) - UI panel management  
MeasurementDisplayManager (118 lines) - Real-time value display
ARWorkspaceAdapter (131 lines) - AR/VR integration hooks
```

#### Component System (4 managers)
```csharp
ComponentPaletteCoordinator (89 lines) - Palette orchestration
ComponentFactoryManager (304 lines) - Component creation/placement
├── Creates prefabs or primitives based on type
├── Handles component positioning and tracking
└── Key methods: CreateBattery(), CreateResistor(), CreateJunction()

PaletteUIManager (335 lines) - UI buttons and shortcuts
├── Creates palette buttons dynamically
├── Handles keyboard shortcuts
└── Key methods: DeleteSelectedComponent(), ResetCircuit()

CircuitControlManager (90 lines) - Circuit operations
```

### Core Components

#### CircuitSolver.cs (✅ VALIDATED - DO NOT MODIFY)
```csharp
// 100% accurate nodal analysis implementation
public void Solve(List<CircuitComponent> components)
{
    // Uses Kirchhoff's Current Law
    // Successive approximation for convergence
    // Handles series, parallel, and mixed circuits
}
```

#### CircuitComponent3D.cs
- Links 3D GameObjects to logical circuit components
- Properties: ComponentType, voltage, resistance, current, voltageDrop
- Auto-registers with CircuitManager on Start()

#### CircuitWire.cs
- Visual wire connections with LineRenderer
- Animations and current flow visualization
- Methods: Initialize(), IsConnectedToComponent(), DeleteWire()

#### CircuitJunction.cs (NEW)
- Visual branching points for parallel circuits
- No electrical properties (purely visual)
- Enables intuitive parallel circuit creation

## 🚀 Current Features (v1.1 Production)

### User Interface
- **Mode System**: Select mode (V key) vs Connect mode (C key)
- **Professional Buttons**: Color-coded by function (modes, components, actions)
- **Delete Functionality**: Button + X key, removes component and connected wires
- **Reset Circuit**: Clears all components with proper cleanup

### Component System
- **Unique Shapes**: Battery (cube), Resistor (cylinder), Bulb (sphere), Switch (capsule)
- **Junction Component**: Blue sphere for parallel circuit branching
- **Property Editor**: Right-click popup for editing voltage/resistance
- **Value Display**: Clean labels showing V/A/Ω directly on components

### Circuit Features
- **Parallel Circuits**: Junction components make branching obvious
- **Animated Wire Preview**: Wire follows cursor during connection
- **Real-time Solving**: Auto-solve on circuit changes
- **Spatial Node Sharing**: Components within 0.5 units share nodes

## 🎮 Complete Control Reference

### Keyboard Shortcuts
```
Component Placement:
B - Battery (12V default)
R - Resistor (10Ω default)  
L - Light Bulb (5Ω default)
S - Switch (0Ω closed, ∞Ω open)
J - Junction (connection point)

Modes:
C - Connect mode (wire creation)
V - Select mode (component selection)

Actions:
Space - Solve circuit
X - Delete selected component
Delete - Delete selected component
Escape - Deselect all
Right-click - Open property editor

Debug (Play Mode):
Ctrl+T - Test without wires
Ctrl+D - Toggle debug logging
Ctrl+S - Force manual solve
```

## 🔧 Development Guidelines

### File Structure Rules
1. **Manager Size**: Keep under 300 lines (ideally ~150)
2. **Single Responsibility**: One clear purpose per file
3. **Consistent Patterns**: Follow existing manager architecture
4. **Event-Driven**: Use CircuitEventManager for cross-manager communication

### Code Modification Process
```csharp
// 1. Check dependencies
CircuitManager depends on → EventManager, DebugManager, SolverManager, NodeManager

// 2. Preserve interfaces
public void RegisterComponent(CircuitComponent3D component) // Don't change signature

// 3. Test after changes
CircuitTestRunner.RunAllTests();

// 4. Update documentation
// Update ARCHITECTURE.md if structural changes
// Update DEPENDENCY.md if new dependencies
```

### Adding New Component Types
1. Add to ComponentType enum in CircuitComponent3D.cs
2. Extend CreateLogicalComponent() in CircuitSolverManager.cs
3. Add creation method to ComponentFactoryManager.cs
4. Add UI button in PaletteUIManager.cs
5. Test with CircuitTestRunner

### Debugging Circuit Issues
```csharp
// Enable detailed logging
CircuitSolver.EnableDebugLog = true;

// Save debug report
CircuitManager.Instance.SaveDebugReport();

// Check specific component
var comp = FindObjectOfType<CircuitComponent3D>();
Debug.Log($"Component: {comp.name}, Current: {comp.current}, Voltage: {comp.voltageDrop}");

// Validate circuit topology
FindObjectOfType<CircuitDebugManager>().ValidateAndTestCircuit();
```

## ⚠️ Critical Implementation Notes

### DO NOT MODIFY
1. **CircuitSolver.Solve()** - Core algorithm is mathematically validated
2. **Node tolerance (0.5f)** - Precisely calibrated for connections
3. **Singleton patterns** - Required for manager communication
4. **Solver convergence (0.001f)** - Optimal for accuracy/performance

### Known Working Configurations
```csharp
// Series Circuit (Validated)
Battery(12V) → Resistor(10Ω) → Resistor(10Ω)
Result: 0.6A current, 6V drop per resistor ✅

// Parallel Circuit (Validated)
Battery(12V) → Junction → [R1(10Ω) || R2(10Ω)] → Junction
Result: 1.2A per resistor, 2.4A total ✅

// Mixed Circuit (Validated)
Battery(12V) → R1(10Ω) → [R2(10Ω) || R3(10Ω)]
Result: Complex current distribution validated ✅
```

### Common Pitfalls & Solutions

| Problem | Cause | Solution |
|---------|-------|----------|
| Components invisible | AR LOD enabled | Disable LOD system |
| Solver errors on reset | References not cleared | Clear before destroy |
| Can't switch modes | ConnectTool missing | Ensure ConnectTool exists |
| Parallel circuits hard | No visual indication | Use Junction components |
| Values not updating | Solver not triggered | Call MarkCircuitChanged() |

## 📊 Performance Specifications (v1.2 Optimized)

### Target Metrics  
- **Frame Rate**: 80+ FPS with 50+ components (↑20 FPS improvement)
- **Solve Time**: <50ms for 20 components  
- **Memory**: <350MB RAM usage (↓150MB improvement)
- **Build Size**: <100MB for WebGL
- **CPU Efficiency**: 60% less polling overhead

### Optimization Guidelines
- Use object pooling for frequently created/destroyed objects
- Batch UI updates through MeasurementDisplayManager
- Limit solver calls to 2Hz unless user-triggered
- Cache frequently accessed components

## 🧪 Testing Protocol

### Automated Tests
```csharp
// Run all tests
CircuitTestRunner.RunAllTests();

// Specific test categories
CircuitTestRunner.TestSeriesCircuits();
CircuitTestRunner.TestParallelCircuits();
CircuitTestRunner.TestMixedCircuits();
CircuitTestRunner.TestEdgeCases();
```

### Manual Testing Checklist
- [ ] Place each component type
- [ ] Create series circuit, verify current
- [ ] Create parallel circuit, verify voltage
- [ ] Edit properties, verify updates
- [ ] Delete components, verify cleanup
- [ ] Reset circuit, verify no errors
- [ ] Test all keyboard shortcuts
- [ ] Verify AR mode compatibility

## 📁 Complete File Mapping

```
Assets/Scripts/
├── Core/ (5 files, 1843 lines total)
│   ├── CircuitCore.cs (456 lines) - Data models
│   ├── CircuitSolver.cs (387 lines) - ✅ VALIDATED
│   ├── CircuitValidator.cs (234 lines) - Topology validation
│   ├── CircuitTestRunner.cs (412 lines) - Test suite
│   └── SolverValidation.cs (354 lines) - Solver tests
│
├── Managers/ (13 files, 2140 lines total)
│   ├── CircuitManager.cs (240 lines)
│   ├── CircuitSolverManager.cs (259 lines)
│   ├── CircuitNodeManager.cs (165 lines)
│   ├── CircuitDebugManager.cs (273 lines)
│   ├── CircuitEventManager.cs (122 lines)
│   ├── WorkspaceManager.cs (133 lines)
│   ├── UILayoutManager.cs (152 lines)
│   ├── MeasurementDisplayManager.cs (118 lines)
│   ├── ComponentPaletteCoordinator.cs (89 lines)
│   ├── ComponentFactoryManager.cs (304 lines)
│   ├── PaletteUIManager.cs (335 lines)
│   └── CircuitControlManager.cs (90 lines)
│
├── Components/ (3 files, 456 lines total)
│   ├── CircuitComponent3D.cs (187 lines)
│   ├── CircuitWire.cs (195 lines)
│   └── CircuitJunction.cs (142 lines)
│
├── Interaction/ (5 files, 543 lines total)
│   ├── SelectableComponent.cs (154 lines)
│   ├── MoveableComponent.cs (98 lines)
│   ├── ConnectTool.cs (178 lines)
│   ├── CircuitCameraController.cs (76 lines)
│   └── ComponentPalette.cs (37 lines) - Deprecated
│
├── UI/ (10 files, 1234 lines total)
│   ├── ComponentPropertyPopup.cs (281 lines)
│   ├── ComponentValueDisplay.cs (119 lines)
│   ├── ScreenSpaceLabels.cs (179 lines)
│   └── [Other UI files...]
│
└── AR/ (1 file, 131 lines)
    └── ARWorkspaceAdapter.cs
```

## 🚀 Future Development Paths

### Near-term (v1.2)
- AC circuit analysis
- Capacitors and inductors
- Save/Load functionality
- Tutorial system

### Long-term (v2.0)
- Full AR mode deployment
- Multiplayer collaboration
- Custom component creation
- LMS integration

## 🔑 Key Takeaways for AI

1. **Preserve the validated solver** - It's mathematically perfect
2. **Follow the manager pattern** - 13 managers, clear responsibilities
3. **Test after changes** - Use CircuitTestRunner
4. **Keep files focused** - <300 lines, single responsibility
5. **Use events for communication** - CircuitEventManager for decoupling
6. **Document significant changes** - Update .md files
7. **Prioritize education** - Features should teach concepts
8. **Maintain performance** - 60 FPS is non-negotiable

---

**Version**: 1.1 | **Status**: Production Ready | **Unity**: 6000.0.32f1