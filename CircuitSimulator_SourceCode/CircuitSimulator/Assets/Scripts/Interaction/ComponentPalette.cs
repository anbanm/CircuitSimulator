using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class ComponentPalette : MonoBehaviour
{
    [Header("UI References")]
    public Transform paletteContainer;  // Parent for buttons
    public Button buttonPrefab;         // Button prefab to duplicate
    
    [Header("Component Prefabs (Optional)")]
    public GameObject batteryPrefab;    // Custom battery prefab
    public GameObject resistorPrefab;   // Custom resistor prefab
    public GameObject bulbPrefab;       // Custom bulb prefab
    public GameObject switchPrefab;     // Custom switch prefab
    
    [Header("Placement Settings")]
    public Transform canvasPlane;       // Where to place components
    public float spacing = 2f;          // Distance between components
    
    private int componentCount = 0;     // Counter for positioning
    private List<GameObject> placedComponents = new List<GameObject>();
    private Circuit3DManager cachedManager; // Cache to avoid FindObjectOfType calls
    
    void Start()
    {
        CreatePaletteButtons();
        CacheManagerReference();
    }
    
    void Update()
    {
        // Keyboard shortcuts while UI is disabled
        if (Input.GetKeyDown(KeyCode.B))
        {
            Debug.Log("Placing Battery (B key)");
            PlaceBattery();
        }
        if (Input.GetKeyDown(KeyCode.R))
        {
            Debug.Log("Placing Resistor (R key)");
            PlaceResistor();
        }
        if (Input.GetKeyDown(KeyCode.L))
        {
            Debug.Log("Placing Bulb (L key)");
            PlaceBulb();
        }
        if (Input.GetKeyDown(KeyCode.S))
        {
            Debug.Log("Placing Switch (S key)");
            PlaceSwitch();
        }
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("Solving Circuit (SPACE key)");
            ManualSolve();
        }
        if (Input.GetKeyDown(KeyCode.T))
        {
            Debug.Log("Testing Circuit (T key)");
            TestCircuit();
        }
    }
    
    void CacheManagerReference()
    {
        cachedManager = Circuit3DManager.Instance;
        if (cachedManager == null)
        {
            cachedManager = FindObjectOfType<Circuit3DManager>();
        }
    }
    
    Circuit3DManager GetManager()
    {
        if (cachedManager == null)
        {
            CacheManagerReference();
        }
        return cachedManager;
    }
    
    void CreatePaletteButtons()
    {
        CreateButton("BATTERY", Color.red, PlaceBattery);
        CreateButton("RESISTOR", Color.yellow, PlaceResistor);
        CreateButton("BULB", Color.white, PlaceBulb);
        CreateButton("SWITCH", Color.gray, PlaceSwitch);
        CreateButton("WIRE TOOL", Color.blue, ActivateWireTool);
        CreateButton("SOLVE!", Color.green, ManualSolve);
        CreateButton("VALIDATE", Color.cyan, ValidateCircuit);
        CreateButton("TEST", Color.cyan, TestCircuit);
        CreateButton("DEBUG", Color.magenta, DebugRegistration);
        CreateButton("REPORT", Color.yellow, SaveReport);
    }
    
    void ValidateCircuit()
    {
        Circuit3DManager manager = GetManager();
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
        Circuit3DManager manager = GetManager();
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
        Circuit3DManager manager = GetManager();
        
        if (manager != null)
        {
            Debug.Log("Save debug report triggered from palette");
            manager.SaveDebugReport();
        }
        else
        {
            Debug.LogWarning("No Circuit3DManager found!");
        }
    }
    
    void DebugRegistration()
    {
        Circuit3DManager manager = GetManager();
        
        if (manager != null)
        {
            Debug.Log("Debug registration triggered from palette");
            manager.DebugComponentRegistration();
        }
        else
        {
            Debug.LogWarning("No Circuit3DManager found!");
        }
    }
    
    void TestCircuit()
    {
        Circuit3DManager manager = GetManager();
        
        if (manager != null)
        {
            Debug.Log("Test circuit triggered from palette");
            manager.ValidateAndTestCircuit();
        }
        else
        {
            Debug.LogWarning("No Circuit3DManager found!");
        }
    }
    
    void CreateButton(string label, Color color, System.Action onClick)
    {
        if (buttonPrefab == null || paletteContainer == null)
        {
            Debug.LogWarning($"Cannot create button '{label}' - buttonPrefab or paletteContainer not assigned");
            return;
        }
        
        // Create button
        Button newButton = Instantiate(buttonPrefab, paletteContainer);
        newButton.name = $"Button_{label}";
        
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
    
    public void PlaceBattery() { PlaceComponent("Battery", Color.red); }
    public void PlaceResistor() { PlaceComponent("Resistor", Color.yellow); }
    public void PlaceBulb() { PlaceComponent("Bulb", Color.white); }
    public void PlaceSwitch() { PlaceComponent("Switch", Color.gray); }
    
    void ActivateWireTool()
    {
        Debug.Log("🔌 Wire Tool Activated - Click on two components to connect them");
        
        // Find or create ConnectTool
        ConnectTool connectTool = FindObjectOfType<ConnectTool>();
        if (connectTool == null)
        {
            GameObject connectToolObj = new GameObject("ConnectTool");
            connectTool = connectToolObj.AddComponent<ConnectTool>();
        }
        
        // Activate connect mode
        connectTool.SetConnectMode();
    }
    
    GameObject CreateComponentObject(string componentName, Vector3 position)
    {
        GameObject prefab = GetPrefabForComponent(componentName);
        
        if (prefab != null)
        {
            // Use custom prefab
            Debug.Log($"Using custom prefab for {componentName}");
            return Instantiate(prefab, position, Quaternion.identity);
        }
        else
        {
            // Create default primitive
            Debug.Log($"Using default primitive for {componentName}");
            return GameObject.CreatePrimitive(PrimitiveType.Cube);
        }
    }
    
    GameObject GetPrefabForComponent(string componentName)
    {
        switch (componentName)
        {
            case "Battery": return batteryPrefab;
            case "Resistor": return resistorPrefab;
            case "Bulb": return bulbPrefab;
            case "Switch": return switchPrefab;
            default: return null;
        }
    }
    
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
            
            // Get prefab for component type or create default cube
            GameObject componentObject = CreateComponentObject(name, position);
            if (componentObject == null)
            {
                Debug.LogError($"Failed to create {name} component!");
                return;
            }
            
            componentObject.transform.position = position;
            componentObject.transform.SetParent(canvasPlane);
            componentObject.name = $"{name}_{componentCount}";
            
            // Set color
            var renderer = componentObject.GetComponent<Renderer>();
            if (renderer != null && renderer.material != null)
            {
                renderer.material.color = color;
            }
            else
            {
                Debug.LogWarning("Failed to set material color");
            }
            
            // Add circuit component capability
            Debug.Log($"Adding CircuitComponent3D to {componentObject.name}");
            CircuitComponent3D circuitComp = componentObject.GetComponent<CircuitComponent3D>();
            if (circuitComp == null)
            {
                circuitComp = componentObject.AddComponent<CircuitComponent3D>();
            }
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
            SelectableComponent selectable = componentObject.GetComponent<SelectableComponent>();
            if (selectable == null)
            {
                selectable = componentObject.AddComponent<SelectableComponent>();
            }
            
            // Add movement capability
            Debug.Log("Adding MoveableComponent");
            MoveableComponent moveable = componentObject.GetComponent<MoveableComponent>();
            if (moveable == null)
            {
                moveable = componentObject.AddComponent<MoveableComponent>();
            }
            
            // Add to list
            placedComponents.Add(componentObject);
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
