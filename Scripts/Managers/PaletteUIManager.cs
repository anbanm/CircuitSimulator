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
        ConnectTool connectTool = ComponentRegistry.Instance.GetManager<ConnectTool>();
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
        // Auto-find palette container if not assigned
        if (paletteContainer == null)
        {
            paletteContainer = FindPaletteContainer();
        }

        // Create button prefab if not assigned
        if (buttonPrefab == null)
        {
            buttonPrefab = CreateButtonPrefab();
        }

        if (buttonPrefab == null || paletteContainer == null)
        {
            Debug.LogWarning("[PaletteUIManager] Cannot create UI buttons - falling back to keyboard shortcuts");
            Debug.Log("Controls: B=Battery, R=Resistor, L=Bulb, S=Switch, C=Connect Mode, V=Select Mode");
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
        
        // Educational assistance toggle
        CreateButton("Help", new Color(0.3f, 0.7f, 0.3f, 1f), () => MisconceptionAlert.Instance.ToggleMisconceptionDetection(), "Toggle learning assistance (M key)");
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
        
        // Set smaller button size (half the original)
        RectTransform buttonRect = newButton.GetComponent<RectTransform>();
        if (buttonRect != null)
        {
            Vector2 currentSize = buttonRect.sizeDelta;
            buttonRect.sizeDelta = new Vector2(currentSize.x * 0.5f, currentSize.y * 0.5f);
        }
        
        // Set button text (handle both Text and TextMeshPro)
        Text buttonText = newButton.GetComponentInChildren<Text>();
        if (buttonText != null)
        {
            buttonText.text = label;
            buttonText.fontSize = Mathf.Max(8, buttonText.fontSize / 2); // Half font size, minimum 8
        }
        else
        {
            // Try TextMeshPro if regular Text not found
            var tmpText = newButton.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            if (tmpText != null)
            {
                tmpText.text = label;
                tmpText.fontSize = Mathf.Max(8, tmpText.fontSize / 2); // Half font size, minimum 8
            }
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

    private Transform FindPaletteContainer()
    {
        // Try to find existing palette container
        GameObject paletteObj = GameObject.Find("ComponentPalette");
        if (paletteObj != null)
        {
            Debug.Log("[PaletteUIManager] Found existing ComponentPalette container");
            return paletteObj.transform;
        }

        // Look for Canvas and create palette panel
        Canvas mainCanvas = FindFirstObjectByType<Canvas>();
        if (mainCanvas != null)
        {
            GameObject palettePanel = CreatePalettePanel(mainCanvas.transform);
            Debug.Log("[PaletteUIManager] Created ComponentPalette panel in Canvas");
            return palettePanel.transform;
        }

        Debug.LogError("[PaletteUIManager] No Canvas found - cannot create palette container");
        return null;
    }

    private GameObject CreatePalettePanel(Transform canvasTransform)
    {
        // Create main palette panel
        GameObject palettePanel = new GameObject("ComponentPalette");
        palettePanel.transform.SetParent(canvasTransform);

        // Add RectTransform and set up layout
        RectTransform rectTransform = palettePanel.AddComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0, 1); // Top-left anchor
        rectTransform.anchorMax = new Vector2(0, 1);
        rectTransform.anchoredPosition = new Vector2(10, -10); // Offset from top-left
        rectTransform.sizeDelta = new Vector2(200, 400); // Width x Height

        // Add background image
        Image background = palettePanel.AddComponent<Image>();
        background.color = new Color(0.1f, 0.1f, 0.1f, 0.8f); // Dark semi-transparent

        // Add layout group for automatic button arrangement
        VerticalLayoutGroup layoutGroup = palettePanel.AddComponent<VerticalLayoutGroup>();
        layoutGroup.spacing = 5f;
        layoutGroup.padding = new RectOffset(10, 10, 10, 10);
        layoutGroup.childAlignment = TextAnchor.UpperCenter;
        layoutGroup.childControlWidth = true;
        layoutGroup.childControlHeight = false;
        layoutGroup.childForceExpandWidth = true;
        layoutGroup.childForceExpandHeight = false;

        // Add content size fitter to adjust panel size
        ContentSizeFitter sizeFitter = palettePanel.AddComponent<ContentSizeFitter>();
        sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        return palettePanel;
    }

    private Button CreateButtonPrefab()
    {
        // Create a basic button prefab programmatically
        GameObject buttonObj = new GameObject("ButtonPrefab");

        // Add RectTransform
        RectTransform rectTransform = buttonObj.AddComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(180, 30); // Button size

        // Add Image component for button background
        Image buttonImage = buttonObj.AddComponent<Image>();
        buttonImage.color = new Color(0.3f, 0.3f, 0.4f, 1f); // Default gray

        // Add Button component
        Button button = buttonObj.AddComponent<Button>();

        // Create text child for button label
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform);

        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        textRect.anchoredPosition = Vector2.zero;

        Text buttonText = textObj.AddComponent<Text>();
        buttonText.text = "Button";
        buttonText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        buttonText.fontSize = 14;
        buttonText.color = Color.white;
        buttonText.alignment = TextAnchor.MiddleCenter;

        Debug.Log("[PaletteUIManager] Created button prefab programmatically");
        return button;
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
        ConnectTool connectTool = ComponentRegistry.Instance.GetManager<ConnectTool>();
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
        ConnectTool connectTool = ComponentRegistry.Instance.GetManager<ConnectTool>();
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
    
    #region Unity Lifecycle

    void Start()
    {
        // Auto-initialize if not already initialized
        if (factoryManager == null)
        {
            factoryManager = FindFirstObjectByType<ComponentFactoryManager>();
            if (factoryManager == null)
            {
                Debug.LogError("[PaletteUIManager] ComponentFactoryManager not found!");
            }
            else
            {
                Debug.Log("[PaletteUIManager] Found ComponentFactoryManager, ready to create components");
            }
        }

        if (controlManager == null)
        {
            controlManager = FindFirstObjectByType<CircuitControlManager>();
        }

        // Ensure ConnectTool exists
        EnsureConnectToolExists();

        // CRITICAL FIX: Actually initialize the UI buttons
        if (factoryManager != null)
        {
            Initialize(factoryManager, controlManager);
        }
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
        
        // M key - Toggle Misconception Detection (educational assistance)
        if (Input.GetKeyDown(KeyCode.M))
        {
            Debug.Log("Toggle Misconception Detection (M key)");
            MisconceptionAlert.Instance.ToggleMisconceptionDetection();
        }
    }
    
    #endregion
}