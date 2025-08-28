using UnityEngine;

/// <summary>
/// Displays component values (voltage, current, resistance) directly above components
/// Uses clean, minimal labeling instead of dashboard blocks
/// </summary>
public class ComponentValueDisplay : MonoBehaviour
{
    [Header("Display Settings")]
    public float heightOffset = 1.5f;
    public Color voltageColor = new Color(0.9f, 0.2f, 0.2f);
    public Color currentColor = new Color(0.2f, 0.6f, 0.9f);
    public Color resistanceColor = new Color(0.7f, 0.7f, 0.3f);
    
    private CircuitComponent3D circuitComponent;
    private TextMesh voltageLabel;
    private TextMesh currentLabel;
    private TextMesh resistanceLabel;
    
    void Start()
    {
        circuitComponent = GetComponent<CircuitComponent3D>();
        if (circuitComponent == null) return;
        
        CreateLabels();
    }
    
    void OnEnable()
    {
        // Ensure labels exist when component is re-enabled
        if (circuitComponent != null && voltageLabel == null)
        {
            CreateLabels();
        }
    }
    
    void CreateLabels()
    {
        Debug.Log($"Creating labels for {gameObject.name}");
        
        // Only create labels if they don't exist
        if (voltageLabel == null)
        {
            voltageLabel = CreateLabel("Voltage", new Vector3(0, heightOffset, 0), voltageColor);
        }
        
        if (currentLabel == null)
        {
            currentLabel = CreateLabel("Current", new Vector3(0, heightOffset - 0.3f, 0), currentColor);
        }
        
        // Create resistance label (only for resistors/bulbs)
        if (resistanceLabel == null && 
            (circuitComponent.ComponentType == ComponentType.Resistor || 
             circuitComponent.ComponentType == ComponentType.Bulb))
        {
            resistanceLabel = CreateLabel("Resistance", new Vector3(0, heightOffset - 0.6f, 0), resistanceColor);
        }
    }
    
    TextMesh CreateLabel(string name, Vector3 localPosition, Color color)
    {
        GameObject labelObj = new GameObject($"{name}Label");
        labelObj.transform.SetParent(transform, false);
        labelObj.transform.localPosition = localPosition;
        labelObj.transform.localScale = Vector3.one * 0.15f;
        
        TextMesh textMesh = labelObj.AddComponent<TextMesh>();
        textMesh.text = "";
        textMesh.fontSize = 24;
        textMesh.color = color;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        textMesh.characterSize = 0.1f;
        
        // Make label always face camera
        labelObj.AddComponent<FaceCamera>();
        
        return textMesh;
    }
    
    void Update()
    {
        UpdateLabels();
    }
    
    void UpdateLabels()
    {
        if (circuitComponent == null) return;
        
        // Check if labels still exist, recreate if needed
        if (voltageLabel == null || currentLabel == null)
        {
            Debug.Log($"Labels missing on {gameObject.name}, recreating...");
            // Recreate labels without destroying existing ones
            CreateLabels();
        }
        
        // Update voltage
        if (voltageLabel != null)
        {
            if (circuitComponent.ComponentType == ComponentType.Battery)
            {
                voltageLabel.text = $"{circuitComponent.voltage:F1}V";
            }
            else if (Mathf.Abs(circuitComponent.voltageDrop) > 0.01f)
            {
                voltageLabel.text = $"{circuitComponent.voltageDrop:F2}V";
            }
            else
            {
                voltageLabel.text = "";
            }
        }
        
        // Update current
        if (currentLabel != null)
        {
            if (Mathf.Abs(circuitComponent.current) > 0.01f)
            {
                currentLabel.text = $"{circuitComponent.current:F2}A";
            }
            else
            {
                currentLabel.text = "";
            }
        }
        
        // Update resistance
        if (resistanceLabel != null)
        {
            resistanceLabel.text = $"{circuitComponent.resistance:F1}Ω";
        }
    }
}

/// <summary>
/// Makes text always face the camera
/// </summary>
public class FaceCamera : MonoBehaviour
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
            transform.rotation = Quaternion.LookRotation(transform.position - mainCamera.transform.position);
        }
    }
}