using UnityEngine;
// using UnityEngine.UI; // DISABLED - install Legacy UI package for Unity 6

#if UNITY_UI_ENABLED
public class ComponentLabel : MonoBehaviour
{
    [Header("Label Settings")]
    public Vector3 labelOffset = new Vector3(0, 0.7f, 0);
    public float labelScale = 0.0005f; // MUCH smaller scale
    
    [Header("Label Colors")]
    public Color voltageColor = Color.red;
    public Color currentColor = Color.blue;
    public Color resistanceColor = Color.green;
    public Color backgroundColor = new Color(0, 0, 0, 0.8f);
    
    private CircuitComponent3D circuitComponent;
    private GameObject labelCanvas;
    private Text voltageText;
    private Text currentText;
    private Text resistanceText;
    private Camera mainCamera;
    
    void Start()
    {
        circuitComponent = GetComponent<CircuitComponent3D>();
        mainCamera = Camera.main;
        CreateLabelUI();
    }
    
    void CreateLabelUI()
    {
        // Create canvas for this component's labels
        labelCanvas = new GameObject($"{name}_Labels");
        labelCanvas.transform.SetParent(transform);
        labelCanvas.transform.localPosition = labelOffset;
        
        // Add Canvas component
        Canvas canvas = labelCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = mainCamera;
        
        // Scale the canvas
        labelCanvas.transform.localScale = Vector3.one * labelScale;
        
        // Add CanvasScaler for consistent sizing
        CanvasScaler scaler = labelCanvas.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        
        // Create background panel
        GameObject backgroundPanel = CreateBackgroundPanel();
        
        // Create label texts
        CreateLabelTexts(backgroundPanel);
        
        // Make labels face camera
        labelCanvas.AddComponent<BillboardLabel>();
    }
    
    GameObject CreateBackgroundPanel()
    {
        GameObject panel = new GameObject("BackgroundPanel");
        panel.transform.SetParent(labelCanvas.transform);
        panel.transform.localPosition = Vector3.zero;
        
        // Add RectTransform
        RectTransform rect = panel.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(80, 40); // Even smaller panel
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        
        // Add background image
        Image background = panel.AddComponent<Image>();
        background.color = backgroundColor;
        
        return panel;
    }
    
    void CreateLabelTexts(GameObject parent)
    {
        // Voltage label
        voltageText = CreateTextLabel("VoltageLabel", parent, new Vector2(0, 15));
        voltageText.color = voltageColor;
        voltageText.text = "V: 0.0V";
        
        // Current label  
        currentText = CreateTextLabel("CurrentLabel", parent, new Vector2(0, 0));
        currentText.color = currentColor;
        currentText.text = "I: 0.0A";
        
        // Resistance label
        resistanceText = CreateTextLabel("ResistanceLabel", parent, new Vector2(0, -15));
        resistanceText.color = resistanceColor;
        resistanceText.text = "R: 0.0Ω";
    }
    
    Text CreateTextLabel(string name, GameObject parent, Vector2 position)
    {
        GameObject textObj = new GameObject(name);
        textObj.transform.SetParent(parent.transform);
        
        RectTransform rect = textObj.AddComponent<RectTransform>();
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(70, 10); // Much smaller text areas
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        
        Text text = textObj.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 8; // Even smaller font
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        
        return text;
    }
    
    void Update()
    {
        UpdateLabels();
    }
    
    void UpdateLabels()
    {
        if (circuitComponent == null) return;
        
        // Update voltage display
        if (voltageText != null)
        {
            if (circuitComponent.ComponentType == ComponentType.Battery)
            {
                voltageText.text = $"V: {circuitComponent.voltage:F1}V";
            }
            else
            {
                voltageText.text = $"V: {circuitComponent.voltageDrop:F2}V";
            }
        }
        
        // Update current display
        if (currentText != null)
        {
            currentText.text = $"I: {circuitComponent.current:F3}A";
        }
        
        // Update resistance display
        if (resistanceText != null)
        {
            if (circuitComponent.ComponentType == ComponentType.Switch)
            {
                // Show switch state instead of resistance
                resistanceText.text = circuitComponent.resistance < 1f ? "CLOSED" : "OPEN";
            }
            else if (circuitComponent.resistance >= 1000f)
            {
                resistanceText.text = $"R: {circuitComponent.resistance/1000f:F1}kΩ";
            }
            else
            {
                resistanceText.text = $"R: {circuitComponent.resistance:F1}Ω";
            }
        }
        
        // Update label visibility based on selection or connection
        bool shouldShow = ShouldShowLabels();
        if (labelCanvas != null)
        {
            labelCanvas.SetActive(shouldShow);
        }
    }
    
    bool ShouldShowLabels()
    {
        // Show labels if:
        // 1. Component is selected
        // 2. Component has current flowing (connected to circuit)
        // 3. Always show for batteries
        
        SelectableComponent selectable = GetComponent<SelectableComponent>();
        bool isSelected = selectable != null && selectable.IsSelected();
        bool hasCurrentFlow = Mathf.Abs(circuitComponent.current) > 0.001f;
        bool isBattery = circuitComponent.ComponentType == ComponentType.Battery;
        
        return isSelected || hasCurrentFlow || isBattery;
    }
}

// Helper component to make labels always face camera
public class BillboardLabel : MonoBehaviour
{
    private Camera mainCamera;
    
    void Start()
    {
        mainCamera = Camera.main;
    }
    
    void LateUpdate()
    {
        if (mainCamera != null)
        {
            // Make the label face the camera
            Vector3 lookDirection = mainCamera.transform.position - transform.position;
            lookDirection.y = 0; // Keep labels upright
            
            if (lookDirection != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(-lookDirection);
            }
        }
    }
}
#else
// Placeholder when UI is disabled
public class ComponentLabel : MonoBehaviour
{
    void Start()
    {
        Debug.Log("ComponentLabel: Disabled. Install Legacy UI package for Unity 6 to enable UI labels.");
    }
}
#endif