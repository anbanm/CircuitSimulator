using UnityEngine;
using UnityEngine.UI;
using TMPro;

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
    private TextMeshPro voltageLabel;
    private TextMeshPro currentLabel;
    private TextMeshPro resistanceLabel;
    
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
        
        // Create voltage label
        voltageLabel = CreateLabel("Voltage", new Vector3(0, heightOffset, 0), voltageColor);
        
        // Create current label  
        currentLabel = CreateLabel("Current", new Vector3(0, heightOffset - 0.3f, 0), currentColor);
        
        // Create resistance label (only for resistors/bulbs)
        if (circuitComponent.ComponentType == ComponentType.Resistor || 
            circuitComponent.ComponentType == ComponentType.Bulb)
        {
            resistanceLabel = CreateLabel("Resistance", new Vector3(0, heightOffset - 0.6f, 0), resistanceColor);
        }
    }
    
    TextMeshPro CreateLabel(string name, Vector3 localPosition, Color color)
    {
        GameObject labelObj = new GameObject($"{name}Label");
        labelObj.transform.SetParent(transform);
        labelObj.transform.localPosition = localPosition;
        
        TextMeshPro tmp = labelObj.AddComponent<TextMeshPro>();
        tmp.text = "";
        tmp.fontSize = 3;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontStyle = FontStyles.Bold;
        
        // Make label always face camera
        labelObj.AddComponent<FaceCamera>();
        
        return tmp;
    }
    
    void Update()
    {
        UpdateLabels();
    }
    
    void UpdateLabels()
    {
        if (circuitComponent == null) return;
        
        // Check if labels still exist, recreate if needed
        if (voltageLabel == null || currentLabel == null || 
            (circuitComponent.ComponentType == ComponentType.Resistor || circuitComponent.ComponentType == ComponentType.Bulb) && resistanceLabel == null)
        {
            // Clean up any orphaned labels
            foreach (Transform child in transform)
            {
                if (child.name.Contains("Label"))
                {
                    Destroy(child.gameObject);
                }
            }
            // Recreate labels
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