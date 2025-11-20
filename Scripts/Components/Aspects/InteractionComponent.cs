using UnityEngine;
using CircuitSimulator.Services;

namespace CircuitSimulator.Components
{
    /// <summary>
    /// Aspect component responsible for user interaction capabilities.
    /// Handles selection, movement, connection, and property editing.
    /// </summary>
    public class InteractionComponent : MonoBehaviour
    {
        [Header("Interaction Settings")]
        [SerializeField] private bool allowSelection = true;
        [SerializeField] private bool allowMovement = true;
        [SerializeField] private bool allowConnection = true;
        [SerializeField] private bool allowPropertyEditing = true;
        [SerializeField] private bool allowDeletion = true;

        [Header("Movement")]
        [SerializeField] private bool useGridSnapping = true;
        [SerializeField] private float gridSize = 1f;
        [SerializeField] private LayerMask groundLayer = 1; // Default layer

        [Header("Connection Points")]
        [SerializeField] private Transform leftTerminal;
        [SerializeField] private Transform rightTerminal;
        [SerializeField] private float connectionRadius = 0.5f;

        // State
        private bool isSelected = false;
        private bool isDragging = false;
        private bool isFixed = false; // For challenge pre-placed components
        private Vector3 originalPosition;
        private Vector3 dragOffset;

        // CRITICAL: Static tracker to ensure only ONE component is selected at a time
        private static InteractionComponent currentlySelected = null;

        // Component references
        private ElectricalComponent electricalComponent;
        private VisualComponent visualComponent;
        private ICircuitManager circuitManager;

        // Events
        public System.Action OnSelected;
        public System.Action OnDeselected;
        public System.Action<Vector3> OnMoved;
        public System.Action OnDeleted;
        public System.Action<InteractionComponent> OnConnectionRequested;

        // Properties
        public bool IsSelected => isSelected;
        public bool IsDragging => isDragging;
        public bool IsFixed => isFixed;
        public Transform LeftTerminal => leftTerminal;
        public Transform RightTerminal => rightTerminal;

        void Start()
        {
            InitializeInteraction();
            FindComponentReferences();
            SetupConnectionPoints();
        }

        private void InitializeInteraction()
        {
            // Get circuit manager service
            circuitManager = ServiceLocator.Instance.TryGet<ICircuitManager>();

            // Ensure collider for interaction
            var collider = GetComponent<Collider>();
            if (collider == null)
            {
                var boxCollider = gameObject.AddComponent<BoxCollider>();
                boxCollider.isTrigger = false; // We want physical interaction
            }

            // Store original position
            originalPosition = transform.position;
        }

        private void FindComponentReferences()
        {
            electricalComponent = GetComponent<ElectricalComponent>();
            visualComponent = GetComponent<VisualComponent>();
        }

