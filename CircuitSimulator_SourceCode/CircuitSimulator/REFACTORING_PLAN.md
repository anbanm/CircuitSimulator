# Circuit Simulator - Comprehensive Refactoring Plan

**Version:** 2.1
**Date:** October 25, 2025
**Status:** Planning Phase
**Target Completion:** Q1 2026

---

## Executive Summary

This plan addresses **17 files exceeding 350 lines**, with focus on the 3 critical manager files that violate the 300-line guideline established in CLAUDE.md. The refactoring will improve maintainability, testability, and adherence to Single Responsibility Principle (SRP).

### Key Metrics
- **Files to refactor:** 17 files (>350 lines)
- **Priority files:** 3 manager files (ComponentFactoryManager, PaletteUIManager, ConnectTool)
- **Total lines to restructure:** ~1,908 lines
- **Estimated effort:** 40-60 hours
- **Risk level:** Medium (requires careful dependency management)

---

## Phase 1: ComponentFactoryManager Refactoring (847 lines → 4 files)

### Priority: 🔴 CRITICAL
### Current State: Single file with 10+ responsibilities
### Target: 4 focused service classes (~200 lines each)

### Problem Analysis
ComponentFactoryManager.cs violates SRP by handling:
1. ✗ Component prefab management
2. ✗ GameObject instantiation (prefabs + primitives)
3. ✗ Visual appearance setup (materials, colors)
4. ✗ Circuit properties assignment (voltage, resistance)
5. ✗ Interaction capabilities (SelectableComponent, MoveableComponent)
6. ✗ Terminal/connection point creation
7. ✗ ComponentDefinition support (ScriptableObjects)
8. ✗ Component tracking and lifecycle management
9. ✗ Position calculation and grid placement
10. ✗ IComponentFactory interface implementation

### Refactoring Strategy

#### 1.1 Create ComponentCreationService.cs (~200 lines)
**Responsibility:** Core GameObject instantiation logic

```csharp
public class ComponentCreationService : MonoBehaviour
{
    // Prefab references
    [Header("Component Prefabs")]
    public GameObject batteryPrefab;
    public GameObject resistorPrefab;
    public GameObject bulbPrefab;
    public GameObject switchPrefab;

    // Methods to extract:
    - CreateComponentObject(name, position)
    - CreatePrimitiveForComponent(name, position)
    - GetPrefabForComponent(name)
    - CreatePrimitiveComponent(definition, position)
}
```

**Lines extracted:** ~180 lines
**Dependencies:** None (pure creation logic)

---

#### 1.2 Create ComponentVisualsService.cs (~150 lines)
**Responsibility:** Visual appearance and material management

```csharp
public class ComponentVisualsService : MonoBehaviour
{
    // Methods to extract:
    - SetupComponentVisuals(componentObject, color)
    - ApplyComponentDefinition(component, definition)
    - SetMaterialColor(renderer, color)
    - EnsureMaterialExists(renderer)
}
```

**Lines extracted:** ~120 lines
**Dependencies:** None (only Unity Renderer API)

---

#### 1.3 Create ComponentSetupService.cs (~250 lines)
**Responsibility:** Circuit properties, interaction, and terminals

```csharp
public class ComponentSetupService : MonoBehaviour
{
    // Dependencies
    private ComponentVisualsService visualsService;

    // Methods to extract:
    - SetupCircuitComponent(componentObject, type, voltage, resistance)
    - SetupComponentInteraction(componentObject)
    - SetupConnectionTerminals(componentObject)
    - SetupDefinitionBasedComponent(componentObject, definition)
    - AddCircuitComponent3D(componentObject, type, voltage, resistance)
}
```

**Lines extracted:** ~230 lines
**Dependencies:** ComponentVisualsService, CircuitComponent3D, SelectableComponent, MoveableComponent

---

#### 1.4 Create ComponentPlacementService.cs (~120 lines)
**Responsibility:** Position calculation and grid management

```csharp
public class ComponentPlacementService : MonoBehaviour
{
    [Header("Placement Settings")]
    public Transform workspacePlane;
    public float spacing = 2f;
    public bool useRandomizedPositioning = false;

    private int componentCount = 0;

    // Methods to extract:
    - GetNextPlacementPosition()
    - CalculateGridPosition(index)
    - CalculateRandomPosition()
    - FindNearestFreePosition(position)
}
```

**Lines extracted:** ~100 lines
**Dependencies:** WorkspaceManager (for plane reference)

---

#### 1.5 Refactor ComponentFactoryManager.cs (~280 lines)
**New Responsibility:** Orchestration and IComponentFactory implementation

