using UnityEngine;

public class SelectableComponent : MonoBehaviour
{
    [Header("Selection Visual")]
    public Color normalColor;
    public Color selectedColor = Color.cyan;
    
    private Renderer componentRenderer;
    private bool isSelected = false;
    private static SelectableComponent currentlySelected = null;
    
    void Start()
    {
        componentRenderer = GetComponent<Renderer>();
        if (componentRenderer != null)
        {
            normalColor = componentRenderer.material.color;
        }
    }
    
    void OnMouseDown()
    {
        // Check if connect tool is active
        ConnectTool connectTool = FindFirstObjectByType<ConnectTool>();
        if (connectTool != null && connectTool.IsConnectMode())
        {
            connectTool.OnComponentClicked(this);
        }
        else
        {
            SelectThis();
        }
    }
    
    void OnMouseOver()
    {
        // Right-click to edit properties
        if (Input.GetMouseButtonDown(1))
        {
            Debug.Log($"Right-click detected on {gameObject.name}");
            CircuitComponent3D circuitComp = GetComponent<CircuitComponent3D>();
            if (circuitComp != null)
            {
                Debug.Log($"CircuitComponent3D found: Type = {circuitComp.ComponentType}");
                ComponentPropertyPopup popup = ComponentPropertyPopup.Instance;
                if (popup != null)
                {
                    popup.ShowForComponent(circuitComp);
                    Debug.Log($"Opening property popup for {gameObject.name} ({circuitComp.ComponentType})");
                }
                else
                {
                    Debug.LogError("ComponentPropertyPopup.Instance is null!");
                }
            }
            else
            {
                Debug.LogError($"No CircuitComponent3D on {gameObject.name}!");
            }
        }
    }
    
    public void SetHighlight(bool highlighted)
    {
        if (componentRenderer != null)
        {
            if (highlighted)
            {
                componentRenderer.material.color = Color.magenta;
            }
            else
            {
                // Return to normal or selected color
                componentRenderer.material.color = isSelected ? selectedColor : normalColor;
            }
        }
    }
    
    public void SelectThis()
    {
        // Deselect previous component
        if (currentlySelected != null && currentlySelected != this)
        {
            currentlySelected.Deselect();
        }
        
        // Select this component
        isSelected = true;
        currentlySelected = this;
        
        if (componentRenderer != null)
        {
            componentRenderer.material.color = selectedColor;
        }
        
        Debug.Log($"Selected: {gameObject.name}");
    }
    
    public void Deselect()
    {
        isSelected = false;
        
        if (componentRenderer != null)
        {
            componentRenderer.material.color = normalColor;
        }
        
        if (currentlySelected == this)
        {
            currentlySelected = null;
        }
    }
    
    void Update()
    {
        // ESC to deselect all
        if (Input.GetKeyDown(KeyCode.Escape) && isSelected)
        {
            Deselect();
        }
        
        // Delete key to remove selected component
        if (Input.GetKeyDown(KeyCode.Delete) && isSelected)
        {
            DeleteThis();
        }
    }
    
    void DeleteThis()
    {
        Debug.Log($"Deleting: {gameObject.name}");
        
        // Clear selection reference
        if (currentlySelected == this)
        {
            currentlySelected = null;
        }
        
        // Remove from ComponentFactoryManager list if needed
        ComponentFactoryManager factoryManager = FindFirstObjectByType<ComponentFactoryManager>();
        if (factoryManager != null)
        {
            factoryManager.RemoveComponent(gameObject);
        }
        
        Destroy(gameObject);
    }
    
    public bool IsSelected()
    {
        return isSelected;
    }
    
    
    public static SelectableComponent GetCurrentlySelected()
    {
        return currentlySelected;
    }
}
