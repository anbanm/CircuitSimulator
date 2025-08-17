using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Plane-based UI system designed for both desktop and AR
// All UI elements are positioned on a 3D plane for consistent AR/desktop experience
public class CircuitWorkspaceUI : MonoBehaviour
{
    [Header("Workspace Setup")]
    public Transform workspacePlane; // The main plane where circuits are built
    public float workspaceSize = 10f; // Size of the workspace area
    
    [Header("UI Panel Positions (relative to workspace)")]
    public Vector3 toolPanelOffset = new Vector3(-6f, 0.1f, 0f);
    public Vector3 infoPanelOffset = new Vector3(6f, 0.1f, 0f);
    public Vector3 controlPanelOffset = new Vector3(0f, 0.1f, -6f);
    
    [Header("UI Prefabs")]
    public GameObject toolButtonPrefab;
    public GameObject infoPanelPrefab;
    public GameObject sliderPrefab;
    
    [Header("AR Optimization")]
    public bool optimizeForAR = true;
    public float arUIScale = 0.7f;
    public float maxViewDistance = 5f; // Max distance for AR visibility
    
    private GameObject toolPanel;
    private GameObject infoPanel;
    private GameObject controlPanel;
    private Camera playerCamera;
    
    void Start()
    {
        playerCamera = Camera.main;
        SetupWorkspace();
        CreateToolPanel();
        CreateInfoPanel();
        CreateControlPanel();
    }
    
    void SetupWorkspace()
    {
        if (workspacePlane == null)
        {
            // Create workspace plane if not assigned
            GameObject plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            plane.name = "CircuitWorkspace";
            plane.transform.localScale = Vector3.one * workspaceSize;
            workspacePlane = plane.transform;
            
            // Make it visually appealing for AR
            var renderer = plane.GetComponent<Renderer>();
            var material = new Material(Shader.Find("Standard"));
            material.color = new Color(0.9f, 0.9f, 1f, 0.3f); // Light blue, semi-transparent
            material.SetFloat("_Mode", 3); // Transparent mode
            renderer.material = material;
        }
        
        // Set workspace as reference point for AR tracking
        if (optimizeForAR)
        {
            // Add AR anchor component here when implementing AR
            Debug.Log("Workspace ready for AR tracking");
        }
    }
    
    void CreateToolPanel()
    {
        // Create floating tool panel next to workspace
        toolPanel = new GameObject("ToolPanel");
        toolPanel.transform.SetParent(workspacePlane);
        toolPanel.transform.localPosition = toolPanelOffset;
        toolPanel.transform.localRotation = Quaternion.identity;
        
        // Create background for tool panel
        var background = CreatePanelBackground(toolPanel.transform, new Vector2(2f, 4f), "Tool Panel");
        
        // Add component buttons
        CreateComponentButton("Battery", ComponentType.Battery, 0);
        CreateComponentButton("Resistor", ComponentType.Resistor, 1);
        CreateComponentButton("Bulb", ComponentType.Bulb, 2);
        CreateComponentButton("Switch", ComponentType.Switch, 3);
        CreateComponentButton("Wire Tool", ComponentType.Wire, 4);
        
        // Add utility buttons
        CreateUtilityButton("Clear All", 5, ClearAllComponents);
        CreateUtilityButton("Test Circuit", 6, TestCurrentCircuit);
    }
    
    void CreateInfoPanel()
    {
        // Create floating info panel for circuit measurements
        infoPanel = new GameObject("InfoPanel");
        infoPanel.transform.SetParent(workspacePlane);
        infoPanel.transform.localPosition = infoPanelOffset;
        infoPanel.transform.localRotation = Quaternion.identity;
        
        var background = CreatePanelBackground(infoPanel.transform, new Vector2(2.5f, 3f), "Circuit Info");
        
        // Create measurement displays
        CreateMeasurementDisplay("Total Current:", "0.00 A", 0);
        CreateMeasurementDisplay("Total Voltage:", "0.00 V", 1);
        CreateMeasurementDisplay("Total Power:", "0.00 W", 2);
        CreateMeasurementDisplay("Components:", "0", 3);
        
        // Add validation status display
        CreateValidationDisplay(4);
    }
    
