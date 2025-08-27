using UnityEngine;
using System;

/// <summary>
/// Handles UI layout and panel positioning for the workspace
/// Manages tool panels, info panels, and control panels
/// </summary>
public class UILayoutManager : MonoBehaviour
{
    [Header("UI Panel Positions (relative to workspace)")]
    public Vector3 toolPanelOffset = new Vector3(-6f, 0.1f, 0f);
    public Vector3 infoPanelOffset = new Vector3(6f, 0.1f, 0f);
    public Vector3 controlPanelOffset = new Vector3(0f, 0.1f, -6f);
    
    [Header("UI Prefabs")]
    public GameObject toolButtonPrefab;
    public GameObject infoPanelPrefab;
    public GameObject sliderPrefab;
    
    // Panel references
    private GameObject toolPanel;
    private GameObject infoPanel;
    private GameObject controlPanel;
    
    // Manager reference
    private WorkspaceManager workspaceManager;
    
    public void Initialize(WorkspaceManager workspace)
    {
        workspaceManager = workspace;
        CreateAllPanels();
    }
    
    private void CreateAllPanels()
    {
        // DISABLED - White blocks removed, using PaletteUIManager instead
        // CreateToolPanel();
        // CreateInfoPanel();
        // CreateControlPanel();
        Debug.Log("UILayoutManager panels disabled - using PaletteUIManager for clean UI");
    }
    
    #region Tool Panel
    
    private void CreateToolPanel()
    {
        toolPanel = new GameObject("ToolPanel");
        
        if (workspaceManager?.WorkspacePlane != null)
        {
            toolPanel.transform.SetParent(workspaceManager.WorkspacePlane);
            toolPanel.transform.localPosition = toolPanelOffset;
            toolPanel.transform.localRotation = Quaternion.identity;
        }
        
        // Create component buttons
        CreateComponentButton("🔋 BATTERY", ComponentType.Battery, 0);
        CreateComponentButton("⚡ RESISTOR", ComponentType.Resistor, 1);
        CreateComponentButton("💡 BULB", ComponentType.Bulb, 2);
        CreateComponentButton("🔘 SWITCH", ComponentType.Switch, 3);
        CreateComponentButton("🔌 WIRE", ComponentType.Wire, 4);
        
        Debug.Log("Tool panel created");
    }
    
    private void CreateComponentButton(string label, ComponentType componentType, int index)
    {
        Vector3 buttonPos = new Vector3(0, 0, index * -1.5f);
        
        GameObject button = CreateButton(label, buttonPos, toolPanel.transform);
        
        // Add click handler
        var uiButton = button.AddComponent<UIButton3D>();
        uiButton.OnClick = () => SelectComponent(componentType);
        
        Debug.Log($"Created component button: {label}");
    }
    
    #endregion
    
    #region Info Panel
    
    private void CreateInfoPanel()
    {
        infoPanel = new GameObject("InfoPanel");
        
        if (workspaceManager?.WorkspacePlane != null)
        {
            infoPanel.transform.SetParent(workspaceManager.WorkspacePlane);
            infoPanel.transform.localPosition = infoPanelOffset;
            infoPanel.transform.localRotation = Quaternion.identity;
        }
        
        // Create measurement displays
        CreateMeasurementDisplay("Voltage", "0.0V", 0);
        CreateMeasurementDisplay("Current", "0.0A", 1);
        CreateMeasurementDisplay("Power", "0.0W", 2);
        CreateMeasurementDisplay("Resistance", "0.0Ω", 3);
        
        // Create validation display
        CreateValidationDisplay(4);
        
        Debug.Log("Info panel created");
    }
    
    private void CreateMeasurementDisplay(string label, string value, int index)
    {
        Vector3 displayPos = new Vector3(0, 0, index * -1.2f);
        
        // Create label
        GameObject labelObj = CreateText($"{label}:", displayPos, infoPanel.transform, 0.8f);
        
        // Create value text
        GameObject valueObj = CreateText(value, displayPos + Vector3.right * 2f, infoPanel.transform, 1.0f);
        
        // Create measurement display component
        var display = labelObj.AddComponent<MeasurementDisplay>();
        display.Initialize(label, valueObj.GetComponent<TMPro.TextMeshPro>());
        
        Debug.Log($"Created measurement display: {label}");
    }
    
    private void CreateValidationDisplay(int index)
    {
        Vector3 statusPos = new Vector3(0, 0, index * -1.2f);
        
        GameObject statusText = CreateText("Circuit Status: Ready", statusPos, infoPanel.transform, 0.9f);
        statusText.GetComponent<TMPro.TextMeshPro>().color = Color.green;
        
        Debug.Log("Created validation display");
    }
    
    #endregion
    
    #region Control Panel
    
