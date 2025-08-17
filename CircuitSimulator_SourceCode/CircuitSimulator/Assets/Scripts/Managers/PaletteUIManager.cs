using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages the component palette UI buttons and interactions
/// Handles button creation and UI layout for component selection
/// </summary>
public class PaletteUIManager : MonoBehaviour
{
    [Header("UI References")]
    public Transform paletteContainer;
    public Button buttonPrefab;
    
    // Manager references
    private ComponentFactoryManager factoryManager;
    private CircuitControlManager controlManager;
    
    public void Initialize(ComponentFactoryManager factory, CircuitControlManager control)
    {
        factoryManager = factory;
        controlManager = control;
        
        CreatePaletteButtons();
        Debug.Log("PaletteUIManager initialized");
    }
    
    #region Button Creation
    
    private void CreatePaletteButtons()
    {
        if (buttonPrefab == null || paletteContainer == null)
        {
            Debug.LogWarning("Cannot create palette buttons - buttonPrefab or paletteContainer not assigned");
            return;
        }
        
        // Component buttons
        CreateButton("BATTERY", Color.red, () => factoryManager?.CreateBattery());
        CreateButton("RESISTOR", Color.yellow, () => factoryManager?.CreateResistor());
        CreateButton("BULB", Color.white, () => factoryManager?.CreateBulb());
        CreateButton("SWITCH", Color.gray, () => factoryManager?.CreateSwitch());
        CreateButton("WIRE TOOL", Color.blue, ActivateWireTool);
        
        // Control buttons
        CreateButton("SOLVE!", Color.green, () => controlManager?.SolveCircuit());
        CreateButton("VALIDATE", Color.cyan, () => controlManager?.ValidateCircuit());
        CreateButton("TEST", Color.cyan, () => controlManager?.TestCircuit());
        CreateButton("DEBUG", Color.magenta, () => controlManager?.DebugRegistration());
        CreateButton("REPORT", Color.yellow, () => controlManager?.SaveReport());
    }
    
    private void CreateButton(string label, Color color, System.Action onClick)
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
        
        Debug.Log($"Created palette button: {label}");
    }
    
    #endregion
    
    #region Component Actions
    
    public void PlaceBattery()
    {
        factoryManager?.CreateBattery();
    }
    
    public void PlaceResistor()
    {
        factoryManager?.CreateResistor();
    }
    
    public void PlaceBulb()
    {
        factoryManager?.CreateBulb();
    }
    
    public void PlaceSwitch()
    {
        factoryManager?.CreateSwitch();
    }
    
    private void ActivateWireTool()
    {
        Debug.Log("🔌 Wire Tool Activated - Click on two components to connect them");
        
        // Find or create ConnectTool
        ConnectTool connectTool = FindFirstObjectByType<ConnectTool>();
        if (connectTool == null)
        {
            GameObject connectToolObj = new GameObject("ConnectTool");
            connectTool = connectToolObj.AddComponent<ConnectTool>();
        }
        
        // Activate connect mode
        connectTool.SetConnectMode();
    }
    
    #endregion
    
    #region Keyboard Shortcuts
    
    void Update()
    {
        HandleKeyboardShortcuts();
    }
    
    private void HandleKeyboardShortcuts()
    {
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
            controlManager?.SolveCircuit();
        }
        if (Input.GetKeyDown(KeyCode.T))
        {
            Debug.Log("Testing Circuit (T key)");
            controlManager?.TestCircuit();
        }
    }
    
    #endregion
}