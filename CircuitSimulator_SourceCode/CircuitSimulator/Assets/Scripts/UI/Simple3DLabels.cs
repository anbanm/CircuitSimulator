using UnityEngine;

public class Simple3DLabels : MonoBehaviour
{
    [Header("Label Settings")]
    public Vector3 labelOffset = new Vector3(0, 1.2f, 0);
    public float textSize = 0.05f; // Smaller text size
    
    private CircuitComponent3D circuitComponent;
    private GameObject labelObject;
    private TextMesh textMesh;
    private Camera mainCamera;
    
    void Start()
    {
        circuitComponent = GetComponent<CircuitComponent3D>();
        mainCamera = Camera.main;
        CreateSimpleLabel();
    }
    
    void CreateSimpleLabel()
    {
        // Create simple 3D text object
        labelObject = new GameObject($"{name}_Label");
        labelObject.transform.SetParent(transform);
        labelObject.transform.localPosition = labelOffset;
        
        // Add TextMesh component
        textMesh = labelObject.AddComponent<TextMesh>();
        textMesh.text = "V:0 I:0 R:0";
        textMesh.fontSize = 10; // Smaller font
        textMesh.characterSize = textSize;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.color = Color.black;
        
        // Add MeshRenderer for visibility
        MeshRenderer renderer = labelObject.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.material = new Material(Shader.Find("GUI/Text Shader"));
            renderer.material.color = Color.black;
        }
    }
    
    void Update()
    {
        UpdateLabelText();
        FaceCamera();
    }
    
    void UpdateLabelText()
    {
        if (circuitComponent == null || textMesh == null) return;
        
        // Create simple single-line label
        string voltage = "";
        string current = $"I:{circuitComponent.current:F2}A";
        string resistance = "";
        
        // Format voltage based on component type
        if (circuitComponent.ComponentType == ComponentType.Battery)
        {
            voltage = $"V:{circuitComponent.voltage:F1}V";
        }
        else
        {
            voltage = $"V:{circuitComponent.voltageDrop:F2}V";
        }
        
        // Format resistance
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
        
        // Combine into single line with better spacing
        textMesh.text = $"{voltage} | {current} | {resistance}";
        
        // Only show if component is active or selected
        bool shouldShow = ShouldShowLabel();
        labelObject.SetActive(shouldShow);
    }
    
    void FaceCamera()
    {
        if (mainCamera != null && labelObject != null)
        {
            // Make label always face camera
            labelObject.transform.LookAt(mainCamera.transform);
            
            // Flip the text so it's not backwards
            labelObject.transform.Rotate(0, 180, 0);
        }
    }
    
    bool ShouldShowLabel()
    {
        SelectableComponent selectable = GetComponent<SelectableComponent>();
        bool isSelected = selectable != null && selectable.IsSelected();
        bool hasCurrentFlow = Mathf.Abs(circuitComponent.current) > 0.001f;
        bool isBattery = circuitComponent.ComponentType == ComponentType.Battery;
        
        return isSelected || hasCurrentFlow || isBattery;
    }
}