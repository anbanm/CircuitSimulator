using UnityEngine;

public class CircuitCameraController : MonoBehaviour
{
    [Header("Camera Settings")]
    public float panSpeed = 2f;
    public float zoomSpeed = 2f;
    public float rotationSpeed = 2f;
    
    [Header("Limits")]
    public float minZoom = 3f;
    public float maxZoom = 20f;
    public float minY = 2f;
    public float maxY = 20f;
    
    [Header("Auto Focus")]
    public Transform circuitArea; // The plane where components are placed
    
    private Camera cam;
    private Vector3 lastMousePosition;
    private bool isDragging = false;
    
    void Start()
    {
        cam = GetComponent<Camera>();
        
        // Set up good default camera position for circuit building
        SetupDefaultPosition();
    }
    
    void SetupDefaultPosition()
    {
        if (circuitArea != null)
        {
            // Position camera to look at circuit area from a good angle
            Vector3 targetPos = circuitArea.position + new Vector3(0, 8, -6);
            transform.position = targetPos;
            transform.LookAt(circuitArea.position);
        }
        else
        {
            // Default position if no circuit area assigned
            transform.position = new Vector3(0, 8, -6);
            transform.rotation = Quaternion.Euler(35, 0, 0);
        }
    }
    
    void Update()
    {
        HandleMouseInput();
        HandleKeyboardInput();
    }
    
    void HandleMouseInput()
    {
        // Mouse wheel zoom
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0f)
        {
            Zoom(scroll * zoomSpeed);
        }
        
        // Right mouse button - rotate camera
        if (Input.GetMouseButtonDown(1))
        {
            lastMousePosition = Input.mousePosition;
            isDragging = true;
        }
        
        if (Input.GetMouseButtonUp(1))
        {
            isDragging = false;
        }
        
        if (isDragging && Input.GetMouseButton(1))
        {
            Vector3 mouseDelta = Input.mousePosition - lastMousePosition;
            
            // Rotate around the circuit area
            if (circuitArea != null)
            {
                transform.RotateAround(circuitArea.position, Vector3.up, mouseDelta.x * rotationSpeed * Time.deltaTime);
                transform.RotateAround(circuitArea.position, transform.right, -mouseDelta.y * rotationSpeed * Time.deltaTime);
            }
            
            lastMousePosition = Input.mousePosition;
        }
        
        // Middle mouse button - pan
        if (Input.GetMouseButton(2))
        {
            Vector3 mouseDelta = Input.mousePosition - lastMousePosition;
            
            Vector3 move = new Vector3(-mouseDelta.x, 0, -mouseDelta.y) * panSpeed * Time.deltaTime * 0.01f;
            transform.Translate(move, Space.World);
        }
        
        if (Input.GetMouseButtonDown(2))
        {
            lastMousePosition = Input.mousePosition;
        }
    }
    
    void HandleKeyboardInput()
    {
        // WASD movement
        Vector3 moveInput = Vector3.zero;
        
        if (Input.GetKey(KeyCode.W)) moveInput += Vector3.forward;
        if (Input.GetKey(KeyCode.S)) moveInput += Vector3.back;
        if (Input.GetKey(KeyCode.A)) moveInput += Vector3.left;
        if (Input.GetKey(KeyCode.D)) moveInput += Vector3.right;
        if (Input.GetKey(KeyCode.Q)) moveInput += Vector3.down;
        if (Input.GetKey(KeyCode.E)) moveInput += Vector3.up;
        
        if (moveInput != Vector3.zero)
        {
            transform.Translate(moveInput * panSpeed * Time.deltaTime, Space.World);
        }
        
        // F key - focus on circuit area
        if (Input.GetKeyDown(KeyCode.F))
        {
            FocusOnCircuitArea();
        }
        
        // R key - reset camera
        if (Input.GetKeyDown(KeyCode.R))
        {
            SetupDefaultPosition();
        }
    }
    
    void Zoom(float zoomAmount)
    {
        if (cam.orthographic)
        {
            cam.orthographicSize = Mathf.Clamp(cam.orthographicSize - zoomAmount, minZoom, maxZoom);
        }
        else
        {
            Vector3 newPos = transform.position + transform.forward * zoomAmount;
            
            // Limit zoom based on Y position
            if (newPos.y >= minY && newPos.y <= maxY)
            {
                transform.position = newPos;
            }
        }
    }
    
    void FocusOnCircuitArea()
    {
        if (circuitArea != null)
        {
            // Smoothly move camera to focus on circuit area
            StartCoroutine(SmoothFocus(circuitArea.position));
        }
    }
    
    System.Collections.IEnumerator SmoothFocus(Vector3 target)
    {
        Vector3 startPos = transform.position;
        Vector3 targetPos = target + new Vector3(0, 8, -6);
        Quaternion startRot = transform.rotation;
        Quaternion targetRot = Quaternion.LookRotation((target - targetPos).normalized);
        
        float elapsed = 0f;
        float duration = 1f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            transform.position = Vector3.Lerp(startPos, targetPos, t);
            transform.rotation = Quaternion.Lerp(startRot, targetRot, t);
            
            yield return null;
        }
        
        transform.position = targetPos;
        transform.rotation = targetRot;
    }
    
    void OnDrawGizmos()
    {
        // Draw circuit area indicator
        if (circuitArea != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(circuitArea.position, new Vector3(10, 0.1f, 10));
        }
    }
}

// Instructions for setup:
/*
HOW TO USE:
1. Add this script to your Main Camera
2. Assign the circuit plane/area to "Circuit Area" field
3. Camera controls:
   - Mouse Wheel: Zoom in/out
   - Right Mouse + Drag: Rotate around circuit
   - Middle Mouse + Drag: Pan camera
   - WASD: Move camera
   - Q/E: Move up/down
   - F: Focus on circuit area
   - R: Reset to default position

RECOMMENDED SETTINGS:
- Pan Speed: 2
- Zoom Speed: 2
- Rotation Speed: 2
- Min Zoom: 3
- Max Zoom: 20
- Min Y: 2
- Max Y: 20
*/