        private void SetupConnectionPoints()
        {
            // First try to find terminals created by ComponentTerminalManager
            // Check for all possible terminal naming conventions
            Transform existingLeftTerminal = transform.Find("LeftTerminal")
                                           ?? transform.Find("NegativeTerminal")
                                           ?? transform.Find("InputTerminal");
            Transform existingRightTerminal = transform.Find("RightTerminal")
                                            ?? transform.Find("PositiveTerminal")
                                            ?? transform.Find("OutputTerminal");

            if (existingLeftTerminal != null && existingRightTerminal != null)
            {
                // Use terminals created by ComponentTerminalManager
                leftTerminal = existingLeftTerminal;
                rightTerminal = existingRightTerminal;
                Debug.Log($"[InteractionComponent] Using existing terminals for {gameObject.name}: {existingLeftTerminal.name}, {existingRightTerminal.name}");
                return;
            }

            // Fallback: Create basic terminal points if they don't exist
            if (leftTerminal == null)
            {
                var leftTerminalGO = new GameObject("LeftTerminal");
                leftTerminalGO.transform.SetParent(transform);
                leftTerminalGO.transform.localPosition = new Vector3(-0.5f, 0, 0);
                leftTerminal = leftTerminalGO.transform;

                // Add basic visual indicator (fallback)
                var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                sphere.transform.SetParent(leftTerminal);
                sphere.transform.localPosition = Vector3.zero;
                sphere.transform.localScale = Vector3.one * 0.2f;
                sphere.GetComponent<Renderer>().material.color = Color.red;
                sphere.name = "TerminalIndicator";

                // Remove collider from indicator
                Destroy(sphere.GetComponent<Collider>());
            }

            if (rightTerminal == null)
            {
                var rightTerminalGO = new GameObject("RightTerminal");
                rightTerminalGO.transform.SetParent(transform);
                rightTerminalGO.transform.localPosition = new Vector3(0.5f, 0, 0);
                rightTerminal = rightTerminalGO.transform;

                // Add basic visual indicator (fallback)
                var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                sphere.transform.SetParent(rightTerminal);
                sphere.transform.localPosition = Vector3.zero;
                sphere.transform.localScale = Vector3.one * 0.2f;
                sphere.GetComponent<Renderer>().material.color = Color.blue;
                sphere.name = "TerminalIndicator";

                // Remove collider from indicator
                Destroy(sphere.GetComponent<Collider>());
            }

            Debug.Log($"[InteractionComponent] Created fallback terminals for {gameObject.name}");
        }

        #region Mouse Interaction

        void OnMouseDown()
        {
            if (!allowSelection && !allowMovement) return;

            // Check if this is a connection request
            var connectTool = FindFirstObjectByType<ConnectTool>();
            if (connectTool != null && connectTool.IsInConnectMode)
            {
                HandleConnectionClick();
                return;
            }

            // Handle selection and movement
            if (allowSelection)
                SetSelected(true);

            if (allowMovement && !isFixed)
                StartDragging();
        }

        void OnMouseDrag()
        {
            if (!isDragging || isFixed) return;

            Vector3 mouseWorldPos = GetMouseWorldPosition();
            Vector3 newPosition = mouseWorldPos + dragOffset;

            if (useGridSnapping)
                newPosition = SnapToGrid(newPosition);

            transform.position = newPosition;
        }

        void OnMouseUp()
        {
            if (isDragging)
                StopDragging();
        }

        void OnMouseOver()
        {
            // Right-click to edit properties (works even when not selected)
            if (Input.GetMouseButtonDown(1) && allowPropertyEditing)
            {
                OpenPropertyEditor();
                return;
            }

            // Provide visual feedback when hovering
            if (visualComponent != null && !isSelected)
            {
                visualComponent.SetHighlighted(true);
            }
        }

        void OnMouseExit()
        {
            // Remove hover feedback
            if (visualComponent != null && !isSelected)
            {
                visualComponent.SetHighlighted(false);
            }
        }

        #endregion

        #region Selection Management

        public void SetSelected(bool selected)
        {
            if (isSelected == selected) return;

            if (selected)
            {
                // CRITICAL FIX: Deselect previously selected component first
                if (currentlySelected != null && currentlySelected != this)
                {
                    currentlySelected.SetSelected(false);
                }

                // Deselect any selected wire
                var selectedWire = CircuitWire.GetCurrentlySelectedWire();
                if (selectedWire != null)
                {
                    selectedWire.DeselectWire();
                }

                // Mark this as currently selected
                currentlySelected = this;
            }
            else
            {
                // Clear selection if this was the selected component
                if (currentlySelected == this)
                {
                    currentlySelected = null;
                }
            }

            isSelected = selected;

            // Update visual feedback
            if (visualComponent != null)
                visualComponent.SetHighlighted(selected);

            // Fire events
            if (selected)
                OnSelected?.Invoke();
            else
                OnDeselected?.Invoke();

            Debug.Log($"[InteractionComponent] {gameObject.name} {(selected ? "selected" : "deselected")}");
        }

        public static void DeselectAll()
        {
            var allInteractionComponents = FindObjectsByType<InteractionComponent>(FindObjectsSortMode.None);
            foreach (var component in allInteractionComponents)
            {
                component.SetSelected(false);
            }
        }

