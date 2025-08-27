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
        
        // Ensure ConnectTool always exists for C/V key shortcuts
        EnsureConnectToolExists();
        
        CreatePaletteButtons();
        Debug.Log("PaletteUIManager initialized");
    }
    
    private void EnsureConnectToolExists()
    {
        ConnectTool connectTool = FindFirstObjectByType<ConnectTool>();
        if (connectTool == null)
        {
            GameObject connectToolObj = new GameObject("ConnectTool");
            connectTool = connectToolObj.AddComponent<ConnectTool>();
            Debug.Log("ConnectTool created for keyboard shortcuts");
        }
    }
    
    #region Button Creation
    
    private void CreatePaletteButtons()
    {
        if (buttonPrefab == null || paletteContainer == null)
        {
            Debug.LogWarning("Cannot create palette buttons - buttonPrefab or paletteContainer not assigned");
            return;
        }
        
        // Professional color scheme
        Color modeColor = new Color(0.2f, 0.6f, 0.9f, 1f);      // Professional blue
        Color componentColor = new Color(0.3f, 0.3f, 0.4f, 1f);  // Dark gray
        Color actionColor = new Color(0.2f, 0.7f, 0.3f, 1f);     // Professional green
        Color utilityColor = new Color(0.5f, 0.5f, 0.6f, 1f);    // Medium gray
        
        // Mode buttons (at the top) - Simple text, no unicode
        CreateButton("Select", modeColor, ActivateSelectMode, "Click to select and move components");
        CreateButton("Connect", modeColor, ActivateConnectMode, "Click two components to connect with wire");
        
        // Component buttons with simple names
        CreateButton("Battery", new Color(0.8f, 0.2f, 0.2f, 1f), () => factoryManager?.CreateBattery(), "Add power source (12V)");
        CreateButton("Resistor", new Color(0.8f, 0.6f, 0.2f, 1f), () => factoryManager?.CreateResistor(), "Add resistance (10Ω)");
        CreateButton("Bulb", new Color(0.9f, 0.9f, 0.3f, 1f), () => factoryManager?.CreateBulb(), "Add light bulb (5Ω)");
        CreateButton("Switch", componentColor, () => factoryManager?.CreateSwitch(), "Add on/off switch");
        CreateButton("Junction", new Color(0.5f, 0.5f, 0.7f, 1f), () => factoryManager?.CreateJunction(), "Add junction for parallel circuits");
        
        // Control buttons with clear names
        CreateButton("Solve", actionColor, () => controlManager?.SolveCircuit(), "Calculate circuit");
        CreateButton("Test", utilityColor, () => controlManager?.TestCircuit(), "Test circuit");
        CreateButton("Delete", new Color(0.8f, 0.3f, 0.3f, 1f), DeleteSelectedComponent, "Delete selected component");
        CreateButton("Reset", utilityColor, () => ResetCircuit(), "Clear all components");
    }
    
    private void CreateButton(string label, Color color, System.Action onClick, string tooltip = "")
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
        
        // Set button color for normal buttons
        Image buttonImage = newButton.GetComponent<Image>();
        if (buttonImage != null && color != Color.clear)
            buttonImage.color = color;
        
        // Add click listener if provided
        if (onClick != null)
        {
            newButton.onClick.AddListener(() => onClick());
        }
        
        // Add tooltip if provided (as a Unity Tooltip component)
        if (!string.IsNullOrEmpty(tooltip))
        {
            // Store tooltip in the button's name for now (could add custom tooltip system later)
            newButton.name = $"Button_{label.Replace(" ", "_")}_{tooltip}";
        }
        
        Debug.Log($"Created palette button: {label}");
    }
    
    private void DeleteSelectedComponent()
    {
        // Get the currently selected component
        SelectableComponent selected = SelectableComponent.GetCurrentlySelected();
        
        if (selected == null)
        {
            Debug.LogWarning("No component selected to delete. Select a component first.");
            return;
        }
        
        Debug.Log($"Deleting selected component: {selected.gameObject.name}");
        
        // Get the CircuitComponent3D to properly unregister from CircuitManager
        CircuitComponent3D circuitComp = selected.GetComponent<CircuitComponent3D>();
        if (circuitComp != null)
        {
            // Unregister from CircuitManager (will trigger proper cleanup)
            CircuitManager circuitManager = CircuitManager.Instance;
            if (circuitManager != null)
            {
                circuitManager.UnregisterComponent(circuitComp);
            }
        }
        
        // Remove from ComponentFactoryManager tracking
        if (factoryManager != null)
        {
            factoryManager.RemoveComponent(selected.gameObject);
        }
        
        // Find and remove any connected wires
        CircuitWire[] allWires = FindObjectsByType<CircuitWire>(FindObjectsSortMode.None);
        foreach (var wire in allWires)
        {
            if (wire.IsConnectedToComponent(selected.gameObject))
            {
                Debug.Log($"Removing connected wire: {wire.gameObject.name}");
                
                // Unregister wire from CircuitManager
                CircuitManager circuitManager = CircuitManager.Instance;
                if (circuitManager != null)
                {
                    circuitManager.UnregisterWire(wire.gameObject);
                }
                
                Destroy(wire.gameObject);
            }
        }
        
        // Finally destroy the component
        Destroy(selected.gameObject);
        
        Debug.Log("Selected component deleted successfully");
    }
    
    private void ResetCircuit()
    {
        Debug.Log("Resetting circuit - clearing all components");
        
        // First, tell CircuitManager to clear its internal lists
        CircuitManager circuitManager = CircuitManager.Instance;
        if (circuitManager != null)
        {
            // Clear the manager's internal tracking before destroying objects
            circuitManager.ClearAllComponents();
        }
        
        // Reset the factory manager's component tracking
        if (factoryManager != null)
        {
            factoryManager.ResetComponentTracking();
        }
        
        // Now find and destroy all circuit components
        CircuitComponent3D[] components = FindObjectsByType<CircuitComponent3D>(FindObjectsSortMode.None);
        foreach (var comp in components)
        {
            if (comp != null && comp.gameObject != null)
            {
                Destroy(comp.gameObject);
            }
        }
        
        // Find and destroy all wires
        CircuitWire[] wires = FindObjectsByType<CircuitWire>(FindObjectsSortMode.None);
        foreach (var wire in wires)
        {
            if (wire != null && wire.gameObject != null)
            {
                Destroy(wire.gameObject);
            }
        }
        
        Debug.Log("Circuit reset complete");
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
    
    private void ActivateSelectMode()
    {
        Debug.Log("👆 Select Mode Activated - Click components to select them");
        
        // Find ConnectTool (should always exist now)
        ConnectTool connectTool = FindFirstObjectByType<ConnectTool>();
        if (connectTool != null)
        {
            connectTool.SetSelectMode();
        }
        else
        {
            Debug.LogError("ConnectTool not found! This shouldn't happen.");
        }
    }
    
    private void ActivateConnectMode()
    {
        Debug.Log("🔌 Connect Mode Activated - Click on two components to connect them");
        
        // Find ConnectTool (should always exist now)
        ConnectTool connectTool = FindFirstObjectByType<ConnectTool>();
        if (connectTool != null)
        {
            connectTool.SetConnectMode();
        }
        else
        {
            Debug.LogError("ConnectTool not found! This shouldn't happen.");
        }
    }
    
    // Deprecated - kept for compatibility
    private void ActivateWireTool()
    {
        ActivateConnectMode();
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
        if (Input.GetKeyDown(KeyCode.J))
        {
            Debug.Log("Placing Junction (J key)");
            factoryManager?.CreateJunction();
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
        if (Input.GetKeyDown(KeyCode.X))
        {
            Debug.Log("Deleting Selected Component (X key)");
            DeleteSelectedComponent();
        }
    }
    
    #endregion
}