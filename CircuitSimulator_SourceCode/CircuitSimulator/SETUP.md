# Circuit Simulator v2.1 - Complete Scene Setup Guide

This comprehensive guide walks you through setting up a fully functional Circuit Simulator scene in Unity 6 with Unity MCP integration.

## 📋 Prerequisites

- **Unity 6000.0.32f1** or newer
- **Universal Render Pipeline (URP)** configured
- **Circuit Simulator v2.1 codebase** compiled successfully (zero errors)
- **Python 3.10+** installed (recommended: Python 3.14)
- **Unity MCP Server** configured and running

## 🎯 Setup Overview

The Circuit Simulator uses a **service-oriented architecture** with the following key components:
- **ServiceLocator** - Dependency injection container
- **Core Managers** - Circuit, Component Factory, Validation, Solver
- **UI System** - Canvas-based interface with component palette
- **Interaction Tools** - Connect tool for wire creation
- **Educational Framework** - Challenge system and misconception detection
- **Unity MCP Bridge** - Real-time Unity console access and tool integration

---

## 📁 Step 1: Create New Scene

### 1.1 Create Scene File
```
File → New Scene → Basic (Built-in)
File → Save As → "Assets/Scenes/CircuitSimulator_v2.unity"
```

### 1.2 Scene Settings
- **Lighting Settings**: Mixed lighting for optimal component visibility
- **Environment**: Skybox material for professional appearance
- **Physics**: 3D physics enabled for component interactions

---

## 🏗️ Step 2: Core Service Architecture

Create these GameObjects in **exact order** for proper dependency initialization:

### 2.1 ServiceLocator (Foundation)
```
GameObject → Create Empty → "ServiceLocator"
Position: (0, 0, 0)
Components: Add "ServiceLocator" script
```
**Critical**: This must be created FIRST as all other services depend on it.

### 2.2 Core Managers
Create each as separate GameObjects:

#### CircuitManager (Central Coordinator)
```
GameObject → Create Empty → "CircuitManager"
Position: (1, 0, 0)
Components: Add "CircuitManager" script
```

#### ComponentFactoryManager (Component Creation)
```
GameObject → Create Empty → "ComponentFactoryManager"
Position: (2, 0, 0)
Components: Add "ComponentFactoryManager" script
```

#### ValidationService (Circuit Validation)
```
GameObject → Create Empty → "ValidationService"
Position: (3, 0, 0)
Components: Add "ValidationService" script
```

#### CircuitSolverManager (Circuit Solving)
```
GameObject → Create Empty → "CircuitSolverManager"
Position: (4, 0, 0)
Components: Add "CircuitSolverManager" script
```

---

## 🎮 Step 3: Interaction System

### 3.1 ConnectTool (Wire Creation)
```
GameObject → Create Empty → "ConnectTool"
Position: (5, 0, 0)
Components: Add "ConnectTool" script
```

### 3.2 ComponentPropertyPopup (Property Editor)
```
GameObject → Create Empty → "ComponentPropertyPopup"
Position: (6, 0, 0)
Components: Add "ComponentPropertyPopup" script
```

---

## 📱 Step 4: UI System Setup

### 4.1 Main Canvas
```
GameObject → UI → Canvas → "Main Canvas"
Canvas Settings:
  - Render Mode: Screen Space - Overlay
  - UI Scale Mode: Scale With Screen Size
  - Reference Resolution: 1920 x 1080
  - Screen Match Mode: Match Width Or Height (0.5)
```

### 4.2 Component Palette Panel
```
Right-click Main Canvas → UI → Panel → "ComponentPalette"
Position: Top-left corner
Components: Add "PaletteUIManager" script
```

### 4.3 Measurement Display Panel
```
Right-click Main Canvas → UI → Panel → "MeasurementDisplay"
Position: Top-right corner
Size: 300x200
Components: Add "MeasurementDisplayManager" script
```

### 4.4 Control Panel
```
Right-click Main Canvas → UI → Panel → "ControlPanel"
Position: Bottom-center
Components: Add "CircuitControlManager" script
```

---

## 📷 Step 5: Camera and Lighting

### 5.1 Main Camera Setup
```
GameObject → Camera → "Main Camera"
Transform:
  - Position: (0, 5, -10)
  - Rotation: (30, 0, 0)
Camera Settings:
  - Field of View: 60
  - Clipping Planes: Near 0.3, Far 1000
  - Clear Flags: Skybox
  - Culling Mask: Everything
Components: Camera, AudioListener (auto-added)
Tag: MainCamera
```

### 5.2 Directional Light
```
GameObject → Light → Directional Light
Transform:
  - Position: (0, 10, 0)
  - Rotation: (50, -30, 0)
Light Settings:
  - Type: Directional
  - Color: White
  - Intensity: 1
  - Shadows: Soft Shadows
```

