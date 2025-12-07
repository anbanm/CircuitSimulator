# Circuit Simulator Architecture

## 📁 **Directory Structure**
```
Assets/Scripts/
├── Core/           # Circuit logic (solver, components)
├── Managers/       # 13 specialized managers (modular architecture)
├── Components/     # CircuitComponent3D, CircuitWire
├── Interaction/    # User input (selection, movement, connection)
├── UI/            # Dashboard, visualizer, controls
└── AR/            # AR-specific adaptations
```

## 🏗️ **Manager Architecture (v1.2)**

### **Circuit System (5 managers)**
| Manager | Lines | Responsibility |
|---------|-------|----------------|
| CircuitManager | 240 | Central hub, component/wire registration, ComponentRegistry integration |
| CircuitSolverManager | 259 | Solving logic, timing, solver integration |
| CircuitNodeManager | 165 | Spatial nodes (0.5f tolerance), junction connectivity |
| CircuitDebugManager | 273 | Logging, reports, debugging |
| CircuitEventManager | 122 | Event-driven notifications, label updates |

### **Workspace System (4 managers)**
| Manager | Lines | Responsibility |
|---------|-------|----------------|
| WorkspaceManager | 133 | Workspace coordination, AR mode |
| UILayoutManager | 152 | Panel layout, button creation |
| MeasurementDisplayManager | 118 | Real-time metrics display |
| ARWorkspaceAdapter | 131 | AR scaling, tracking, LOD |

### **Component System (4 managers)**
| Manager | Lines | Responsibility |
|---------|-------|----------------|
| ComponentPaletteCoordinator | 89 | Palette coordination |
| ComponentFactoryManager | 304 | Component creation/placement |
| PaletteUIManager | 267 | UI buttons, shortcuts |
| CircuitControlManager | 90 | Circuit operations |

## 🔗 **Dependencies**
See [DEPENDENCY.md](./DEPENDENCY.md) for detailed dependency maps and analysis.

## 🔌 **Topology Layer (Path-Centric Traversal)**

> See **[TOPOLOGY_PATH_TRAVERSAL.md](../TOPOLOGY_PATH_TRAVERSAL.md)** for the authoritative topology design.

The topology layer bridges visual wires and the circuit solver using **path-centric traversal**:

```
Visual Layer (wires, endpoints, snapping)
    ↓
Topology Layer (JunctionTopologyManager.TraceTerminalPaths())
    ↓
Solver Layer (CircuitSolver nodal analysis)
```

**Key Algorithm**: BFS from each terminal through wire chains to find connected terminals. Terminals reachable via wire paths share the same `CircuitNode`.

**Why Path-Centric**: Wires can chain through multiple wire-to-wire junctions before reaching a component terminal. The old junction-centric approach failed on these chains.

## 🎯 **Key Paths**

**Component Registration:**
`Start() → CircuitManager.RegisterComponent() → EventManager.Notify() → SolverManager.MarkChanged()`

**Circuit Solving:**
`Button/Space → CircuitManager.SolveCircuit() → SolverManager.Solve() → CircuitSolver.Solve() → Update Components`

**Component Creation:**
`UI Click → PaletteUIManager → ComponentFactory.Create() → CircuitComponent3D → CircuitManager.Register()`

## 📊 **Architectural Patterns (v1.2)**

1. **Singleton**: CircuitManager.Instance (central coordinator)
2. **Manager**: Specialized managers with single responsibilities  
3. **Registry**: ComponentRegistry for O(1) manager lookups
4. **Observer**: CircuitEventManager for state notifications
5. **Factory**: ComponentFactoryManager for creation
6. **Adapter**: ARWorkspaceAdapter for AR features
7. **Event-Driven**: Label updates only on circuit changes

## ⚡ **Performance (v1.2 Optimized)**

| Hotspot | Solution |
|---------|----------|
| ~~FindObjectsOfType calls~~ | ✅ ComponentRegistry O(1) lookups |
| ~~Label polling every 0.1s~~ | ✅ Event-driven label updates |
| ~~Memory leaks~~ | ✅ Proper cleanup on scene changes |
| SolverManager.Update() | Throttled with solveDelay |
| NodeManager.BuildNodes() | Spatial indexing for O(n²) → O(n log n) |
| Junction connectivity | ✅ Visual-only, spatial node system handles connectivity |

## 🧪 **Testing Example (v1.2)**
```csharp
// Integration test - using optimized ComponentRegistry
var manager = CircuitManager.Instance;
var factory = ComponentRegistry.Instance.GetManager<ComponentFactoryManager>();

var battery = factory.CreateBattery();
var resistor = factory.CreateResistor();
var junction = factory.CreateJunction(); // Visual connection aid
var wire = ConnectTool.CreateWire(battery, resistor);

manager.SolveCircuit();
Assert.IsTrue(battery.current > 0);
// Junction provides visual feedback but doesn't affect electrical calculations
```

## 📈 **Results**

**Before:** 3 monolithic files (1,780 lines)
**After:** 13 focused managers (avg 165 lines each)

**Benefits:**
- ✅ Readable (all files < 310 lines)
- ✅ Testable (independent managers)
- ✅ Maintainable (clear responsibilities)
- ✅ Scalable (easy to add features)
- ✅ AR-Ready (dedicated AR components)

## 🚀 **Usage (v1.2)**

```csharp
// Old (deprecated)
Circuit3DManager.Instance.RegisterComponent(comp);

// New (modular)
CircuitManager.Instance.RegisterComponent(comp);

// v1.2 Optimized - ComponentRegistry for O(1) lookups
var solver = ComponentRegistry.Instance.GetManager<CircuitSolverManager>();
solver.EnableDebugMode(true);

// Event-driven label updates
LabelManager.Instance.UpdateLabelsForComponent(comp);
```

## 🎯 **Current Status: v1.2 Phase 1 Performance Optimized**

### **Migration Complete**
- Circuit3DManager → 5 specialized managers ✅
- CircuitWorkspaceUI → 4 workspace managers ✅ 
- ComponentPalette → 4 component managers ✅
- All deprecated references updated ✅
- Unity 6 setup complete ✅
- Professional UI implemented ✅
- Mode switching (Select/Connect) ✅
- Animated wire preview ✅
- Component positioning fixed ✅

### **Ready for Production**
- ✅ Fully functional circuit simulator
- ✅ Professional UI with mode switching
- ✅ Component creation with different primitive shapes
- ✅ Animated wire preview system
- ✅ Reset functionality with proper cleanup
- ✅ Keyboard shortcuts and visual feedback
- ✅ Validated nodal analysis solver
- ✅ AR-ready architecture

**Next:** Performance optimization, educational content integration