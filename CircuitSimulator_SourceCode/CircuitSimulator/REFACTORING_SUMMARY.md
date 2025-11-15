# Refactoring Plan - Quick Reference

## 📊 Overview

**Goal:** Reduce 3 oversized manager files from 1,908 lines to 650 lines (-66%)

**Timeline:** 4-5 weeks (4 sprints)

**Priority Files:**
1. 🔴 **ComponentFactoryManager.cs** (847 lines → 280 lines)
2. 🟠 **PaletteUIManager.cs** (498 lines → 200 lines)
3. 🟠 **ConnectTool.cs** (563 lines → 170 lines)

---

## 🎯 Phase 1: ComponentFactoryManager (Week 1-2)

### Split into 4 services + 1 orchestrator:

```
ComponentFactoryManager.cs (847)
    ↓ REFACTOR ↓
┌────────────────────────────────────────────┐
│ ComponentCreationService.cs      (~200)   │ ← Instantiation
│ ComponentVisualsService.cs       (~150)   │ ← Materials/Colors
│ ComponentSetupService.cs         (~250)   │ ← Properties/Interaction
│ ComponentPlacementService.cs     (~120)   │ ← Positioning
│ ComponentFactoryManager.cs       (~280)   │ ← Orchestration
└────────────────────────────────────────────┘
```

**Key Extractions:**
- **CreationService:** Prefab/primitive instantiation
- **VisualsService:** Material setup, color assignment
- **SetupService:** CircuitComponent3D, terminals, interaction
- **PlacementService:** Grid positioning, collision detection
- **FactoryManager:** Public API, component tracking

---

## 🎯 Phase 2: PaletteUIManager (Week 3)

### Split into 3 controllers + 1 orchestrator:

```
PaletteUIManager.cs (498)
    ↓ REFACTOR ↓
┌────────────────────────────────────────────┐
│ PaletteUIController.cs           (~180)   │ ← UI Creation
│ ComponentPlacementController.cs  (~120)   │ ← Actions
│ KeyboardShortcutHandler.cs       (~100)   │ ← Input
│ PaletteUIManager.cs              (~200)   │ ← Coordination
└────────────────────────────────────────────┘
```

**Key Extractions:**
- **UIController:** Button creation, layout, panel management
- **PlacementController:** Component creation calls, mode switching
- **ShortcutHandler:** Keyboard input processing
- **UIManager:** Initialization, event wiring

---

## 🎯 Phase 3: ConnectTool (Week 4)

### Split into 4 services + 1 orchestrator:

```
ConnectTool.cs (563)
    ↓ REFACTOR ↓
┌────────────────────────────────────────────┐
│ ConnectionModeController.cs      (~100)   │ ← Mode State
│ TerminalSelectionHandler.cs     (~150)   │ ← Selection
│ WirePreviewRenderer.cs           (~120)   │ ← Visualization
│ WireCreationService.cs           (~120)   │ ← Wire Creation
│ ConnectTool.cs                   (~170)   │ ← Orchestration
└────────────────────────────────────────────┘
```

**Key Extractions:**
- **ModeController:** Select/Connect mode management
- **SelectionHandler:** Terminal click logic
- **PreviewRenderer:** Wire preview animation
- **CreationService:** Final wire LineRenderer setup
- **ConnectTool:** Event coordination, singleton

---

## 📋 Implementation Checklist

### Week 1-2: ComponentFactoryManager
- [ ] Create 4 service files with empty classes
- [ ] Extract ComponentCreationService (instantiation)
- [ ] Extract ComponentVisualsService (materials)
- [ ] Extract ComponentSetupService (properties/interaction)
- [ ] Extract ComponentPlacementService (positioning)
- [ ] Update ComponentFactoryManager (orchestration only)
- [ ] Run integration tests
- [ ] Update ARCHITECTURE.md