    void CreateControlPanel()
    {
        // Create control panel for circuit simulation controls
        controlPanel = new GameObject("ControlPanel");
        controlPanel.transform.SetParent(workspacePlane);
        controlPanel.transform.localPosition = controlPanelOffset;
        controlPanel.transform.localRotation = Quaternion.identity;
        
        var background = CreatePanelBackground(controlPanel.transform, new Vector2(4f, 1.5f), "Simulation Controls");
        
        // Add simulation controls
        CreateControlButton("▶ Solve", 0, SolveCircuit);
        CreateControlButton("⏸ Pause", 1, PauseSimulation);
        CreateControlButton("🔄 Reset", 2, ResetCircuit);
        CreateControlButton("📊 Debug", 3, ToggleDebugView);
    }
    
    GameObject CreatePanelBackground(Transform parent, Vector2 size, string title)
    {
        GameObject panel = new GameObject("PanelBackground");
        panel.transform.SetParent(parent);
        panel.transform.localPosition = Vector3.zero;
        panel.transform.localRotation = Quaternion.Euler(90, 0, 0); // Face up for AR
        
        // Create visual background
        var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.transform.SetParent(panel.transform);
        quad.transform.localPosition = Vector3.zero;
        quad.transform.localScale = new Vector3(size.x, size.y, 1);
        
        // Style for AR visibility
        var material = new Material(Shader.Find("Standard"));
        material.color = new Color(0.2f, 0.2f, 0.3f, 0.8f);
        material.SetFloat("_Mode", 3); // Transparent
        quad.GetComponent<Renderer>().material = material;
        
        // Add title text
        CreateUIText(panel.transform, title, new Vector3(0, size.y/2 - 0.2f, -0.01f), 0.3f);
        
        // Scale for AR if needed
        if (optimizeForAR)
        {
            panel.transform.localScale = Vector3.one * arUIScale;
        }
        
        return panel;
    }
    
