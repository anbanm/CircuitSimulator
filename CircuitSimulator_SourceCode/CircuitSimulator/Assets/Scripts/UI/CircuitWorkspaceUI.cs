using UnityEngine;

/// <summary>
/// DEPRECATED: CircuitWorkspaceUI has been refactored into a modular architecture.
/// 
/// MIGRATION GUIDE:
/// - Use WorkspaceManager for core workspace functionality
/// - Use UILayoutManager for UI layout and panels  
/// - Use MeasurementDisplayManager for measurement displays
/// - Use ARWorkspaceAdapter for AR-specific features
/// 
/// The new architecture provides better separation of concerns,
/// improved maintainability, and enhanced AR support.
/// </summary>
[System.Obsolete("Use WorkspaceManager and its specialized managers instead", true)]
public class CircuitWorkspaceUI : MonoBehaviour
{
    // Legacy properties for backward compatibility during migration
    [Header("DEPRECATED - Use WorkspaceManager instead")]
    public Transform workspacePlane;
    public float workspaceSize = 10f;
    
    [Header("DEPRECATED - Use UILayoutManager instead")]
    public Vector3 toolPanelOffset = new Vector3(-6f, 0.1f, 0f);
    public Vector3 infoPanelOffset = new Vector3(6f, 0.1f, 0f);
    public Vector3 controlPanelOffset = new Vector3(0f, 0.1f, -6f);
    
    [Header("DEPRECATED - Use ARWorkspaceAdapter instead")]
    public bool optimizeForAR = true;
    public float arUIScale = 0.7f;
    public float maxViewDistance = 5f;
    
    void Start()
    {
        // AUTO-MIGRATION: Automatically migrate to new architecture
        PerformAutoMigration();
    }
    
    /// <summary>
    /// Automatically migrates legacy CircuitWorkspaceUI to the new modular architecture
    /// </summary>
    private void PerformAutoMigration()
    {
        Debug.LogWarning("CircuitWorkspaceUI is deprecated. Auto-migrating to WorkspaceManager architecture...");
        
        // Create or find WorkspaceManager
        WorkspaceManager workspaceManager = FindFirstObjectByType<WorkspaceManager>();
        if (workspaceManager == null)
        {
            GameObject managerObj = new GameObject("WorkspaceManager");
            workspaceManager = managerObj.AddComponent<WorkspaceManager>();
            
            // Transfer legacy settings
            workspaceManager.workspacePlane = this.workspacePlane;
            workspaceManager.workspaceSize = this.workspaceSize;
            workspaceManager.optimizeForAR = this.optimizeForAR;
            workspaceManager.arUIScale = this.arUIScale;
            workspaceManager.maxViewDistance = this.maxViewDistance;
        }
        
        Debug.Log("✅ Migration complete. WorkspaceManager is now handling workspace functionality.");
        
        // Disable this deprecated component
        this.enabled = false;
    }
    
    // DEPRECATED METHODS - Kept for reference only
    // All functionality has been moved to specialized managers
    
    #region Deprecated Methods - Use WorkspaceManager instead
    
    [System.Obsolete("Use UILayoutManager.CreateToolPanel() instead")]
    void CreateToolPanel()
    {
        Debug.LogError("CreateToolPanel() is deprecated. Use UILayoutManager instead.");
    }
    
    [System.Obsolete("Use MeasurementDisplayManager instead")]
    void CreateInfoPanel()
    {
        Debug.LogError("CreateInfoPanel() is deprecated. Use MeasurementDisplayManager instead.");
    }
    
    [System.Obsolete("Use UILayoutManager instead")]
    void CreateControlPanel()
    {
        Debug.LogError("CreateControlPanel() is deprecated. Use UILayoutManager instead.");
    }
    
    #endregion
    
    #region Migration Note
    
    /// <summary>
    /// All methods from this class have been moved to specialized managers:
    /// 
    /// WorkspaceManager.cs - Core workspace functionality
    /// UILayoutManager.cs - UI layout and panels  
    /// MeasurementDisplayManager.cs - Measurement displays
    /// ARWorkspaceAdapter.cs - AR-specific features
    /// 
    /// This provides better separation of concerns and maintainability.
    /// </summary>
    
    #endregion
}