### Week 3: PaletteUIManager
- [ ] Create 3 controller files
- [ ] Extract PaletteUIController (UI creation)
- [ ] Extract ComponentPlacementController (actions)
- [ ] Extract KeyboardShortcutHandler (input)
- [ ] Update PaletteUIManager (coordination)
- [ ] Test all keyboard shortcuts
- [ ] Test component creation via buttons

### Week 4: ConnectTool
- [ ] Create 4 service files
- [ ] Extract ConnectionModeController (mode state)
- [ ] Extract TerminalSelectionHandler (selection)
- [ ] Extract WirePreviewRenderer (preview)
- [ ] Extract WireCreationService (wire creation)
- [ ] Update ConnectTool (orchestration)
- [ ] Test wire creation end-to-end
- [ ] Test preview animation

### Week 5: Documentation & Validation
- [ ] Update ARCHITECTURE.md with service diagrams
- [ ] Update DEPENDENCY.md with new dependencies
- [ ] Update CLAUDE.md with refactoring notes
- [ ] Run performance benchmarks
- [ ] Verify all managers < 300 lines
- [ ] Final integration testing

---

## ⚠️ Critical Safety Rules

### During Refactoring:
1. ✅ **Keep public APIs unchanged** (facade pattern)
2. ✅ **Test after every extraction** (incremental validation)
3. ✅ **Maintain git commits** (easy rollback)
4. ✅ **Extract pure functions first** (safest changes)
5. ✅ **Update documentation immediately** (prevent knowledge loss)

### DO NOT:
1. ❌ Change CircuitSolver.cs (validated, proven logic)
2. ❌ Break ServiceLocator registration
3. ❌ Modify Unity lifecycle order without testing
4. ❌ Remove old code until new code is validated
5. ❌ Merge without full test suite passing

---

## 🧪 Testing Strategy

### After Each Service Extraction:
```bash
# 1. Run Unity Play mode
# 2. Press B (create battery)
# 3. Press R (create resistor)
# 4. Press C (connect mode)
# 5. Click terminals to create wire
# 6. Press V (select mode)
# 7. Drag component
# 8. Press X (delete)
# 9. Press Space (solve circuit)
# 10. Check console for errors
```

### Integration Tests:
- Component creation (all types)
- Visual appearance (colors, materials)
- Interaction (selection, movement)
- Wire connections (terminals, preview)
- Circuit solving (voltage/current)

---

## 📈 Success Metrics

| Metric | Before | Target | Change |
|--------|--------|--------|--------|
| ComponentFactoryManager | 847 | 280 | -67% |
| PaletteUIManager | 498 | 200 | -60% |
| ConnectTool | 563 | 170 | -70% |
| **Average Manager Size** | **636** | **217** | **-66%** |
| Largest Manager File | 847 | 280 | -67% |

**Guideline Compliance:**
- Before: 3/3 managers violate 300-line guideline (100% non-compliance)
- After: 0/3 managers violate guideline (100% compliance ✅)

---

## 🔄 Rollback Plan

If critical issues arise:
1. Revert to commit: `git reset --hard <pre-refactor-commit>`
2. Cherry-pick bug fixes: `git cherry-pick <fix-commits>`
3. Schedule retrospective to adjust approach
4. Document lessons learned in REFACTORING_PLAN.md

---

## 📚 Additional Resources

- **Full Plan:** REFACTORING_PLAN.md (detailed 500+ line document)
- **Architecture:** ARCHITECTURE.md (system design)
- **Dependencies:** DEPENDENCY.md (initialization order)
- **Guidelines:** CLAUDE.md (coding standards)

---

## 🚦 Current Status

**Phase:** Planning Complete ✅
**Next Step:** Create ComponentCreationService.cs
**Estimated Start:** Ready to begin
**Risk Level:** Medium (careful dependency management required)

---

**Last Updated:** October 25, 2025
**Document Owner:** Development Team
**Status:** Approved for Implementation
