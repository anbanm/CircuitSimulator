using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Main coordinator for the component palette system
/// Replaces the monolithic ComponentPalette with a modular architecture
/// </summary>
public class ComponentPaletteCoordinator : MonoBehaviour
{
    [Header("Legacy Settings (for migration)")]
    public Transform paletteContainer;
    public Button buttonPrefab;
    public GameObject batteryPrefab;
    public GameObject resistorPrefab;
    public GameObject bulbPrefab;
    public GameObject switchPrefab;
    public Transform canvasPlane;
    public float spacing = 2f;
    
    // Specialized managers
    private ComponentFactoryManager factoryManager;
    private PaletteUIManager uiManager;
    private CircuitControlManager controlManager;
    
    void Start()
    {
        InitializeManagers();
    }
    
    private void InitializeManagers()
    {
        // Get or create specialized managers
        factoryManager = GetComponent<ComponentFactoryManager>();
        if (factoryManager == null)
            factoryManager = gameObject.AddComponent<ComponentFactoryManager>();
            
        uiManager = GetComponent<PaletteUIManager>();
        if (uiManager == null)
            uiManager = gameObject.AddComponent<PaletteUIManager>();
            
        controlManager = GetComponent<CircuitControlManager>();
        if (controlManager == null)
            controlManager = gameObject.AddComponent<CircuitControlManager>();
        
        // Transfer legacy settings to factory manager
        factoryManager.batteryPrefab = this.batteryPrefab;
        factoryManager.resistorPrefab = this.resistorPrefab;
        factoryManager.bulbPrefab = this.bulbPrefab;
        factoryManager.switchPrefab = this.switchPrefab;
        factoryManager.Initialize(canvasPlane, spacing);
        
        // Transfer legacy settings to UI manager
        uiManager.paletteContainer = this.paletteContainer;
        uiManager.buttonPrefab = this.buttonPrefab;
        
        // Initialize managers
        controlManager.Initialize();
        uiManager.Initialize(factoryManager, controlManager);
        
        Debug.Log("ComponentPaletteCoordinator initialized with modular architecture");
    }
    
    #region Public API - Delegates to specialized managers
    
    public void PlaceBattery()
    {
        factoryManager?.CreateBattery();
    }
    
    public void PlaceResistor()
    {
        factoryManager?.CreateResistor();
    }
    
    public void PlaceBulb()
    {
        factoryManager?.CreateBulb();
    }
    
    public void PlaceSwitch()
    {
        factoryManager?.CreateSwitch();
    }
    
    public void RemoveComponent(GameObject component)
    {
        factoryManager?.RemoveComponent(component);
    }
    
    public void ClearAllComponents()
    {
        factoryManager?.ClearAllComponents();
    }
    
    #endregion
    
    #region Manager Access
    
    public ComponentFactoryManager GetFactoryManager()
    {
        return factoryManager;
    }
    
    public PaletteUIManager GetUIManager()
    {
        return uiManager;
    }
    
    public CircuitControlManager GetControlManager()
    {
        return controlManager;
    }
    
    #endregion
}