using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Triggers tooltip display on hover for UI elements
/// </summary>
public class TooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public string tooltipText;
    
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!string.IsNullOrEmpty(tooltipText))
        {
            TooltipManager.Instance.ShowTooltip(tooltipText, transform.position);
        }
    }
    
    public void OnPointerExit(PointerEventData eventData)
    {
        TooltipManager.Instance.HideTooltip();
    }
}