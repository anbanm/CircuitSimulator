using UnityEngine;

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
    private TextMesh currentLabel;
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
        labelObj.transform.SetParent(transform, false);
        labelObj.transform.localScale = Vector3.one * 0.4f;
        
        currentLabel = labelObj.AddComponent<TextMesh>();
        currentLabel.text = "";
        currentLabel.fontSize = 30;
        currentLabel.color = currentColor;
        currentLabel.anchor = TextAnchor.MiddleCenter;
        currentLabel.alignment = TextAlignment.Center;
        currentLabel.characterSize = 0.25f;
        
        // Make label face camera (manual rotation in Update)
        // labelObj.AddComponent<FaceCamera>(); // Removed - missing component
    }
    
    // Performance optimization: throttle display updates
    private float updateInterval = 0.1f; // 10 FPS update rate
    private float nextUpdateTime = 0f;
    private float lastDisplayedCurrent = float.MinValue;
    
    void Update()
    {
        // Early exit if components are missing
        if (currentLabel == null || circuitWire == null) return;
        
        // Only update if enough time has passed or current has changed significantly
        if (Time.time >= nextUpdateTime || Mathf.Abs(circuitWire.current - lastDisplayedCurrent) > 0.001f)
        {
            UpdateCurrentDisplay();
            PositionLabel();
            lastDisplayedCurrent = circuitWire.current;
            nextUpdateTime = Time.time + updateInterval;
        }
    }
    
    void UpdateCurrentDisplay()
    {
        if (currentLabel == null || circuitWire == null) return;

        // Show current if significant
        if (Mathf.Abs(circuitWire.current) > 0.01f)
        {
            // EDUCATIONAL FIX: Display absolute value (magnitude only) to avoid confusing students
            // The sign is used internally for animation direction but shouldn't be shown in UI
            currentLabel.text = $"{Mathf.Abs(circuitWire.current):F2}A";
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