```csharp
public class ComponentFactoryManager : MonoBehaviour, IComponentFactory
{
    // Service dependencies
    private ComponentCreationService creationService;
    private ComponentVisualsService visualsService;
    private ComponentSetupService setupService;
    private ComponentPlacementService placementService;

    // Component tracking (stays here)
    private List<GameObject> placedComponents = new List<GameObject>();

    // Public API methods (orchestrate services):
    - CreateBattery()
    - CreateResistor()
    - CreateBulb()
    - CreateSwitch()
    - CreateJunction()
    - CreateComponent(type, position)
    - CreateComponentFromDefinition(definition, position)
    - RemoveComponent(component)
    - ClearAllComponents()

    // IComponentFactory interface implementation
}
```

**Remaining lines:** ~280 lines
**Dependencies:** All 4 service classes

---

### 1.6 Migration Steps

**Step 1: Create Service Files (Non-breaking)**
- Create 4 new service files with empty classes
- Add [RequireComponent] attributes for auto-attachment
- Register services with ServiceLocator

**Step 2: Extract Pure Functions (Safe)**
- Move private static methods first (no state dependencies)
- Move prefab/primitive creation logic
- Move visual setup methods

**Step 3: Extract Stateful Logic (Careful)**
- Move component tracking to PlacementService
- Update references incrementally
- Test after each move

**Step 4: Update ComponentFactoryManager (Orchestration)**
- Inject service dependencies
- Replace internal calls with service calls
- Keep public API unchanged (facade pattern)

**Step 5: Cleanup and Testing**
- Remove old methods from ComponentFactoryManager
- Run integration tests
- Update ARCHITECTURE.md documentation

**Step 6: Update CLAUDE.md**
- Document new service architecture
- Update dependency graph
- Add migration notes for future AI assistants

---

### 1.7 Testing Strategy

**Unit Tests:**
- ComponentCreationService: Test prefab vs primitive creation
- ComponentVisualsService: Test material assignment, color changes
- ComponentSetupService: Test CircuitComponent3D properties
- ComponentPlacementService: Test grid positioning, collision detection

**Integration Tests:**
- Test ComponentFactoryManager orchestration
- Test service initialization order
- Test ServiceLocator registration

**Manual Testing:**
- Create each component type (Battery, Resistor, Bulb, Switch, Junction)
- Verify visuals, terminals, interaction
- Test ScriptableObject-based components

---

## Phase 2: PaletteUIManager Refactoring (498 lines → 3 files)

### Priority: 🟠 HIGH
### Current State: Single file with 5+ responsibilities
### Target: 3 focused controller classes (~165 lines each)

### Problem Analysis
PaletteUIManager.cs handles:
1. ✗ Button creation and UI layout
2. ✗ Component placement actions
3. ✗ Mode switching (Select/Connect)
4. ✗ Component deletion/reset actions
5. ✗ Keyboard shortcut handling

### Refactoring Strategy

#### 2.1 Create PaletteUIController.cs (~180 lines)
**Responsibility:** UI creation and layout

```csharp
public class PaletteUIController : MonoBehaviour
{
    [Header("UI References")]
    public Transform paletteContainer;
    public Button buttonPrefab;

    // Methods to extract:
    - CreatePaletteButtons()
    - CreateButton(label, color, onClick, tooltip)
    - FindPaletteContainer()
    - CreatePalettePanel(canvasTransform)
    - CreateButtonPrefab()
}
```

**Lines extracted:** ~160 lines
**Dependencies:** Unity UI system

---

#### 2.2 Create ComponentPlacementController.cs (~120 lines)
**Responsibility:** Component placement and mode actions

```csharp
public class ComponentPlacementController : MonoBehaviour
{
    // Dependencies
    private ComponentFactoryManager factoryManager;
    private ConnectTool connectTool;

    // Methods to extract:
    - PlaceBattery()
    - PlaceResistor()
    - PlaceBulb()
    - PlaceSwitch()
    - ActivateSelectMode()
    - ActivateConnectMode()
    - ActivateWireTool()
    - DeleteSelectedComponent()
    - ResetCircuit()
}
```

**Lines extracted:** ~110 lines
**Dependencies:** ComponentFactoryManager, ConnectTool, CircuitControlManager

---

#### 2.3 Create KeyboardShortcutHandler.cs (~100 lines)
**Responsibility:** Keyboard input processing

