using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Real-time misconception detection and intervention for Grade 7 students
/// Catches common electrical circuit mistakes and provides immediate feedback
/// 
/// Educational Impact:
/// - M1 (Sink Model): Catches one-wire circuits immediately
/// - Incomplete circuits: Shows when electricity can't flow
/// - Age-appropriate language: "Let's try..." instead of "Error"
/// </summary>
public class MisconceptionAlert : MonoBehaviour
{
    [Header("UI Settings")]
    public Color alertColor = new Color(1f, 0.3f, 0.3f, 0.9f); // Soft red, not harsh
    public Color successColor = new Color(0.3f, 1f, 0.3f, 0.9f);
    public float alertDuration = 3f;
    public float fadeTime = 0.5f;
    
    [Header("Educational Settings")]
    public bool enableMisconceptionDetection = true;
    public bool showPositiveReinforcement = true;
    
    // Toggle state persistence
    private static bool globalToggleState = true;
    
    // UI Components
    private Canvas alertCanvas;
    private GameObject alertPanel;
    private Text alertText;
    private Image alertBackground;
    
    // State
    private bool isShowingAlert = false;
    private Coroutine currentAlertCoroutine;
    
    // Singleton for easy access
    private static MisconceptionAlert instance;
    public static MisconceptionAlert Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject alertObj = new GameObject("MisconceptionAlert");
                instance = alertObj.AddComponent<MisconceptionAlert>();
                DontDestroyOnLoad(alertObj);
            }
            return instance;
        }
    }
    
    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeUI();
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        // Start checking for misconceptions every 2 seconds
        if (enableMisconceptionDetection)
        {
            InvokeRepeating("CheckForMisconceptions", 1f, 2f);
        }
    }
    
    void InitializeUI()
    {
        // Create UI Canvas
        alertCanvas = gameObject.AddComponent<Canvas>();
        alertCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        alertCanvas.sortingOrder = 1000; // Above everything else
        
        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        
        gameObject.AddComponent<GraphicRaycaster>();
        
        // Create Alert Panel
        alertPanel = new GameObject("AlertPanel");
        alertPanel.transform.SetParent(alertCanvas.transform, false);
        
        alertBackground = alertPanel.AddComponent<Image>();
        alertBackground.color = alertColor;
        
        RectTransform panelRect = alertPanel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.2f, 0.8f);
        panelRect.anchorMax = new Vector2(0.8f, 0.95f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
        
        // Create Alert Text
        GameObject textObj = new GameObject("AlertText");
        textObj.transform.SetParent(alertPanel.transform, false);
        
        alertText = textObj.AddComponent<Text>();
        alertText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        alertText.fontSize = 28;
        alertText.color = Color.white;
        alertText.alignment = TextAnchor.MiddleCenter;
        alertText.text = "";
        
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(20, 10);
        textRect.offsetMax = new Vector2(-20, -10);
        
        // Start hidden
        alertPanel.SetActive(false);
        
        Debug.Log("MisconceptionAlert UI initialized");
    }
    
    void CheckForMisconceptions()
    {
        if (!enableMisconceptionDetection || isShowingAlert) return;
        
        CircuitManager circuitManager = CircuitManager.Instance;
        if (circuitManager == null || circuitManager.ComponentCount == 0) return;
        
        // Check for common Grade 7 misconceptions
        CheckM1SingleWireCircuits(circuitManager);
        CheckIncompleteCircuits(circuitManager);
        CheckUnconnectedBatteries(circuitManager);
        CheckPositiveCircuitCompletion(circuitManager);
    }
    
    void CheckM1SingleWireCircuits(CircuitManager circuitManager)
    {
        // M1 Misconception: Students think one wire is enough
        var batteries = FindBatteries(circuitManager);
        
        foreach (var battery in batteries)
        {
            int wireCount = battery.GetWireCount();
            if (wireCount == 1)
            {
                ShowAlert("🔋 Batteries need TWO connections - one at each end! Try connecting the other side.", alertColor);
                return;
            }
        }
    }
    
    void CheckIncompleteCircuits(CircuitManager circuitManager)
    {
        // Check if we have components but no current flow
        if (circuitManager.ComponentCount >= 2 && circuitManager.WireCount >= 1)
        {
            var batteries = FindBatteries(circuitManager);
            if (batteries.Count > 0)
            {
                float totalCurrent = 0f;
                foreach (var battery in batteries)
                {
                    totalCurrent += Mathf.Abs(battery.current);
                }
                
                if (totalCurrent < 0.01f) // No significant current flowing
                {
                    ShowAlert("⚡ Electricity needs a complete path! Try connecting all components in a loop.", alertColor);
                    return;
                }
            }
        }
    }
    
    void CheckUnconnectedBatteries(CircuitManager circuitManager)
    {
        var batteries = FindBatteries(circuitManager);
        
        foreach (var battery in batteries)
        {
            if (battery.GetWireCount() == 0)
            {
                ShowAlert("🔌 Your battery isn't connected! Click the Connect tool (C key) and connect some wires.", alertColor);
                return;
            }
        }
    }
    
    void CheckPositiveCircuitCompletion(CircuitManager circuitManager)
    {
        // Positive reinforcement when students complete working circuits
        if (!showPositiveReinforcement) return;
        
        var batteries = FindBatteries(circuitManager);
        if (batteries.Count > 0 && circuitManager.ComponentCount >= 3) // Battery + at least 2 other components
        {
            float totalCurrent = 0f;
            foreach (var battery in batteries)
            {
                totalCurrent += Mathf.Abs(battery.current);
            }
            
            if (totalCurrent > 0.1f) // Good current flow
            {
                var bulbs = FindBulbs(circuitManager);
                if (bulbs.Count > 0)
                {
                    ShowAlert("🌟 Excellent! Your circuit is working and the electricity is flowing!", successColor, 2f);
                    return;
                }
            }
        }
    }
    
    System.Collections.Generic.List<CircuitComponent3D> FindBatteries(CircuitManager circuitManager)
    {
        var batteries = new System.Collections.Generic.List<CircuitComponent3D>();
        foreach (var component in circuitManager.Components)
        {
            if (component != null && component.ComponentType == ComponentType.Battery)
            {
                batteries.Add(component);
            }
        }
        return batteries;
    }
    
    System.Collections.Generic.List<CircuitComponent3D> FindBulbs(CircuitManager circuitManager)
    {
        var bulbs = new System.Collections.Generic.List<CircuitComponent3D>();
        foreach (var component in circuitManager.Components)
        {
            if (component != null && component.ComponentType == ComponentType.Bulb)
            {
                bulbs.Add(component);
            }
        }
        return bulbs;
    }
    
    public void ShowAlert(string message, Color bgColor, float duration = 0f)
    {
        if (isShowingAlert)
        {
            // Cancel existing alert
            if (currentAlertCoroutine != null)
            {
                StopCoroutine(currentAlertCoroutine);
            }
        }
        
        float actualDuration = duration > 0f ? duration : alertDuration;
        currentAlertCoroutine = StartCoroutine(ShowAlertCoroutine(message, bgColor, actualDuration));
    }
    
    IEnumerator ShowAlertCoroutine(string message, Color bgColor, float duration)
    {
        isShowingAlert = true;
        
        // Setup alert
        alertText.text = message;
        alertBackground.color = bgColor;
        alertPanel.SetActive(true);
        
        // Fade in
        yield return StartCoroutine(FadeAlert(0f, 1f, fadeTime));
        
        // Wait
        yield return new WaitForSeconds(duration);
        
        // Fade out
        yield return StartCoroutine(FadeAlert(1f, 0f, fadeTime));
        
        // Hide
        alertPanel.SetActive(false);
        isShowingAlert = false;
        currentAlertCoroutine = null;
        
        Debug.Log($"Showed misconception alert: {message}");
    }
    
    IEnumerator FadeAlert(float fromAlpha, float toAlpha, float duration)
    {
        float startTime = Time.time;
        Color bgColor = alertBackground.color;
        Color textColor = alertText.color;
        
        while (Time.time - startTime < duration)
        {
            float progress = (Time.time - startTime) / duration;
            float alpha = Mathf.Lerp(fromAlpha, toAlpha, progress);
            
            bgColor.a = alpha;
            textColor.a = alpha;
            
            alertBackground.color = bgColor;
            alertText.color = textColor;
            
            yield return null;
        }
        
        // Ensure final alpha
        bgColor.a = toAlpha;
        textColor.a = toAlpha;
        alertBackground.color = bgColor;
        alertText.color = textColor;
    }
    
    // Manual testing and debugging
    [ContextMenu("Test M1 Alert")]
    public void TestM1Alert()
    {
        ShowAlert("🔋 Batteries need TWO connections - one at each end! Try connecting the other side.", alertColor);
    }
    
    [ContextMenu("Test Success Alert")]
    public void TestSuccessAlert()
    {
        ShowAlert("🌟 Excellent! Your circuit is working and the electricity is flowing!", successColor);
    }
    
    [ContextMenu("Test Incomplete Circuit Alert")]
    public void TestIncompleteAlert()
    {
        ShowAlert("⚡ Electricity needs a complete path! Try connecting all components in a loop.", alertColor);
    }
    
    // Public toggle methods for UI or keyboard shortcuts
    public void ToggleMisconceptionDetection()
    {
        enableMisconceptionDetection = !enableMisconceptionDetection;
        globalToggleState = enableMisconceptionDetection;
        
        string status = enableMisconceptionDetection ? "ON" : "OFF";
        Debug.Log($"Misconception Detection: {status}");
        
        // Show feedback to user
        if (enableMisconceptionDetection)
        {
            ShowAlert("📚 Learning assistance is ON - I'll help you avoid common mistakes!", successColor, 2f);
        }
        else
        {
            ShowAlert("🔧 Learning assistance is OFF - Explore freely!", new Color(0.5f, 0.5f, 1f, 0.9f), 2f);
        }
    }
    
    public void SetMisconceptionDetection(bool enabled)
    {
        enableMisconceptionDetection = enabled;
        globalToggleState = enabled;
        Debug.Log($"Misconception Detection set to: {enabled}");
    }
    
    public bool IsMisconceptionDetectionEnabled()
    {
        return enableMisconceptionDetection;
    }
    
    void OnDestroy()
    {
        CancelInvoke();
        if (currentAlertCoroutine != null)
        {
            StopCoroutine(currentAlertCoroutine);
        }
    }
}