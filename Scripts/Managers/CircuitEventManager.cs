using UnityEngine;
using System;
using CircuitSimulator.Services;

/// <summary>
/// Handles all circuit events and notifications
/// Provides event system for circuit state changes
/// Registers with ServiceLocator for dependency injection
/// </summary>
public class CircuitEventManager : MonoBehaviour
{
    // Events for circuit state changes
    public static event Action<CircuitComponent3D> ComponentRegistered;
    public static event Action<CircuitComponent3D> ComponentUnregistered;
    public static event Action<GameObject> WireRegistered;
    public static event Action<GameObject> WireUnregistered;
    public static event Action CircuitChanged;
    public static event Action CircuitSolved;
    
    // Event timing
    private float lastEventTime = 0f;
    private const float EVENT_THROTTLE = 0.1f; // Prevent event spam

    void Start()
    {
        // Register with ServiceLocator for future interface implementation
        // For now, register as concrete type
        if (ServiceLocator.Instance != null)
        {
            // Could implement IEventManager interface in the future
        }
    }
    
    public void Update()
    {
        // Handle any update-based event logic here
        // Currently no continuous events needed
    }
    
    #region Event Triggers
    
    public void OnComponentRegistered(CircuitComponent3D component)
    {
        if (CanTriggerEvent())
        {
            ComponentRegistered?.Invoke(component);
        }
    }
    
    public void OnComponentUnregistered(CircuitComponent3D component)
    {
        if (CanTriggerEvent())
        {
            ComponentUnregistered?.Invoke(component);
        }
    }
    
    public void OnWireRegistered(GameObject wire)
    {
        if (CanTriggerEvent())
        {
            WireRegistered?.Invoke(wire);
        }
    }
    
    public void OnWireUnregistered(GameObject wire)
    {
        if (CanTriggerEvent())
        {
            WireUnregistered?.Invoke(wire);
        }
    }
    
    public void OnCircuitChanged()
    {
        if (CanTriggerEvent())
        {
            CircuitChanged?.Invoke();
        }
    }
    
    public void OnCircuitSolved()
    {
        CircuitSolved?.Invoke();
    }
    
    #endregion
    
    #region Event Utilities
    
    private bool CanTriggerEvent()
    {
        // Throttle events to prevent spam
        if (Time.time - lastEventTime < EVENT_THROTTLE)
        {
            return false;
        }
        
        lastEventTime = Time.time;
        return true;
    }
    
    public static void ClearAllEvents()
    {
        ComponentRegistered = null;
        ComponentUnregistered = null;
        WireRegistered = null;
        WireUnregistered = null;
        CircuitChanged = null;
        CircuitSolved = null;
        
    }
    
    #endregion
    
    #region Unity Events
    
    void OnDestroy()
    {
        // Clean up events when manager is destroyed
        ClearAllEvents();
    }
    
    #endregion
    
    #region Public API for External Systems
    
    /// <summary>
    /// Subscribe to component events
    /// </summary>
    public static void SubscribeToComponentEvents(
        Action<CircuitComponent3D> onRegistered, 
        Action<CircuitComponent3D> onUnregistered)
    {
        ComponentRegistered += onRegistered;
        ComponentUnregistered += onUnregistered;
    }
    
    /// <summary>
    /// Subscribe to wire events
    /// </summary>
    public static void SubscribeToWireEvents(
        Action<GameObject> onRegistered, 
        Action<GameObject> onUnregistered)
    {
        WireRegistered += onRegistered;
        WireUnregistered += onUnregistered;
    }
    
    /// <summary>
    /// Subscribe to circuit state events
    /// </summary>
    public static void SubscribeToCircuitEvents(
        Action onChanged, 
        Action onSolved)
    {
        CircuitChanged += onChanged;
        CircuitSolved += onSolved;
    }
    
    /// <summary>
    /// Unsubscribe from component events
    /// </summary>
    public static void UnsubscribeFromComponentEvents(
        Action<CircuitComponent3D> onRegistered, 
        Action<CircuitComponent3D> onUnregistered)
    {
        ComponentRegistered -= onRegistered;
        ComponentUnregistered -= onUnregistered;
    }
    
    /// <summary>
    /// Unsubscribe from wire events
    /// </summary>
    public static void UnsubscribeFromWireEvents(
        Action<GameObject> onRegistered, 
        Action<GameObject> onUnregistered)
    {
        WireRegistered -= onRegistered;
        WireUnregistered -= onUnregistered;
    }
    
    /// <summary>
    /// Unsubscribe from circuit state events
    /// </summary>
    public static void UnsubscribeFromCircuitEvents(
        Action onChanged, 
        Action onSolved)
    {
        CircuitChanged -= onChanged;
        CircuitSolved -= onSolved;
    }
    
    #endregion
}