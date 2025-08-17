#!/bin/bash
# Circuit Simulator - Quick Setup Script for New Machine

echo "🔌 Circuit Simulator - Project Setup"
echo "=================================="

# Create Unity project structure
echo "📁 Creating Unity project structure..."
mkdir -p "CircuitSimulator/Assets/Scripts"
mkdir -p "CircuitSimulator/Assets/Materials"
mkdir -p "CircuitSimulator/Assets/Prefabs"

# Copy source files
echo "📋 Copying source scripts..."
cp Scripts/*.cs "CircuitSimulator/Assets/Scripts/"

# Create project settings template
echo "⚙️ Creating Unity project template..."
cat > "CircuitSimulator/ProjectSettings/ProjectVersion.txt" << EOF
m_EditorVersion: 2022.3.17f1
m_EditorVersionWithRevision: 2022.3.17f1 (3e67ad1cf70c)
EOF

echo "✅ Project structure created!"
echo ""
echo "📝 Next Steps:"
echo "1. Open Unity Hub"
echo "2. Click 'Add' and select the CircuitSimulator folder"
echo "3. Open project in Unity 2022.3 LTS"
echo "4. Follow README.md setup instructions"
echo ""
echo "🎯 Quick Test:"
echo "1. Create Empty GameObject → Add Circuit3DManager script"
echo "2. Create Cube → Add CircuitComponent3D script (Battery)"
echo "3. Create Sphere → Add CircuitComponent3D script (Bulb)" 
echo "4. Create Cylinder → Add CircuitWire script"
echo "5. Press Play and check Console for circuit solving!"
echo ""
echo "🐛 Debug Commands:"
echo "- Ctrl+S: Manual solve"
echo "- Right-click Circuit3DManager → Force Register All Components"
echo "- Right-click Circuit3DManager → Test Known Working Circuit"