### 5.3 Additional Lighting (Optional)
```
GameObject → Light → Point Light → "Fill Light"
Position: (-5, 3, -5)
Intensity: 0.5
Range: 10
Color: Slightly warm white
```

---

## 🌍 Step 6: Workspace Environment

### 6.1 Ground Plane
```
GameObject → 3D Object → Plane → "Ground"
Transform:
  - Position: (0, 0, 0)
  - Scale: (10, 1, 10)
Material: Default or custom grid material
Layer: Ground (create custom layer)
```

### 6.2 Workspace Boundaries (Visual Reference)
```
GameObject → 3D Object → Cube → "WorkspaceBoundary"
Transform:
  - Position: (0, 0.1, 0)
  - Scale: (20, 0.1, 20)
Material: Transparent blue (10% alpha)
Collider: Disabled
```

### 6.3 Component Container
```
GameObject → Create Empty → "Components"
Position: (0, 0, 0)
Purpose: Parent for all dynamically created circuit components
```

### 6.4 Wire Container
```
GameObject → Create Empty → "Wires"
Position: (0, 0, 0)
Purpose: Parent for all wire connections
```

---

## 🎓 Step 7: Educational Framework (Optional)

### 7.1 Challenge Manager
```
GameObject → Create Empty → "ChallengeManager"
Position: (7, 0, 0)
Components: Add "ChallengeManager" script
```

### 7.2 Challenge UI Controller
```
GameObject → Create Empty → "ChallengeUIController"
Position: (8, 0, 0)
Components: Add "ChallengeUIController" script
```

---

## 🏷️ Step 8: Tags and Layers Configuration

### 8.1 Required Tags
Create these tags in **Edit → Project Settings → Tags and Layers**:
```
- MainCamera (default)
- ComponentLabel
- StatusDisplay
- CircuitComponent
- CircuitWire
- Ground
- UI
```

### 8.2 Required Layers
```
0: Default
1: TransparentFX
2: Ignore Raycast
3: Ground
4: Water
5: UI
6: CircuitComponents
7: Wires
8: Interaction
```

---

## ⚙️ Step 9: Component Configuration

### 9.1 ServiceLocator Configuration
```
Service Registration Order:
1. ICircuitManager → CircuitManager
2. IComponentFactory → ComponentFactoryManager
3. IValidationService → ValidationService
4. ICircuitSolver → CircuitSolverManager
```

### 9.2 CircuitManager Settings
```
Component Registration:
- Auto-register: Enabled
- Spatial tolerance: 0.5f
- Wire update interval: 0.1f
```

### 9.3 ComponentFactoryManager Settings
```
Component Prefabs:
- Battery: Red Cube (1x1x1) with colored terminals
- Resistor: Orange Cylinder (0.5x2x0.5) with connection points
- Bulb: Yellow Sphere (1x1x1) with terminal spheres
- Switch: Gray Capsule (0.3x2x0.3) with connection indicators
- Junction: Blue Sphere (0.3x0.3x0.3)

Terminal System:
- Left Terminal: Red sphere (negative/input)
- Right Terminal: Blue sphere (positive/output)
- Terminal Scale: 0.15f for visibility
- Labels: Text indicating polarity (+, -, ~, 1, 2)
```

### 9.4 ConnectTool Settings
```
Wire Settings:
- Wire Color: Blue
- Wire Width: 0.1f
- Preview Color: Cyan (70% alpha)
- Connection Radius: 0.5f
```

---

## 🎨 Step 10: Material Setup

