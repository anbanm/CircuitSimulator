using UnityEngine;
using System.Collections.Generic;
using CircuitSimulator.Services;
using CircuitSimulator;
using CircuitSimulator.Components;
using ComponentDefinitionLegacy = CircuitSimulator.ComponentDefinition;

/// <summary>
/// Handles component creation and placement logic
/// Implements IComponentFactory interface and integrates with ServiceLocator
/// </summary>
public class ComponentFactoryManager : MonoBehaviour, IComponentFactory
{
    [Header("Component Prefabs")]
    public GameObject batteryPrefab;
    public GameObject resistorPrefab;
    public GameObject bulbPrefab;
    public GameObject switchPrefab;

    [Header("Component Definitions (ScriptableObjects)")]
    [Tooltip("Optional ThemedComponentDefinition assets for themed components")]
    public CircuitSimulator.Components.ThemedComponentDefinition[] componentDefinitions;

    [Header("Placement Settings")]
    public Transform canvasPlane;
    public float spacing = 2f;

    // IComponentFactory Events
    public System.Action<CircuitComponent3D> OnComponentCreated { get; set; }
    public System.Action<CircuitWire> OnWireCreated { get; set; }

    // IComponentFactory Properties
    public bool UseRandomizedPositioning { get; set; } = false;
    public float ComponentSpacing { get; set; } = 2f;
    public Transform ComponentParent { get; set; }

    void Awake()
    {
        // Register with ServiceLocator
        ServiceLocator.Instance.Register<IComponentFactory>(this);

        // Auto-find Components GameObject if canvasPlane not set or incorrectly assigned
        if (canvasPlane == null || canvasPlane.name == "Main Canvas")
        {
            GameObject componentsObj = GameObject.Find("Components");
            if (componentsObj != null)
            {
                canvasPlane = componentsObj.transform;
            }
            else
            {
                // Create it if it doesn't exist
                componentsObj = new GameObject("Components");
                canvasPlane = componentsObj.transform;
            }
        }

        // Initialize properties
        ComponentSpacing = spacing;
        ComponentParent = canvasPlane;

    }

    void Start()
    {
        // CRITICAL FIX: Disable AR LOD system that's hiding components
        // This must run in Start() AFTER ARWorkspaceAdapter is initialized
        var arAdapter = FindFirstObjectByType<ARWorkspaceAdapter>();
        if (arAdapter != null)
        {
            arAdapter.DisableARMode();
        }
        else
        {
            Debug.LogWarning("[ComponentFactoryManager] ARWorkspaceAdapter not found");
        }
    }
    
    private int componentCount = 0;
    private List<GameObject> placedComponents = new List<GameObject>();
    
    public void Initialize(Transform plane, float componentSpacing = 2f)
    {
        canvasPlane = plane;
        spacing = componentSpacing;
        // Ensure the list is initialized
        if (placedComponents == null)
        {
            placedComponents = new List<GameObject>();
        }
    }
    
    public void ResetComponentTracking()
    {
        // Clear the placed components list and reset counter
        placedComponents.Clear();
        componentCount = 0;
    }

