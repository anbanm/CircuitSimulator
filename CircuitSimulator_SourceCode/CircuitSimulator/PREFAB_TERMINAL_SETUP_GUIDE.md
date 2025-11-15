# Prefab Terminal Setup Guide

## How to Set Up Connection Points on Your Prefabs

### Method 1: Simple Widget Objects (Recommended)

1. **Open your component prefab** in the Prefab Editor

2. **Create empty GameObjects as children** for each connection point:
   ```
   BatteryPrefab
   ├── Mesh/Model (your 3D model)
   ├── ConnectionPoint_Left  (Empty GameObject)
   └── ConnectionPoint_Right (Empty GameObject)
   ```

3. **Position the empty GameObjects** where you want wires to connect:
   - Use the Transform tool to place them precisely
   - They can be inside, outside, or on the surface of your model

4. **Name them using conventions** (the system auto-detects these):
   - `ConnectionPoint_Left` / `ConnectionPoint_Right`
   - `Terminal_Positive` / `Terminal_Negative`
   - `ConnectPoint_1` / `ConnectPoint_2`
   - `CP_In` / `CP_Out`
   - Or any name containing "ConnectionPoint" or "Terminal"

### Method 2: Using ConnectionPointMarker Component

1. **Create empty GameObjects** on your prefab as above

2. **Add the ConnectionPointMarker component** to each:
   ```csharp
   [Add Component] → ConnectionPointMarker
   ```

3. **Configure in Inspector:**
   - `Is Positive Terminal`: Check for positive/blue terminals
   - `Custom Label`: Set to "+", "-", "~", "1", "2", etc.
   - `Terminal Color`: Choose red, blue, or custom
   - `Show Visual Indicator`: Auto-create sphere
   - `Indicator Scale`: Size of the terminal sphere
   - `Connection Radius`: How close wires need to be

### Method 3: Pre-Made Visual Terminals

1. **Create your terminal visuals** directly in the prefab:
   ```
   BatteryPrefab
   ├── BatteryMesh
   ├── LeftTerminal (GameObject with Sphere mesh)
   │   └── TerminalIndicator (Sphere at 0,0,0)
   └── RightTerminal (GameObject with Sphere mesh)
       └── TerminalIndicator (Sphere at 0,0,0)
   ```

2. **Name the parent objects** with the connection point convention

3. **The system will detect and enhance** existing visuals

### Examples for Different Components

#### Battery Prefab
```
Battery
├── BatteryBody (3D model)
├── ConnectionPoint_Negative (at left side, x=-0.75)
│   └── [Auto-generated red sphere]
└── ConnectionPoint_Positive (at right side, x=0.75)
    └── [Auto-generated blue sphere]
```

#### Resistor Prefab
```
Resistor
├── ResistorBody (Cylinder model)
├── Terminal_1 (at left end)
└── Terminal_2 (at right end)
```

#### LED/Bulb Prefab
```
LED
├── LEDBody (Custom model)
├── ConnectionPoint_Anode (longer leg position)
└── ConnectionPoint_Cathode (shorter leg position)
```

#### Switch Prefab
```
Switch
├── SwitchBody
├── SwitchLever (animated part)
├── Terminal_Input (left contact)
└── Terminal_Output (right contact)
```

### Advanced: Multi-Terminal Components

For components with more than 2 terminals:

```
Transistor
├── TransistorBody
├── Terminal_Base
├── Terminal_Collector
└── Terminal_Emitter
```

### How the System Works

1. **When a component is created**, ComponentFactoryManager calls `SetupConnectionTerminals()`

2. **The system searches for widgets** using these strategies:
   - Looks for GameObjects with names containing "ConnectionPoint", "Terminal", etc.
   - Checks for GameObjects with "ConnectionPoint" or "Terminal" tags
   - Looks for ConnectionPointMarker components

3. **For each widget found**:
   - Determines polarity from name or position
   - Creates/updates visual indicator (sphere)
   - Adds appropriate color (red/blue)
   - Creates text label if needed
   - Adds ConnectionPointData for runtime tracking

4. **If no widgets found**, falls back to automatic placement based on bounds

### Testing Your Prefab

1. **Assign your prefab** to ComponentFactoryManager:
   - `batteryPrefab` = Your custom battery prefab
   - `resistorPrefab` = Your custom resistor prefab
   - etc.

2. **Enter Play mode** and create a component

3. **Check the Hierarchy** to see the generated terminals:
   - Look for "TerminalIndicator" spheres
   - Check colors (red = negative, blue = positive)
   - Verify positions match your widgets

### Wire Connection Behavior

With widget-based terminals:
- Wires snap to the **exact widget position**
- No more guessing about connection points
- Visual feedback shows available terminals
- Multiple wires can connect to same terminal (if configured)

### Troubleshooting

**Terminals not appearing:**
- Check widget GameObject names contain "ConnectionPoint" or "Terminal"
- Ensure widgets are children of the prefab root
- Check console for "Found X pre-defined connection widgets" message

**Wrong colors:**
- Name widgets explicitly: "ConnectionPoint_Positive", "ConnectionPoint_Negative"
- Or use ConnectionPointMarker component to set colors

**Terminals in wrong position:**
- Adjust the widget GameObject positions in Prefab Editor
- Remember positions are in local space relative to prefab root

### Benefits of Widget System

✅ **Precise control** over connection point positions
✅ **Works with any 3D model** (imported or primitive)
✅ **Visual editing** in Unity Editor
✅ **Reusable** across similar components
✅ **Supports complex components** with multiple terminals
✅ **Future-proof** for advanced circuit features