using UnityEngine;
using TMPro;

/// <summary>
/// Displays current flow on wires
/// Shows amperage directly on wire connections
/// </summary>
public class WireValueDisplay : MonoBehaviour
{
    [Header("Display Settings")]
    public Color currentColor = new Color(0.2f, 0.8f, 0.2f);
    public float fontSize = 2.5f;
    
    private CircuitWire circuitWire;
    private TextMeshPro currentLabel;
    private LineRenderer lineRenderer;
    
    void Start()
    {
        circuitWire = GetComponent<CircuitWire>();
        lineRenderer = GetComponent<LineRenderer>();
        
        if (circuitWire != null)
        {
            CreateCurrentLabel();
        }
    }
    
    void CreateCurrentLabel()
    {
        GameObject labelObj = new GameObject("CurrentLabel");
        labelObj.transform.SetParent(transform);
        
        currentLabel = labelObj.AddComponent<TextMeshPro>();
        currentLabel.text = "";
        currentLabel.fontSize = fontSize;
        currentLabel.color = currentColor;
        currentLabel.alignment = TextAlignmentOptions.Center;
        currentLabel.fontStyle = FontStyles.Bold;
        
        // Make label face camera
        labelObj.AddComponent<FaceCamera>();
    }
    
    void Update()
    {
        UpdateCurrentDisplay();
        PositionLabel();
    }
    
    void UpdateCurrentDisplay()
    {
        if (currentLabel == null || circuitWire == null) return;
        
        // Show current if significant
        if (Mathf.Abs(circuitWire.current) > 0.01f)
        {
            currentLabel.text = $"{circuitWire.current:F2}A";
        }
        else
        {
            currentLabel.text = "";
        }
    }
    
    void PositionLabel()
    {
        if (currentLabel == null || lineRenderer == null) return;
        
        // Position at middle of wire
        if (lineRenderer.positionCount >= 2)
        {
            Vector3 start = lineRenderer.GetPosition(0);
            Vector3 end = lineRenderer.GetPosition(1);
            Vector3 midPoint = (start + end) * 0.5f;
            
            // Offset slightly above the wire
            currentLabel.transform.position = midPoint + Vector3.up * 0.5f;
        }
    }
}