```csharp
public class KeyboardShortcutHandler : MonoBehaviour
{
    // Dependencies
    private ComponentPlacementController placementController;

    // Configurable shortcuts
    [Header("Shortcuts")]
    public KeyCode batteryKey = KeyCode.B;
    public KeyCode resistorKey = KeyCode.R;
    public KeyCode bulbKey = KeyCode.L;
    public KeyCode switchKey = KeyCode.S;
    public KeyCode selectModeKey = KeyCode.V;
    public KeyCode connectModeKey = KeyCode.C;
    public KeyCode deleteKey = KeyCode.X;

    // Methods to extract:
    - HandleKeyboardShortcuts()
    - ProcessComponentKeys()
    - ProcessModeKeys()
    - ProcessActionKeys()
}
```

**Lines extracted:** ~90 lines
**Dependencies:** ComponentPlacementController

---

#### 2.4 Refactor PaletteUIManager.cs (~200 lines)
**New Responsibility:** Coordination and initialization

```csharp
public class PaletteUIManager : MonoBehaviour
{
    // Service dependencies
    private PaletteUIController uiController;
    private ComponentPlacementController placementController;
    private KeyboardShortcutHandler shortcutHandler;

    // Initialization and lifecycle
    public void Initialize(ComponentFactoryManager factory, CircuitControlManager control)
    {
        // Initialize services
        // Setup event bindings
        // Create UI
    }

    void Update()
    {
        // Delegate to shortcutHandler
    }
}
```

**Remaining lines:** ~200 lines

---

### 2.5 Migration Steps

**Step 1: Create Controller Files**
- Create 3 controller classes
- Define public APIs
- Add component dependencies

**Step 2: Extract UI Logic**
- Move button creation to PaletteUIController
- Update button callbacks to use new controllers

**Step 3: Extract Placement Logic**
- Move component creation calls to ComponentPlacementController
- Update mode switching logic

**Step 4: Extract Input Logic**
- Move keyboard handling to KeyboardShortcutHandler
- Make shortcuts configurable

**Step 5: Update PaletteUIManager**
- Inject controllers
- Simplify to coordination role
- Maintain public API compatibility

---

## Phase 3: ConnectTool Refactoring (563 lines → 4 files)

### Priority: 🟠 HIGH
### Current State: Single file with 6+ responsibilities
### Target: 4 focused service classes (~140 lines each)

### Problem Analysis
ConnectTool.cs handles:
1. ✗ Mode management (Select/Connect switching)
2. ✗ Component click handling
3. ✗ Terminal selection logic
4. ✗ Wire preview rendering
5. ✗ Wire creation and LineRenderer setup
6. ✗ Update loop for preview animation

### Refactoring Strategy

#### 3.1 Create ConnectionModeController.cs (~100 lines)
**Responsibility:** Mode state management

```csharp
public class ConnectionModeController : MonoBehaviour
{
    public enum Mode { Select, Connect }

    [Header("UI References")]
    public Button selectButton;
    public Button connectButton;

    private Mode _currentMode = Mode.Select;

    public event System.Action<Mode> OnModeChanged;

    // Methods to extract:
    - SetSelectMode()
    - SetConnectMode()
    - IsConnectMode()
    - UpdateButtonStates()
}
```

**Lines extracted:** ~80 lines
**Dependencies:** Unity UI

---

#### 3.2 Create TerminalSelectionHandler.cs (~150 lines)
**Responsibility:** Terminal click and selection logic

```csharp
public class TerminalSelectionHandler : MonoBehaviour
{
    private ComponentTerminal _firstTerminal = null;
    private ComponentTerminal _secondTerminal = null;

    public event System.Action<ComponentTerminal, ComponentTerminal> OnTerminalsSelected;

    // Methods to extract:
    - OnComponentClicked(component)
    - HandleTerminalClick(terminal)
    - HandleInteractionComponentClick(interactionComponent)
    - ClearSelection()
    - IsValidConnection(terminal1, terminal2)
}
```

**Lines extracted:** ~130 lines
**Dependencies:** ComponentTerminal, InteractionComponent

---

#### 3.3 Create WirePreviewRenderer.cs (~120 lines)
**Responsibility:** Wire preview visualization

```csharp
public class WirePreviewRenderer : MonoBehaviour
{
    [Header("Preview Settings")]
    public Color previewColor = Color.yellow;
    public float previewWidth = 0.08f;
    public float updateInterval = 0.02f;

    private GameObject _wirePreview;
    private LineRenderer _previewLineRenderer;
    private Camera _mainCamera;

    // Methods to extract:
    - ShowWirePreview(startPosition)
    - UpdateWirePreview()
    - HideWirePreview()
    - CreatePreviewObject()
}
```

**Lines extracted:** ~100 lines
**Dependencies:** Camera.main

---

#### 3.4 Create WireCreationService.cs (~120 lines)
**Responsibility:** Final wire creation

