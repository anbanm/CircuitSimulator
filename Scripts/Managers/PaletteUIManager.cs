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
    }
    
    private void EnsureConnectToolExists()
    {
        ConnectTool connectTool = ComponentRegistry.Instance.GetManager<ConnectTool>();
        if (connectTool == null)
        {
            GameObject connectToolObj = new GameObject("ConnectTool");
            connectTool = connectToolObj.AddComponent<ConnectTool>();
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
            return;
        }
        
        // Professional color scheme
        Color modeColor = new Color(0.2f, 0.6f, 0.9f, 1f);      // Professional blue
        Color componentColor = new Color(0.3f, 0.3f, 0.4f, 1f);  // Dark gray
        Color actionColor = new Color(0.2f, 0.7f, 0.3f, 1f);     // Professional green
        Color utilityColor = new Color(0.5f, 0.5f, 0.6f, 1f);    // Medium gray
        
        // Wire button - creates a physical draggable wire
        CreateButton("Wire", new Color(0.3f, 0.7f, 0.9f, 1f), CreatePhysicalWire, "Create a draggable wire (W key)");

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

        // Exit button to validate and return to challenge flow
        CreateButton("Exit", new Color(0.6f, 0.3f, 0.8f, 1f), ExitSimulator, "Validate circuit and exit (ESC key)");
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

        // Keep original button size for readability
        RectTransform buttonRect = newButton.GetComponent<RectTransform>();
        if (buttonRect != null)
        {
            // Don't scale down - keep at original size or set to readable size
            Vector2 currentSize = buttonRect.sizeDelta;
            // If size is too small, set to minimum readable size
            if (currentSize.x < 100 || currentSize.y < 40)
            {
                buttonRect.sizeDelta = new Vector2(120, 50);
            }
        }

        // Set button text (handle both Text and TextMeshPro)
        Text buttonText = newButton.GetComponentInChildren<Text>();
        if (buttonText != null)
        {
            buttonText.text = label;
            buttonText.fontSize = Mathf.Max(16, buttonText.fontSize); // Minimum 16 for readability
        }
        else
        {
            // Try TextMeshPro if regular Text not found
            var tmpText = newButton.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            if (tmpText != null)
            {
                tmpText.text = label;
                tmpText.fontSize = Mathf.Max(16, tmpText.fontSize); // Minimum 16 for readability
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
        
    }
    
    private void ResetCircuit()
    {
        
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
        
    }

    private Transform FindPaletteContainer()
    {
        // Try to find existing palette container
        GameObject paletteObj = GameObject.Find("ComponentPalette");
        if (paletteObj != null)
        {
            return paletteObj.transform;
        }

        // Look for Canvas and create palette panel
        Canvas mainCanvas = FindFirstObjectByType<Canvas>();
        if (mainCanvas != null)
        {
            GameObject palettePanel = CreatePalettePanel(mainCanvas.transform);
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
    
    private void CreatePhysicalWire()
    {
        // For button clicks: place wire near last component (mouse is on UI)
        // Find ConnectTool
        ConnectTool connectTool = ComponentRegistry.Instance.GetManager<ConnectTool>();
        if (connectTool == null)
        {
            Debug.LogError("ConnectTool not found! This shouldn't happen.");
            return;
        }

        // Get position of last placed component
        Vector3 componentPos = factoryManager?.GetLastComponentPosition() ?? Vector3.zero;

        // Keep wire on the SAME PLANE as components (Y = 0.5)
        // Only offset in horizontal plane (X, Z) to make wire visible next to component
        Vector3 wirePosition = new Vector3(
            componentPos.x + 1.5f,  // Offset right
            0.5f,                    // SAME Y as workspace plane
            componentPos.z           // Same Z depth
        );

        // Create wire at that position
        connectTool.CreateDraggableWireAtPosition(wirePosition);
    }

    private void CreatePhysicalWireAtCursor()
    {
        // For W key: place wire at cursor position (user's mouse location)
        ConnectTool connectTool = ComponentRegistry.Instance.GetManager<ConnectTool>();
        if (connectTool != null)
        {
            // Use reflection to call private CreateDraggableWire (uses cursor position)
            var method = connectTool.GetType().GetMethod("CreateDraggableWire",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (method != null)
            {
                method.Invoke(connectTool, null);
            }
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
            PlaceBattery();
        }
        if (Input.GetKeyDown(KeyCode.R))
        {
            PlaceResistor();
        }
        if (Input.GetKeyDown(KeyCode.L))
        {
            PlaceBulb();
        }
        if (Input.GetKeyDown(KeyCode.S))
        {
            PlaceSwitch();
        }
        if (Input.GetKeyDown(KeyCode.J))
        {
            factoryManager?.CreateJunction();
        }
        if (Input.GetKeyDown(KeyCode.W))
        {
            CreatePhysicalWireAtCursor();
        }
        if (Input.GetKeyDown(KeyCode.Space))
        {
            controlManager?.SolveCircuit();
        }
        if (Input.GetKeyDown(KeyCode.T))
        {
            controlManager?.TestCircuit();
        }
        if (Input.GetKeyDown(KeyCode.X))
        {
            DeleteSelectedComponent();
        }
        
        // M key - Toggle Misconception Detection (educational assistance)
        if (Input.GetKeyDown(KeyCode.M))
        {
            MisconceptionAlert.Instance.ToggleMisconceptionDetection();
        }

        // ESC key - Exit simulator (with validation)
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ExitSimulator();
        }
    }

    #endregion

    #region Exit and Validation

    private void ExitSimulator()
    {
        Debug.Log("[PaletteUIManager] Exit button pressed");

        // Try to find CircuitSimulatorAdapter (if simulator was instantiated in Challenge_scene)
        var simulatorAdapter = FindFirstObjectByType<CircuitSimulatorAdapter>();
        if (simulatorAdapter != null)
        {
            Debug.Log("[PaletteUIManager] Found CircuitSimulatorAdapter - triggering user exit");

            // Trigger OnSimulatorClosed event - ChallengeFlowManager will handle cleanup
            simulatorAdapter.UserRequestedExit();
            return;
        }

        // If no CircuitSimulatorAdapter found, we're in standalone mode
        // Check if we're returning from a challenge (check sessionData)
        var sessionData = UnityEngine.Resources.FindObjectsOfTypeAll<ChallengeSessionData>();
        if (sessionData != null && sessionData.Length > 0 && sessionData[0].isChallengeActive)
        {
            Debug.Log("[PaletteUIManager] Standalone mode - Returning to Challenge_scene");

            // Validate circuit
            if (controlManager != null)
            {
                controlManager.ValidateCircuit();
            }

            // Mark that we're returning from simulator (phase 2 complete)
            sessionData[0].simulatorCompleted = true;

            // Return to Challenge_scene
            UnityEngine.SceneManagement.SceneManager.LoadScene("Challenge_scene");
        }
        else
        {
            // Standalone simulator mode - just close
            Debug.Log("[PaletteUIManager] Standalone simulator mode - closing simulator");
            UnityEngine.SceneManagement.SceneManager.LoadScene("Main_Scene");
        }
    }

    #endregion
}