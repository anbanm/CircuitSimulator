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
        
        // Face camera behavior (manual rotation in Update)
        // labelObj.AddComponent<FaceCamera>(); // Removed - missing component
        
        return label;
    }
    
    // Performance optimization: throttle position updates
    private float updateInterval = 0.1f; // 10 FPS update rate
    private float nextUpdateTime = 0f;
    private Vector3 lastTargetPosition;
    
    void Update()
    {
        // If target component is destroyed, destroy this label
        if (targetComponent == null)
        {
            CleanupAndDestroy();
            return;
        }
        
        // Only update position if component has moved or enough time has passed
        Vector3 currentTargetPosition = targetComponent.transform.position;
        if (Time.time >= nextUpdateTime || Vector3.Distance(currentTargetPosition, lastTargetPosition) > 0.01f)
        {
            transform.position = currentTargetPosition + offset;
            lastTargetPosition = currentTargetPosition;
            nextUpdateTime = Time.time + updateInterval;
        }
    }
    
    void OnDestroy()
    {
        // Ensure proper cleanup when label is destroyed
        CleanupReferences();
    }
    
    private void CleanupAndDestroy()
    {
        CleanupReferences();
        Destroy(gameObject);
    }
    
    private void CleanupReferences()
    {
        // Clear references to prevent memory leaks
        targetComponent = null;
        textMesh = null;
        
        // Notify LabelManager if it exists
        if (LabelManager.Instance != null)
        {
            // The LabelManager will handle removing this from its dictionary
        }
    }
    
    // Event-driven text update instead of every frame
    public void UpdateTextValues()
    {
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