        #endregion

        #region Movement and Dragging

        private void StartDragging()
        {
            if (!allowMovement || isFixed) return;

            isDragging = true;
            Vector3 mouseWorldPos = GetMouseWorldPosition();
            dragOffset = transform.position - mouseWorldPos;

            Debug.Log($"[InteractionComponent] Started dragging {gameObject.name}");
        }

        private void StopDragging()
        {
            if (!isDragging) return;

            isDragging = false;

            // Notify that component moved
            OnMoved?.Invoke(transform.position);

            // Notify circuit manager if position changed significantly
            if (Vector3.Distance(originalPosition, transform.position) > 0.1f)
            {
                circuitManager?.MarkCircuitChanged();
                originalPosition = transform.position;
            }

            Debug.Log($"[InteractionComponent] Stopped dragging {gameObject.name}");
        }

        private Vector3 GetMouseWorldPosition()
        {
            Vector3 mouseScreenPos = Input.mousePosition;
            Camera camera = Camera.main;
            if (camera == null)
            {
                Debug.LogWarning("[InteractionComponent] No main camera found, using first available camera");
                camera = FindFirstObjectByType<Camera>();
                if (camera == null)
                {
                    Debug.LogError("[InteractionComponent] No camera found in scene");
                    return transform.position;
                }
            }

            mouseScreenPos.z = camera.WorldToScreenPoint(transform.position).z;
            return camera.ScreenToWorldPoint(mouseScreenPos);
        }

        private Vector3 SnapToGrid(Vector3 position)
        {
            float snappedX = Mathf.Round(position.x / gridSize) * gridSize;
            float snappedZ = Mathf.Round(position.z / gridSize) * gridSize;
            return new Vector3(snappedX, position.y, snappedZ);
        }

        public void SetPosition(Vector3 newPosition)
        {
            transform.position = newPosition;
            originalPosition = newPosition;
        }

        #endregion

        #region Connection Handling

        private void HandleConnectionClick()
        {
            if (!allowConnection) return;

            OnConnectionRequested?.Invoke(this);
            Debug.Log($"[InteractionComponent] Connection requested for {gameObject.name}");
        }

        public Vector3 GetConnectionPoint(bool useLeftTerminal = true)
        {
            Transform terminal = useLeftTerminal ? leftTerminal : rightTerminal;
            return terminal != null ? terminal.position : transform.position;
        }

        public Transform GetNearestTerminal(Vector3 worldPosition)
        {
            if (leftTerminal == null && rightTerminal == null) return transform;

            float leftDistance = leftTerminal != null ?
                Vector3.Distance(worldPosition, leftTerminal.position) : float.MaxValue;
            float rightDistance = rightTerminal != null ?
                Vector3.Distance(worldPosition, rightTerminal.position) : float.MaxValue;

            return leftDistance < rightDistance ? leftTerminal : rightTerminal;
        }

        public bool IsWithinConnectionRange(Vector3 worldPosition)
        {
            float distanceToLeft = leftTerminal != null ?
                Vector3.Distance(worldPosition, leftTerminal.position) : float.MaxValue;
            float distanceToRight = rightTerminal != null ?
                Vector3.Distance(worldPosition, rightTerminal.position) : float.MaxValue;

            return Mathf.Min(distanceToLeft, distanceToRight) <= connectionRadius;
        }

        #endregion

        #region Property Editing