    public Vector3 GetLastComponentPosition()
    {
        // Return position of last placed component, or origin if none
        if (placedComponents != null && placedComponents.Count > 0)
        {
            GameObject lastComponent = placedComponents[placedComponents.Count - 1];
            if (lastComponent != null)
            {
                return lastComponent.transform.position;
            }
        }
        return Vector3.zero;
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
    
    public GameObject CreateJunction()
    {

        if (canvasPlane == null)
        {
            Debug.LogError("canvasPlane is not assigned! Cannot place junction.");
            return null;
        }

        // Use the current count of placed components for positioning
        int currentIndex = placedComponents.Count;
        Vector3 position = canvasPlane.position + new Vector3(currentIndex * spacing, 0.5f, 0);

        // Create junction object
        GameObject junction = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        junction.name = $"Junction_{currentIndex}";
        junction.transform.position = position;
        junction.transform.localScale = Vector3.one * 0.7f;

        // Set visual appearance
        Renderer renderer = junction.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = new Color(0.5f, 0.5f, 0.7f, 1f);
        }

        // FIXED: Make junctions CONNECTABLE by adding electrical component support
        // Junctions act as 0-resistance wires electrically
        CircuitComponent3D circuitComp = junction.AddComponent<CircuitComponent3D>();
        circuitComp.ComponentType = ComponentType.Wire;
        circuitComp.resistance = 0f; // Zero resistance
        circuitComp.voltage = 0f;

        // Add connection points so wires can attach
        ComponentConnectionPoints connectionPoints = junction.AddComponent<ComponentConnectionPoints>();
        // Set up 4 connection points around the junction (N, S, E, W)
        connectionPoints.connectionPoints = new List<ComponentConnectionPoints.ConnectionPoint>
        {
            new ComponentConnectionPoints.ConnectionPoint { localPosition = new Vector3(0, 0.5f, 0), label = "Top" },
            new ComponentConnectionPoints.ConnectionPoint { localPosition = new Vector3(0, -0.5f, 0), label = "Bottom" },
            new ComponentConnectionPoints.ConnectionPoint { localPosition = new Vector3(0.5f, 0, 0), label = "Right" },
            new ComponentConnectionPoints.ConnectionPoint { localPosition = new Vector3(-0.5f, 0, 0), label = "Left" }
        };

        // Setup terminals for wire connections
        SetupConnectionTerminals(junction);

        // Add selection capability for wire connections
        SelectableComponent selectable = junction.AddComponent<SelectableComponent>();

        // Add movement capability
        MoveableComponent moveable = junction.AddComponent<MoveableComponent>();

        // Add junction script for visual behavior and connection logic
        CircuitJunction junctionScript = junction.AddComponent<CircuitJunction>();

        // Track junction
        placedComponents.Add(junction);

        return junction;
    }
    
    #endregion
    
    #region Core Creation Logic
    
    private GameObject CreateComponent(string name, ComponentType type, Color color, float voltage, float resistance)
    {
        
        if (canvasPlane == null)
        {
            Debug.LogError("canvasPlane is not assigned! Cannot place component.");
            return null;
        }
        
        try
        {
            // Use the current count of placed components for positioning
            int currentIndex = placedComponents.Count;

            // FIXED: Use camera-relative positioning instead of old grid system
            Vector3 position = GetNextPlacementPosition();

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
            componentObject.name = $"{name}_{currentIndex}";
            
            // Setup visual appearance
            SetupComponentVisuals(componentObject, color);
            
            // Add circuit functionality
            SetupCircuitComponent(componentObject, type, voltage, resistance);
            
            // Add interaction capabilities
            SetupComponentInteraction(componentObject);

            // Setup connection terminals (CRITICAL for terminal-to-terminal wiring)
            SetupComponentTerminals(componentObject);

            // Track component
            placedComponents.Add(componentObject);
            componentCount = placedComponents.Count;

            // Fire interface event
            var circuitComponent = componentObject.GetComponent<CircuitComponent3D>();
            if (circuitComponent != null)
            {
                OnComponentCreated?.Invoke(circuitComponent);
            }

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
            return Instantiate(prefab, position, Quaternion.identity);
        }
        else
        {
            return CreatePrimitiveForComponent(componentName, position);
        }
    }
    
    private GameObject CreatePrimitiveForComponent(string componentName, Vector3 position)
    {
        GameObject componentObject;

        switch (componentName)
        {
            case "Battery":
                // Battery = Cube (rectangular like real battery)
                componentObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                componentObject.transform.localScale = new Vector3(1.5f, 0.8f, 0.6f);
                break;

            case "Resistor":
                // Resistor = Cylinder (cylindrical like real resistor)
                componentObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                componentObject.transform.localScale = new Vector3(0.4f, 1.2f, 0.4f);
                componentObject.transform.Rotate(0, 0, 90); // Horizontal
                break;

            case "Bulb":
                // Bulb = Sphere (bulb-shaped)
                componentObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                componentObject.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);
                break;

            case "Switch":
                // Switch = Capsule (toggle switch shape)
                componentObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                componentObject.transform.localScale = new Vector3(0.6f, 0.4f, 1.0f);
                componentObject.transform.Rotate(90, 0, 0); // Flat orientation
                break;

            default:
                componentObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                break;
        }

