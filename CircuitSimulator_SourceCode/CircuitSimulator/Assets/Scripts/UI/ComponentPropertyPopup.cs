// Component property editing popup UI
// Future implementation for Grade 7-level circuit understanding

using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Provides a popup interface for editing circuit component properties
/// Allows students to modify resistance values, voltage settings, etc.
/// </summary>
public class ComponentPropertyPopup : MonoBehaviour
{
    [Header("UI References")]
    public GameObject popupPanel;
    public Text componentNameText;
    public InputField resistanceInput;
    public InputField voltageInput;
    public Button saveButton;
    public Button cancelButton;
    public Button deleteButton;
    
    [Header("Current Component")]
    private CircuitComponent3D currentComponent;
    private CircuitComponent currentCircuitComponent;
    
    void Start()
    {
        // Initialize UI event handlers
        if (saveButton != null)
            saveButton.onClick.AddListener(SaveProperties);
        
        if (cancelButton != null)
            cancelButton.onClick.AddListener(CancelEdit);
        
        if (deleteButton != null)
            deleteButton.onClick.AddListener(DeleteComponent);
        
        // Initially hide the popup
        HidePopup();
    }
    
    /// <summary>
    /// Shows the property editor for a specific component
    /// </summary>
    public void ShowPopupForComponent(CircuitComponent3D component3D)
    {
        currentComponent = component3D;
        currentCircuitComponent = component3D.GetComponent<CircuitComponent>();
        
        if (currentCircuitComponent == null)
        {
            Debug.LogWarning("[ComponentPropertyPopup] No CircuitComponent found on selected object");
            return;
        }
        
        // Update UI with current component properties
        UpdateUIWithComponentData();
        
        // Show the popup
        if (popupPanel != null)
            popupPanel.SetActive(true);
        
        Debug.Log($"[ComponentPropertyPopup] Showing properties for {currentCircuitComponent.Id}");
    }
    
    /// <summary>
    /// Hides the property editor popup
    /// </summary>
    public void HidePopup()
    {
        if (popupPanel != null)
            popupPanel.SetActive(false);
        
        currentComponent = null;
        currentCircuitComponent = null;
    }
    
    /// <summary>
    /// Updates the UI fields with the current component's data
    /// </summary>
    private void UpdateUIWithComponentData()
    {
        if (currentCircuitComponent == null) return;
        
        // Set component name
        if (componentNameText != null)
            componentNameText.text = currentCircuitComponent.Id;
        
        // Set resistance value (if applicable)
        if (resistanceInput != null)
        {
            if (currentCircuitComponent is Resistor || currentCircuitComponent is Bulb)
            {
                resistanceInput.text = currentCircuitComponent.Resistance.ToString("F1");
                resistanceInput.interactable = true;
            }
            else
            {
                resistanceInput.text = "N/A";
                resistanceInput.interactable = false;
            }
        }
        
        // Set voltage value (if applicable)
        if (voltageInput != null)
        {
            if (currentCircuitComponent is Battery battery)
            {
                voltageInput.text = battery.Voltage.ToString("F1");
                voltageInput.interactable = true;
            }
            else
            {
                voltageInput.text = "N/A";
                voltageInput.interactable = false;
            }
        }
    }
    
    /// <summary>
    /// Saves the modified properties to the component
    /// </summary>
    private void SaveProperties()
    {
        if (currentCircuitComponent == null) return;
        
        try
        {
            // Save resistance value
            if (resistanceInput != null && resistanceInput.interactable)
            {
                if (float.TryParse(resistanceInput.text, out float resistance))
                {
                    if (resistance > 0f)
                    {
                        // TODO: Update component resistance
                        // This requires modifying the CircuitComponent classes to allow runtime changes
                        Debug.Log($"[ComponentPropertyPopup] Would set resistance to {resistance}Ω");
                    }
                    else
                    {
                        Debug.LogWarning("[ComponentPropertyPopup] Resistance must be positive");
                        return;
                    }
                }
                else
                {
                    Debug.LogWarning("[ComponentPropertyPopup] Invalid resistance value");
                    return;
                }
            }
            
            // Save voltage value
            if (voltageInput != null && voltageInput.interactable)
            {
                if (float.TryParse(voltageInput.text, out float voltage))
                {
                    if (voltage > 0f)
                    {
                        // TODO: Update battery voltage
                        Debug.Log($"[ComponentPropertyPopup] Would set voltage to {voltage}V");
                    }
                    else
                    {
                        Debug.LogWarning("[ComponentPropertyPopup] Voltage must be positive");
                        return;
                    }
                }
                else
                {
                    Debug.LogWarning("[ComponentPropertyPopup] Invalid voltage value");
                    return;
                }
            }
            
            Debug.Log($"[ComponentPropertyPopup] Properties saved for {currentCircuitComponent.Id}");
            
            // TODO: Trigger circuit re-calculation if needed
            // FindObjectOfType<Circuit3DManager>()?.RecalculateCircuit();
            
        }
        catch (Exception e)
        {
            Debug.LogError($"[ComponentPropertyPopup] Error saving properties: {e.Message}");
        }
        
        HidePopup();
    }
    
    /// <summary>
    /// Cancels editing and closes the popup
    /// </summary>
    private void CancelEdit()
    {
        Debug.Log("[ComponentPropertyPopup] Edit cancelled");
        HidePopup();
    }
    
    /// <summary>
    /// Deletes the current component from the circuit
    /// </summary>
    private void DeleteComponent()
    {
        if (currentComponent == null) return;
        
        Debug.Log($"[ComponentPropertyPopup] Deleting component {currentCircuitComponent?.Id}");
        
        // TODO: Remove component from circuit and destroy GameObject
        // This should also remove any connected wires
        
        // For now, just destroy the GameObject
        Destroy(currentComponent.gameObject);
        
        HidePopup();
    }
    
    /// <summary>
    /// Validates input values in real-time
    /// </summary>
    public void ValidateInputs()
    {
        bool isValid = true;
        
        // Validate resistance input
        if (resistanceInput != null && resistanceInput.interactable)
        {
            if (!float.TryParse(resistanceInput.text, out float resistance) || resistance <= 0f)
            {
                isValid = false;
            }
        }
        
        // Validate voltage input
        if (voltageInput != null && voltageInput.interactable)
        {
            if (!float.TryParse(voltageInput.text, out float voltage) || voltage <= 0f)
            {
                isValid = false;
            }
        }
        
        // Enable/disable save button based on validation
        if (saveButton != null)
            saveButton.interactable = isValid;
    }
}
