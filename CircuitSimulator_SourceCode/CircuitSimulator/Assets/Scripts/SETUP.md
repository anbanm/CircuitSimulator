# Unity Circuit Simulator - Setup Guide

## 📋 **Prerequisites**

- **Unity Version**: Unity 6 (6000.0.32f1) or later
- **IDE**: Visual Studio 2022 / JetBrains Rider / VS Code
- **Platform**: Windows 10/11, macOS 12+, Ubuntu 20.04+
- **RAM**: 8GB minimum, 16GB recommended
- **Disk Space**: 10GB free space

## 🚀 **Quick Setup**

### **1. Open Project in Unity**
```bash
1. Open Unity Hub
2. Click "Add" → Navigate to: CircuitSimulator_SourceCode/CircuitSimulator/
3. Select Unity 6 (6000.0.32f1)
4. Click "Open"
```

### **2. Import Required Packages**
In Unity, go to **Window → Package Manager**:
- ✅ TextMeshPro (Essential)
- ✅ UI Toolkit
- ✅ Universal RP (for better graphics)
- ✅ XR Plugin Management (for AR features)

### **3. Fix Any Import Errors**
```
Edit → Project Settings → Player → Configuration
- Set "Api Compatibility Level" to ".NET Standard 2.1"
- Set "Scripting Backend" to "Mono" (for testing) or "IL2CPP" (for production)
```

## 🏗️ **Scene Setup**

### **Step 1: Create Main Scene**
```
1. File → New Scene
2. Save as: Assets/Scenes/CircuitSimulator.unity
```

### **Step 2: Setup Core Managers**
Create these GameObjects in hierarchy:

```
CircuitSimulator (Scene Root)
├── Managers
│   ├── CircuitManager (Add CircuitManager.cs)
│   ├── WorkspaceManager (Add WorkspaceManager.cs)
│   └── ComponentPaletteCoordinator (Add ComponentPaletteCoordinator.cs)
├── Workspace
│   └── WorkspacePlane (3D Plane, scale: 10,1,10)
├── UI
│   ├── Canvas (Screen Space - Overlay)
│   └── UIDocument (for UI Toolkit)
└── Camera
    └── Main Camera (Position: 0,10,-10, Rotation: 45,0,0)
```

### **Step 3: Configure Managers**

**CircuitManager GameObject:**
1. Add Component → CircuitManager
2. Add Component → CircuitSolverManager
3. Add Component → CircuitNodeManager
4. Add Component → CircuitDebugManager
5. Add Component → CircuitEventManager
6. Add Component → CircuitValidationManager

**WorkspaceManager GameObject:**
1. Add Component → WorkspaceManager
2. Add Component → UILayoutManager
3. Add Component → MeasurementDisplayManager
4. Add Component → ARWorkspaceAdapter
5. Set "Workspace Plane" reference → WorkspacePlane

**ComponentPaletteCoordinator GameObject:**
1. Add Component → ComponentPaletteCoordinator
2. Add Component → ComponentFactoryManager
3. Add Component → PaletteUIManager
4. Add Component → CircuitControlManager
5. Set "Canvas Plane" reference → WorkspacePlane

## 🧪 **Testing the Setup**

### **Test 1: Basic Component Creation**
```csharp
// Add this test script to any GameObject
using UnityEngine;

public class CircuitTest : MonoBehaviour
{
    void Start()
    {
        // Test manager initialization
        var circuitManager = CircuitManager.Instance;
        if (circuitManager != null)
        {
            Debug.Log("✅ CircuitManager initialized");
        }
        
        // Test component creation
        var factory = FindFirstObjectByType<ComponentFactoryManager>();
        if (factory != null)
        {
            var battery = factory.CreateBattery();
            Debug.Log($"✅ Battery created at {battery.transform.position}");
        }
    }
}
```

### **Test 2: Keyboard Shortcuts**
Press these keys in Play Mode:
- **B** - Place Battery
- **R** - Place Resistor  
- **L** - Place Bulb
- **S** - Place Switch
- **Space** - Solve Circuit
- **T** - Test Circuit

### **Test 3: Circuit Solving**
```csharp
// Manual test in Play Mode
1. Place a Battery (B key)
2. Place a Resistor (R key)
3. Click Wire Tool button
4. Click Battery, then Resistor to connect
5. Press Space to solve
6. Check Console for "✅ Circuit solved: 2 components"
```

