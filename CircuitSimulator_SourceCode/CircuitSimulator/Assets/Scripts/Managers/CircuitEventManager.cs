using UnityEngine;
using System;

/// <summary>
/// Handles all circuit events and notifications
/// Provides event system for circuit state changes
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
            Debug.Log($"[Event] Component registered: {component.name}");
        }
    }
    
    public void OnComponentUnregistered(CircuitComponent3D component)
    {
        if (CanTriggerEvent())
        {
            ComponentUnregistered?.Invoke(component);
            Debug.Log($"[Event] Component unregistered: {component.name}");
        }
    }
    
    public void OnWireRegistered(GameObject wire)
    {
        if (CanTriggerEvent())
        {
            WireRegistered?.Invoke(wire);
            Debug.Log($"[Event] Wire registered: {wire.name}");
        }
    }
    
    public void OnWireUnregistered(GameObject wire)
    {
        if (CanTriggerEvent())
        {
            WireUnregistered?.Invoke(wire);
            Debug.Log($"[Event] Wire unregistered: {wire.name}");
        }
    }
    
    public void OnCircuitChanged()
    {
        if (CanTriggerEvent())
        {
            CircuitChanged?.Invoke();
            Debug.Log("[Event] Circuit changed");
        }
    }
    
    public void OnCircuitSolved()
    {
        CircuitSolved?.Invoke();
        Debug.Log("[Event] Circuit solved");
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
        
        Debug.Log("[Event] All events cleared");
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