using UnityEngine;

/// <summary>
/// One-time cleanup script to remove old ComponentValueDisplay components
/// </summary>
public class CleanupOldLabels : MonoBehaviour
{
    void Start()
    {
        // Find and remove all old ComponentValueDisplay components
        ComponentValueDisplay[] oldDisplays = FindObjectsOfType<ComponentValueDisplay>();
        foreach (var display in oldDisplays)
        {
            Debug.Log($"Removing old ComponentValueDisplay from {display.gameObject.name}");
            
            // Destroy any child labels
            foreach (Transform child in display.transform)
            {
                if (child.name.Contains("Label"))
                {
                    Destroy(child.gameObject);
                }
            }
            
            // Destroy the component itself
            Destroy(display);
        }
        
        if (oldDisplays.Length > 0)
        {
            Debug.Log($"Cleaned up {oldDisplays.Length} old label components");
        }
        
        // Self-destruct after cleanup
        Destroy(gameObject, 1f);
    }
}