using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Manages persistent labels for all components
/// </summary>
public class LabelManager : MonoBehaviour
{
    private static LabelManager instance;
    public static LabelManager Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject manager = new GameObject("LabelManager");
                instance = manager.AddComponent<LabelManager>();
                DontDestroyOnLoad(manager);
            }
            return instance;
        }
    }
    
    private Dictionary<CircuitComponent3D, List<PersistentLabel>> componentLabels = new Dictionary<CircuitComponent3D, List<PersistentLabel>>();
    
    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        // Check for labels every frame
        InvokeRepeating("EnsureAllLabelsExist", 0.5f, 0.1f);
    }
    
    public void RegisterComponent(CircuitComponent3D component)
    {
        if (component == null || componentLabels.ContainsKey(component)) return;
        
        List<PersistentLabel> labels = new List<PersistentLabel>();
        
        // Create voltage label
        labels.Add(PersistentLabel.Create(component, "voltage", 
            new Vector3(0, 1.5f, 0), 
            new Color(0.9f, 0.2f, 0.2f)));
        
        // Create current label
        labels.Add(PersistentLabel.Create(component, "current", 
            new Vector3(0, 1.2f, 0), 
            new Color(0.2f, 0.6f, 0.9f)));
        
        // Create resistance label if applicable
        if (component.ComponentType == ComponentType.Resistor || 
            component.ComponentType == ComponentType.Bulb)
        {
            labels.Add(PersistentLabel.Create(component, "resistance", 
                new Vector3(0, 0.9f, 0), 
                new Color(0.7f, 0.7f, 0.3f)));
        }
        
        componentLabels[component] = labels;
        Debug.Log($"Registered labels for {component.name}");
    }
    
    void EnsureAllLabelsExist()
    {
        // Find all components in scene
        CircuitComponent3D[] allComponents = FindObjectsOfType<CircuitComponent3D>();
        
        foreach (var component in allComponents)
        {
            if (component != null && !componentLabels.ContainsKey(component))
            {
                RegisterComponent(component);
            }
        }
        
        // Clean up null entries
        List<CircuitComponent3D> toRemove = new List<CircuitComponent3D>();
        foreach (var kvp in componentLabels)
        {
            if (kvp.Key == null)
            {
                toRemove.Add(kvp.Key);
                // Destroy orphaned labels
                foreach (var label in kvp.Value)
                {
                    if (label != null && label.gameObject != null)
                    {
                        Destroy(label.gameObject);
                    }
                }
            }
        }
        
        foreach (var key in toRemove)
        {
            componentLabels.Remove(key);
        }
    }
    
    public void UnregisterComponent(CircuitComponent3D component)
    {
        if (componentLabels.ContainsKey(component))
        {
            foreach (var label in componentLabels[component])
            {
                if (label != null && label.gameObject != null)
                {
                    Destroy(label.gameObject);
                }
            }
            componentLabels.Remove(component);
        }
    }
}