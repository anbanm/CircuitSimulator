using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages tooltip display for UI elements
/// </summary>
public class TooltipManager : MonoBehaviour
{
    private static TooltipManager instance;
    public static TooltipManager Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject tooltipObj = new GameObject("TooltipManager");
                instance = tooltipObj.AddComponent<TooltipManager>();
            }
            return instance;
        }
    }
    
    private GameObject tooltipPanel;
    private Text tooltipText;
    private Canvas tooltipCanvas;
    
    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            CreateTooltipUI();
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }
    
    void CreateTooltipUI()
    {
        // Create canvas for tooltips
        GameObject canvasObj = new GameObject("TooltipCanvas");
        canvasObj.transform.SetParent(transform);
        
        tooltipCanvas = canvasObj.AddComponent<Canvas>();
        tooltipCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        tooltipCanvas.sortingOrder = 1000; // Always on top
        
        canvasObj.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasObj.AddComponent<GraphicRaycaster>();
        
        // Create tooltip panel
        tooltipPanel = new GameObject("TooltipPanel");
        tooltipPanel.transform.SetParent(canvasObj.transform, false);
        
        RectTransform panelRect = tooltipPanel.AddComponent<RectTransform>();
        panelRect.sizeDelta = new Vector2(200, 30);
        
        Image panelImage = tooltipPanel.AddComponent<Image>();
        panelImage.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);
        
        // Create text
        GameObject textObj = new GameObject("TooltipText");
        textObj.transform.SetParent(tooltipPanel.transform, false);
        
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.one * 5; // 5px padding
        textRect.offsetMax = Vector2.one * -5;
        
        tooltipText = textObj.AddComponent<Text>();
        tooltipText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        tooltipText.fontSize = 12;
        tooltipText.color = Color.white;
        tooltipText.alignment = TextAnchor.MiddleCenter;
        
        // Hide initially
        tooltipPanel.SetActive(false);
    }
    
    public void ShowTooltip(string text, Vector3 position)
    {
        if (tooltipPanel == null || tooltipText == null) return;
        
        tooltipText.text = text;
        
        // Convert world position to screen position
        Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(Camera.main, position);
        
        // Adjust position to keep tooltip on screen
        RectTransform panelRect = tooltipPanel.GetComponent<RectTransform>();
        panelRect.position = screenPos + Vector2.up * 40; // Offset above cursor
        
        tooltipPanel.SetActive(true);
    }
    
    public void HideTooltip()
    {
        if (tooltipPanel != null)
            tooltipPanel.SetActive(false);
    }
}