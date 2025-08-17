using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Manages real-time measurement displays and circuit metrics
/// Updates UI with current, voltage, power readings
/// </summary>
public class MeasurementDisplayManager : MonoBehaviour
{
    private WorkspaceManager workspaceManager;
    private List<MeasurementDisplay> measurementDisplays = new List<MeasurementDisplay>();
    
    // Update timing
    private float updateInterval = 0.5f;
    private float lastUpdateTime = 0f;
    
    public void Initialize(WorkspaceManager workspace)
    {
        workspaceManager = workspace;
        FindMeasurementDisplays();
        Debug.Log("MeasurementDisplayManager initialized");
    }
    
    public void Update()
    {
        // Update measurements at regular intervals
        if (Time.time - lastUpdateTime >= updateInterval)
        {
            UpdateAllMeasurements();
            lastUpdateTime = Time.time;
        }
    }
    
    private void FindMeasurementDisplays()
    {
        var displays = FindObjectsByType<MeasurementDisplay>(FindObjectsSortMode.None);
        measurementDisplays.AddRange(displays);
        Debug.Log($"Found {measurementDisplays.Count} measurement displays");
    }
    
    private void UpdateAllMeasurements()
    {
        var circuitManager = CircuitManager.Instance;
        if (circuitManager == null) return;
        
        var components = FindObjectsByType<CircuitComponent3D>(FindObjectsSortMode.None);
        
        // Calculate totals
        float totalCurrent = 0f;
        float totalVoltage = 0f;
        float totalPower = 0f;
        float totalResistance = 0f;
        
        // Find battery for total voltage
        CircuitComponent3D battery = null;
        foreach (var comp in components)
        {
            if (comp.ComponentType == ComponentType.Battery)
            {
                battery = comp;
                totalVoltage = comp.voltage;
                totalCurrent = comp.current;
                break;
            }
        }
        
        // Calculate total power and resistance
        if (battery != null && totalCurrent > 0.001f)
        {
            totalPower = totalVoltage * totalCurrent;
            totalResistance = totalVoltage / totalCurrent;
        }
        
        // Update display values
        UpdateMeasurementDisplay("Voltage", $"{totalVoltage:F2}V");
        UpdateMeasurementDisplay("Current", $"{totalCurrent:F3}A");
        UpdateMeasurementDisplay("Power", $"{totalPower:F3}W");
        UpdateMeasurementDisplay("Resistance", totalResistance > 0 ? $"{totalResistance:F2}Ω" : "∞Ω");
        
        // Update component count info
        int componentCount = circuitManager.ComponentCount;
        int wireCount = circuitManager.WireCount;
        UpdateStatusDisplay($"Components: {componentCount}, Wires: {wireCount}");
    }
    
    private void UpdateMeasurementDisplay(string label, string value)
    {
        foreach (var display in measurementDisplays)
        {
            if (display != null && display.Label == label)
            {
                display.UpdateValue(value);
                break;
            }
        }
    }
    
    private void UpdateStatusDisplay(string status)
    {
        // Find and update circuit status display
        var statusObjects = GameObject.FindGameObjectsWithTag("StatusDisplay");
        foreach (var obj in statusObjects)
        {
            var textMesh = obj.GetComponent<TMPro.TextMeshPro>();
            if (textMesh != null)
            {
                textMesh.text = $"Circuit Status: {status}";
                
                // Color based on circuit state
                var circuitManager = CircuitManager.Instance;
                if (circuitManager != null && circuitManager.ComponentCount > 0)
                {
                    textMesh.color = Color.green;
                }
                else
                {
                    textMesh.color = Color.yellow;
                }
                break;
            }
        }
    }
    
    public void RegisterMeasurementDisplay(MeasurementDisplay display)
    {
        if (!measurementDisplays.Contains(display))
        {
            measurementDisplays.Add(display);
            Debug.Log($"Registered measurement display: {display.Label}");
        }
    }
    
    public void UnregisterMeasurementDisplay(MeasurementDisplay display)
    {
        if (measurementDisplays.Contains(display))
        {
            measurementDisplays.Remove(display);
            Debug.Log($"Unregistered measurement display: {display.Label}");
        }
    }
    
    public void SetUpdateInterval(float interval)
    {
        updateInterval = Mathf.Max(0.1f, interval);
        Debug.Log($"Measurement update interval set to {updateInterval}s");
    }
}

/// <summary>
/// Individual measurement display component
/// Shows a label and value pair
/// </summary>
public class MeasurementDisplay : MonoBehaviour
{
    public string Label { get; private set; }
    private TMPro.TextMeshPro valueText;
    
    public void Initialize(string label, TMPro.TextMeshPro textComponent)
    {
        Label = label;
        valueText = textComponent;
        
        // Register with manager
        var manager = FindFirstObjectByType<MeasurementDisplayManager>();
        manager?.RegisterMeasurementDisplay(this);
        
        Debug.Log($"MeasurementDisplay initialized: {label}");
    }
    
    public void UpdateValue(string newValue)
    {
        if (valueText != null)
        {
            valueText.text = newValue;
        }
    }
    
    void OnDestroy()
    {
        // Unregister when destroyed
        var manager = FindFirstObjectByType<MeasurementDisplayManager>();
        manager?.UnregisterMeasurementDisplay(this);
    }
}

/// <summary>
/// Simple 3D UI button component
/// Handles click events for workspace buttons
/// </summary>
public class UIButton3D : MonoBehaviour
{
    public System.Action OnClick { get; set; }
    
    void OnMouseDown()
    {
        OnClick?.Invoke();
        
        // Visual feedback
        var renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            var originalColor = renderer.material.color;
            renderer.material.color = Color.yellow;
            
            // Reset color after a short delay
            Invoke(nameof(ResetColor), 0.2f);
        }
    }
    
    private void ResetColor()
    {
        var renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = Color.white;
        }
    }
}