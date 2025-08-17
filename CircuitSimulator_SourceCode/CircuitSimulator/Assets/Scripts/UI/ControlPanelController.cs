using UnityEngine;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;

/// <summary>
/// Modern UI Toolkit controller for the circuit simulator control panel
/// Handles all button interactions for component placement and circuit actions
/// </summary>
public class ControlPanelController : MonoBehaviour
{
    [Header("UI References")]
    public UIDocument uiDocument;
    
    [Header("Component References")]
    public ComponentPaletteCoordinator paletteCoordinator;
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
        
        // Find component palette coordinator if not assigned
        if (paletteCoordinator == null)
            paletteCoordinator = FindFirstObjectByType<ComponentPaletteCoordinator>();
            
        // Find connect tool if not assigned
        if (connectTool == null)
            connectTool = FindFirstObjectByType<ConnectTool>();
        
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
        if (paletteCoordinator == null)
        {
            Debug.LogWarning("ComponentPaletteCoordinator not found!");
            return;
        }
        
        Debug.Log($"🔧 Placing {componentType}");
        
        switch (componentType)
        {
            case "Battery":
                paletteCoordinator.PlaceBattery();
                break;
            case "Resistor":
                paletteCoordinator.PlaceResistor();
                break;
            case "Bulb":
                paletteCoordinator.PlaceBulb();
                break;
            case "Switch":
                paletteCoordinator.PlaceSwitch();
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
        var circuitManager = CircuitManager.Instance;
        if (circuitManager != null)
        {
            circuitManager.SolveCircuit();
            ShowButtonFeedback(solveBtn);
        }
        else
        {
            Debug.LogWarning("CircuitManager not found!");
        }
    }
    
    void ValidateCircuit()
    {
        Debug.Log("🔍 Validating Circuit");
        var circuitManager = CircuitManager.Instance;
        if (circuitManager != null)
        {
            circuitManager.ValidateAndTestCircuit();
            ShowButtonFeedback(validateBtn);
        }
        else
        {
            Debug.LogWarning("CircuitManager not found!");
        }
    }
    
    void TestCircuit()
    {
        Debug.Log("🧪 Testing Circuit");
        var circuitManager = CircuitManager.Instance;
        if (circuitManager != null)
        {
            circuitManager.TestCircuitComponents();
            ShowButtonFeedback(testBtn);
        }
        else
        {
            Debug.LogWarning("CircuitManager not found!");
        }
    }
    
    void DebugCircuit()
    {
        Debug.Log("🐛 Debug Circuit Registration");
        var circuitManager = CircuitManager.Instance;
        if (circuitManager != null)
        {
            circuitManager.DebugComponentRegistration();
            ShowButtonFeedback(debugBtn);
        }
        else
        {
            Debug.LogWarning("CircuitManager not found!");
        }
    }
    
    void GenerateReport()
    {
        Debug.Log("📊 Generating Circuit Report");
        var circuitManager = CircuitManager.Instance;
        if (circuitManager != null)
        {
            circuitManager.SaveDebugReport();
            ShowButtonFeedback(reportBtn);
        }
        else
        {
            Debug.LogWarning("CircuitManager not found!");
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