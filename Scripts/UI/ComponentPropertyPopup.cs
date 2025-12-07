using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// In-game property editor popup that appears in 3D space
/// Allows editing component voltage and resistance values
/// </summary>
public class ComponentPropertyPopup : MonoBehaviour
{
    [Header("Popup Settings")]
    public float popupDistance = 2f;
    public float popupHeight = 1.5f;
    public float updateInterval = 0.1f; // Frame throttling for Update()
    
    private GameObject popupCanvas;
    private InputField voltageInput;
    private InputField resistanceInput;
    private Text titleText;
    private Button applyButton;
    private Button cancelButton;
    
    // Cached references for performance
    private GameObject voltageLabel;
    private GameObject resistanceLabel;
    private Camera cachedMainCamera;
    private float lastUpdateTime;
    
    private CircuitComponent3D currentComponent;
    private static ComponentPropertyPopup instance;
    
    public static ComponentPropertyPopup Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject popupObj = new GameObject("ComponentPropertyPopup");
                instance = popupObj.AddComponent<ComponentPropertyPopup>();
                instance.CreatePopupUI();
            }
            return instance;
        }
    }
    
    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            EnsureEventSystem();
            CreatePopupUI();
            Debug.Log("✅ ComponentPropertyPopup initialized successfully");
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    void EnsureEventSystem()
    {
        // Check if EventSystem exists, create one if not (required for UI input)
        if (UnityEngine.EventSystems.EventSystem.current == null)
        {
            GameObject eventSystemObj = new GameObject("EventSystem");
            eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystemObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            Debug.Log("✅ Created EventSystem for UI input");
        }
    }
    
    void CreatePopupUI()
    {
        // Create world space canvas for popup
        popupCanvas = new GameObject("PropertyPopupCanvas");
        popupCanvas.transform.SetParent(transform);
        
        Canvas canvas = popupCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 100; // Ensure popup appears in front of other UI

        RectTransform canvasRect = popupCanvas.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(400, 250); // Larger canvas size

        // Add canvas scaler for world space
        popupCanvas.AddComponent<CanvasScaler>();
        popupCanvas.AddComponent<GraphicRaycaster>();
        
        // Create background panel
        GameObject panel = new GameObject("Panel");
        panel.transform.SetParent(popupCanvas.transform);
        
        RectTransform panelRect = panel.AddComponent<RectTransform>();
        panelRect.sizeDelta = new Vector2(300, 200); // Reasonable size
        panelRect.localPosition = Vector3.zero;
        panelRect.localScale = Vector3.one; // Full scale for visibility
        
        Image panelImage = panel.AddComponent<Image>();
        panelImage.color = new Color(0.2f, 0.2f, 0.2f, 0.95f);
        
        // Create title
        GameObject titleObj = CreateUIText("Title", panel.transform, new Vector2(0, 60), "Edit Component");
        titleText = titleObj.GetComponent<Text>();
        titleText.fontSize = 24;
        titleText.alignment = TextAnchor.MiddleCenter;
        
        // Create voltage input field
        voltageLabel = CreateUIText("VoltageLabel", panel.transform, new Vector2(-70, 20), "Voltage:");
        GameObject voltageField = CreateInputField("VoltageInput", panel.transform, new Vector2(30, 20));
        voltageInput = voltageField.GetComponent<InputField>();
        
        // Create resistance input field
        resistanceLabel = CreateUIText("ResistanceLabel", panel.transform, new Vector2(-70, -20), "Resistance:");
        GameObject resistanceField = CreateInputField("ResistanceInput", panel.transform, new Vector2(30, -20));
        resistanceInput = resistanceField.GetComponent<InputField>();
        
        // Create buttons
        GameObject applyBtn = CreateButton("ApplyButton", panel.transform, new Vector2(-50, -60), "Apply", ApplyChanges);
        applyButton = applyBtn.GetComponent<Button>();
        
        GameObject cancelBtn = CreateButton("CancelButton", panel.transform, new Vector2(50, -60), "Cancel", ClosePopup);
        cancelButton = cancelBtn.GetComponent<Button>();
        
        // Hide initially
        popupCanvas.SetActive(false);
    }
    
    GameObject CreateUIText(string name, Transform parent, Vector2 position, string text)
    {
        GameObject textObj = new GameObject(name);
        textObj.transform.SetParent(parent);
        
        RectTransform rect = textObj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(100, 30);
        rect.anchoredPosition = position;
        
        Text textComp = textObj.AddComponent<Text>();
        textComp.text = text;
        textComp.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        textComp.fontSize = 18;
        textComp.color = Color.white;
        
        return textObj;
    }
    
    GameObject CreateInputField(string name, Transform parent, Vector2 position)
    {
        GameObject inputObj = new GameObject(name);
        inputObj.transform.SetParent(parent);
        
        RectTransform rect = inputObj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(100, 30);
        rect.anchoredPosition = position;
        
        Image image = inputObj.AddComponent<Image>();
        image.color = new Color(0.3f, 0.3f, 0.3f);
        
        InputField input = inputObj.AddComponent<InputField>();
        input.contentType = InputField.ContentType.DecimalNumber;
        
        // Create text child for input
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(inputObj.transform);
        
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.sizeDelta = new Vector2(90, 30);
        textRect.anchoredPosition = Vector2.zero;
        
        Text inputText = textObj.AddComponent<Text>();
        inputText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        inputText.fontSize = 16;
        inputText.color = Color.white;
        inputText.alignment = TextAnchor.MiddleCenter;
        
        input.textComponent = inputText;
        
        return inputObj;
    }
    
    GameObject CreateButton(string name, Transform parent, Vector2 position, string text, UnityEngine.Events.UnityAction onClick)
    {
        GameObject buttonObj = new GameObject(name);
        buttonObj.transform.SetParent(parent);
        
        RectTransform rect = buttonObj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(80, 30);
        rect.anchoredPosition = position;
        
        Image image = buttonObj.AddComponent<Image>();
        image.color = new Color(0.4f, 0.4f, 0.4f);
        
        Button button = buttonObj.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(onClick);
        
        // Create text child
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform);
        
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.sizeDelta = new Vector2(80, 30);
        textRect.anchoredPosition = Vector2.zero;
        
        Text buttonText = textObj.AddComponent<Text>();
        buttonText.text = text;
        buttonText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        buttonText.fontSize = 16;
        buttonText.color = Color.white;
        buttonText.alignment = TextAnchor.MiddleCenter;
        
        return buttonObj;
    }
    
    public void ShowForComponent(CircuitComponent3D component)
    {
        if (component == null || popupCanvas == null) return;
        
        currentComponent = component;
        
        // Position popup relative to camera view
        if (cachedMainCamera == null)
        {
            cachedMainCamera = Camera.main;
            if (cachedMainCamera == null)
            {
                Debug.LogWarning("ComponentPropertyPopup: Camera.main is null, cannot position popup");
                return;
            }
        }
        Camera mainCamera = cachedMainCamera;
        
        Vector3 componentPos = component.transform.position;
        Vector3 cameraPos = mainCamera.transform.position;
        Vector3 cameraForward = mainCamera.transform.forward;
        Vector3 cameraRight = mainCamera.transform.right;
        Vector3 cameraUp = mainCamera.transform.up;
        
        // Calculate distance from camera to component
        float distanceToComponent = Vector3.Distance(cameraPos, componentPos);
        
        // Position popup offset from component in camera space
        Vector3 popupOffset = cameraRight * 2f + cameraUp * 1f; // Right and up from camera view
        Vector3 popupPos = componentPos + popupOffset;
        
        // Move popup slightly toward camera so it's not obscured
        popupPos += -cameraForward * 0.5f;
        
        popupCanvas.transform.position = popupPos;
        
        // Make popup face camera directly
        popupCanvas.transform.LookAt(popupCanvas.transform.position + cameraForward, cameraUp);
        
        // Scale based on distance for consistent apparent size
        float scale = distanceToComponent * 0.003f; // Much smaller multiplier
        popupCanvas.transform.localScale = Vector3.one * Mathf.Clamp(scale, 0.01f, 0.05f);
        
        // Set title
        titleText.text = $"Edit {component.ComponentType}";
        
        // Show/hide fields based on component type
        bool showVoltage = component.ComponentType == ComponentType.Battery;
        bool showResistance = component.ComponentType == ComponentType.Resistor ||
                             component.ComponentType == ComponentType.Bulb;
        
        Debug.Log($"Component type: {component.ComponentType}, ShowVoltage: {showVoltage}, ShowResistance: {showResistance}");
        
        // Get parent containers (the input field itself)
        if (voltageInput != null && voltageInput.gameObject != null)
        {
            voltageInput.gameObject.SetActive(showVoltage);
            // Use cached reference instead of GameObject.Find
            if (voltageLabel != null) voltageLabel.SetActive(showVoltage);
        }
        
        if (resistanceInput != null && resistanceInput.gameObject != null)
        {
            resistanceInput.gameObject.SetActive(showResistance);
            // Use cached reference instead of GameObject.Find
            if (resistanceLabel != null) resistanceLabel.SetActive(showResistance);
        }
        
        // Set current values
        if (showVoltage)
            voltageInput.text = component.voltage.ToString("F1");
        if (showResistance)
            resistanceInput.text = component.resistance.ToString("F1");
        
        // Show popup
        popupCanvas.SetActive(true);
        
        Debug.Log($"🎯 Property popup shown for {component.name}");
    }
    
    void ApplyChanges()
    {
        if (currentComponent == null) return;
        
        // Apply voltage changes
        if (voltageInput.gameObject.activeInHierarchy && !string.IsNullOrEmpty(voltageInput.text))
        {
            if (float.TryParse(voltageInput.text, out float voltage))
            {
                currentComponent.voltage = Mathf.Clamp(voltage, 0.1f, 100f);
                Debug.Log($"Set {currentComponent.name} voltage to {voltage}V");
            }
        }
        
        // Apply resistance changes
        if (resistanceInput.gameObject.activeInHierarchy && !string.IsNullOrEmpty(resistanceInput.text))
        {
            if (float.TryParse(resistanceInput.text, out float resistance))
            {
                currentComponent.resistance = Mathf.Clamp(resistance, 0.1f, 10000f);
                Debug.Log($"Set {currentComponent.name} resistance to {resistance}Ω");
            }
        }
        
        // Mark circuit for re-solving
        CircuitManager.Instance?.MarkCircuitChanged();
        
        ClosePopup();
    }
    
    void ClosePopup()
    {
        if (popupCanvas != null)
            popupCanvas.SetActive(false);
        currentComponent = null;
    }
    
    void Update()
    {
        // Frame throttling for performance
        if (Time.time - lastUpdateTime < updateInterval)
            return;
        lastUpdateTime = Time.time;
        
        // Early exit if popup is not active
        if (popupCanvas == null || !popupCanvas.activeInHierarchy)
        {
            // Still check for ESC to close if popup exists
            if (popupCanvas != null && Input.GetKeyDown(KeyCode.Escape))
            {
                ClosePopup();
            }
            return;
        }
        
        // Close on ESC
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ClosePopup();
            return;
        }
        
        // Early exit if component is null
        if (currentComponent == null)
        {
            ClosePopup();
            return;
        }
        
        // Cache camera reference if needed
        if (cachedMainCamera == null)
        {
            cachedMainCamera = Camera.main;
        }
        
        // Keep popup facing camera and positioned correctly
        if (cachedMainCamera != null)
        {
            Vector3 componentPos = currentComponent.transform.position;
            Vector3 cameraPos = cachedMainCamera.transform.position;
            Vector3 cameraForward = cachedMainCamera.transform.forward;
            Vector3 cameraRight = cachedMainCamera.transform.right;
            Vector3 cameraUp = cachedMainCamera.transform.up;
            
            // Calculate distance from camera to component
            float distanceToComponent = Vector3.Distance(cameraPos, componentPos);
            
            // Position popup offset from component in camera space
            Vector3 popupOffset = cameraRight * 2f + cameraUp * 1f;
            Vector3 popupPos = componentPos + popupOffset;
            
            // Move popup slightly toward camera
            popupPos += -cameraForward * 0.5f;
            
            popupCanvas.transform.position = popupPos;
            
            // Make popup face camera directly
            popupCanvas.transform.LookAt(popupCanvas.transform.position + cameraForward, cameraUp);
            
            // Consistent scale based on distance
            float scale = distanceToComponent * 0.003f;
            popupCanvas.transform.localScale = Vector3.one * Mathf.Clamp(scale, 0.01f, 0.05f);
        }
    }
}