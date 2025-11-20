using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Popup UI for editing component properties (voltage, resistance, etc.)
/// Appears when right-clicking or double-clicking a component
/// </summary>
public class ComponentPropertyEditor : MonoBehaviour
{
    [Header("UI References")]
    public GameObject popupPanel;
    public TMP_InputField voltageInput;
    public TMP_InputField resistanceInput;
    public Button applyButton;
    public Button cancelButton;
    public TextMeshProUGUI titleText;
    
    private CircuitComponent3D currentComponent;
    private static ComponentPropertyEditor instance;
    
    public static ComponentPropertyEditor Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<ComponentPropertyEditor>();
                if (instance == null)
                {
                    instance = CreatePropertyEditor();
                }
            }
            return instance;
        }
    }
    
    static ComponentPropertyEditor CreatePropertyEditor()
    {
        // Create UI Canvas if needed
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("PropertyEditorCanvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }
        
        // Create property editor panel
        GameObject editorObj = new GameObject("ComponentPropertyEditor");
        editorObj.transform.SetParent(canvas.transform);
        ComponentPropertyEditor editor = editorObj.AddComponent<ComponentPropertyEditor>();
        
        // Create popup panel
        editor.CreatePopupUI();
        
        return editor;
    }
    
    void CreatePopupUI()
    {
        // Create panel
        popupPanel = new GameObject("PropertyPopup");
        popupPanel.transform.SetParent(transform);
        
        RectTransform panelRect = popupPanel.AddComponent<RectTransform>();
        panelRect.sizeDelta = new Vector2(300, 200);
        panelRect.anchoredPosition = Vector2.zero;
        
        Image panelImage = popupPanel.AddComponent<Image>();
        panelImage.color = new Color(0.2f, 0.2f, 0.2f, 0.95f);
        
        // Create title
        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(popupPanel.transform);
        RectTransform titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.sizeDelta = new Vector2(280, 30);
        titleRect.anchoredPosition = new Vector2(0, 70);
        
        titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "Component Properties";
        titleText.fontSize = 18;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.color = Color.white;
        
        // Create voltage input
        CreateInputField("Voltage", new Vector2(0, 20), out voltageInput);
        
        // Create resistance input
        CreateInputField("Resistance", new Vector2(0, -20), out resistanceInput);
        
        // Create apply button
        applyButton = CreateButton("Apply", new Vector2(-60, -70), Color.green, ApplyChanges);
        
        // Create cancel button
        cancelButton = CreateButton("Cancel", new Vector2(60, -70), Color.red, Cancel);
        
        // Hide by default
        popupPanel.SetActive(false);
    }
    
    void CreateInputField(string label, Vector2 position, out TMP_InputField inputField)
    {
        // Create container
        GameObject container = new GameObject($"{label}Container");
        container.transform.SetParent(popupPanel.transform);
        RectTransform containerRect = container.AddComponent<RectTransform>();
        containerRect.sizeDelta = new Vector2(280, 30);
        containerRect.anchoredPosition = position;
        
        // Create label
        GameObject labelObj = new GameObject($"{label}Label");
        labelObj.transform.SetParent(container.transform);
        RectTransform labelRect = labelObj.AddComponent<RectTransform>();
        labelRect.sizeDelta = new Vector2(100, 30);
        labelRect.anchoredPosition = new Vector2(-90, 0);
        
        TextMeshProUGUI labelText = labelObj.AddComponent<TextMeshProUGUI>();
        labelText.text = $"{label}:";
        labelText.fontSize = 14;
        labelText.color = Color.white;
        labelText.alignment = TextAlignmentOptions.MidlineRight;
        
        // Create input field
        GameObject inputObj = new GameObject($"{label}Input");
        inputObj.transform.SetParent(container.transform);
        RectTransform inputRect = inputObj.AddComponent<RectTransform>();
        inputRect.sizeDelta = new Vector2(140, 30);
        inputRect.anchoredPosition = new Vector2(50, 0);
        
        Image inputImage = inputObj.AddComponent<Image>();
        inputImage.color = new Color(0.3f, 0.3f, 0.3f);
        
        inputField = inputObj.AddComponent<TMP_InputField>();
        inputField.contentType = TMP_InputField.ContentType.DecimalNumber;
        
        // Create text component for input
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(inputObj.transform);
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.sizeDelta = new Vector2(130, 30);
        textRect.anchoredPosition = Vector2.zero;
        
        TextMeshProUGUI inputText = textObj.AddComponent<TextMeshProUGUI>();
        inputText.fontSize = 14;
        inputText.color = Color.white;
        inputText.alignment = TextAlignmentOptions.Center;
        
        inputField.textComponent = inputText;
    }
    
    Button CreateButton(string text, Vector2 position, Color color, System.Action onClick)
    {
        GameObject buttonObj = new GameObject($"{text}Button");
        buttonObj.transform.SetParent(popupPanel.transform);
        RectTransform buttonRect = buttonObj.AddComponent<RectTransform>();
        buttonRect.sizeDelta = new Vector2(80, 30);
        buttonRect.anchoredPosition = position;
        
        Image buttonImage = buttonObj.AddComponent<Image>();
        buttonImage.color = color * 0.7f;
        
        Button button = buttonObj.AddComponent<Button>();
        button.targetGraphic = buttonImage;
        button.onClick.AddListener(() => onClick());
        
        // Add text
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform);
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.sizeDelta = new Vector2(80, 30);
        textRect.anchoredPosition = Vector2.zero;
        
        TextMeshProUGUI buttonText = textObj.AddComponent<TextMeshProUGUI>();
        buttonText.text = text;
        buttonText.fontSize = 14;
        buttonText.color = Color.white;
        buttonText.alignment = TextAlignmentOptions.Center;
        
        return button;
    }
    
    public void ShowForComponent(CircuitComponent3D component)
    {
        if (component == null) return;
        
        currentComponent = component;
        
        // Set title based on component type
        titleText.text = $"Edit {component.ComponentType}";
        
        // Show/hide fields based on component type
        bool showVoltage = component.ComponentType == ComponentType.Battery;
        bool showResistance = component.ComponentType == ComponentType.Resistor || 
                              component.ComponentType == ComponentType.Bulb;
        
        voltageInput.gameObject.SetActive(showVoltage);
        resistanceInput.gameObject.SetActive(showResistance);
        
        // Set current values
        if (showVoltage)
            voltageInput.text = component.voltage.ToString("F1");
        if (showResistance)
            resistanceInput.text = component.resistance.ToString("F1");
        
        // Position popup near mouse
        RectTransform rect = popupPanel.GetComponent<RectTransform>();
        rect.position = Input.mousePosition;
        
        // Show popup
        popupPanel.SetActive(true);
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
        
        // Close popup
        Cancel();
    }
    
    void Cancel()
    {
        popupPanel.SetActive(false);
        currentComponent = null;
    }
    
    void Update()
    {
        // Close on ESC
        if (popupPanel.activeInHierarchy && Input.GetKeyDown(KeyCode.Escape))
        {
            Cancel();
        }
    }
}