    private void CreateControlPanel()
    {
        controlPanel = new GameObject("ControlPanel");
        
        if (workspaceManager?.WorkspacePlane != null)
        {
            controlPanel.transform.SetParent(workspaceManager.WorkspacePlane);
            controlPanel.transform.localPosition = controlPanelOffset;
            controlPanel.transform.localRotation = Quaternion.identity;
        }
        
        // Create action buttons
        CreateControlButton("✅ SOLVE", 0, () => SolveCircuit());
        CreateControlButton("🔍 VALIDATE", 1, () => ValidateCircuit());
        CreateControlButton("🧪 TEST", 2, () => TestCircuit());
        CreateControlButton("🗑️ CLEAR", 3, () => ClearAll());
        CreateControlButton("📊 REPORT", 4, () => GenerateReport());
        
        Debug.Log("Control panel created");
    }
    
    private void CreateControlButton(string label, int index, System.Action action)
    {
        Vector3 buttonPos = new Vector3(index * 2f, 0, 0);
        
        GameObject button = CreateButton(label, buttonPos, controlPanel.transform);
        
        // Add click handler
        var uiButton = button.AddComponent<UIButton3D>();
        uiButton.OnClick = action;
        
        Debug.Log($"Created control button: {label}");
    }
    
    #endregion
    
    #region UI Creation Utilities
    
    private GameObject CreateButton(string text, Vector3 localPos, Transform parent)
    {
        GameObject buttonObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        buttonObj.name = $"Button_{text}";
        buttonObj.transform.SetParent(parent);
        buttonObj.transform.localPosition = localPos;
        buttonObj.transform.localScale = Vector3.one * (workspaceManager?.IsARMode == true ? workspaceManager.ARScale : 1f);
        
        // Add text
        CreateText(text, Vector3.up * 0.6f, buttonObj.transform, 0.5f);
        
        return buttonObj;
    }
    
    private GameObject CreateText(string text, Vector3 localPos, Transform parent, float size = 1f)
    {
        GameObject textObj = new GameObject($"Text_{text}");
        textObj.transform.SetParent(parent);
        textObj.transform.localPosition = localPos;
        textObj.transform.localRotation = Quaternion.identity;
        
        var textMesh = textObj.AddComponent<TMPro.TextMeshPro>();
        textMesh.text = text;
        textMesh.fontSize = size * (workspaceManager?.IsARMode == true ? workspaceManager.ARScale : 1f);
        textMesh.color = Color.white;
        textMesh.alignment = TMPro.TextAlignmentOptions.Center;
        
        // Make text readable in AR
        if (workspaceManager?.IsARMode == true)
        {
            textMesh.fontSize *= 2f; // Larger text for AR readability
        }
        
        return textObj;
    }
    
    #endregion
    
    #region Button Actions
    
    private void SelectComponent(ComponentType componentType)
    {
        Debug.Log($"Selected component: {componentType}");
        
        if (componentType == ComponentType.Wire)
        {
            ActivateWireTool();
        }
        else
        {
            var factoryManager = FindFirstObjectByType<ComponentFactoryManager>();
            if (factoryManager != null)
            {
                PlaceComponentType(componentType, factoryManager);
            }
        }
    }
    
    private void ActivateWireTool()
    {
        Debug.Log("🔌 Wire Tool Activated");
        
        var connectTool = FindFirstObjectByType<ConnectTool>();
        if (connectTool == null)
        {
            GameObject connectToolObj = new GameObject("ConnectTool");
            connectTool = connectToolObj.AddComponent<ConnectTool>();
        }
        
        connectTool.SetConnectMode();
    }
    
    private void PlaceComponentType(ComponentType componentType, ComponentFactoryManager factoryManager)
    {
        switch (componentType)
        {
            case ComponentType.Battery:
                factoryManager.CreateBattery();
                break;
            case ComponentType.Resistor:
                factoryManager.CreateResistor();
                break;
            case ComponentType.Bulb:
                factoryManager.CreateBulb();
                break;
            case ComponentType.Switch:
                factoryManager.CreateSwitch();
                break;
        }
    }
    
    private void SolveCircuit()
    {
        var circuitManager = CircuitManager.Instance;
        circuitManager?.SolveCircuit();
    }
    
    private void ValidateCircuit()
    {
        workspaceManager?.ValidateWorkspace();
    }
    
    private void TestCircuit()
    {
        var circuitManager = CircuitManager.Instance;
        circuitManager?.TestCircuitComponents();
    }
    
    private void ClearAll()
    {
        workspaceManager?.ClearWorkspace();
    }
    
    private void GenerateReport()
    {
        var circuitManager = CircuitManager.Instance;
        circuitManager?.SaveDebugReport();
    }
    
    #endregion
}