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
        
        // Start checking for current flow
        InvokeRepeating("UpdateCurrentFlow", 0.1f, 0.1f);
        
        if (showDebugInfo)
            Debug.Log($"CurrentFlowVisualizer initialized on wire: {name}");
    }
    
    void Update()
    {
        if (!isFlowActive) return;
        
        // Spawn new dots if current is flowing
        if (currentMagnitude > minCurrentToShow)
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
        
        // Get current from the wire's connected components
        float newCurrent = GetWireCurrentMagnitude();
        
        if (Mathf.Abs(newCurrent - currentMagnitude) > 0.001f)
        {
            currentMagnitude = newCurrent;
            isFlowActive = currentMagnitude > minCurrentToShow;
            
            // Adjust spawn rate based on current magnitude
            if (currentMagnitude > 0f)
            {
                // Higher current = more frequent dots
                dotSpawnInterval = Mathf.Lerp(0.8f, 0.2f, currentMagnitude / 3f);
            }
            
            if (showDebugInfo)
                Debug.Log($"Wire {name}: Current = {currentMagnitude:F3}A, Flow active = {isFlowActive}");
        }
    }
    
    void SpawnCurrentDot()
    {
        if (wireRenderer.positionCount < 2) return;
        
        // Create dot at start of wire
        Vector3 startPos = wireRenderer.GetPosition(0);
        
        GameObject dotObj = CreateDotGameObject(startPos);
        CurrentDot dot = dotObj.AddComponent<CurrentDot>();
        
        // Initialize dot
        dot.Initialize(wireRenderer, currentMagnitude, maxSpeed, currentColor, dotSize);
        
        activeDots.Add(dot);
        
        if (showDebugInfo)
            Debug.Log($"Spawned current dot on {name}, active dots: {activeDots.Count}");
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
    
    float GetWireCurrentMagnitude()
    {
        // Get current from connected components
        if (circuitWire.startComponent != null)
        {
            CircuitComponent3D startComp = circuitWire.startComponent.GetComponent<CircuitComponent3D>();
            if (startComp != null)
            {
                return Mathf.Abs(startComp.current);
            }
        }
        
        return 0f;
    }
    
    void OnDestroy()
    {
        // Clean up all dots
        foreach (var dot in activeDots)
        {
            if (dot != null && dot.gameObject != null)
            {
                Destroy(dot.gameObject);
            }
        }
        activeDots.Clear();
        
        // Stop repeating invoke
        CancelInvoke();
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
    private float currentMagnitude;
    private float maxSpeed;
    private float currentPosition = 0f; // 0 to 1 along wire
    private bool isCompleted = false;
    
    public bool IsCompleted => isCompleted;
    
    public void Initialize(LineRenderer wire, float current, float maxSpeed, Color color, float size)
    {
        this.wireRenderer = wire;
        this.currentMagnitude = current;
        this.maxSpeed = maxSpeed;
        
        // Set visual properties
        transform.localScale = Vector3.one * size;
        
        // Position at start of wire
        currentPosition = 0f;
        UpdatePosition();
    }
    
    public void UpdateMovement()
    {
        if (isCompleted || wireRenderer == null) return;
        
        // Calculate movement speed based on current magnitude
        // Higher current = faster dots (educational: more current = more electron flow)
        float speed = Mathf.Lerp(0.1f, maxSpeed, currentMagnitude / 3f);
        
        // Move along wire
        currentPosition += speed * Time.deltaTime;
        
        // Check if completed
        if (currentPosition >= 1f)
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