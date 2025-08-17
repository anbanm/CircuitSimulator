using UnityEngine;
using System.Collections.Generic;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif

// Debug visualization system - great for development and educational purposes!
// Perfect for showing students how circuits actually work in 3D/AR
public class CircuitDebugVisualizer : MonoBehaviour
{
    [Header("Debug Visualization Settings")]
    public bool showNodes = true;
    public bool showCurrentFlow = true;
    public bool showDisconnectedComponents = true;
    public bool showComponentLabels = true;
    
    [Header("Visual Styling")]
    public float nodeSize = 0.1f;
    public float arrowSize = 0.3f;
    public Color nodeColor = Color.yellow;
    public Color currentFlowColor = Color.green;
    public Color disconnectedColor = Color.red;
    public Color connectedColor = Color.blue;
    
    [Header("AR-Optimized Settings")]
    public bool enableForAR = true;
    public float arScaleFactor = 0.5f; // Scale down for AR viewing
    
    private Dictionary<CircuitNode, Color> nodeColors = new Dictionary<CircuitNode, Color>();
    private Dictionary<CircuitNode, Vector3> nodePositions = new Dictionary<CircuitNode, Vector3>();
    
    void Update()
    {
        // Update node positions and colors in real-time
        UpdateNodeVisualization();
    }
    
    void UpdateNodeVisualization()
    {
        var manager = CircuitManager.Instance;
        if (manager == null) return;
        
        nodeColors.Clear();
        nodePositions.Clear();
        
        var components = FindObjectsByType<CircuitComponent3D>(FindObjectsSortMode.None);
        
        foreach (var comp in components)
        {
            if (comp.logicalComponent != null)
            {
                var nodeA = comp.logicalComponent.NodeA;
                var nodeB = comp.logicalComponent.NodeB;
                
                // Assign colors to nodes if not already assigned
                if (!nodeColors.ContainsKey(nodeA))
                {
                    nodeColors[nodeA] = GetColorForNode(nodeA);
                }
                if (!nodeColors.ContainsKey(nodeB))
                {
                    nodeColors[nodeB] = GetColorForNode(nodeB);
                }
                
                // Calculate node positions (component terminals)
                Vector3 compPos = comp.transform.position;
                nodePositions[nodeA] = compPos + Vector3.left * 0.3f;
                nodePositions[nodeB] = compPos + Vector3.right * 0.3f;
            }
        }
    }
    
    Color GetColorForNode(CircuitNode node)
    {
        // Color nodes based on voltage level for educational value
        if (float.IsNaN(node.Voltage) || node.Voltage == 0f)
        {
            return Color.gray; // Ground or unconnected
        }
        
        // Map voltage to color spectrum (red = high voltage, blue = low voltage)
        float normalizedVoltage = Mathf.Clamp01(node.Voltage / 12f); // Assume max 12V
        return Color.Lerp(Color.blue, Color.red, normalizedVoltage);
    }
    
    void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;
        
        var manager = CircuitManager.Instance;
        if (manager == null) return;
        
        float scale = enableForAR ? arScaleFactor : 1f;
        
