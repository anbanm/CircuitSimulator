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
    
    private GameObject popupCanvas;
    private InputField voltageInput;
    private InputField resistanceInput;
    private Text titleText;
    private Button applyButton;
    private Button cancelButton;
    
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
            CreatePopupUI();
            Debug.Log("✅ ComponentPropertyPopup initialized successfully");
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }
    
    void CreatePopupUI()
    {
        // Create world space canvas for popup
        popupCanvas = new GameObject("PropertyPopupCanvas");
        popupCanvas.transform.SetParent(transform);
        
        Canvas canvas = popupCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        
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
        GameObject voltageLabel = CreateUIText("VoltageLabel", panel.transform, new Vector2(-70, 20), "Voltage:");
        GameObject voltageField = CreateInputField("VoltageInput", panel.transform, new Vector2(30, 20));
        voltageInput = voltageField.GetComponent<InputField>();
        
        // Create resistance input field
        GameObject resistanceLabel = CreateUIText("ResistanceLabel", panel.transform, new Vector2(-70, -20), "Resistance:");
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
        
        // Position popup near component in world space
        Vector3 componentPos = component.transform.position;
        Vector3 cameraPos = Camera.main.transform.position;
        Vector3 dirToCamera = (cameraPos - componentPos).normalized;
        
        // Position popup closer and more visible
        Vector3 popupPos = componentPos + Vector3.up * 2f + dirToCamera * 1f;
        popupCanvas.transform.position = popupPos;
        popupCanvas.transform.LookAt(cameraPos);
        
        // Use larger scale for better visibility
        popupCanvas.transform.localScale = Vector3.one * 0.1f;
        
        // Set title
        titleText.text = $"Edit {component.ComponentType}";
        
        // Show/hide fields based on component type
        bool showVoltage = component.ComponentType == ComponentType.Battery;
        bool showResistance = component.ComponentType == ComponentType.Resistor ||
                             component.ComponentType == ComponentType.Bulb;
        
        voltageInput.transform.parent.gameObject.SetActive(showVoltage);
        resistanceInput.transform.parent.gameObject.SetActive(showResistance);
        
        // Set current values
        if (showVoltage)
            voltageInput.text = component.voltage.ToString("F1");
        if (showResistance)
            resistanceInput.text = component.resistance.ToString("F1");
        
        // Show popup
        popupCanvas.SetActive(true);
        
        Debug.Log($"🎯 Property popup shown for {component.name} at position {popupPos}");
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
        // Close on ESC
        if (popupCanvas != null && popupCanvas.activeInHierarchy && Input.GetKeyDown(KeyCode.Escape))
        {
            ClosePopup();
        }
        
        // Keep popup facing camera
        if (popupCanvas != null && popupCanvas.activeInHierarchy && Camera.main != null)
        {
            Vector3 cameraPos = Camera.main.transform.position;
            popupCanvas.transform.LookAt(2 * popupCanvas.transform.position - cameraPos);
        }
    }
}