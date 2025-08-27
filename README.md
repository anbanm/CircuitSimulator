# 🔌 Circuit Simulator 3D

**Interactive 3D circuit simulator for Grade 7-12 physics education** - Built with Unity 6

![Unity](https://img.shields.io/badge/Unity-6000.0-black?logo=unity)
![Status](https://img.shields.io/badge/Status-Production_Ready-success)
![License](https://img.shields.io/badge/License-MIT-green)

## ✨ Key Features

- **3D Interactive Components** - Drag, drop, and connect batteries, resistors, bulbs, switches
- **Accurate Physics** - 100% validated nodal analysis solver using Kirchhoff's laws  
- **Parallel Circuits** - Junction components make branching circuits simple
- **Real-time Feedback** - See voltage, current, and resistance directly on components
- **Property Editing** - Right-click any component to modify values

## 🚀 Quick Start

### Prerequisites
- Unity 6 (6000.0.32f1+)
- 8GB RAM minimum
- Visual Studio 2022 or JetBrains Rider

### Installation
```bash
# Clone repository
git clone https://github.com/yourusername/CircuitSimulator.git

# Open in Unity Hub
1. Add project → Navigate to CircuitSimulator_SourceCode/CircuitSimulator/
2. Select Unity 6 → Open
3. Load scene: Assets/Scenes/CircuitSimulator.unity
4. Press Play!
```

## 🎮 Controls

| Key | Action | | Key | Action |
|-----|--------|-|-----|--------|
| **B** | Battery | | **C** | Connect Mode |
| **R** | Resistor | | **V** | Select Mode |
| **L** | Light Bulb | | **X** | Delete Selected |
| **S** | Switch | | **Space** | Solve Circuit |
| **J** | Junction | | **Right-Click** | Edit Properties |

## 💡 Creating Circuits

### Simple Circuit
```
1. Place Battery (B) and Bulb (L)
2. Connect Mode (C)
3. Click Battery → Click Bulb
4. Solve (Space)
```

### Parallel Circuit
```
1. Battery (B) → Junction (J) → Two Resistors (R,R) → Junction (J)
2. Connect: Battery→Junction1→Both Resistors→Junction2→Battery
3. Solve to see current division!
```

## 🏗️ Architecture

**13 Specialized Managers** across 3 systems:
- **Circuit System** (5) - Core solving and node management
- **Workspace System** (4) - UI and display management
- **Component System** (4) - Creation and interaction

See [ARCHITECTURE.md](CircuitSimulator_SourceCode/CircuitSimulator/Assets/Scripts/ARCHITECTURE.md) for details.

## 📚 Educational Value

### Concepts Taught
- Ohm's Law (V = IR)
- Series vs Parallel Circuits
- Kirchhoff's Current & Voltage Laws
- Power Calculations (P = VI)

### Misconceptions Addressed
- Current "used up" by components
- One-wire circuits
- Constant current from batteries

## 📁 Project Structure

```
CircuitSimulator_SourceCode/
├── Assets/Scripts/
│   ├── Core/         # Circuit logic & solver
│   ├── Managers/     # 13 specialized managers
│   ├── Components/   # 3D component scripts
│   ├── Interaction/  # User input handling
│   └── UI/          # UI controllers
└── Documentation/    # Setup guides & architecture
```

## 🛠️ Development

### Testing
- **Automated**: `CircuitTestRunner.RunAllTests()`
- **Debug Mode**: Ctrl+D during play
- **Force Solve**: Ctrl+S

### Performance
- 60 FPS with 50+ components
- <50ms solve time
- Optimized for mobile/AR

## 📖 Documentation

- [Setup Guide](CircuitSimulator_SourceCode/CircuitSimulator/Assets/Scripts/SETUP.md)
- [Architecture](CircuitSimulator_SourceCode/CircuitSimulator/Assets/Scripts/ARCHITECTURE.md)
- [Dependencies](CircuitSimulator_SourceCode/CircuitSimulator/Assets/Scripts/DEPENDENCY.md)
- [Parallel Circuits](CircuitSimulator_SourceCode/CircuitSimulator/PARALLEL_CIRCUITS_GUIDE.md)

## 🚧 Roadmap

**v1.2**: Capacitors, Inductors, AC Analysis, Save/Load
**v2.0**: Full AR Mode, Multiplayer, Circuit Challenges

## 📝 License

MIT License - See [LICENSE](LICENSE) file

## 🤝 Contributing

Fork → Feature Branch → Commit → Push → Pull Request

See [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines.

---

**Built for education** ⚡🎓 | [Issues](https://github.com/yourusername/CircuitSimulator/issues) | [Discussions](https://github.com/yourusername/CircuitSimulator/discussions)