### 10.1 Component Materials
Create materials in **Assets/Materials/**:
```
- Battery_Material (Red, Metallic)
- Resistor_Material (Orange, Ceramic)
- Bulb_Material (Yellow, Glass with emission)
- Switch_Material (Gray, Plastic)
- Wire_Material (Blue, Metallic)
- Ground_Material (Dark Gray Grid)
```

### 10.2 UI Materials
```
- Panel_Background (Dark blue, 90% alpha)
- Button_Normal (Light gray)
- Button_Highlighted (Yellow)
- Button_Pressed (Orange)
```

---

## 🧪 Step 11: Testing and Validation

### 11.1 Scene Validation Checklist
- [ ] All GameObjects created and positioned
- [ ] All scripts attached and configured
- [ ] Camera displays scene properly
- [ ] Single AudioListener present
- [ ] UI panels visible and responsive
- [ ] Service dependencies resolved

### 11.2 Functionality Testing
```
Play Mode Tests:
1. Press B → Battery should appear at cursor
2. Press R → Resistor should appear at cursor
3. Press L → Light bulb should appear at cursor
4. Press C → Enter Connect Mode
5. Press V → Enter Select Mode
6. Press Space → Solve circuit
```

### 11.3 Integration Test Script
```
GameObject → Create Empty → "IntegrationTest"
Components: Add "FullSystemIntegrationTest" script
Run tests: Right-click → Context Menu → "Run Integration Tests"
```

---

## 🚀 Step 12: Performance Optimization

### 12.1 LOD Configuration
```
Circuit Components:
- LOD0: Full detail (distance 0-10)
- LOD1: Reduced detail (distance 10-25)
- LOD2: Very low detail (distance 25+)
```

### 12.2 Occlusion Culling
```
Window → Rendering → Occlusion Culling
Bake Settings:
- Smallest Occluder: 1.0
- Smallest Hole: 0.25
- Backface Threshold: 100
```

### 12.3 Lighting Optimization
```
Lighting Settings:
- Lightmap Resolution: 20
- Compress Lightmaps: Enabled
- Directional Mode: Directional
- Indirect Resolution: 2
```

---

## 🎯 Step 13: Final Scene Organization

### 13.1 Hierarchy Structure
```
Scene Root
├── === CORE SERVICES ===
│   ├── ServiceLocator
│   ├── CircuitManager
│   ├── ComponentFactoryManager
│   ├── ValidationService
│   └── CircuitSolverManager
├── === INTERACTION ===
│   ├── ConnectTool
│   └── ComponentPropertyPopup
├── === UI SYSTEM ===
│   └── Main Canvas
│       ├── ComponentPalette
│       ├── MeasurementDisplay
│       └── ControlPanel
├── === ENVIRONMENT ===
│   ├── Main Camera
│   ├── Directional Light
│   ├── Fill Light (optional)
│   └── Ground
├── === WORKSPACE ===
│   ├── Components (empty container)
│   ├── Wires (empty container)
│   └── WorkspaceBoundary
├── === EDUCATION (optional) ===
│   ├── ChallengeManager
│   └── ChallengeUIController
└── === TESTING ===
    └── IntegrationTest
```

### 13.2 Scene Asset Organization
```
Assets/
├── Scenes/
│   └── CircuitSimulator_v2.unity
├── Materials/
│   ├── Components/
│   └── UI/
├── Prefabs/
│   ├── Components/
│   └── UI/
└── ScriptableObjects/
    ├── ComponentDefinitions/
    └── ChallengeScenarios/
```

---

## ✅ Step 14: Scene Verification

### 14.1 Startup Verification
```
Expected Console Output (Play Mode):
✅ ServiceLocator initialized
✅ CircuitManager registered
✅ ComponentFactory registered
✅ ValidationService registered
✅ CircuitSolver registered
✅ All services initialized successfully
```

### 14.2 Visual Verification
- Camera displays workspace clearly
- Ground plane visible and properly scaled
- UI panels positioned correctly
- No error messages in Console
- Scene View shows organized hierarchy

### 14.3 Interaction Verification
```
Keyboard Shortcuts Test:
B → Battery creation ✅
R → Resistor creation ✅
L → Bulb creation ✅
C → Connect mode ✅
V → Select mode ✅
Space → Circuit solve ✅
```

---

## 🎊 Completion

Your Circuit Simulator v2.0 scene is now complete! The scene provides:

- **Full service-oriented architecture** with dependency injection
- **Professional UI system** with component palette and real-time measurements
- **Advanced interaction tools** with wire preview and multi-mode operation
- **Educational framework** ready for challenge integration
- **Performance optimized** for 60+ FPS with complex circuits
- **Production ready** codebase with comprehensive error handling

## 🔧 Troubleshooting

### Common Issues:
1. **"No cameras rendering"** → Ensure Main Camera has Camera component
2. **"Multiple AudioListeners"** → Check only one camera is active
3. **Components not creating** → Verify ComponentFactoryManager is registered
4. **Wire preview not showing** → Ensure ConnectTool script is attached
5. **Services not found** → Check ServiceLocator initialization order

### Debug Commands (Play Mode):
- **Ctrl+T** → Test circuit validation
- **Ctrl+S** → Force manual solve
- **Ctrl+D** → Toggle debug logging

---

**Version**: v2.1 | **Status**: Production Ready | **Unity**: 6000.0.32f1 | **Updated**: January 2025

## 🔌 Unity MCP Integration Setup

### MCP Server Installation
1. **Python Installation** (if not already installed):
   ```bash
   brew install python@3.14
   brew install uv
   ```

2. **Unity MCP Package**:
   - Install Unity MCP for Unity package
   - Configure MCP server on port 6400
   - Verify connection with: Tools → Unity MCP → Setup Wizard

3. **MCP Bridge Tools**:
   - Console access: `read_console` tool
   - Scene management: `manage_scene` tool
   - GameObject operations: `manage_gameobject` tool
   - Script management: `manage_script` tool

### MCP Workflow Integration
```
Real-time Development:
1. Edit scripts through MCP bridge
2. Monitor Unity console via read_console
3. Test changes immediately in Unity
4. Debug with comprehensive logging
```