        // DISABLED: ComponentConnectionPoints creates duplicate terminals
        // ComponentTerminalManager handles terminal creation now
        // AddComponentConnectionPoints(componentObject, componentName);

        return componentObject;
    }

    private void AddComponentConnectionPoints(GameObject componentObject, string componentName)
    {
        // Add ComponentConnectionPoints script for gizmo-based terminal positioning
        ComponentConnectionPoints connectionPoints = componentObject.AddComponent<ComponentConnectionPoints>();

        // Setup default connection points based on component type
        switch (componentName)
        {
            case "Battery":
                // Battery: Positive on right, Negative on left
                connectionPoints.connectionPoints = new List<ComponentConnectionPoints.ConnectionPoint>
                {
                    new ComponentConnectionPoints.ConnectionPoint
                    {
                        localPosition = new Vector3(-0.75f, 0, 0), // Left side = Negative
                        polarity = ElectricalPolarity.Negative,
                        gizmoColor = Color.blue,
                        label = "-"
                    },
                    new ComponentConnectionPoints.ConnectionPoint
                    {
                        localPosition = new Vector3(0.75f, 0, 0), // Right side = Positive
                        polarity = ElectricalPolarity.Positive,
                        gizmoColor = Color.red,
                        label = "+"
                    }
                };
                break;

            case "Resistor":
            case "Bulb":
                // Resistor/Bulb: Bidirectional terminals (no polarity)
                connectionPoints.connectionPoints = new List<ComponentConnectionPoints.ConnectionPoint>
                {
                    new ComponentConnectionPoints.ConnectionPoint
                    {
                        localPosition = new Vector3(-0.6f, 0, 0), // Terminal A
                        polarity = ElectricalPolarity.Neutral,
                        gizmoColor = Color.yellow,
                        label = "A"
                    },
                    new ComponentConnectionPoints.ConnectionPoint
                    {
                        localPosition = new Vector3(0.6f, 0, 0), // Terminal B
                        polarity = ElectricalPolarity.Neutral,
                        gizmoColor = Color.yellow,
                        label = "B"
                    }
                };
                break;

            case "Switch":
                // Switch: Bidirectional terminals (no true polarity, just endpoints)
                connectionPoints.connectionPoints = new List<ComponentConnectionPoints.ConnectionPoint>
                {
                    new ComponentConnectionPoints.ConnectionPoint
                    {
                        localPosition = new Vector3(-0.5f, 0, 0), // Terminal 1
                        polarity = ElectricalPolarity.Neutral,
                        gizmoColor = Color.green,
                        label = "1"
                    },
                    new ComponentConnectionPoints.ConnectionPoint
                    {
                        localPosition = new Vector3(0.5f, 0, 0), // Terminal 2
                        polarity = ElectricalPolarity.Neutral,
                        gizmoColor = Color.green,
                        label = "2"
                    }
                };
                break;

            default:
                // Default: Two neutral terminals
                connectionPoints.connectionPoints = new List<ComponentConnectionPoints.ConnectionPoint>
                {
                    new ComponentConnectionPoints.ConnectionPoint
                    {
                        localPosition = new Vector3(-0.5f, 0, 0),
                        polarity = ElectricalPolarity.Neutral,
                        gizmoColor = Color.white,
                        label = "1"
                    },
                    new ComponentConnectionPoints.ConnectionPoint
                    {
                        localPosition = new Vector3(0.5f, 0, 0),
                        polarity = ElectricalPolarity.Neutral,
                        gizmoColor = Color.white,
                        label = "2"
                    }
                };
                break;
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
        if (renderer == null)
        {
            Debug.LogError($"MeshRenderer is missing on {componentObject.name}! Component will be invisible.");
            return;
        }

        // CRITICAL FIX: Ensure MeshRenderer is enabled
        renderer.enabled = true;

        // Ensure material exists - create default if missing
        if (renderer.sharedMaterial == null)
        {
            Debug.LogWarning($"Material missing on {componentObject.name}, creating default material");
            // Use Unity's built-in Standard shader as fallback
            Shader standardShader = Shader.Find("Standard");
            if (standardShader != null)
            {
                renderer.material = new Material(standardShader);
            }
            else
            {
                // Absolute fallback - use the default diffuse material
                renderer.material = new Material(Shader.Find("Diffuse"));
            }
        }

        // Set color on material instance
        if (renderer.material != null)
        {
            renderer.material.color = color;
        }
        else
        {
            Debug.LogError($"Failed to create material for {componentObject.name}");
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

        // CRITICAL FIX: Add InteractionComponent for terminal-based wire connections
        CircuitSimulator.Components.InteractionComponent interactionComp = componentObject.GetComponent<CircuitSimulator.Components.InteractionComponent>();
        if (interactionComp == null)
        {
            interactionComp = componentObject.AddComponent<CircuitSimulator.Components.InteractionComponent>();
        }

        // Add connection terminals for electrical connections
        SetupConnectionTerminals(componentObject);

        // DISABLED - Using PersistentLabel system instead
        // ComponentValueDisplay valueDisplay = componentObject.GetComponent<ComponentValueDisplay>();
        // if (valueDisplay == null)
        // {
        //     valueDisplay = componentObject.AddComponent<ComponentValueDisplay>();
        // }
    }

    private void SetupConnectionTerminals(GameObject componentObject)
    {
        CircuitComponent3D circuitComp = componentObject.GetComponent<CircuitComponent3D>();
        if (circuitComp == null) return;

        // NEW: Check if component has gizmo-based connection points
        ComponentConnectionPoints connectionPoints = componentObject.GetComponent<ComponentConnectionPoints>();
        if (connectionPoints != null)
        {
            connectionPoints.CreateRuntimeTerminals();
            return;
        }

        // Check if component has a ThemedComponentDefinition with custom connection points
        CircuitSimulator.Components.ThemedComponentDefinitionApplier definitionApplier = componentObject.GetComponent<CircuitSimulator.Components.ThemedComponentDefinitionApplier>();
        if (definitionApplier != null && definitionApplier.definition != null)
        {
            // Custom connection points will be handled by the ThemedComponentDefinitionApplier
            return;
        }

        // NO FALLBACK - Components MUST have gizmo-based connection points or ThemedComponentDefinition
        Debug.LogWarning($"Component {componentObject.name} has no connection points defined! Add ComponentConnectionPoints or use a ThemedComponentDefinition.");
    }

    private void SetupComponentTerminals(GameObject componentObject)
    {
        // Get the ComponentTerminalManager from CircuitManager
        CircuitComponent3D circuitComp = componentObject.GetComponent<CircuitComponent3D>();
        if (circuitComp == null)
        {
            Debug.LogWarning($"Cannot setup terminals: {componentObject.name} has no CircuitComponent3D");
            return;
        }

        // Find ComponentTerminalManager
        ComponentTerminalManager terminalManager = FindFirstObjectByType<ComponentTerminalManager>();
        if (terminalManager == null)
        {
            Debug.LogWarning("ComponentTerminalManager not found! Terminals will not be created.");
            return;
        }

        // Create terminals for this component
        terminalManager.SetupComponentTerminals(circuitComp);
    }

    #endregion

    #region ComponentDefinition Support

    /// <summary>
    /// Create a component from a ThemedComponentDefinition ScriptableObject
    /// </summary>
    public GameObject CreateComponentFromDefinition(ThemedComponentDefinition definition, Vector3? position = null)
    {
        if (definition == null)
        {
            Debug.LogError("ComponentDefinition is null");
            return null;
        }

        Vector3 createPosition = position ?? GetNextPlacementPosition();


        GameObject componentObject;

        // Use custom prefab if available, otherwise create primitive
        if (definition.visualProperties.customPrefab != null)
        {
            componentObject = Instantiate(definition.visualProperties.customPrefab, createPosition, Quaternion.identity);
            componentObject.name = definition.displayName;

            // Ensure it has a ThemedComponentDefinitionApplier
            // TEMPORARY COMMENT: CircuitSimulator.Components.ThemedComponentDefinitionApplier applier = componentObject.GetComponent<CircuitSimulator.Components.ThemedComponentDefinitionApplier>();
            // if (applier == null)
            // {
            //     applier = componentObject.AddComponent<CircuitSimulator.Components.ThemedComponentDefinitionApplier>();
            // }
            // applier.definition = definition;
        }
        else
        {
            // Create primitive and set up as defined component
            componentObject = CreatePrimitiveComponent(definition, createPosition);
        }

        // Setup transform
        componentObject.transform.position = createPosition;
        componentObject.transform.SetParent(canvasPlane);

        // Setup circuit functionality
        SetupDefinitionBasedComponent(componentObject, definition);

        // Setup interaction capabilities
        SetupComponentInteraction(componentObject);

        // Track component
        placedComponents.Add(componentObject);
        componentCount = placedComponents.Count;

        return componentObject;
    }

    private GameObject CreatePrimitiveComponent(ThemedComponentDefinition definition, Vector3 position)
    {
        // Create the primitive GameObject
        GameObject componentObject = GameObject.CreatePrimitive(definition.visualProperties.primitiveType);
        componentObject.name = definition.displayName;

        // Apply visual properties
        componentObject.transform.localScale = definition.visualProperties.scale;

        // Set material and color
        Renderer renderer = componentObject.GetComponent<Renderer>();
        if (renderer != null)
        {
            if (definition.visualProperties.defaultMaterial != null)
            {
                renderer.material = definition.visualProperties.defaultMaterial;
            }
            else
            {
                renderer.material.color = definition.visualProperties.defaultColor;
            }
        }

        // Add ThemedComponentDefinitionApplier for runtime management
        // TEMPORARY COMMENT: CircuitSimulator.Components.ThemedComponentDefinitionApplier applier = componentObject.AddComponent<CircuitSimulator.Components.ThemedComponentDefinitionApplier>();
        // applier.definition = definition;

        return componentObject;
    }

    private void SetupDefinitionBasedComponent(GameObject componentObject, ThemedComponentDefinition definition)
    {
        // Add CircuitComponent3D
        CircuitComponent3D circuitComp = componentObject.GetComponent<CircuitComponent3D>();
        if (circuitComp == null)
        {
            circuitComp = componentObject.AddComponent<CircuitComponent3D>();
        }

        // Convert ThemedBaseComponentType to ComponentType
        ComponentType componentType = ConvertThemedBaseType(definition.baseType);

        // Set component properties from definition
        circuitComp.ComponentType = componentType;
        circuitComp.voltage = definition.electricalProperties.defaultVoltage;
        circuitComp.resistance = definition.electricalProperties.defaultResistance;
        circuitComp.current = definition.electricalProperties.defaultCurrent;

    }

    /// <summary>
    /// Get ComponentDefinition by name or base type
    /// </summary>
    public CircuitSimulator.Components.ThemedComponentDefinition GetComponentDefinition(string definitionName)
    {
        if (componentDefinitions == null) return null;

        foreach (var definition in componentDefinitions)
        {
            if (definition != null && definition.displayName == definitionName)
            {
                return definition;
            }
        }

        return null;
    }

    /// <summary>
    /// Get all ComponentDefinitions of a specific base type
    /// </summary>
    public CircuitSimulator.Components.ThemedComponentDefinition[] GetComponentDefinitionsByType(CircuitSimulator.Components.ThemedBaseComponentType baseType)
    {
        if (componentDefinitions == null) return new CircuitSimulator.Components.ThemedComponentDefinition[0];

        List<CircuitSimulator.Components.ThemedComponentDefinition> matchingDefinitions = new List<CircuitSimulator.Components.ThemedComponentDefinition>();

        foreach (var definition in componentDefinitions)
        {
            if (definition != null && definition.baseType == baseType)
            {
                matchingDefinitions.Add(definition);
            }
        }

        return matchingDefinitions.ToArray();
    }

    #endregion

    #region Component Management
    
    public void RemoveComponent(GameObject component)
    {
        if (placedComponents.Contains(component))
        {
            placedComponents.Remove(component);
        }
    }
    
    public void ClearAllComponents()
    {
        foreach (var component in placedComponents)
        {
            if (component != null)
            {
                if (Application.isPlaying)
                    Destroy(component);
                else
                    DestroyImmediate(component);
            }
        }
        placedComponents.Clear();
        componentCount = 0;
    }
    
    public List<GameObject> GetPlacedComponents()
    {
        // Remove null references
        placedComponents.RemoveAll(item => item == null);
        return new List<GameObject>(placedComponents);
    }
    
    public int ComponentCount => placedComponents.Count;
    
    #endregion

    #region IComponentFactory Implementation

    // Component Creation with Vector3 position
    public CircuitComponent3D CreateComponent(ComponentType type, Vector3 position)
    {
        GameObject componentGO = null;
        Vector3 oldPosition = canvasPlane != null ? canvasPlane.position : Vector3.zero;

        // Temporarily set canvas position to match desired position
        if (canvasPlane != null)
            canvasPlane.position = position;

        try
        {
            switch (type)
            {
                case ComponentType.Battery:
                    componentGO = CreateBattery();
                    break;
                case ComponentType.Resistor:
                    componentGO = CreateResistor();
                    break;
                case ComponentType.Bulb:
                    componentGO = CreateBulb();
                    break;
                case ComponentType.Switch:
                    componentGO = CreateSwitch();
                    break;
                case ComponentType.Junction:
                    componentGO = CreateJunction();
                    break;
                default:
                    Debug.LogError($"Unsupported component type: {type}");
                    return null;
            }
        }
        finally
        {
            // Restore canvas position
            if (canvasPlane != null)
                canvasPlane.position = oldPosition;
        }

        if (componentGO != null)
        {
            // Set exact position
            componentGO.transform.position = position;
            return componentGO.GetComponent<CircuitComponent3D>();
        }

        return null;
    }

    // Component Creation with ComponentDefinition (Legacy)
    public CircuitComponent3D CreateComponent(ComponentDefinitionLegacy definition, Vector3 position)
    {
        if (definition == null)
        {
            Debug.LogError("ComponentDefinition is null");
            return null;
        }

        // Create base component based on definition type
        var component = CreateComponent(ConvertBaseType(definition.baseType), position);
        if (component == null) return null;

        // Apply definition properties
        ApplyComponentDefinition(component, definition);

        return component;
    }

    // Specific Component Factories with default positions
    public CircuitComponent3D CreateBattery(Vector3 position = default)
    {
        if (position == default) position = GetNextPlacementPosition();
        return CreateComponent(ComponentType.Battery, position);
    }

    public CircuitComponent3D CreateResistor(Vector3 position = default)
    {
        if (position == default) position = GetNextPlacementPosition();
        return CreateComponent(ComponentType.Resistor, position);
    }

    public CircuitComponent3D CreateBulb(Vector3 position = default)
    {
        if (position == default) position = GetNextPlacementPosition();
        return CreateComponent(ComponentType.Bulb, position);
    }

    public CircuitComponent3D CreateSwitch(Vector3 position = default)
    {
        if (position == default) position = GetNextPlacementPosition();
        return CreateComponent(ComponentType.Switch, position);
    }

    public CircuitComponent3D CreateJunction(Vector3 position = default)
    {
        if (position == default) position = GetNextPlacementPosition();
        return CreateComponent(ComponentType.Junction, position);
    }

    // Custom Components
    public CircuitComponent3D CreateCustomComponent(ComponentDefinitionLegacy definition, Vector3 position)
    {
        return CreateComponent(definition, position);
    }

    // Wire Creation
    public CircuitWire CreateWire(CircuitComponent3D startComponent, CircuitComponent3D endComponent)
    {
        if (startComponent == null || endComponent == null)
        {
            Debug.LogError("Cannot create wire: start or end component is null");
            return null;
        }

        // Create wire GameObject
        var wireGO = new GameObject($"Wire_{startComponent.name}_to_{endComponent.name}");
        var wire = wireGO.AddComponent<CircuitWire>();

        // Initialize wire
        wire.Initialize(startComponent, endComponent);

        // Fire interface event
        OnWireCreated?.Invoke(wire);

        return wire;
    }

    // Helper Methods
    private Vector3 GetNextPlacementPosition()
    {
        // Place components in front of camera at workspace level (Y=0.5)
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            mainCamera = FindFirstObjectByType<Camera>();
        }

        Vector3 spawnPosition;

        if (mainCamera != null)
        {
            // Get camera forward direction (projected onto XZ plane)
            Vector3 cameraForward = mainCamera.transform.forward;
            cameraForward.y = 0;
            cameraForward.Normalize();

            // Get camera position
            Vector3 cameraPos = mainCamera.transform.position;

            // Calculate spawn point: In front of camera, on workspace plane
            float distanceFromCamera = 8f; // Spawn 8 units in front of camera
            Vector3 centerPoint = cameraPos + (cameraForward * distanceFromCamera);
            centerPoint.y = 0.5f; // Always at workspace height

            // Spread components in a grid pattern
            int componentsPlaced = placedComponents.Count;
            int row = componentsPlaced / 3; // 3 components per row
            int col = componentsPlaced % 3;

            // Calculate offset from center
            Vector3 cameraRight = mainCamera.transform.right;
            cameraRight.y = 0;
            cameraRight.Normalize();

            Vector3 rowOffset = -cameraForward * (row * ComponentSpacing);
            Vector3 colOffset = cameraRight * ((col - 1) * ComponentSpacing); // -1, 0, +1 for centering

            spawnPosition = centerPoint + rowOffset + colOffset;
        }
        else
        {
            // Fallback: Use old grid system
            var basePosition = ComponentParent != null ? ComponentParent.position : Vector3.zero;
            spawnPosition = basePosition + new Vector3(placedComponents.Count * ComponentSpacing, 0.5f, 0);
        }

        return spawnPosition;
    }

    private ComponentType ConvertBaseType(CircuitSimulator.BaseComponentType baseType)
    {
        switch (baseType)
        {
            case CircuitSimulator.BaseComponentType.Battery: return ComponentType.Battery;
            case CircuitSimulator.BaseComponentType.Resistor: return ComponentType.Resistor;
            case CircuitSimulator.BaseComponentType.Bulb: return ComponentType.Bulb;
            case CircuitSimulator.BaseComponentType.Switch: return ComponentType.Switch;
            case CircuitSimulator.BaseComponentType.Junction: return ComponentType.Junction;
            default: return ComponentType.Resistor; // Default fallback
        }
    }

    private ComponentType ConvertThemedBaseType(ThemedBaseComponentType baseType)
    {
        switch (baseType)
        {
            case ThemedBaseComponentType.Battery: return ComponentType.Battery;
            case ThemedBaseComponentType.Resistor: return ComponentType.Resistor;
            case ThemedBaseComponentType.Bulb: return ComponentType.Bulb;
            case ThemedBaseComponentType.Switch: return ComponentType.Switch;
            case ThemedBaseComponentType.Junction: return ComponentType.Junction;
            default: return ComponentType.Resistor; // Default fallback
        }
    }

    private void ApplyComponentDefinition(CircuitComponent3D component, ComponentDefinitionLegacy definition)
    {
        if (component == null || definition == null) return;

        // Apply electrical properties
        var electrical = definition.electricalProperties;
        component.voltage = electrical.defaultVoltage;
        component.resistance = electrical.defaultResistance;
        component.current = electrical.defaultCurrent;

        // Apply visual properties
        var visual = definition.visualProperties;
        if (visual.customPrefab == null)
        {
            // Apply scale
            component.transform.localScale = visual.scale;

            // Apply material/color
            var renderer = component.GetComponent<Renderer>();
            if (renderer != null)
            {
                if (visual.defaultMaterial != null)
                {
                    renderer.material = visual.defaultMaterial;
                }
                else
                {
                    renderer.material.color = visual.defaultColor;
                }
            }
        }

        // Update display name
        component.name = definition.displayName;
        component.gameObject.name = definition.displayName;

    }

    #endregion

    #region Testing

    [ContextMenu("Test Simple Placement")]
    public void TestSimplePlacement()
    {
        // Test cube creation disabled - was creating unwanted white blocks
    }

    #endregion
}