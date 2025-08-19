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

## 🏗️ **Manager Architecture**

### **Circuit System (5 managers)**
| Manager | Lines | Responsibility |
|---------|-------|----------------|
| CircuitManager | 240 | Central hub, component/wire registration |
| CircuitSolverManager | 259 | Solving logic, timing, solver integration |
| CircuitNodeManager | 165 | Spatial nodes, connectivity graph |
| CircuitDebugManager | 273 | Logging, reports, debugging |
| CircuitEventManager | 122 | State change notifications |

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
**After:** 13 focused managers (avg 165 lines each)

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

## 🎯 **Current Status: v1.0 Production Ready**

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