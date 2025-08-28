using UnityEngine;

/// <summary>
/// A completely independent label that manages itself and never gets destroyed
/// </summary>
public class PersistentLabel : MonoBehaviour
{
    private TextMesh textMesh;
    private CircuitComponent3D targetComponent;
    private string labelType; // "voltage", "current", or "resistance"
    private Vector3 offset;
    private Color labelColor;
    
    public static PersistentLabel Create(CircuitComponent3D component, string type, Vector3 offset, Color color)
    {
        // Create independent GameObject that won't be destroyed with parent
        GameObject labelObj = new GameObject($"{component.name}_{type}Label");
        
        // Don't parent to component - keep it independent
        labelObj.transform.position = component.transform.position + offset;
        labelObj.transform.localScale = Vector3.one * 0.5f;
        
        // Add persistent label component
        PersistentLabel label = labelObj.AddComponent<PersistentLabel>();
        label.targetComponent = component;
        label.labelType = type;
        label.offset = offset;
        label.labelColor = color;
        
        // Create text mesh
        label.textMesh = labelObj.AddComponent<TextMesh>();
        label.textMesh.fontSize = 36;
        label.textMesh.color = color;
        label.textMesh.anchor = TextAnchor.MiddleCenter;
        label.textMesh.alignment = TextAlignment.Center;
        label.textMesh.characterSize = 0.3f;
        
        // Add face camera behavior
        labelObj.AddComponent<FaceCamera>();
        
        return label;
    }
    
    void Update()
    {
        // If target component is destroyed, destroy this label
        if (targetComponent == null)
        {
            Destroy(gameObject);
            return;
        }
        
        // Follow the component
        transform.position = targetComponent.transform.position + offset;
        
        // Update text based on type
        UpdateText();
    }
    
    void UpdateText()
    {
        if (textMesh == null || targetComponent == null) return;
        
        switch (labelType)
        {
            case "voltage":
                if (targetComponent.ComponentType == ComponentType.Battery)
                {
                    textMesh.text = $"{targetComponent.voltage:F1}V";
                }
                else if (Mathf.Abs(targetComponent.voltageDrop) > 0.01f)
                {
                    textMesh.text = $"{targetComponent.voltageDrop:F2}V";
                }
                else
                {
                    textMesh.text = "";
                }
                break;
                
            case "current":
                if (Mathf.Abs(targetComponent.current) > 0.01f)
                {
                    textMesh.text = $"{targetComponent.current:F2}A";
                }
                else
                {
                    textMesh.text = "";
                }
                break;
                
            case "resistance":
                if (targetComponent.ComponentType == ComponentType.Resistor || 
                    targetComponent.ComponentType == ComponentType.Bulb)
                {
                    textMesh.text = $"{targetComponent.resistance:F1}Ω";
                }
                else
                {
                    textMesh.text = "";
                }
                break;
        }
    }
}