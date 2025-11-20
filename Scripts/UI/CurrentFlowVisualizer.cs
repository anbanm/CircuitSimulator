using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Makes electricity VISIBLE for Grade 7 students through animated flow dots
/// Addresses major misconceptions by showing current flow visually
/// 
/// Educational Impact:
/// - M2 Misconception: Students see current is SAME throughout series circuit
/// - M1 Misconception: No flow = incomplete circuit (immediate visual feedback)
/// - Parallel Understanding: Students see current SPLITTING at junctions
/// </summary>
public class CurrentFlowVisualizer : MonoBehaviour
{
    [Header("Visual Settings")]
    public Color currentColor = Color.cyan;
    public float dotSize = 0.1f;
    public float maxSpeed = 2f; // Units per second at max current
    public int maxDotsPerWire = 10;
    
    [Header("Educational Settings")]
    public float minCurrentToShow = 0.01f; // Hide very small currents
    public bool showDebugInfo = false;
    
    // Private state
    private CircuitWire circuitWire;
    private LineRenderer wireRenderer;
    private List<CurrentDot> activeDots = new List<CurrentDot>();
    private float currentMagnitude = 0f;
    private bool isFlowActive = false;
    private CircuitSolverManager solverManager;

    // Dot spawning
    private float dotSpawnInterval = 0.5f; // Time between dot spawns
    private float timeSinceLastDot = 0f;

    void Start()
    {
        // Get required components
        circuitWire = GetComponent<CircuitWire>();
        wireRenderer = GetComponent<LineRenderer>();

        if (circuitWire == null || wireRenderer == null)
        {
            Debug.LogError($"CurrentFlowVisualizer on {name} requires CircuitWire and LineRenderer components!");
            enabled = false;
            return;
        }

        // EVENT-DRIVEN: Subscribe to circuit solved event for immediate updates
        solverManager = FindFirstObjectByType<CircuitSolverManager>();
        if (solverManager != null)
        {
            solverManager.OnCircuitSolved += OnCircuitSolved;
            solverManager.OnSolveError += OnSolveError;  // Also listen for errors to clear dots
            if (showDebugInfo)
                Debug.Log($"✅ CurrentFlowVisualizer subscribed to circuit events");
        }
        else
        {
            Debug.LogWarning("CurrentFlowVisualizer: Could not find CircuitSolverManager for event subscription");
        }

        // Initial update
        UpdateCurrentFlow();

        if (showDebugInfo)
            Debug.Log($"CurrentFlowVisualizer initialized on wire: {name}");
    }

    /// <summary>
    /// EVENT HANDLER: Called immediately when circuit is solved
    /// Replaces the old polling system (InvokeRepeating) with instant updates
    /// </summary>
    void OnCircuitSolved()
    {
        UpdateCurrentFlow();

        if (showDebugInfo)
            Debug.Log($"⚡ CurrentFlowVisualizer received OnCircuitSolved event - updating immediately!");
    }

    /// <summary>
    /// EVENT HANDLER: Called when circuit solve fails (broken/incomplete circuit)
    /// Clears all dots since current should be 0
    /// </summary>
    void OnSolveError(string errorMessage)
    {
        UpdateCurrentFlow();  // This will detect current = 0 and clear dots

        if (showDebugInfo)
            Debug.Log($"⚠️ CurrentFlowVisualizer received OnSolveError: {errorMessage} - clearing dots");
    }
    
    void Update()
    {
        if (!isFlowActive) return;

        // Spawn new dots if current is flowing (check absolute value)
        if (Mathf.Abs(currentMagnitude) > minCurrentToShow)
        {
            timeSinceLastDot += Time.deltaTime;

            if (timeSinceLastDot >= dotSpawnInterval && activeDots.Count < maxDotsPerWire)
            {
                SpawnCurrentDot();
                timeSinceLastDot = 0f;
            }
        }

        // Update existing dots
        UpdateCurrentDots();

        // Clean up completed dots
        CleanupCompletedDots();
    }
    
    void UpdateCurrentFlow()
    {
        if (circuitWire == null) return;

        // Get current WITH DIRECTION from the wire's connected components
        float newCurrent = GetWireCurrentMagnitude();

        if (Mathf.Abs(newCurrent - currentMagnitude) > 0.001f)
        {
            bool wasActive = isFlowActive;
            currentMagnitude = newCurrent;
            isFlowActive = Mathf.Abs(currentMagnitude) > minCurrentToShow; // Use absolute value for activation check

            // If flow stopped (current dropped to ~0), clear all existing dots immediately
            if (wasActive && !isFlowActive)
            {
                ClearAllDots();
                if (showDebugInfo)
                    Debug.Log($"🛑 Wire {name}: Current dropped to 0, clearing all dots");
            }

            // Adjust spawn rate based on current magnitude (absolute value)
            float absCurrent = Mathf.Abs(currentMagnitude);
            if (absCurrent > 0f)
            {
                // Higher current = more frequent dots
                dotSpawnInterval = Mathf.Lerp(0.8f, 0.2f, absCurrent / 3f);
            }

            if (showDebugInfo)
                Debug.Log($"Wire {name}: Current = {currentMagnitude:F3}A, Flow active = {isFlowActive}, Direction = {(currentMagnitude >= 0 ? "forward" : "reverse")}");
        }
    }
    
