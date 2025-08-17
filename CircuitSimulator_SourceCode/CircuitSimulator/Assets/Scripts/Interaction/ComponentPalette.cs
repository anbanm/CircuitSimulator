using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// DEPRECATED: ComponentPalette has been refactored into a modular architecture.
/// 
/// MIGRATION GUIDE:
/// - Use ComponentPaletteCoordinator for main coordination
/// - Use ComponentFactoryManager for component creation and placement
/// - Use PaletteUIManager for UI buttons and interactions
/// - Use CircuitControlManager for circuit operations
/// 
/// The new architecture provides better separation of concerns,
/// improved maintainability, and cleaner component management.
/// </summary>
[System.Obsolete("Use ComponentPaletteCoordinator and its specialized managers instead", true)]
public class ComponentPalette : MonoBehaviour
{
    // Legacy properties for backward compatibility during migration
    [Header("DEPRECATED - Use ComponentPaletteCoordinator instead")]
    public Transform paletteContainer;
    public Button buttonPrefab;
    
    [Header("DEPRECATED - Use ComponentFactoryManager instead")]
    public GameObject batteryPrefab;
    public GameObject resistorPrefab;
    public GameObject bulbPrefab;
    public GameObject switchPrefab;
    
    [Header("DEPRECATED - Use ComponentFactoryManager instead")]
    public Transform canvasPlane;
    public float spacing = 2f;
    
    void Start()
    {
        // AUTO-MIGRATION: Automatically migrate to new architecture
        PerformAutoMigration();
    }
    
    /// <summary>
    /// Automatically migrates legacy ComponentPalette to the new modular architecture
    /// </summary>
    private void PerformAutoMigration()
    {
        Debug.LogWarning("ComponentPalette is deprecated. Auto-migrating to ComponentPaletteCoordinator architecture...");
        
        // Create or find ComponentPaletteCoordinator
        ComponentPaletteCoordinator coordinator = FindFirstObjectByType<ComponentPaletteCoordinator>();
        if (coordinator == null)
        {
            GameObject coordinatorObj = new GameObject("ComponentPaletteCoordinator");
            coordinator = coordinatorObj.AddComponent<ComponentPaletteCoordinator>();
            
            // Transfer legacy settings
            coordinator.paletteContainer = this.paletteContainer;
            coordinator.buttonPrefab = this.buttonPrefab;
            coordinator.batteryPrefab = this.batteryPrefab;
            coordinator.resistorPrefab = this.resistorPrefab;
            coordinator.bulbPrefab = this.bulbPrefab;
            coordinator.switchPrefab = this.switchPrefab;
            coordinator.canvasPlane = this.canvasPlane;
            coordinator.spacing = this.spacing;
        }
        
        Debug.Log("✅ Migration complete. ComponentPaletteCoordinator is now handling palette functionality.");
        
        // Disable this deprecated component
        this.enabled = false;
    }
    
    // DEPRECATED METHODS - Kept for reference only
    // All functionality has been moved to specialized managers
    
    #region Deprecated Methods - Use ComponentPaletteCoordinator instead
    
    [System.Obsolete("Use PaletteUIManager.HandleKeyboardShortcuts() instead")]
    void Update()
    {
        // Keyboard shortcuts moved to PaletteUIManager
    }
    
    // All remaining methods are deprecated and moved to specialized managers
    
    [System.Obsolete("Use ComponentFactoryManager.CreateBattery() instead")]
    public void PlaceBattery()
    {
        Debug.LogError("PlaceBattery() is deprecated. Use ComponentFactoryManager instead.");
    }
    
    [System.Obsolete("Use ComponentFactoryManager.CreateResistor() instead")]
    public void PlaceResistor()
    {
        Debug.LogError("PlaceResistor() is deprecated. Use ComponentFactoryManager instead.");
    }
    
    [System.Obsolete("Use ComponentFactoryManager.CreateBulb() instead")]
    public void PlaceBulb()
    {
        Debug.LogError("PlaceBulb() is deprecated. Use ComponentFactoryManager instead.");
    }
    
    [System.Obsolete("Use ComponentFactoryManager.CreateSwitch() instead")]
    public void PlaceSwitch()
    {
        Debug.LogError("PlaceSwitch() is deprecated. Use ComponentFactoryManager instead.");
    }
    
    [System.Obsolete("Use ComponentFactoryManager.RemoveComponent() instead")]
    public void RemoveComponent(GameObject component)
    {
        Debug.LogError("RemoveComponent() is deprecated. Use ComponentFactoryManager instead.");
    }
    
    #endregion
    
    #region Migration Note
    
    /// <summary>
    /// All methods from this class have been moved to specialized managers:
    /// 
    /// ComponentPaletteCoordinator.cs - Main coordination
    /// ComponentFactoryManager.cs - Component creation and placement
    /// PaletteUIManager.cs - UI buttons and interactions
    /// CircuitControlManager.cs - Circuit operations
    /// 
    /// This provides better separation of concerns and maintainability.
    /// </summary>
    
    #endregion
}