```csharp
public class WireCreationService : MonoBehaviour
{
    [Header("Wire Settings")]
    public Color wireColor = Color.blue;
    public float wireWidth = 0.1f;

    private List<GameObject> _wires = new List<GameObject>();

    public event System.Action<CircuitWire> OnWireCreated;

    // Methods to extract:
    - CreateWireBetweenTerminals(terminal1, terminal2, component1, component2)
    - CreateWireGameObject(startPos, endPos)
    - SetupLineRenderer(wire)
    - RegisterWire(wire)
    - RemoveWire(wire)
}
```

**Lines extracted:** ~100 lines
**Dependencies:** CircuitComponent3D, CircuitWire

---

#### 3.5 Refactor ConnectTool.cs (~170 lines)
**New Responsibility:** Orchestration and event coordination

```csharp
public class ConnectTool : MonoBehaviour
{
    // Service dependencies
    private ConnectionModeController modeController;
    private TerminalSelectionHandler terminalHandler;
    private WirePreviewRenderer previewRenderer;
    private WireCreationService wireCreationService;

    public static ConnectTool Instance { get; private set; }

    // Public API (facade)
    public void SetSelectMode() => modeController.SetSelectMode();
    public void SetConnectMode() => modeController.SetConnectMode();
    public bool IsConnectMode() => modeController.IsConnectMode();
    public void OnComponentClicked(SelectableComponent component)
    {
        terminalHandler.OnComponentClicked(component);
    }

    // Event handlers
    private void OnTerminalsSelected(terminal1, terminal2)
    {
        wireCreationService.CreateWireBetweenTerminals(...);
        previewRenderer.HideWirePreview();
    }
}
```

**Remaining lines:** ~170 lines

---

### 3.6 Migration Steps

**Step 1: Create Service Files**
- Create 4 service classes
- Define public APIs and events

**Step 2: Extract Mode Logic**
- Move mode switching to ConnectionModeController
- Update button references

**Step 3: Extract Terminal Logic**
- Move terminal selection to TerminalSelectionHandler
- Implement selection events

**Step 4: Extract Rendering Logic**
- Move preview to WirePreviewRenderer
- Move wire creation to WireCreationService

**Step 5: Update ConnectTool**
- Wire up event handlers
- Maintain singleton pattern
- Keep public API unchanged

**Step 6: Testing**
- Test mode switching
- Test terminal selection with both new and old components
- Test wire preview animation
- Test wire creation in Play mode

---

## Phase 4: Additional Files (Optional - Lower Priority)

### 4.1 CircuitSolverManager.cs (352 lines)
**Status:** 🟡 SLIGHTLY OVER LIMIT
**Action:** Monitor, refactor if exceeds 400 lines
**Suggested split:** Separate validation logic from solving logic

### 4.2 CircuitManager.cs (352 lines)
**Status:** 🟡 SLIGHTLY OVER LIMIT
**Action:** Monitor, refactor if exceeds 400 lines
**Suggested split:** Extract component registration into separate service

### 4.3 Test Files (>500 lines)
**Status:** ✅ ACCEPTABLE
**Action:** None required
**Rationale:** Comprehensive test coverage naturally creates longer files

---

## Phase 5: Documentation and Verification

### 5.1 Update Documentation
- [ ] Update ARCHITECTURE.md with new service structure
- [ ] Update DEPENDENCY.md with service dependencies
- [ ] Update CLAUDE.md with refactoring history
- [ ] Create migration guide for future developers

### 5.2 Code Quality Metrics
- [ ] Verify all managers < 300 lines
- [ ] Run code complexity analysis
- [ ] Check cyclomatic complexity scores
- [ ] Verify test coverage maintained/improved

### 5.3 Performance Validation
- [ ] Benchmark component creation time
- [ ] Verify no performance regression
- [ ] Check memory allocation patterns
- [ ] Test with 50+ components in scene

---

## Implementation Timeline

### Sprint 1 (Week 1-2): ComponentFactoryManager
- Days 1-3: Create service files and extract pure functions
- Days 4-6: Extract stateful logic and update references
- Days 7-10: Testing and integration

### Sprint 2 (Week 3): PaletteUIManager
- Days 1-2: Create controller files
- Days 3-4: Extract and wire up logic
- Day 5: Testing

### Sprint 3 (Week 4): ConnectTool
- Days 1-2: Create service files
- Days 3-4: Extract and orchestrate
- Day 5: Testing

### Sprint 4 (Week 5): Documentation and Polish
- Days 1-2: Update all documentation
- Days 3-4: Performance validation
- Day 5: Final integration testing

---

## Risk Management

