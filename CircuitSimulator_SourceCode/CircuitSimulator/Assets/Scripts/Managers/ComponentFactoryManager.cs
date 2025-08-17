using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Handles component creation and placement logic
/// Manages component prefabs and instantiation
/// </summary>
public class ComponentFactoryManager : MonoBehaviour
{
    [Header("Component Prefabs")]
    public GameObject batteryPrefab;
    public GameObject resistorPrefab;
    public GameObject bulbPrefab;
    public GameObject switchPrefab;
    
    [Header("Placement Settings")]
    public Transform canvasPlane;
    public float spacing = 2f;
    
    private int componentCount = 0;
    private List<GameObject> placedComponents = new List<GameObject>();
    
    public void Initialize(Transform plane, float componentSpacing = 2f)
    {
        canvasPlane = plane;
        spacing = componentSpacing;
        Debug.Log("ComponentFactoryManager initialized");
    }
    
    #region Component Creation
    
    public GameObject CreateBattery()
    {
        return CreateComponent("Battery", ComponentType.Battery, Color.red, 6f, 0f);
    }
    
    public GameObject CreateResistor()
    {
        return CreateComponent("Resistor", ComponentType.Resistor, Color.yellow, 0f, 100f);
    }
    
    public GameObject CreateBulb()
    {
        return CreateComponent("Bulb", ComponentType.Bulb, Color.white, 0f, 50f);
    }
    
    public GameObject CreateSwitch()
    {
        return CreateComponent("Switch", ComponentType.Switch, Color.gray, 0f, 0f);
    }
    
    #endregion
    
    #region Core Creation Logic
    
    private GameObject CreateComponent(string name, ComponentType type, Color color, float voltage, float resistance)
    {
        Debug.Log($"Creating {name}");
        
        if (canvasPlane == null)
        {
            Debug.LogError("canvasPlane is not assigned! Cannot place component.");
            return null;
        }
        
        try
        {
            // Calculate position
            Vector3 position = canvasPlane.position + new Vector3(componentCount * spacing, 0.5f, 0);
            
            // Create component object
            GameObject componentObject = CreateComponentObject(name, position);
            if (componentObject == null)
            {
                Debug.LogError($"Failed to create {name} component!");
                return null;
            }
            
            // Setup transform
            componentObject.transform.position = position;
            componentObject.transform.SetParent(canvasPlane);
            componentObject.name = $"{name}_{componentCount}";
            
            // Setup visual appearance
            SetupComponentVisuals(componentObject, color);
            
            // Add circuit functionality
            SetupCircuitComponent(componentObject, type, voltage, resistance);
            
            // Add interaction capabilities
            SetupComponentInteraction(componentObject);
            
            // Track component
            placedComponents.Add(componentObject);
            componentCount++;
            
            Debug.Log($"Successfully created {name} at position {position}. Total components: {componentCount}");
            return componentObject;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to create {name}: {e.Message}\n{e.StackTrace}");
            return null;
        }
    }
    
    private GameObject CreateComponentObject(string componentName, Vector3 position)
    {
        GameObject prefab = GetPrefabForComponent(componentName);
        
        if (prefab != null)
        {
            Debug.Log($"Using custom prefab for {componentName}");
            return Instantiate(prefab, position, Quaternion.identity);
        }
        else
        {
            Debug.Log($"Using default primitive for {componentName}");
            return GameObject.CreatePrimitive(PrimitiveType.Cube);
        }
    }
    
    private GameObject GetPrefabForComponent(string componentName)
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
    
    private void SetupComponentVisuals(GameObject componentObject, Color color)
    {
        var renderer = componentObject.GetComponent<Renderer>();
        if (renderer != null && renderer.material != null)
        {
            renderer.material.color = color;
        }
        else
        {
            Debug.LogWarning("Failed to set material color");
        }
    }
    
    private void SetupCircuitComponent(GameObject componentObject, ComponentType type, float voltage, float resistance)
    {
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
        
        // Set component properties
        circuitComp.ComponentType = type;
        circuitComp.voltage = voltage;
        circuitComp.resistance = resistance;
        
        Debug.Log($"Set as {type}: {voltage}V, {resistance}Ω");
    }
    
    private void SetupComponentInteraction(GameObject componentObject)
    {
        // Add selection capability
        SelectableComponent selectable = componentObject.GetComponent<SelectableComponent>();
        if (selectable == null)
        {
            selectable = componentObject.AddComponent<SelectableComponent>();
        }
        
        // Add movement capability
        MoveableComponent moveable = componentObject.GetComponent<MoveableComponent>();
        if (moveable == null)
        {
            moveable = componentObject.AddComponent<MoveableComponent>();
        }
    }
    
    #endregion
    
    #region Component Management
    
    public void RemoveComponent(GameObject component)
    {
        if (placedComponents.Contains(component))
        {
            placedComponents.Remove(component);
            Debug.Log($"Removed {component.name} from component list");
        }
    }
    
    public void ClearAllComponents()
    {
        foreach (var component in placedComponents)
        {
            if (component != null)
            {
                DestroyImmediate(component);
            }
        }
        placedComponents.Clear();
        componentCount = 0;
        Debug.Log("All components cleared");
    }
    
    public List<GameObject> GetPlacedComponents()
    {
        // Remove null references
        placedComponents.RemoveAll(item => item == null);
        return new List<GameObject>(placedComponents);
    }
    
    public int ComponentCount => placedComponents.Count;
    
    #endregion
    
    #region Testing
    
    [ContextMenu("Test Simple Placement")]
    public void TestSimplePlacement()
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
    
    #endregion
}