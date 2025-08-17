using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Modern UI Toolkit controller for the circuit simulator control panel
/// Handles all button interactions for component placement and circuit actions
/// </summary>
public class ControlPanelController : MonoBehaviour
{
    [Header("UI References")]
    public UIDocument uiDocument;
    
    [Header("Component References")]
    public ComponentPalette componentPalette;
    public ConnectTool connectTool;
    
    private VisualElement root;
    private Button batteryBtn, resistorBtn, bulbBtn, switchBtn, wireBtn;
    private Button solveBtn, validateBtn, testBtn, debugBtn, reportBtn;
    
    void Start()
    {
        // Get UI Document if not assigned
        if (uiDocument == null)
            uiDocument = GetComponent<UIDocument>();
        
        if (uiDocument == null)
        {
            Debug.LogError("ControlPanelController: No UIDocument found!");
            return;
        }
        
        // Get the root visual element
        root = uiDocument.rootVisualElement;
        
        // Find component palette if not assigned
        if (componentPalette == null)
            componentPalette = FindObjectOfType<ComponentPalette>();
            
        // Find connect tool if not assigned
        if (connectTool == null)
            connectTool = FindObjectOfType<ConnectTool>();
        
        SetupButtons();
    }
    
    void SetupButtons()
    {
        // Get component buttons
        batteryBtn = root.Q<Button>("battery-btn");
        resistorBtn = root.Q<Button>("resistor-btn");
        bulbBtn = root.Q<Button>("bulb-btn");
        switchBtn = root.Q<Button>("switch-btn");
        wireBtn = root.Q<Button>("wire-btn");
        
        // Get action buttons
        solveBtn = root.Q<Button>("solve-btn");
        validateBtn = root.Q<Button>("validate-btn");
        testBtn = root.Q<Button>("test-btn");
        debugBtn = root.Q<Button>("debug-btn");
        reportBtn = root.Q<Button>("report-btn");
        
        // Setup component button events
        if (batteryBtn != null)
            batteryBtn.clicked += () => PlaceComponent("Battery");
        if (resistorBtn != null)
            resistorBtn.clicked += () => PlaceComponent("Resistor");
        if (bulbBtn != null)
            bulbBtn.clicked += () => PlaceComponent("Bulb");
        if (switchBtn != null)
            switchBtn.clicked += () => PlaceComponent("Switch");
        if (wireBtn != null)
            wireBtn.clicked += ActivateWireTool;
        
        // Setup action button events
        if (solveBtn != null)
            solveBtn.clicked += SolveCircuit;
        if (validateBtn != null)
            validateBtn.clicked += ValidateCircuit;
        if (testBtn != null)
            testBtn.clicked += TestCircuit;
        if (debugBtn != null)
            debugBtn.clicked += DebugCircuit;
        if (reportBtn != null)
            reportBtn.clicked += GenerateReport;
        
        Debug.Log("ControlPanelController: All buttons connected successfully!");
    }
    
    // Component Placement Methods
    void PlaceComponent(string componentType)
    {
        if (componentPalette == null)
        {
            Debug.LogWarning("ComponentPalette not found!");
            return;
        }
        
        Debug.Log($"🔧 Placing {componentType}");
        
        switch (componentType)
        {
            case "Battery":
                componentPalette.PlaceBattery();
                break;
            case "Resistor":
                componentPalette.PlaceResistor();
                break;
            case "Bulb":
                componentPalette.PlaceBulb();
                break;
            case "Switch":
                componentPalette.PlaceSwitch();
                break;
        }
        
        // Visual feedback
        ShowButtonFeedback(GetButtonForComponent(componentType));
    }
    
    void ActivateWireTool()
    {
        Debug.Log("🔌 Wire Tool Activated");
        
        if (connectTool == null)
        {
            // Create ConnectTool if it doesn't exist
            GameObject connectToolObj = new GameObject("ConnectTool");
            connectTool = connectToolObj.AddComponent<ConnectTool>();
        }
        
        connectTool.SetConnectMode();
        ShowButtonFeedback(wireBtn);
    }
    
    // Action Methods
    void SolveCircuit()
    {
        Debug.Log("✅ Solving Circuit");
        var manager = Circuit3DManager.Instance;
        if (manager != null)
        {
            manager.SolveCircuit();
            ShowButtonFeedback(solveBtn);
        }
        else
        {
            Debug.LogWarning("Circuit3DManager not found!");
        }
    }
    
    void ValidateCircuit()
    {
        Debug.Log("🔍 Validating Circuit");
        if (componentPalette != null)
        {
            // Use ComponentPalette's validation method
            var manager = componentPalette.GetManager();
            if (manager != null)
            {
                manager.SolveCircuit(); // This includes validation
                ShowButtonFeedback(validateBtn);
            }
        }
    }
    
    void TestCircuit()
    {
        Debug.Log("🧪 Testing Circuit");
        if (componentPalette != null)
        {
            var manager = componentPalette.GetManager();
            if (manager != null)
            {
                manager.ValidateAndTestCircuit();
                ShowButtonFeedback(testBtn);
            }
        }
    }
    
    void DebugCircuit()
    {
        Debug.Log("🐛 Debug Circuit Registration");
        if (componentPalette != null)
        {
            var manager = componentPalette.GetManager();
            if (manager != null)
            {
                manager.DebugComponentRegistration();
                ShowButtonFeedback(debugBtn);
            }
        }
    }
    
    void GenerateReport()
    {
        Debug.Log("📊 Generating Circuit Report");
        if (componentPalette != null)
        {
            var manager = componentPalette.GetManager();
            if (manager != null)
            {
                manager.SaveDebugReport();
                ShowButtonFeedback(reportBtn);
            }
        }
    }
    
    // Helper Methods
    Button GetButtonForComponent(string componentType)
    {
        switch (componentType)
        {
            case "Battery": return batteryBtn;
            case "Resistor": return resistorBtn;
            case "Bulb": return bulbBtn;
            case "Switch": return switchBtn;
            default: return null;
        }
    }
    
    void ShowButtonFeedback(Button button)
    {
        if (button != null)
        {
            // Add visual feedback class temporarily
            button.AddToClassList("button-pressed");
            
            // Remove feedback after short delay
            StartCoroutine(RemoveFeedbackAfterDelay(button));
        }
    }
    
    System.Collections.IEnumerator RemoveFeedbackAfterDelay(Button button)
    {
        yield return new WaitForSeconds(0.2f);
        button.RemoveFromClassList("button-pressed");
    }
}