## 🎮 **UI Setup**

### **Option A: Legacy UI (uGUI)**
```
1. Create Canvas (if not exists)
2. Add Panel → ControlPanel
3. Add Buttons:
   - Battery Button → OnClick: ComponentPaletteCoordinator.PlaceBattery()
   - Resistor Button → OnClick: ComponentPaletteCoordinator.PlaceResistor()
   - Solve Button → OnClick: CircuitManager.Instance.SolveCircuit()
```

### **Option B: Modern UI Toolkit**
```
1. Create UIDocument
2. Assign UXML: Assets/UI/CircuitControlPanel.uxml
3. Add ControlPanelController.cs
4. Set references in Inspector
```

## 🐛 **Common Issues & Solutions**

### **Issue 1: "CircuitManager not found"**
```
Solution: Ensure CircuitManager GameObject exists and has CircuitManager.cs attached
```

### **Issue 2: "Components not registering"**
```
Solution: Check that CircuitComponent3D.cs is on component prefabs
Components need Collider for selection/interaction
```

### **Issue 3: "TextMeshPro errors"**
```
Solution: Window → TextMeshPro → Import TMP Essential Resources
```

### **Issue 4: "Deprecated warnings"**
```
Solution: These are expected during migration. Code will auto-migrate at runtime.
To remove: Update references from Circuit3DManager to CircuitManager
```

## ✅ **Validation Checklist**

Run through this checklist to ensure proper setup:

- [ ] Unity 6 project opens without errors
- [ ] All managers visible in Hierarchy
- [ ] Console shows "CircuitManager initialized" on Play
- [ ] Can place components with keyboard shortcuts
- [ ] Wire tool connects components
- [ ] Space key solves circuit
- [ ] No red errors in Console (yellow warnings OK)

## 📊 **Performance Settings**

### **For Development:**
```
Edit → Project Settings → Quality
- Set to "Medium"
- Disable shadows for better editor performance
```

### **For AR Testing:**
```
Edit → Project Settings → XR Plug-in Management
- Enable "ARCore" (Android) or "ARKit" (iOS)
- Install XR packages as prompted
```

## 🔧 **Debug Tools**

### **Enable Debug Visualization:**
```csharp
1. Add CircuitDebugVisualizer.cs to any GameObject
2. Set properties:
   - Show Nodes: ✓
   - Show Currents: ✓
   - Show Node IDs: ✓
3. Play Mode → See visual debugging in Scene view
```

### **Enable Debug Logging:**
```csharp
CircuitSolver.EnableDebugLog = true;
var debugManager = FindFirstObjectByType<CircuitDebugManager>();
debugManager.EnableFileLogging = true;
```

### **Generate Debug Report:**
```csharp
CircuitManager.Instance.SaveDebugReport();
// Check: CircuitDebugReport_[timestamp].txt in project root
```

## 📁 **Project Structure**

After setup, your project should have:
```
Assets/
├── Scenes/
│   └── CircuitSimulator.unity
├── Scripts/
│   ├── Core/          # Circuit logic
│   ├── Managers/      # 14 manager scripts
│   ├── Components/    # Component scripts
│   ├── Interaction/   # User input
│   └── UI/           # UI controllers
├── Prefabs/
│   ├── Battery.prefab
│   ├── Resistor.prefab
│   ├── Bulb.prefab
│   └── Switch.prefab
└── Materials/
    └── CircuitMaterials/
```

## 🎯 **Next Steps**

1. **Test Basic Functionality**: Follow Test 1-3 above
2. **Customize Components**: Modify prefab visuals/properties
3. **Add Educational Content**: Implement tutorials/lessons
4. **Enable AR Mode**: Configure XR settings for mobile
5. **Performance Profiling**: Use Unity Profiler to optimize

## 📞 **Support**

- **Documentation**: See [ARCHITECTURE.md](./ARCHITECTURE.md)
- **Dependencies**: See [DEPENDENCY.md](./DEPENDENCY.md)
- **Unity Forums**: https://forum.unity.com
- **Circuit Physics**: Based on Kirchhoff's Laws

---

**✨ Happy Circuit Building!**