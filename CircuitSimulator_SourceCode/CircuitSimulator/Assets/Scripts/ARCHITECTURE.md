# Circuit Simulator Architecture

## 📁 **Directory Structure**
```
Assets/Scripts/
├── Core/           # Circuit logic (solver, validator, components)
├── Managers/       # 14 specialized managers (modular architecture)
├── Components/     # CircuitComponent3D, CircuitWire
├── Interaction/    # User input (selection, movement, connection)
├── UI/            # Dashboard, visualizer, controls
└── AR/            # AR-specific adaptations
```

## 🏗️ **Manager Architecture**

### **Circuit System (5 managers)**
| Manager | Lines | Responsibility |
|---------|-------|----------------|
| CircuitManager | 185 | Central hub, component/wire registration |
| CircuitSolverManager | 157 | Solving logic, timing, solver integration |
| CircuitNodeManager | 120 | Spatial nodes, connectivity graph |
| CircuitDebugManager | 132 | Logging, reports, debugging |
| CircuitEventManager | 118 | State change notifications |

### **Workspace System (4 managers)**
| Manager | Lines | Responsibility |
|---------|-------|----------------|
| WorkspaceManager | 202 | Workspace coordination, AR mode |
| UILayoutManager | 293 | Panel layout, button creation |
| MeasurementDisplayManager | 184 | Real-time metrics display |
| ARWorkspaceAdapter | 306 | AR scaling, tracking, LOD |

### **Component System (4 managers)**
| Manager | Lines | Responsibility |
|---------|-------|----------------|
| ComponentPaletteCoordinator | 87 | Palette coordination |
| ComponentFactoryManager | 195 | Component creation/placement |
| PaletteUIManager | 124 | UI buttons, shortcuts |
| CircuitControlManager | 89 | Circuit operations |

## 🔗 **Dependencies**
See [DEPENDENCY.md](./DEPENDENCY.md) for detailed dependency maps and analysis.

## 🎯 **Key Paths**

**Component Registration:**
`Start() → CircuitManager.RegisterComponent() → EventManager.Notify() → SolverManager.MarkChanged()`

**Circuit Solving:**
`Button/Space → CircuitManager.SolveCircuit() → SolverManager.Solve() → CircuitSolver.Solve() → Update Components`

**Component Creation:**
`UI Click → PaletteUIManager → ComponentFactory.Create() → CircuitComponent3D → CircuitManager.Register()`

## 📊 **Architectural Patterns**

1. **Singleton**: CircuitManager.Instance (central coordinator)
2. **Manager**: Specialized managers with single responsibilities
3. **Observer**: CircuitEventManager for state notifications
4. **Factory**: ComponentFactoryManager for creation
5. **Adapter**: ARWorkspaceAdapter for AR features

## ⚡ **Performance**

| Hotspot | Solution |
|---------|----------|
| SolverManager.Update() | Throttled with solveDelay |
| NodeManager.BuildNodes() | Spatial indexing for O(n²) → O(n log n) |
| MeasurementDisplay.Update() | Update interval limiting |

## 🧪 **Testing Example**
```csharp
// Integration test
var manager = CircuitManager.Instance;
var factory = FindFirstObjectByType<ComponentFactoryManager>();

var battery = factory.CreateBattery();
var resistor = factory.CreateResistor();
var wire = ConnectTool.CreateWire(battery, resistor);

manager.SolveCircuit();
Assert.IsTrue(battery.current > 0);
```

## 📈 **Results**

**Before:** 3 monolithic files (1,780 lines)
**After:** 14 focused managers (avg 150 lines each)

**Benefits:**
- ✅ Readable (all files < 310 lines)
- ✅ Testable (independent managers)
- ✅ Maintainable (clear responsibilities)
- ✅ Scalable (easy to add features)
- ✅ AR-Ready (dedicated AR components)

## 🚀 **Usage**

```csharp
// Old (deprecated)
Circuit3DManager.Instance.RegisterComponent(comp);

// New (modular)
CircuitManager.Instance.RegisterComponent(comp);

// Direct manager access
var solver = FindFirstObjectByType<CircuitSolverManager>();
solver.EnableDebugMode(true);
```

## 📋 **Migration Complete**
- Circuit3DManager → 6 specialized managers ✅
- CircuitWorkspaceUI → 4 workspace managers ✅
- ComponentPalette → 4 component managers ✅
- All deprecated references updated ✅

**Next:** Testing, performance profiling, complete AR features