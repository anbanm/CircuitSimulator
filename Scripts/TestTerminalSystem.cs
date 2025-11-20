using UnityEngine;

/// <summary>
/// Test script to verify terminal system is working
/// Creates a battery component with terminals automatically
/// </summary>
public class TestTerminalSystem : MonoBehaviour
{
    void Start()
    {
        Debug.Log("TestTerminalSystem: Creating test battery with terminals");
        
        // Create a test battery GameObject
        GameObject batteryObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        batteryObj.name = "TestBatteryWithTerminals";
        batteryObj.transform.position = new Vector3(0, 1, 3);
        
        // Add the CircuitComponent3D script
        CircuitComponent3D batteryComponent = batteryObj.AddComponent<CircuitComponent3D>();
        batteryComponent.ComponentType = ComponentType.Battery;
        batteryComponent.voltage = 12f;
        batteryComponent.resistance = 0.1f;
        
        // Add SelectableComponent for interaction
        batteryObj.AddComponent<SelectableComponent>();
        batteryObj.AddComponent<MoveableComponent>();
        
        Debug.Log($"Created test battery: {batteryObj.name}");
        
        // Create a test resistor
        GameObject resistorObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        resistorObj.name = "TestResistorWithTerminals";
        resistorObj.transform.position = new Vector3(3, 1, 3);
        
        // Add the CircuitComponent3D script
        CircuitComponent3D resistorComponent = resistorObj.AddComponent<CircuitComponent3D>();
        resistorComponent.ComponentType = ComponentType.Resistor;
        resistorComponent.resistance = 10f;
        
        // Add SelectableComponent for interaction
        resistorObj.AddComponent<SelectableComponent>();
        resistorObj.AddComponent<MoveableComponent>();
        
        Debug.Log($"Created test resistor: {resistorObj.name}");
        
        Debug.Log("TestTerminalSystem: Test components created - terminals should appear shortly");
    }
}