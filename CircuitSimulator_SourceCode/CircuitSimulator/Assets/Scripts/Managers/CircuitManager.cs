using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Core circuit manager - handles component and wire registration
/// Singleton pattern for global access
/// </summary>
public class CircuitManager : MonoBehaviour
{
    [Header("Circuit Settings")]
    public bool autoSolve = true;
    public float solveInterval = 0.5f;
    public bool eventBasedSolving = true;
    
    [Header("Status")]
    [SerializeField] private int componentCount = 0;
    [SerializeField] private int wireCount = 0;
    
    // Core data storage
    private List<CircuitComponent3D> components = new List<CircuitComponent3D>();
    private List<GameObject> wires = new List<GameObject>();
    
    // Singleton pattern
    private static CircuitManager instance;
    public static CircuitManager Instance => instance;
    
    // Manager references
    private CircuitSolverManager solverManager;
    private CircuitNodeManager nodeManager;
    private CircuitDebugManager debugManager;
    private CircuitEventManager eventManager;
    
    // Public properties
    public List<CircuitComponent3D> Components => components;
    public List<GameObject> Wires => wires;
    public int ComponentCount => componentCount;
    public int WireCount => wireCount;
    
    void Awake()
    {
        // Singleton setup
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Debug.LogWarning("Multiple CircuitManager instances found! Destroying duplicate.");
            Destroy(gameObject);
            return;
        }
        
        // Initialize managers
        InitializeManagers();
    }
    
    void Start()
    {
        // Initialize solver and register existing components
        solverManager?.Initialize();
        debugManager?.Initialize();
        RegisterExistingComponents();
    }
    
    void Update()
    {
        // Delegate update logic to managers
        solverManager?.Update();
        eventManager?.Update();
    }
    
    private void InitializeManagers()
    {
        // Create or get manager components
        solverManager = GetComponent<CircuitSolverManager>();
        if (solverManager == null)
            solverManager = gameObject.AddComponent<CircuitSolverManager>();
            
        nodeManager = GetComponent<CircuitNodeManager>();
        if (nodeManager == null)
            nodeManager = gameObject.AddComponent<CircuitNodeManager>();
            
        debugManager = GetComponent<CircuitDebugManager>();
        if (debugManager == null)
            debugManager = gameObject.AddComponent<CircuitDebugManager>();
            
        eventManager = GetComponent<CircuitEventManager>();
        if (eventManager == null)
            eventManager = gameObject.AddComponent<CircuitEventManager>();
    }
    
    #region Component Management
    
    public void RegisterComponent(CircuitComponent3D component)
    {
        if (component == null || components.Contains(component)) return;
        
        components.Add(component);
        componentCount = components.Count;
        
        Debug.Log($"Registered component: {component.name} (Total: {componentCount})");
        
        // Notify managers
        eventManager?.OnComponentRegistered(component);
        MarkCircuitChanged();
    }
    
    public void UnregisterComponent(CircuitComponent3D component)
    {
        if (component == null || !components.Contains(component)) return;
        
        components.Remove(component);
        componentCount = components.Count;
        
        Debug.Log($"Unregistered component: {component.name} (Total: {componentCount})");
        
        // Notify managers
        eventManager?.OnComponentUnregistered(component);
        MarkCircuitChanged();
    }
    
    public void RegisterWire(GameObject wire)
    {
        if (wire == null || wires.Contains(wire)) return;
        
        wires.Add(wire);
        wireCount = wires.Count;
        
        Debug.Log($"Registered wire: {wire.name} (Total: {wireCount})");
        
        // Notify managers
        eventManager?.OnWireRegistered(wire);
        MarkCircuitChanged();
    }
    
    public void UnregisterWire(GameObject wire)
    {
        if (wire == null || !wires.Contains(wire)) return;
        
        wires.Remove(wire);
        wireCount = wires.Count;
        
        Debug.Log($"Unregistered wire: {wire.name} (Total: {wireCount})");
        
        // Notify managers
        eventManager?.OnWireUnregistered(wire);
        MarkCircuitChanged();
    }
    
    public void ClearAllComponents()
    {
        Debug.Log("Clearing all component and wire references");
        
        // Clear component list
        components.Clear();
        componentCount = 0;
        
        // Clear wire list
        wires.Clear();
        wireCount = 0;
        
        // Stop any pending solves
        if (solverManager != null)
        {
            solverManager.ClearSolverCache();
        }
        
        // Notify event system
        if (eventManager != null)
        {
            eventManager.OnCircuitChanged();
        }
        
        Debug.Log("All references cleared");
    }
    
    #endregion
    
    #region Circuit State Management
    
    public void MarkCircuitChanged()
    {
        // Notify all managers that circuit has changed
        solverManager?.MarkForSolving();
        eventManager?.OnCircuitChanged();
        debugManager?.LogCircuitChange();
    }
    
    public void SolveCircuit()
    {
        solverManager?.SolveCircuit();
    }
    
    public void SolveCircuitManually()
    {
        solverManager?.SolveCircuitManually();
    }
    
    #endregion
    
    #region Initialization
    
    private void RegisterExistingComponents()
    {
        // Find and register any existing components in the scene
        CircuitComponent3D[] existingComponents = FindObjectsByType<CircuitComponent3D>(FindObjectsSortMode.None);
        foreach (var comp in existingComponents)
        {
            RegisterComponent(comp);
        }
        
        Debug.Log($"Found and registered {existingComponents.Length} existing components");
    }
    
    #endregion
    
    #region Public API for Debugging
    
    public void SaveDebugReport()
    {
        debugManager?.SaveDebugReport();
    }
    
    public void DebugComponentRegistration()
    {
        debugManager?.DebugComponentRegistration();
    }
    
    public void ValidateAndTestCircuit()
    {
        debugManager?.ValidateAndTestCircuit();
    }
    
    public void TestCircuitComponents()
    {
        debugManager?.TestCircuitComponents();
    }
    
    #endregion
}