    void SpawnCurrentDot()
    {
        if (wireRenderer.positionCount < 2) return;

        // ===== NEW APPROACH: Use endpoint isStart flags instead of current sign =====
        // Determine which endpoint is the flow start based on isStart flags
        bool flowForward = true; // Default to forward if flags not set

        if (circuitWire.startEndpoint != null && circuitWire.endEndpoint != null)
        {
            if (circuitWire.startEndpoint.isStart)
            {
                flowForward = true; // Flow from start → end
            }
            else if (circuitWire.endEndpoint.isStart)
            {
                flowForward = false; // Flow from end → start
            }
            else
            {
                // Fallback: No isStart flags set yet, use current magnitude sign
                flowForward = (currentMagnitude >= 0);
                if (showDebugInfo)
                    Debug.LogWarning($"⚠️ No isStart flags set on {name}, falling back to current sign");
            }
        }
        else
        {
            // Fallback if endpoints missing
            flowForward = (currentMagnitude >= 0);
        }

        // Create dot at the correct starting position based on flow direction
        Vector3 startPos;
        if (flowForward)
        {
            startPos = wireRenderer.GetPosition(0); // Start at beginning
        }
        else
        {
            startPos = wireRenderer.GetPosition(wireRenderer.positionCount - 1); // Start at end
        }

        GameObject dotObj = CreateDotGameObject(startPos);
        CurrentDot dot = dotObj.AddComponent<CurrentDot>();

        // Initialize dot with flow direction (use absolute current for speed calculation)
        dot.Initialize(wireRenderer, flowForward, Mathf.Abs(currentMagnitude), maxSpeed, currentColor, dotSize);

        activeDots.Add(dot);

        if (showDebugInfo)
            Debug.Log($"Spawned current dot on {name}, flowForward={flowForward}, startEndpoint.isStart={circuitWire.startEndpoint?.isStart}, endEndpoint.isStart={circuitWire.endEndpoint?.isStart}, active dots: {activeDots.Count}");
    }
    
    GameObject CreateDotGameObject(Vector3 position)
    {
        GameObject dotObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        dotObj.name = $"CurrentDot_{name}";
        dotObj.transform.position = position;
        dotObj.transform.localScale = Vector3.one * dotSize;
        
        // Make it glow
        Renderer dotRenderer = dotObj.GetComponent<Renderer>();
        if (dotRenderer != null)
        {
            Material dotMaterial = new Material(Shader.Find("Standard"));
            dotMaterial.color = currentColor;
            dotMaterial.EnableKeyword("_EMISSION");
            dotMaterial.SetColor("_EmissionColor", currentColor * 0.5f);
            dotRenderer.material = dotMaterial;
        }
        
        // Remove collider to avoid physics interactions
        Collider dotCollider = dotObj.GetComponent<Collider>();
        if (dotCollider != null)
        {
            if (Application.isPlaying)
                Destroy(dotCollider);
            else
                DestroyImmediate(dotCollider);
        }
        
        return dotObj;
    }
    
    void UpdateCurrentDots()
    {
        for (int i = 0; i < activeDots.Count; i++)
        {
            if (activeDots[i] != null)
            {
                activeDots[i].UpdateMovement();
            }
        }
    }
    
    void CleanupCompletedDots()
    {
        for (int i = activeDots.Count - 1; i >= 0; i--)
        {
            if (activeDots[i] == null || activeDots[i].IsCompleted)
            {
                if (activeDots[i] != null && activeDots[i].gameObject != null)
                {
                    Destroy(activeDots[i].gameObject);
                }
                activeDots.RemoveAt(i);
            }
        }
    }

    /// <summary>
    /// Clear all dots immediately (used when circuit breaks or current drops to 0)
    /// </summary>
    void ClearAllDots()
    {
        for (int i = activeDots.Count - 1; i >= 0; i--)
        {
            if (activeDots[i] != null && activeDots[i].gameObject != null)
            {
                Destroy(activeDots[i].gameObject);
            }
        }
        activeDots.Clear();
    }
    