        if (showNodes) DrawNodes(scale);
        if (showCurrentFlow) DrawCurrentFlow(scale);
        if (showDisconnectedComponents) DrawDisconnectedComponents(scale);
    }
    
    void DrawNodes(float scale)
    {
        foreach (var kvp in nodePositions)
        {
            var node = kvp.Key;
            var position = kvp.Value;
            
            // Draw colored sphere for each electrical node
            Gizmos.color = nodeColors.ContainsKey(node) ? nodeColors[node] : nodeColor;
            Gizmos.DrawSphere(position, nodeSize * scale);
            
            // Draw voltage label
            if (showComponentLabels)
            {
                var labelPos = position + Vector3.up * (nodeSize * 2 * scale);
                DrawTextGizmo(labelPos, $"{node.Voltage:F1}V");
            }
        }
    }
    
    void DrawCurrentFlow(float scale)
    {
        var components = FindObjectsByType<CircuitComponent3D>(FindObjectsSortMode.None);
        
        foreach (var comp in components)
        {
            if (comp.logicalComponent != null && Mathf.Abs(comp.current) > 0.01f)
            {
                Vector3 startPos = comp.transform.position + Vector3.left * 0.3f;
                Vector3 endPos = comp.transform.position + Vector3.right * 0.3f;
                
                // Adjust direction based on current direction
                if (comp.current < 0)
                {
                    (startPos, endPos) = (endPos, startPos);
                }
                
                // Color intensity based on current magnitude
                float intensity = Mathf.Clamp01(Mathf.Abs(comp.current) / 3f); // Assume max 3A
                Gizmos.color = Color.Lerp(Color.gray, currentFlowColor, intensity);
                
                // Draw current flow arrow
                DrawArrow(startPos, endPos, arrowSize * scale);
                
                // Draw current magnitude label
                if (showComponentLabels)
                {
                    Vector3 midPoint = (startPos + endPos) / 2f + Vector3.up * 0.4f;
                    DrawTextGizmo(midPoint, $"{comp.current:F2}A");
                }
            }
        }
    }
    
    void DrawDisconnectedComponents(float scale)
    {
        var manager = CircuitManager.Instance;
        var components = FindObjectsByType<CircuitComponent3D>(FindObjectsSortMode.None);
        
        foreach (var comp in components)
        {
            // Check if component is properly connected
            bool isConnected = comp.logicalComponent != null && 
                              comp.logicalComponent.NodeA.ConnectedComponents.Count > 1 &&
                              comp.logicalComponent.NodeB.ConnectedComponents.Count > 1;
            
            if (!isConnected)
            {
                Gizmos.color = disconnectedColor;
                Vector3 pos = comp.transform.position;
                
                // Draw warning indicator for disconnected components
                Gizmos.DrawWireCube(pos, Vector3.one * 0.8f * scale);
                
                if (showComponentLabels)
                {
                    DrawTextGizmo(pos + Vector3.up * 0.8f, "DISCONNECTED");
                }
            }
            else
            {
                // Draw subtle indicator for connected components
                Gizmos.color = connectedColor;
                Vector3 pos = comp.transform.position;
                Gizmos.DrawWireCube(pos, Vector3.one * 0.2f * scale);
            }
        }
    }
    
    void DrawArrow(Vector3 start, Vector3 end, float size)
    {
        Vector3 direction = (end - start).normalized;
        Vector3 perpendicular = Vector3.Cross(direction, Vector3.up).normalized;
        
        // Draw main line
        Gizmos.DrawLine(start, end);
        
        // Draw arrowhead
        Vector3 arrowHead1 = end - direction * size + perpendicular * size * 0.5f;
        Vector3 arrowHead2 = end - direction * size - perpendicular * size * 0.5f;
        
        Gizmos.DrawLine(end, arrowHead1);
        Gizmos.DrawLine(end, arrowHead2);
    }
    
    void DrawTextGizmo(Vector3 position, string text)
    {
        // Note: Unity's Gizmos don't support text directly
        // In a real implementation, you'd use GUI.Label in OnGUI() or create 3D text objects
        
        #if UNITY_EDITOR
        Handles.Label(position, text);
        #endif
    }
    
    // AR-specific visualization methods
    public void ShowNodesInAR(bool show)
    {
        showNodes = show;
    }
    
    public void ShowCurrentFlowInAR(bool show)
    {
        showCurrentFlow = show;
    }
    
    public void SetARScale(float scale)
    {
        arScaleFactor = scale;
    }
    
    // Educational methods for students
    public void HighlightCircuitPath()
    {
        // Highlight the complete circuit path for educational purposes
        var components = FindObjectsByType<CircuitComponent3D>(FindObjectsSortMode.None);
        var battery = components.FirstOrDefault(c => c.ComponentType == ComponentType.Battery);
        
        if (battery != null && battery.logicalComponent != null)
        {
            StartCoroutine(AnimateCircuitPath(battery.logicalComponent));
        }
    }
    
    System.Collections.IEnumerator AnimateCircuitPath(CircuitComponent startComponent)
    {
        // Animate the path current takes through the circuit
        var visited = new HashSet<CircuitNode>();
        var currentNode = startComponent.NodeA;
        
        while (currentNode != null && !visited.Contains(currentNode))
        {
            visited.Add(currentNode);
            
            // Highlight current node
            if (nodePositions.ContainsKey(currentNode))
            {
                // Create temporary highlight effect
                GameObject highlight = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                highlight.transform.position = nodePositions[currentNode];
                highlight.transform.localScale = Vector3.one * nodeSize * 3f;
                highlight.GetComponent<Renderer>().material.color = Color.yellow;
                
                // Remove highlight after delay
                Destroy(highlight, 0.5f);
            }
            
            yield return new WaitForSeconds(0.5f);
            
            // Move to next node in the circuit
            CircuitNode nextNode = null;
            foreach (var comp in currentNode.ConnectedComponents)
            {
                var otherNode = comp.NodeA == currentNode ? comp.NodeB : comp.NodeA;
                if (!visited.Contains(otherNode))
                {
                    nextNode = otherNode;
                    break;
                }
            }
            
            currentNode = nextNode;
        }
    }
    
    // Context menu for easy testing
    [ContextMenu("Test Highlight Circuit Path")]
    public void TestHighlightPath()
    {
        HighlightCircuitPath();
    }
    
    [ContextMenu("Toggle All Debug Visuals")]
    public void ToggleAllDebugVisuals()
    {
        showNodes = !showNodes;
        showCurrentFlow = !showCurrentFlow;
        showDisconnectedComponents = !showDisconnectedComponents;
        showComponentLabels = !showComponentLabels;
    }
}