        public void OpenPropertyEditor()
        {
            if (!allowPropertyEditing)
            {
                Debug.Log($"[InteractionComponent] Property editing not allowed for {gameObject.name}");
                return;
            }

            Debug.Log($"[InteractionComponent] Opening property editor for {gameObject.name}");

            // Find or create property editor
            var propertyPopup = FindFirstObjectByType<ComponentPropertyPopup>();
            if (propertyPopup != null)
            {
                var circuitComponent = GetComponent<CircuitComponent3D>();
                var circuitComponent3D_v2 = GetComponent<CircuitComponent3D_v2>();

                if (circuitComponent != null)
                {
                    Debug.Log($"[InteractionComponent] Found CircuitComponent3D, showing popup");
                    propertyPopup.ShowForComponent(circuitComponent);
                }
                else if (circuitComponent3D_v2 != null)
                {
                    // For v2 components, we may need to create a wrapper or adapter
                    Debug.LogWarning("[InteractionComponent] Property editing for CircuitComponent3D_v2 not yet implemented");
                }
                else
                {
                    Debug.LogWarning($"[InteractionComponent] No circuit component found on {gameObject.name}");
                }
            }
            else
            {
                Debug.LogWarning("[InteractionComponent] ComponentPropertyPopup not found in scene");
            }
        }

        #endregion

        #region Deletion

        public void DeleteComponent()
        {
            if (!allowDeletion || isFixed) return;

            // Notify before deletion
            OnDeleted?.Invoke();

            // Remove from circuit manager
            var circuitComponent = GetComponent<CircuitComponent3D>();
            var circuitComponent3D_v2 = GetComponent<CircuitComponent3D_v2>();

            if (circuitComponent != null)
            {
                circuitManager?.UnregisterComponent(circuitComponent);
            }
            else if (circuitComponent3D_v2 != null)
            {
                circuitManager?.UnregisterComponent(circuitComponent3D_v2);
            }

            // INTENTIONALLY DO NOT DELETE WIRES
            // Wires will become "dangling" (one endpoint disconnected) but remain in scene
            // This allows users to replace components in larger circuits without redoing all wiring
            // The wire's IsFullyConnected() will return false and it won't participate in circuit solving

            // Delete the component
            Destroy(gameObject);

            Debug.Log($"[InteractionComponent] Deleted component {gameObject.name} (wires preserved)");
        }

        #endregion

        #region Challenge Integration

        /// <summary>
        /// Set as fixed component (for challenge pre-placed objects)
        /// </summary>
        public void SetFixed(bool isFixedComponent)
        {
            isFixed = isFixedComponent;
            allowMovement = !isFixedComponent;
            allowDeletion = !isFixedComponent;

            if (visualComponent != null && isFixedComponent)
            {
                // Visual indication that component is fixed
                // Could add a border or different material
            }

            Debug.Log($"[InteractionComponent] {gameObject.name} set as {(isFixedComponent ? "FIXED" : "MOVABLE")}");
        }

        /// <summary>
        /// Configure interaction permissions
        /// </summary>
        public void ConfigurePermissions(bool selection = true, bool movement = true,
                                       bool connection = true, bool editing = true, bool deletion = true)
        {
            allowSelection = selection;
            allowMovement = movement && !isFixed;
            allowConnection = connection;
            allowPropertyEditing = editing;
            allowDeletion = deletion && !isFixed;
        }

        #endregion

        #region Input Handling

        void Update()
        {
            HandleKeyboardInput();
        }

        private void HandleKeyboardInput()
        {
            if (!isSelected) return;

            // Delete key
            if (Input.GetKeyDown(KeyCode.Delete) || Input.GetKeyDown(KeyCode.X))
            {
                DeleteComponent();
            }

            // Property editor
            if (Input.GetKeyDown(KeyCode.E) || Input.GetMouseButtonDown(1)) // E key or right click
            {
                OpenPropertyEditor();
            }

            // Escape to deselect
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                SetSelected(false);
            }
        }

        #endregion

        [ContextMenu("Debug Interaction State")]
        public void DebugInteractionState()
        {
            Debug.Log($"[InteractionComponent] {gameObject.name}:\n" +
                     $"  Selected: {isSelected}\n" +
                     $"  Dragging: {isDragging}\n" +
                     $"  Fixed: {isFixed}\n" +
                     $"  Allow Selection: {allowSelection}\n" +
                     $"  Allow Movement: {allowMovement}\n" +
                     $"  Allow Connection: {allowConnection}\n" +
                     $"  Position: {transform.position}");
        }
    }
}