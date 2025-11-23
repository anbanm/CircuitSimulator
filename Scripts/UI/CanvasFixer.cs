using UnityEngine;

/// <summary>
/// Forces Canvas to ScreenSpace-Camera mode to allow 3D mouse events (OnMouseDown, etc.)
/// CRITICAL: ScreenSpace-Overlay blocks ALL mouse events to 3D objects beneath it!
/// </summary>
public class CanvasFixer : MonoBehaviour
{
    void Awake()
    {
        Canvas canvas = GetComponent<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("CanvasFixer: No Canvas component found!");
            return;
        }

        // Check if already in Camera mode
        if (canvas.renderMode == RenderMode.ScreenSpaceCamera)
        {
            Debug.Log("✅ Canvas already in ScreenSpace-Camera mode");
            return;
        }

        // Find Main Camera
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            mainCamera = FindFirstObjectByType<Camera>();
        }

        if (mainCamera == null)
        {
            Debug.LogError("❌ CanvasFixer: No camera found! Cannot set Canvas to Camera mode.");
            return;
        }

        // Switch to ScreenSpace-Camera mode
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = mainCamera;
        canvas.planeDistance = 100f;

        Debug.Log($"✅ Fixed Canvas to ScreenSpace-Camera mode with camera: {mainCamera.name}");
        Debug.Log("   This allows OnMouseDown events to work on 3D objects!");
    }
}