    void CreateComponentButton(string label, ComponentType componentType, int index)
    {
        float buttonSpacing = 0.5f;
        Vector3 position = new Vector3(0, 1.5f - index * buttonSpacing, -0.01f);
        
        var buttonObj = CreateUIButton(toolPanel.transform, label, position, () => SelectComponent(componentType));
        
        // Add component icon/color coding for AR clarity
        var renderer = buttonObj.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = GetComponentColor(componentType);
        }
    }
    
    void CreateUtilityButton(string label, int index, System.Action action)
    {
        float buttonSpacing = 0.5f;
        Vector3 position = new Vector3(0, 1.5f - index * buttonSpacing, -0.01f);
        
        CreateUIButton(toolPanel.transform, label, position, action);
    }
    
    void CreateControlButton(string label, int index, System.Action action)
    {
        float buttonSpacing = 0.8f;
        Vector3 position = new Vector3(-1.5f + index * buttonSpacing, 0, -0.01f);
        
        CreateUIButton(controlPanel.transform, label, position, action);
    }
    
    GameObject CreateUIButton(Transform parent, string label, Vector3 localPos, System.Action onClick)
    {
        GameObject button = GameObject.CreatePrimitive(PrimitiveType.Cube);
        button.name = $"Button_{label}";
        button.transform.SetParent(parent);
        button.transform.localPosition = localPos;
        button.transform.localScale = new Vector3(0.8f, 0.3f, 0.1f);
        
        // Make button interactive
        var collider = button.GetComponent<BoxCollider>();
        collider.isTrigger = true;
        
        // Add button behavior
        var buttonScript = button.AddComponent<UIButton3D>();
        buttonScript.Initialize(onClick);
        
        // Add text label
        CreateUIText(button.transform, label, new Vector3(0, 0, -0.06f), 0.2f);
        
        return button;
    }
    
    void CreateMeasurementDisplay(string label, string value, int index)
    {
        float displaySpacing = 0.4f;
        Vector3 position = new Vector3(0, 1.2f - index * displaySpacing, -0.01f);
        
        // Create label
        CreateUIText(infoPanel.transform, label, position + Vector3.left * 0.5f, 0.15f);
        
        // Create value display
        var valueText = CreateUIText(infoPanel.transform, value, position + Vector3.right * 0.3f, 0.15f);
        valueText.name = $"Value_{label}";
        
        // Store reference for updates
        var display = infoPanel.AddComponent<MeasurementDisplay>();
        display.Initialize(label, valueText.GetComponent<TextMeshPro>());
    }
    
    void CreateValidationDisplay(int index)
    {
        float displaySpacing = 0.4f;
        Vector3 position = new Vector3(0, 1.2f - index * displaySpacing, -0.01f);
        
        var statusText = CreateUIText(infoPanel.transform, "✅ Circuit Valid", position, 0.15f);
        statusText.name = "ValidationStatus";
        statusText.GetComponent<TextMeshPro>().color = Color.green;
    }
    
    GameObject CreateUIText(Transform parent, string text, Vector3 localPos, float size)
    {
        GameObject textObj = new GameObject($"Text_{text}");
        textObj.transform.SetParent(parent);
        textObj.transform.localPosition = localPos;
        textObj.transform.localRotation = Quaternion.identity;
        
        var textMesh = textObj.AddComponent<TextMeshPro>();
        textMesh.text = text;
        textMesh.fontSize = size * (optimizeForAR ? arUIScale : 1f);
        textMesh.color = Color.white;
        textMesh.alignment = TextAlignmentOptions.Center;
        
        // Make text readable in AR
        if (optimizeForAR)
        {
            textMesh.fontStyle = FontStyles.Bold;
        }
        
        return textObj;
    }
    
    Color GetComponentColor(ComponentType componentType)
    {
        switch (componentType)
        {
            case ComponentType.Battery: return Color.red;
            case ComponentType.Resistor: return Color.blue;
            case ComponentType.Bulb: return Color.yellow;
            case ComponentType.Switch: return Color.green;
            case ComponentType.Wire: return Color.gray;
            default: return Color.white;
        }
    }
    
    // UI Action Methods
    void SelectComponent(ComponentType componentType)
    {
        Debug.Log($"Selected component: {componentType}");
        
        if (componentType == ComponentType.Wire)
        {
            // Activate wire connection mode
            ActivateWireTool();
        }
        else
        {
            // Place component
            var palette = FindObjectOfType<ComponentPalette>();
            if (palette != null)
            {
                PlaceComponentType(componentType);
            }
        }
        
        // Provide visual feedback
        ShowSelectionFeedback(componentType);
    }
    
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
    
    void PlaceComponentType(ComponentType componentType)
    {
        var palette = FindObjectOfType<ComponentPalette>();
        if (palette != null)
        {
            switch (componentType)
            {
                case ComponentType.Battery:
                    palette.PlaceBattery();
                    break;
                case ComponentType.Resistor:
                    palette.PlaceResistor();
                    break;
                case ComponentType.Bulb:
                    palette.PlaceBulb();
                    break;
                case ComponentType.Switch:
                    palette.PlaceSwitch();
                    break;
            }
        }
    }
    
    void ShowSelectionFeedback(ComponentType componentType)
    {
        // Create temporary visual feedback for AR
        var feedbackObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        feedbackObj.transform.position = toolPanel.transform.position + Vector3.up * 0.5f;
        feedbackObj.transform.localScale = Vector3.one * 0.2f;
        feedbackObj.GetComponent<Renderer>().material.color = GetComponentColor(componentType);
        
        Destroy(feedbackObj, 1f);
    }
    
    void ClearAllComponents()
    {
        Debug.Log("Clearing all components");
        
        var components = FindObjectsOfType<CircuitComponent3D>();
        foreach (var comp in components)
        {
            DestroyImmediate(comp.gameObject);
        }
        
        var wires = FindObjectsOfType<CircuitWire>();
        foreach (var wire in wires)
        {
            DestroyImmediate(wire.gameObject);
        }
    }
    
    void TestCurrentCircuit()
    {
        Debug.Log("Testing current circuit");
        
        var manager = Circuit3DManager.Instance;
        if (manager != null)
        {
            manager.SolveCircuit();
        }
    }
    
    void SolveCircuit()
    {
        var manager = Circuit3DManager.Instance;
        if (manager != null)
        {
            manager.SolveCircuit();
        }
    }
    
    void PauseSimulation()
    {
        Debug.Log("Pausing simulation");
        // Implement simulation pause logic
    }
    
    void ResetCircuit()
    {
        Debug.Log("Resetting circuit");
        ClearAllComponents();
    }
    
    void ToggleDebugView()
    {
        var debugVisualizer = FindObjectOfType<CircuitDebugVisualizer>();
        if (debugVisualizer != null)
        {
            debugVisualizer.ToggleAllDebugVisuals();
        }
    }
    
    // Update UI with current circuit state
    void Update()
    {
        if (optimizeForAR)
        {
            UpdateUIForAR();
        }
        
        UpdateMeasurementDisplays();
    }
    
    void UpdateUIForAR()
    {
        // Keep UI panels facing the player camera for AR
        if (playerCamera != null)
        {
            Vector3 cameraPosition = playerCamera.transform.position;
            
            // Only show UI if camera is within reasonable distance
            float distance = Vector3.Distance(cameraPosition, workspacePlane.position);
            bool shouldShowUI = distance <= maxViewDistance;
            
            toolPanel.SetActive(shouldShowUI);
            infoPanel.SetActive(shouldShowUI);
            controlPanel.SetActive(shouldShowUI);
            
            if (shouldShowUI)
            {
                // Make panels face camera (useful for AR)
                Vector3 lookDirection = (cameraPosition - workspacePlane.position).normalized;
                lookDirection.y = 0; // Keep panels flat on the plane
                
                if (lookDirection.magnitude > 0.1f)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(lookDirection, Vector3.up);
                    
                    toolPanel.transform.rotation = targetRotation;
                    infoPanel.transform.rotation = targetRotation;
                    controlPanel.transform.rotation = targetRotation;
                }
            }
        }
    }
    
    void UpdateMeasurementDisplays()
    {
        var displays = FindObjectsOfType<MeasurementDisplay>();
        var manager = Circuit3DManager.Instance;
        
        if (manager == null) return;
        
        var components = FindObjectsOfType<CircuitComponent3D>();
        
        float totalCurrent = 0f;
        float totalVoltage = 0f;
        float totalPower = 0f;
        
        foreach (var comp in components)
        {
            if (comp.ComponentType == ComponentType.Battery)
            {
                totalVoltage += comp.voltage;
                totalCurrent = Mathf.Max(totalCurrent, comp.current);
            }
            
            totalPower += comp.current * comp.voltageDrop;
        }
        
        // Update measurement displays
        foreach (var display in displays)
        {
            switch (display.Label)
            {
                case "Total Current:":
                    display.UpdateValue($"{totalCurrent:F2} A");
                    break;
                case "Total Voltage:":
                    display.UpdateValue($"{totalVoltage:F2} V");
                    break;
                case "Total Power:":
                    display.UpdateValue($"{totalPower:F2} W");
                    break;
                case "Components:":
                    display.UpdateValue($"{components.Length}");
                    break;
            }
        }
    }
}

// Supporting classes for UI functionality
public class UIButton3D : MonoBehaviour
{
    private System.Action onClick;
    private Renderer buttonRenderer;
    private Color originalColor;
    
    public void Initialize(System.Action onClickAction)
    {
        onClick = onClickAction;
        buttonRenderer = GetComponent<Renderer>();
        originalColor = buttonRenderer.material.color;
    }
    
    void OnMouseDown()
    {
        onClick?.Invoke();
        
        // Visual feedback
        StartCoroutine(ButtonPressEffect());
    }
    
    System.Collections.IEnumerator ButtonPressEffect()
    {
        buttonRenderer.material.color = Color.yellow;
        yield return new WaitForSeconds(0.1f);
        buttonRenderer.material.color = originalColor;
    }
}

public class MeasurementDisplay : MonoBehaviour
{
    public string Label { get; private set; }
    private TextMeshPro valueText;
    
    public void Initialize(string label, TextMeshPro textComponent)
    {
        Label = label;
        valueText = textComponent;
    }
    
    public void UpdateValue(string newValue)
    {
        if (valueText != null)
        {
            valueText.text = newValue;
        }
    }
}