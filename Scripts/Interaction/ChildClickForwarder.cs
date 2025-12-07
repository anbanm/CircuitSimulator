using UnityEngine;

/// <summary>
/// Forwards mouse clicks from child objects to the parent component.
/// Attach this to child meshes in prefabs so clicking any part selects/moves the whole component.
/// </summary>
public class ChildClickForwarder : MonoBehaviour
{
    private SelectableComponent _parentSelectable;
    private MoveableComponent _parentMoveable;
    private CircuitComponent3D _parentCircuitComponent;
    private CircuitSimulator.Components.InteractionComponent _parentInteraction;

    private bool _isDraggingFromChild = false;
    private Vector3 _dragOffset;
    private Camera _mainCamera;

    void Start()
    {
        FindParentComponents();
        _mainCamera = Camera.main;
    }

    void FindParentComponents()
    {
        // Look up the hierarchy for the parent with SelectableComponent
        _parentSelectable = GetComponentInParent<SelectableComponent>();
        _parentMoveable = GetComponentInParent<MoveableComponent>();
        _parentCircuitComponent = GetComponentInParent<CircuitComponent3D>();
        _parentInteraction = GetComponentInParent<CircuitSimulator.Components.InteractionComponent>();

        // Ensure this child has a collider for raycasting
        if (GetComponent<Collider>() == null)
        {
            // Try to add a MeshCollider if there's a MeshFilter
            var meshFilter = GetComponent<MeshFilter>();
            if (meshFilter != null && meshFilter.sharedMesh != null)
            {
                var meshCollider = gameObject.AddComponent<MeshCollider>();
                meshCollider.sharedMesh = meshFilter.sharedMesh;
            }
        }

        Debug.Log($"[ChildClickForwarder] {name} found parent: Selectable={_parentSelectable != null}, Moveable={_parentMoveable != null}, Interaction={_parentInteraction != null}");
    }

    void OnMouseDown()
    {
        Debug.Log($"[ChildClickForwarder] OnMouseDown on {name}");

        // Re-find if null (in case of late initialization)
        if (_parentSelectable == null)
        {
            FindParentComponents();
        }

        // Check if connect tool is active - forward to ConnectTool
        if (ComponentRegistry.Instance != null)
        {
            ConnectTool connectTool = ComponentRegistry.Instance.GetManager<ConnectTool>();
            if (connectTool != null && connectTool.IsConnectMode())
            {
                if (_parentSelectable != null)
                {
                    connectTool.OnComponentClicked(_parentSelectable);
                }
                return;
            }
        }

        // Forward selection to parent
        if (_parentSelectable != null)
        {
            _parentSelectable.SelectThis();
            Debug.Log($"[ChildClickForwarder] Selected parent: {_parentSelectable.name}");
        }

        // Start dragging from child - calculate offset from parent transform
        if (_parentSelectable != null && _parentSelectable.IsSelected())
        {
            _isDraggingFromChild = true;
            Transform parentTransform = _parentSelectable.transform;
            Vector3 mouseWorldPos = GetMouseWorldPosition();
            _dragOffset = parentTransform.position - mouseWorldPos;
            _dragOffset.y = 0;
            Debug.Log($"[ChildClickForwarder] Started drag from child, offset: {_dragOffset}");
        }
    }

    void Update()
    {
        // Handle dragging initiated from child
        if (_isDraggingFromChild && Input.GetMouseButton(0))
        {
            HandleDragging();
        }

        // Stop dragging on mouse release
        if (_isDraggingFromChild && Input.GetMouseButtonUp(0))
        {
            _isDraggingFromChild = false;
            Debug.Log($"[ChildClickForwarder] Stopped drag from child");
        }
    }

    void HandleDragging()
    {
        if (_parentSelectable == null) return;

        Transform parentTransform = _parentSelectable.transform;
        Vector3 mouseWorldPos = GetMouseWorldPosition();
        if (mouseWorldPos == Vector3.zero) return;

        Vector3 targetPosition = mouseWorldPos + _dragOffset;
        targetPosition.y = parentTransform.position.y; // Keep Y fixed

        parentTransform.position = targetPosition;
    }

    Vector3 GetMouseWorldPosition()
    {
        if (_mainCamera == null) _mainCamera = Camera.main;
        if (_mainCamera == null) return Vector3.zero;

        Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);

        // Create a plane at the parent's Y position
        float yPos = _parentSelectable != null ? _parentSelectable.transform.position.y : 0.5f;
        Plane plane = new Plane(Vector3.up, new Vector3(0, yPos, 0));

        if (plane.Raycast(ray, out float distance))
        {
            return ray.GetPoint(distance);
        }

        return Vector3.zero;
    }

    void OnMouseOver()
    {
        // Forward right-click for property popup
        if (Input.GetMouseButtonDown(1))
        {
            if (_parentCircuitComponent != null)
            {
                ComponentPropertyPopup popup = ComponentPropertyPopup.Instance;
                if (popup != null)
                {
                    popup.ShowForComponent(_parentCircuitComponent);
                }
            }
        }
    }
}
