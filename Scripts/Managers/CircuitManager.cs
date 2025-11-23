using UnityEngine;
using System.Collections.Generic;
using CircuitSimulator.Services;

/// <summary>
/// Core circuit manager - handles component and wire registration
/// Implements ICircuitManager interface and integrates with ServiceLocator
/// </summary>
public class CircuitManager : MonoBehaviour, ICircuitManager
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
    private ComponentTerminalManager terminalManager;
    
    // Public properties
    public List<CircuitComponent3D> Components => components;
    public List<GameObject> Wires => wires;
    public int ComponentCount => componentCount;
    public int WireCount => wireCount;
    
    // ICircuitManager Events
    public System.Action<CircuitComponent3D> OnComponentRegistered { get; set; }
    public System.Action<CircuitComponent3D> OnComponentUnregistered { get; set; }
    public System.Action<CircuitWire> OnWireRegistered { get; set; }
    public System.Action<CircuitWire> OnWireUnregistered { get; set; }
    public System.Action OnCircuitChanged { get; set; }

    void Awake()
    {
        // Singleton setup
        if (instance == null)
        {
            instance = this;

            // Register with ServiceLocator
            ServiceLocator.Instance.Register<ICircuitManager>(this);

            // Legacy ComponentRegistry support
            if (ComponentRegistry.Instance != null)
            {
                ComponentRegistry.Instance.RegisterManager<CircuitManager>(this);
            }
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
        terminalManager?.Initialize();
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
            
        terminalManager = GetComponent<ComponentTerminalManager>();
        if (terminalManager == null)
            terminalManager = gameObject.AddComponent<ComponentTerminalManager>();
    }
    
    #region Component Management
    
    public void RegisterComponent(CircuitComponent3D component)
    {
        if (component == null || components.Contains(component)) return;

        components.Add(component);
        componentCount = components.Count;


        // Setup terminals for the component
        terminalManager?.OnComponentRegistered(component);

        // Notify managers
        eventManager?.OnComponentRegistered(component);

        // Fire interface event
        OnComponentRegistered?.Invoke(component);

        MarkCircuitChanged();
    }
    
    public void UnregisterComponent(CircuitComponent3D component)
    {
        if (component == null || !components.Contains(component)) return;

        components.Remove(component);
        componentCount = components.Count;


        // Remove terminals for the component
        terminalManager?.OnComponentUnregistered(component);

        // Notify managers
        eventManager?.OnComponentUnregistered(component);

        // Fire interface event
        OnComponentUnregistered?.Invoke(component);

        MarkCircuitChanged();
    }
    
    /// <summary>
    /// Check if a wire is already registered with the CircuitManager
    /// </summary>
    public bool IsWireRegistered(GameObject wire)
    {
        return wire != null && wires.Contains(wire);
    }

    public void RegisterWire(GameObject wire)
    {
        if (wire == null || wires.Contains(wire))
        {
            if (wire != null && wires.Contains(wire))
            {
                Debug.LogWarning($"⚠️ Wire {wire.name} is ALREADY registered in CircuitManager! Skipping duplicate registration.");
            }
            return;
        }

        wires.Add(wire);
        wireCount = wires.Count;


        // Notify managers
        eventManager?.OnWireRegistered(wire);
        MarkCircuitChanged();
    }
    
    public void UnregisterWire(GameObject wire)
    {
        if (wire == null || !wires.Contains(wire)) return;
        
        wires.Remove(wire);
        wireCount = wires.Count;
        
        
        // Notify managers
        eventManager?.OnWireUnregistered(wire);
        MarkCircuitChanged();
    }

    // Interface implementations
    public IReadOnlyList<CircuitComponent3D> GetAllComponents()
    {
        return components.AsReadOnly();
    }

    public IReadOnlyList<CircuitWire> GetAllWires()
    {
        var circuitWires = new List<CircuitWire>();
        foreach (var wire in wires)
        {
            var circuitWire = wire?.GetComponent<CircuitWire>();
            if (circuitWire != null)
            {
                circuitWires.Add(circuitWire);
            }
        }
        return circuitWires.AsReadOnly();
    }

    public void RegisterWire(CircuitWire wire)
    {
        if (wire?.gameObject != null)
        {
            RegisterWire(wire.gameObject);
            OnWireRegistered?.Invoke(wire);
        }
    }

    public void UnregisterWire(CircuitWire wire)
    {
        if (wire?.gameObject != null)
        {
            UnregisterWire(wire.gameObject);
            OnWireUnregistered?.Invoke(wire);
        }
    }

    public CircuitComponent3D GetComponentById(int id)
    {
        return components.Find(c => c.GetInstanceID() == id);
    }

    public void ResetCircuit()
    {
        ClearAllComponents();
        MarkCircuitChanged();
    }

    public void ClearAllComponents()
    {
        
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
        
    }
    
    #endregion
    
    #region Circuit State Management
    
    public void MarkCircuitChanged()
    {
        // Notify all managers that circuit has changed
        solverManager?.MarkForSolving();
        eventManager?.OnCircuitChanged();
        debugManager?.LogCircuitChange();

        // Fire interface event
        OnCircuitChanged?.Invoke();
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
        if (existingComponents != null && existingComponents.Length > 0)
        {
            foreach (var comp in existingComponents)
            {
                if (comp != null) // Check each component is not null
                {
                    RegisterComponent(comp);
                }
            }
        }
        else
        {
        }
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

    #region ICircuitManager Implementation

    // Missing interface methods
    public bool IsCircuitValid()
    {
        return components.Count > 0 && wires.Count > 0;
    }

    public bool HasComponents() => components.Count > 0;
    public bool HasWires() => wires.Count > 0;
    public int GetComponentCount() => componentCount;
    public int GetWireCount() => wireCount;

    #endregion
}