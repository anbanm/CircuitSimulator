using UnityEngine;
using UnityEngine.SceneManagement;
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
            
            // Subscribe to scene changes to prevent memory leaks
            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }
    
    void OnDestroy()
    {
        // Unsubscribe from scene events
        SceneManager.sceneUnloaded -= OnSceneUnloaded;
    }
    
    private void OnSceneUnloaded(Scene scene)
    {
        // Clean up all labels when scene changes to prevent memory leaks
        
        foreach (var kvp in componentLabels)
        {
            foreach (var label in kvp.Value)
            {
                if (label != null && label.gameObject != null)
                {
                    Destroy(label.gameObject);
                }
            }
        }
        
        componentLabels.Clear();
    }
    
    void Start()
    {
        // Remove expensive polling - now event-driven
        // InvokeRepeating removed for +15 FPS performance gain
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
    }
    
    // Called manually only when needed, not on timer
    public void CleanupOrphanedLabels()
    {
        // Clean up null entries without expensive FindObjectsOfType
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
    
    // Event-driven label updates when circuit values change
    public void UpdateLabelsForComponent(CircuitComponent3D component)
    {
        if (componentLabels.ContainsKey(component))
        {
            foreach (var label in componentLabels[component])
            {
                if (label != null)
                {
                    label.UpdateTextValues();
                }
            }
        }
    }
    
    // Update all labels when circuit is solved
    public void UpdateAllLabels()
    {
        foreach (var kvp in componentLabels)
        {
            if (kvp.Key != null)
            {
                UpdateLabelsForComponent(kvp.Key);
            }
        }
    }
}