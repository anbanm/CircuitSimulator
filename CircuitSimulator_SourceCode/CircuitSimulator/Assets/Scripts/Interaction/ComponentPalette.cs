using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class ComponentPalette : MonoBehaviour
{
    [Header("UI References")]
    public Transform paletteContainer;  // Parent for buttons
    public Button buttonPrefab;         // Button prefab to duplicate
    
    [Header("Placement Settings")]
    public Transform canvasPlane;       // Where to place components
    public float spacing = 2f;          // Distance between components
    
    private int componentCount = 0;     // Counter for positioning
    private List<GameObject> placedComponents = new List<GameObject>();
    
    void Start()
    {
        CreatePaletteButtons();
    }
    
    void CreatePaletteButtons()
    {
        CreateButton("BATTERY", Color.red, PlaceBattery);
        CreateButton("RESISTOR", Color.yellow, PlaceResistor);
        CreateButton("BULB", Color.white, PlaceBulb);
        CreateButton("SWITCH", Color.gray, PlaceSwitch);
        CreateButton("SOLVE!", Color.green, ManualSolve);
        CreateButton("VALIDATE", Color.cyan, ValidateCircuit);
        CreateButton("TEST", Color.cyan, TestCircuit);
        CreateButton("DEBUG", Color.magenta, DebugRegistration);
        CreateButton("REPORT", Color.yellow, SaveReport);
    }
    
    void ValidateCircuit()
    {
        Circuit3DManager manager = FindObjectOfType<Circuit3DManager>();
        if (manager != null)
        {
            Debug.Log("Manual validation triggered from palette");
            // The validation will happen automatically in SolveCircuit now
            manager.SolveCircuit();
        }
        else
        {
            Debug.LogWarning("No Circuit3DManager found!");
        }
    }
    
    void ManualSolve()
    {
        Circuit3DManager manager = FindObjectOfType<Circuit3DManager>();
        if (manager != null)
        {
            Debug.Log("Manual solve triggered from palette");
            manager.SolveCircuitManually();
        }
        else
        {
            Debug.LogWarning("No Circuit3DManager found!");
        }
    }
    
    void SaveReport()
    {
        Circuit3DManager manager = Circuit3DManager.Instance;
        if (manager == null) manager = FindObjectOfType<Circuit3DManager>();
        
        if (manager != null)
        {
            Debug.Log("Save debug report triggered from palette");
            // TODO: Add SaveDebugReport method to manager
        }
        else
        {
            Debug.LogWarning("No Circuit3DManager found!");
        }
    }
    
    void DebugRegistration()
    {
        Circuit3DManager manager = Circuit3DManager.Instance;
        if (manager == null) manager = FindObjectOfType<Circuit3DManager>();
        
        if (manager != null)
        {
            Debug.Log("Debug registration triggered from palette");
            // TODO: Add DebugComponentRegistration method to manager
        }
        else
        {
            Debug.LogWarning("No Circuit3DManager found!");
        }
    }
    
    void TestCircuit()
    {
        Circuit3DManager manager = Circuit3DManager.Instance;
        if (manager == null) manager = FindObjectOfType<Circuit3DManager>();
        
        if (manager != null)
        {
            Debug.Log("Test circuit triggered from palette");
            // TODO: Add TestWithoutWires method to manager
        }
        else
        {
            Debug.LogWarning("No Circuit3DManager found!");
        }
    }
    
    void CreateButton(string label, Color color, System.Action onClick)
    {
        // Create button
        Button newButton = Instantiate(buttonPrefab, paletteContainer);
        
        // Set button text (handle both Text and TextMeshPro)
        Text buttonText = newButton.GetComponentInChildren<Text>();
        if (buttonText != null)
        {
            buttonText.text = label;
        }
        else
        {
            // Try TextMeshPro if regular Text not found
            var tmpText = newButton.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            if (tmpText != null)
                tmpText.text = label;
        }
        
        // Set button color
        Image buttonImage = newButton.GetComponent<Image>();
        if (buttonImage != null)
            buttonImage.color = color;
            
        // Add click listener
        newButton.onClick.AddListener(() => onClick());
    }
    
    void PlaceBattery() { PlaceComponent("Battery", Color.red); }
    void PlaceResistor() { PlaceComponent("Resistor", Color.yellow); }
    void PlaceBulb() { PlaceComponent("Bulb", Color.white); }
    void PlaceSwitch() { PlaceComponent("Switch", Color.gray); }
    
    [ContextMenu("Test Simple Placement")]
    void TestSimplePlacement()
    {
        Debug.Log("Testing simple cube placement...");
        
        if (canvasPlane == null)
        {
            Debug.LogError("canvasPlane is null!");
            return;
        }
        
        Vector3 testPos = canvasPlane.position + Vector3.up;
        GameObject testCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        testCube.transform.position = testPos;
        testCube.name = "TestCube";
        testCube.GetComponent<Renderer>().material.color = Color.green;
        
        Debug.Log($"Created test cube at {testPos}");
    }
    
    void PlaceComponent(string name, Color color)
    {
        Debug.Log($"Attempting to place {name}");
        
        if (canvasPlane == null)
        {
            Debug.LogError("canvasPlane is not assigned! Please assign the plane in ComponentPalette inspector.");
            return;
        }
        
        try
        {
            // Calculate position
            Vector3 position = canvasPlane.position + new Vector3(componentCount * spacing, 0.5f, 0);
            Debug.Log($"Placing {name} at position: {position}");
            
            // Create simple cube
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            if (cube == null)
            {
                Debug.LogError("Failed to create cube primitive!");
                return;
            }
            
            cube.transform.position = position;
            cube.transform.SetParent(canvasPlane);
            cube.name = $"{name}_{componentCount}";
            
            // Set color
            var renderer = cube.GetComponent<Renderer>();
            if (renderer != null && renderer.material != null)
            {
                renderer.material.color = color;
            }
            else
            {
                Debug.LogWarning("Failed to set material color");
            }
            
            // Add circuit component capability
            Debug.Log($"Adding CircuitComponent3D to {cube.name}");
            CircuitComponent3D circuitComp = cube.AddComponent<CircuitComponent3D>();
            if (circuitComp == null)
            {
                Debug.LogError("Failed to add CircuitComponent3D!");
                return;
            }
            
            circuitComp.componentColor = color;
            
            // Set component type and properties
            switch (name)
            {
                case "Battery":
                    circuitComp.componentType = ComponentType.Battery;
                    circuitComp.voltage = 6f;
                    circuitComp.resistance = 0f;
                    Debug.Log($"Set as Battery: 6V, 0Ω");
                    break;
                case "Resistor":
                    circuitComp.componentType = ComponentType.Resistor;
                    circuitComp.resistance = 100f;
                    Debug.Log($"Set as Resistor: 100Ω");
                    break;
                case "Bulb":
                    circuitComp.componentType = ComponentType.Bulb;
                    circuitComp.resistance = 50f;
                    Debug.Log($"Set as Bulb: 50Ω");
                    break;
                case "Switch":
                    circuitComp.componentType = ComponentType.Switch;
                    circuitComp.resistance = 0f; // Closed switch
                    Debug.Log($"Set as Switch: Closed (0Ω)");
                    break;
            }
            
            // Add selection capability
            Debug.Log("Adding SelectableComponent");
            SelectableComponent selectable = cube.AddComponent<SelectableComponent>();
            
            // Add movement capability
            Debug.Log("Adding MoveableComponent");
            MoveableComponent moveable = cube.AddComponent<MoveableComponent>();
            
            // Add to list
            placedComponents.Add(cube);
            componentCount++;
            
            Debug.Log($"Successfully placed {name} at position {position}. Total components: {componentCount}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to place {name}: {e.Message}\n{e.StackTrace}");
        }
    }
    
    public void RemoveComponent(GameObject component)
    {
        if (placedComponents.Contains(component))
        {
            placedComponents.Remove(component);
            Debug.Log($"Removed {component.name} from component list");
        }
    }
}