    float GetWireCurrentMagnitude()
    {
        // Get current WITH DIRECTION from the wire's solved current value
        // Positive = flows from start to end, Negative = flows from end to start
        // CircuitWire.UpdateCurrentFromComponents() gets the accurate solved current from the circuit solver
        if (circuitWire != null)
        {
            return circuitWire.current; // Keep sign for direction!
        }

        // Fallback: Try to get from connected components
        if (circuitWire.startComponent != null)
        {
            return circuitWire.startComponent.current; // Keep sign for direction!
        }

        if (circuitWire.endComponent != null)
        {
            return circuitWire.endComponent.current; // Keep sign for direction!
        }

        return 0f;
    }
    
    void OnDestroy()
    {
        // Unsubscribe from events to prevent memory leaks
        if (solverManager != null)
        {
            solverManager.OnCircuitSolved -= OnCircuitSolved;
            solverManager.OnSolveError -= OnSolveError;
        }

        // Clean up all dots
        foreach (var dot in activeDots)
        {
            if (dot != null && dot.gameObject != null)
            {
                Destroy(dot.gameObject);
            }
        }
        activeDots.Clear();
    }
    
    // Educational debug information
    [ContextMenu("Show Current Info")]
    public void ShowCurrentInfo()
    {
        Debug.Log($"=== CURRENT FLOW DEBUG INFO ===");
        Debug.Log($"Wire: {name}");
        Debug.Log($"Current Magnitude: {currentMagnitude:F3}A");
        Debug.Log($"Flow Active: {isFlowActive}");
        Debug.Log($"Active Dots: {activeDots.Count}");
        Debug.Log($"Dot Spawn Interval: {dotSpawnInterval:F2}s");
        
        // Show component currents for comparison
        if (circuitWire != null)
        {
            Debug.Log($"Wire Current: {circuitWire.current:F3}A");
            if (circuitWire.startComponent != null)
            {
                Debug.Log($"Start Component Current: {circuitWire.startComponent.current:F3}A");
            }
        }
    }
    
    [ContextMenu("Force Spawn Test Dot")]
    public void ForceSpawnTestDot()
    {
        // For testing - spawn a dot even if no current
        currentMagnitude = 1.0f; // Fake current for testing
        SpawnCurrentDot();
        Debug.Log("Test dot spawned for educational testing!");
    }
}

/// <summary>
/// Individual current flow dot that moves along the wire
/// </summary>
public class CurrentDot : MonoBehaviour
{
    private LineRenderer wireRenderer;
    private bool flowForward; // True = flow from start→end, False = flow from end→start
    private float currentMagnitudeAbs; // Absolute value for speed calculation
    private float maxSpeed;
    private float currentPosition = 0f; // 0 to 1 along wire
    private bool isCompleted = false;

    public bool IsCompleted => isCompleted;

    public void Initialize(LineRenderer wire, bool flowForward, float currentMagnitudeAbs, float maxSpeed, Color color, float size)
    {
        this.wireRenderer = wire;
        this.flowForward = flowForward;
        this.currentMagnitudeAbs = currentMagnitudeAbs;
        this.maxSpeed = maxSpeed;

        // Set visual properties
        transform.localScale = Vector3.one * size;

        // Position at correct end based on flow direction
        // flowForward=true: start at 0, move to 1
        // flowForward=false: start at 1, move to 0
        currentPosition = flowForward ? 0f : 1f;
        UpdatePosition();
    }

    public void UpdateMovement()
    {
        if (isCompleted || wireRenderer == null) return;

        // Calculate movement speed based on current magnitude (absolute value)
        // Higher current = faster dots (educational: more current = more electron flow)
        float speed = Mathf.Lerp(0.1f, maxSpeed, currentMagnitudeAbs / 3f);

        // Determine direction: flowForward = +1, flowBackward = -1
        float direction = flowForward ? 1f : -1f;

        // Move along wire in the correct direction
        currentPosition += direction * speed * Time.deltaTime;

        // Check if completed (reached either end depending on direction)
        if (flowForward && currentPosition >= 1f) // Forward direction
        {
            isCompleted = true;
            return;
        }
        else if (!flowForward && currentPosition <= 0f) // Reverse direction
        {
            isCompleted = true;
            return;
        }

        UpdatePosition();
    }
    
    void UpdatePosition()
    {
        if (wireRenderer == null || wireRenderer.positionCount < 2) return;
        
        // Interpolate position along wire path
        Vector3 newPosition = GetInterpolatedPosition(currentPosition);
        transform.position = newPosition;
    }
    
    Vector3 GetInterpolatedPosition(float t)
    {
        // Simple linear interpolation between start and end points
        // Could be enhanced for curved wires in the future
        Vector3 startPos = wireRenderer.GetPosition(0);
        Vector3 endPos = wireRenderer.GetPosition(wireRenderer.positionCount - 1);
        
        return Vector3.Lerp(startPos, endPos, t);
    }
}