### High Risks
1. **Breaking existing functionality**
   - Mitigation: Comprehensive testing after each extraction
   - Rollback: Keep original files until full validation

2. **ServiceLocator registration issues**
   - Mitigation: Test registration order carefully
   - Solution: Document initialization dependencies

3. **Event system complexity**
   - Mitigation: Use clear event naming conventions
   - Solution: Create event flow diagrams

### Medium Risks
1. **Performance impact from service calls**
   - Mitigation: Benchmark before/after
   - Solution: Inline critical paths if needed

2. **Unity lifecycle conflicts**
   - Mitigation: Careful Awake/Start ordering
   - Solution: Use explicit initialization

### Low Risks
1. **Merge conflicts if development continues**
   - Mitigation: Work in feature branch
   - Solution: Regular rebasing

---

## Success Criteria

### Quantitative
- ✅ All manager files < 300 lines
- ✅ Average file size < 200 lines
- ✅ Test coverage maintained at >80%
- ✅ No performance regression (±5%)
- ✅ Build time not increased

### Qualitative
- ✅ Each class has single clear responsibility
- ✅ Service dependencies are explicit and documented
- ✅ Code is easier to understand and modify
- ✅ New features easier to add
- ✅ Bugs easier to locate and fix

---

## Rollback Plan

If refactoring causes critical issues:
1. Revert to git commit before refactoring started
2. Cherry-pick any bug fixes made during refactoring
3. Schedule refactoring retrospective
4. Adjust approach based on lessons learned

---

## Post-Refactoring Maintenance

### Code Review Checklist
- [ ] New files follow 300-line guideline
- [ ] Services registered with ServiceLocator
- [ ] Dependencies injected properly
- [ ] Events documented and tested
- [ ] XML documentation comments complete

### Ongoing Monitoring
- Monthly code metrics review
- Quarterly refactoring assessment
- Track files approaching 250+ lines
- Proactive refactoring before 300-line limit

---

## Appendix A: File Size Targets

| File | Current | Target | Reduction |
|------|---------|--------|-----------|
| ComponentFactoryManager.cs | 847 | 280 | -567 (-67%) |
| PaletteUIManager.cs | 498 | 200 | -298 (-60%) |
| ConnectTool.cs | 563 | 170 | -393 (-70%) |
| **Total** | **1,908** | **650** | **-1,258 (-66%)** |

---

## Appendix B: Service Dependency Graph

```
ComponentFactoryManager (orchestrator)
├── ComponentCreationService (instantiation)
├── ComponentVisualsService (appearance)
├── ComponentSetupService (properties)
│   └── ComponentVisualsService
└── ComponentPlacementService (positioning)

PaletteUIManager (orchestrator)
├── PaletteUIController (UI creation)
├── ComponentPlacementController (actions)
│   ├── ComponentFactoryManager
│   └── ConnectTool
└── KeyboardShortcutHandler (input)
    └── ComponentPlacementController

ConnectTool (orchestrator)
├── ConnectionModeController (state)
├── TerminalSelectionHandler (selection)
├── WirePreviewRenderer (visualization)
└── WireCreationService (creation)
```

---

## Appendix C: Testing Checklist

### ComponentFactoryManager Testing
- [ ] Create Battery (prefab + primitive)
- [ ] Create Resistor (prefab + primitive)
- [ ] Create Bulb (prefab + primitive)
- [ ] Create Switch (prefab + primitive)
- [ ] Create Junction (primitive only)
- [ ] Create from ComponentDefinition (ScriptableObject)
- [ ] Verify terminals created correctly
- [ ] Verify SelectableComponent attached
- [ ] Verify MoveableComponent attached
- [ ] Verify CircuitComponent3D properties set
- [ ] Test RemoveComponent
- [ ] Test ClearAllComponents
- [ ] Test component tracking list

### PaletteUIManager Testing
- [ ] UI buttons created correctly
- [ ] Button clicks trigger correct actions
- [ ] Keyboard shortcuts work
- [ ] Mode switching (Select/Connect)
- [ ] Delete selected component
- [ ] Reset circuit
- [ ] Button tooltips display

### ConnectTool Testing
- [ ] Switch to Select mode
- [ ] Switch to Connect mode
- [ ] Select first component/terminal
- [ ] Select second component/terminal
- [ ] Wire created between terminals
- [ ] Wire preview shows during connection
- [ ] Wire preview follows mouse
- [ ] ESC cancels connection
- [ ] Invalid connections prevented
- [ ] Wire registered with CircuitManager

---

**Document Status:** Draft v1.0
**Next Review:** After Phase 1 completion
**Approval Required:** Lead Developer, QA Lead
