using UnityEngine;
using UnityEngine.UI;

public class ScreenSpaceLabels : MonoBehaviour
{
    [Header("Label Settings")]
    public Vector3 worldOffset = new Vector3(0, 1.2f, 0);
    public Vector2 screenOffset = new Vector2(0, 50); // Pixels above object
    public float baseFontSize = 14f; // Larger base font size
    public bool scaleWithDistance = true;
    public float minFontSize = 10f;
    public float maxFontSize = 24f;
    
    private CircuitComponent3D circuitComponent;
    private Camera mainCamera;
    private Canvas screenCanvas;
    private Text labelText;
    private GameObject labelObject;
    
    void Start()
    {
        circuitComponent = GetComponent<CircuitComponent3D>();
        mainCamera = Camera.main;
        CreateScreenSpaceLabel();
    }
    
    void CreateScreenSpaceLabel()
    {
        // Find or create screen space canvas
        screenCanvas = FindObjectOfType<Canvas>();
        if (screenCanvas == null || screenCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            CreateScreenCanvas();
        }
        
        // Create label UI object
        labelObject = new GameObject($"{name}_ScreenLabel");
        labelObject.transform.SetParent(screenCanvas.transform);
        
        // Add RectTransform
        RectTransform rect = labelObject.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(150, 25); // Larger label size
        
        // Add Text component
        labelText = labelObject.AddComponent<Text>();
        labelText.text = "V:0 I:0 R:0";
        labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        labelText.fontSize = (int)baseFontSize; // Use base font size
        labelText.color = Color.black;
        labelText.alignment = TextAnchor.MiddleCenter;
        
        // Add shadow for readability
        Shadow shadow = labelObject.AddComponent<Shadow>();
        shadow.effectColor = Color.white;
        shadow.effectDistance = new Vector2(1, -1);
    }
    
    void CreateScreenCanvas()
    {
        GameObject canvasObj = new GameObject("ScreenLabelCanvas");
        screenCanvas = canvasObj.AddComponent<Canvas>();
        screenCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        screenCanvas.sortingOrder = 100; // Above everything else
        
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();
    }
    
    void Update()
    {
        UpdateLabelText();
        UpdateLabelPosition();
    }
    
    void UpdateLabelText()
    {
        if (circuitComponent == null || labelText == null) return;
        
        // Format values
        string voltage = "";
        string current = $"I:{circuitComponent.current:F2}A";
        string resistance = "";
        
        if (circuitComponent.ComponentType == ComponentType.Battery)
        {
            voltage = $"V:{circuitComponent.voltage:F1}V";
        }
        else
        {
            voltage = $"V:{circuitComponent.voltageDrop:F2}V";
        }
        
        if (circuitComponent.ComponentType == ComponentType.Switch)
        {
            resistance = circuitComponent.resistance < 1f ? "CLOSED" : "OPEN";
        }
        else if (circuitComponent.resistance >= 1000f)
        {
            resistance = $"R:{circuitComponent.resistance/1000f:F1}k";
        }
        else
        {
            resistance = $"R:{circuitComponent.resistance:F1}Ω";
        }
        
        // Simple clean format
        labelText.text = $"{voltage} | {current} | {resistance}";
        
        // Show/hide based on component state
        bool shouldShow = ShouldShowLabel();
        labelObject.SetActive(shouldShow);
    }
    
    void UpdateLabelPosition()
    {
        if (mainCamera == null || labelObject == null) return;
        
        // Get world position of component + offset
        Vector3 worldPos = transform.position + worldOffset;
        
        // Convert to screen position
        Vector3 screenPos = mainCamera.WorldToScreenPoint(worldPos);
        
        // Check if behind camera
        if (screenPos.z <= 0)
        {
            labelObject.SetActive(false);
            return;
        }
        
        // Calculate distance-based scaling
        if (scaleWithDistance)
        {
            float distance = Vector3.Distance(mainCamera.transform.position, transform.position);
            
            // Scale font size based on distance (closer = bigger text)
            float scaleFactor = Mathf.Clamp(20f / distance, 0.5f, 2f);
            int newFontSize = Mathf.RoundToInt(Mathf.Clamp(baseFontSize * scaleFactor, minFontSize, maxFontSize));
            
            if (labelText.fontSize != newFontSize)
            {
                labelText.fontSize = newFontSize;
                
                // Adjust label size based on font size
                RectTransform rect = labelObject.GetComponent<RectTransform>();
                float sizeScale = newFontSize / baseFontSize;
                rect.sizeDelta = new Vector2(150 * sizeScale, 25 * sizeScale);
            }
        }
        
        // Apply screen offset
        screenPos.x += screenOffset.x;
        screenPos.y += screenOffset.y;
        
        // Set UI position
        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.position = screenPos;
    }
    
    bool ShouldShowLabel()
    {
        SelectableComponent selectable = GetComponent<SelectableComponent>();
        bool isSelected = selectable != null && selectable.IsSelected();
        bool hasCurrentFlow = Mathf.Abs(circuitComponent.current) > 0.001f;
        bool isBattery = circuitComponent.ComponentType == ComponentType.Battery;
        
        return isSelected || hasCurrentFlow || isBattery;
    }
    
    void OnDestroy()
    {
        // Clean up label when component is destroyed
        if (labelObject != null)
        {
            Destroy(labelObject);
        }
    }
}
