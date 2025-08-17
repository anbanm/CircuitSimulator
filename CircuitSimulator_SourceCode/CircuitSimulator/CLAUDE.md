# Circuit Simulator - Claude AI Project Notes

## 🚀 **Project Overview**
Unity 3D educational circuit simulator for Grade 7-12 physics education with AR integration capabilities.

## 📋 **TODO: Documentation Fixes Needed**

### **CRITICAL: Fix Documentation Errors**
- [ ] **ARCHITECTURE.md**: Remove all references to "CircuitValidationManager" (doesn't exist)
- [ ] **ARCHITECTURE.md**: Change "14 managers" to "13 managers" 
- [ ] **ARCHITECTURE.md**: Fix Circuit System from "5 managers" to "5 managers" (correct count after removing validation)
- [ ] **DEPENDENCY.md**: Remove CircuitValidationManager from all diagrams and references
- [ ] **Assembly-CSharp.csproj**: Remove CircuitValidationManager from project file
- [ ] **SETUP.md**: Update setup instructions with correct 13 managers

### **Actual Manager Count:**
- **Circuit System**: 5 managers (CircuitManager, CircuitSolverManager, CircuitNodeManager, CircuitDebugManager, CircuitEventManager)
- **Workspace System**: 4 managers (WorkspaceManager, UILayoutManager, MeasurementDisplayManager, ARWorkspaceAdapter)
- **Component System**: 4 managers (ComponentPaletteCoordinator, ComponentFactoryManager, PaletteUIManager, CircuitControlManager)
- **Total**: 13 managers (not 14)

## 🏗️ **Architecture Status**
- ✅ Refactored Circuit3DManager (843 lines) → 5 specialized managers
- ✅ Refactored CircuitWorkspaceUI (543 lines) → 4 workspace managers
- ✅ Refactored ComponentPalette (394 lines) → 4 component managers
- ✅ Updated all deprecated references
- ✅ Fixed compilation errors
- 🔄 **IN PROGRESS**: Unity setup and testing

## 🧪 **Current Unity Setup Progress**
1. ✅ Created scene hierarchy (CircuitManager, WorkspaceManager, ComponentPalette, WorkspacePlane)
2. ✅ Added CircuitCameraController to Main Camera
3. ✅ Added all 13 manager scripts to GameObjects
4. 🔄 **CURRENT**: Setting up Legacy uGUI Canvas + Buttons

### **Correct Manager Scripts to Add:**

**CircuitManager GameObject:**
- CircuitManager.cs
- CircuitSolverManager.cs
- CircuitNodeManager.cs
- CircuitDebugManager.cs
- CircuitEventManager.cs

**WorkspaceManager GameObject:**
- WorkspaceManager.cs
- UILayoutManager.cs
- MeasurementDisplayManager.cs
- ARWorkspaceAdapter.cs (from AR folder)

**ComponentPalette GameObject:**
- ComponentPaletteCoordinator.cs
- ComponentFactoryManager.cs
- PaletteUIManager.cs
- CircuitControlManager.cs

## ⚡ **Key Commands for Testing**
- **B** - Place Battery
- **R** - Place Resistor
- **L** - Place Bulb
- **S** - Place Switch
- **Space** - Solve Circuit
- **T** - Test Circuit

## 📂 **File Structure**
```
Assets/Scripts/
├── Core/ (5 files)
├── Managers/ (12 files)
├── AR/ (1 file: ARWorkspaceAdapter.cs)
├── Components/ (3 files)
├── Interaction/ (5 files)
└── UI/ (8 files)
```

## 🔧 **Unity UI Setup Steps (Legacy uGUI)**

### **Create Canvas Structure:**
1. **Right-click** Hierarchy → **UI** → **Canvas** (name: `UI Canvas`)
2. **Right-click** `UI Canvas` → **UI** → **Panel** (name: `ButtonPanel`)
3. **ButtonPanel** settings:
   - Anchor: **Top-Left**, Position: `X: 10, Y: -10`, Size: `200x600`
   - Add **Grid Layout Group**: Cell Size `180x50`, Spacing `5x10`, Start Axis: Vertical

### **Create Button Prefab:**
4. **Right-click** `ButtonPanel` → **UI** → **Button - TextMeshPro** (name: `Button_Prefab`)
5. **Drag** `Button_Prefab` to **Project** folder (creates prefab)
6. **Delete** instance from ButtonPanel (keep prefab only)

### **Set ComponentPalette References:**
7. **Select** `ComponentPalette` GameObject
8. **ComponentPaletteCoordinator** script:
   - **Palette Container**: Drag `ButtonPanel` from hierarchy
   - **Button Prefab**: Drag `Button_Prefab` from Project

## 🔧 **Component Prefab Setup (Optional)**

### **Quick Test (Uses Primitive Cubes):**
- ComponentFactoryManager automatically creates primitive cubes if no prefabs assigned
- Skip prefab creation for initial testing

### **Custom Prefab Creation (Advanced):**
1. **Create Battery Prefab:**
   - **GameObject** → **3D Object** → **Cube** (name: `Battery_Prefab`)
   - Scale: `X: 1.5, Y: 0.5, Z: 0.8`, Color: Red
   - **Drag** to Project folder, **Delete** from scene

2. **Create Resistor Prefab:**
   - **GameObject** → **3D Object** → **Cylinder** (name: `Resistor_Prefab`)
   - Scale: `X: 0.3, Y: 1, Z: 0.3`, Color: Yellow
   - **Drag** to Project folder, **Delete** from scene

3. **Create Bulb Prefab:**
   - **GameObject** → **3D Object** → **Sphere** (name: `Bulb_Prefab`)
   - Scale: `X: 0.8, Y: 0.8, Z: 0.8`, Color: White
   - **Drag** to Project folder, **Delete** from scene

4. **Create Switch Prefab:**
   - **GameObject** → **3D Object** → **Cube** (name: `Switch_Prefab`)
   - Scale: `X: 0.8, Y: 0.3, Z: 1.2`, Color: Gray
   - **Drag** to Project folder, **Delete** from scene

5. **Assign Prefabs:**
   - **Select** `ComponentPalette` GameObject
   - **ComponentPaletteCoordinator** script: Assign each prefab

## 🚀 **Ready to Test!**

### **Test Steps:**
1. **Press Play** in Unity
2. **Check Console** for initialization messages
3. **UI Buttons** should appear (BATTERY, RESISTOR, BULB, SWITCH, etc.)
4. **Test Keyboard Shortcuts:**
   - **B** - Place Battery
   - **R** - Place Resistor  
   - **L** - Place Bulb
   - **S** - Place Switch
   - **Space** - Solve Circuit
5. **Click Components** to select/move them