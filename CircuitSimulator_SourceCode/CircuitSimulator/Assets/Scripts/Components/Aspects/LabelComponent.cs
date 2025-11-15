using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CircuitSimulator.Components
{
    /// <summary>
    /// Aspect component responsible for label display and value visualization.
    /// Handles voltage, current, and resistance display with educational formatting.
    /// </summary>
    public class LabelComponent : MonoBehaviour
    {
        [Header("Label Settings")]
        [SerializeField] private bool showVoltage = true;
        [SerializeField] private bool showCurrent = true;
        [SerializeField] private bool showResistance = true;
        [SerializeField] private bool showPower = false;
        [SerializeField] private bool showComponentName = true;

        [Header("Display Format")]
        [SerializeField] private string voltageFormat = "F2";
        [SerializeField] private string currentFormat = "F3";
        [SerializeField] private string resistanceFormat = "F1";
        [SerializeField] private string powerFormat = "F2";

        [Header("Label Positioning")]
        [SerializeField] private Vector3 labelOffset = new Vector3(0, 1.5f, 0);
        [SerializeField] private bool faceCamera = true;
        [SerializeField] private bool useScreenSpace = false;

        [Header("Visual Style")]
        [SerializeField] private Color textColor = Color.white;
        [SerializeField] private float fontSize = 12f;
        [SerializeField] private Color backgroundColor = new Color(0, 0, 0, 0.7f);

        // Label objects
        private GameObject labelObject;
        private TextMeshPro textMesh;
        private Canvas labelCanvas;
        private TextMeshProUGUI canvasText;

        // Component references
        private ElectricalComponent electricalComponent;
        private Camera mainCamera;

        // State
        private string currentDisplayText = "";
        private bool isInitialized = false;

        void Start()
        {
            InitializeLabel();
            FindComponentReferences();
            SubscribeToElectricalEvents();
            UpdateDisplay();
        }

        void Update()
        {
            if (faceCamera && mainCamera != null && labelObject != null)
            {
                UpdateCameraFacing();
            }
        }

        void OnDestroy()
        {
            UnsubscribeFromElectricalEvents();
        }

        private void InitializeLabel()
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                Debug.LogWarning("[LabelComponent] No main camera found, using first available camera");
                mainCamera = FindFirstObjectByType<Camera>();
            }

            if (useScreenSpace)
            {
                CreateScreenSpaceLabel();
            }
            else
            {
                CreateWorldSpaceLabel();
            }

            isInitialized = true;
        }

        private void CreateWorldSpaceLabel()
        {
            // Create label GameObject
            labelObject = new GameObject($"{gameObject.name}_Label");
            labelObject.transform.SetParent(transform);
            labelObject.transform.localPosition = labelOffset;

            // Add TextMeshPro component
            textMesh = labelObject.AddComponent<TextMeshPro>();
            textMesh.text = "Initializing...";
            textMesh.fontSize = fontSize;
            textMesh.color = textColor;
            textMesh.alignment = TextAlignmentOptions.Center;

            // Add background
            var backgroundQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            backgroundQuad.transform.SetParent(labelObject.transform);
            backgroundQuad.transform.localPosition = new Vector3(0, 0, 0.01f);
            backgroundQuad.transform.localScale = new Vector3(2f, 1f, 1f);

            var backgroundRenderer = backgroundQuad.GetComponent<Renderer>();
            var shader = Shader.Find("Standard");
            if (shader == null)
            {
                Debug.LogWarning("[LabelComponent] Standard shader not found, using legacy shader");
                shader = Shader.Find("Legacy Shaders/Diffuse");
            }

            if (shader != null)
            {
                var backgroundMaterial = new Material(shader);
                backgroundMaterial.color = backgroundColor;

                // Only set transparency properties if using Standard shader
                if (shader.name == "Standard")
                {
                    backgroundMaterial.SetFloat("_Mode", 3f); // Transparent
                    backgroundMaterial.EnableKeyword("_ALPHABLEND_ON");
                }

                backgroundRenderer.material = backgroundMaterial;
            }
            else
            {
                Debug.LogError("[LabelComponent] Could not find any suitable shader for background material");
            }

            // Remove collider from background
            Destroy(backgroundQuad.GetComponent<Collider>());
        }

        private void CreateScreenSpaceLabel()
        {
            // Create canvas for screen space labels
            labelObject = new GameObject($"{gameObject.name}_ScreenLabel");
            labelCanvas = labelObject.AddComponent<Canvas>();
            labelCanvas.renderMode = RenderMode.WorldSpace;
            labelCanvas.worldCamera = mainCamera;

            var canvasScaler = labelObject.AddComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(1920, 1080);

            labelObject.AddComponent<GraphicRaycaster>();

            // Position canvas
            labelObject.transform.SetParent(transform);
            labelObject.transform.localPosition = labelOffset;
            labelObject.transform.localScale = Vector3.one * 0.01f; // Scale down for world space

            // Create text element
            var textGO = new GameObject("Text");
            textGO.transform.SetParent(labelObject.transform);

            canvasText = textGO.AddComponent<TextMeshProUGUI>();
            canvasText.text = "Initializing...";
            canvasText.fontSize = fontSize;
            canvasText.color = textColor;
            canvasText.alignment = TextAlignmentOptions.Center;

            var rectTransform = textGO.GetComponent<RectTransform>();
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = new Vector2(200, 100);
        }

        private void FindComponentReferences()
        {
            electricalComponent = GetComponent<ElectricalComponent>();
            if (electricalComponent == null)
            {
                Debug.LogWarning($"[LabelComponent] No ElectricalComponent found on {gameObject.name}");
            }
        }

        private void SubscribeToElectricalEvents()
        {
            if (electricalComponent != null)
            {
                electricalComponent.OnVoltageChanged += OnElectricalValueChanged;
                electricalComponent.OnCurrentChanged += OnElectricalValueChanged;
                electricalComponent.OnResistanceChanged += OnElectricalValueChanged;
            }
        }

        private void UnsubscribeFromElectricalEvents()
        {
            if (electricalComponent != null)
            {
                electricalComponent.OnVoltageChanged -= OnElectricalValueChanged;
                electricalComponent.OnCurrentChanged -= OnElectricalValueChanged;
                electricalComponent.OnResistanceChanged -= OnElectricalValueChanged;
            }
        }

        private void OnElectricalValueChanged(float value)
        {
            UpdateDisplay();
        }

        #region Display Updates

        public void UpdateDisplay()
        {
            if (!isInitialized || electricalComponent == null) return;

            string displayText = BuildDisplayText();

            if (displayText != currentDisplayText)
            {
                currentDisplayText = displayText;
                SetLabelText(displayText);
            }
        }

        private string BuildDisplayText()
        {
            var lines = new System.Collections.Generic.List<string>();

            // Component name
            if (showComponentName)
            {
                lines.Add($"<b>{gameObject.name}</b>");
            }

            // Electrical values
            if (showVoltage)
            {
                float voltage = electricalComponent.VoltageDrop;
                lines.Add($"V: {voltage.ToString(voltageFormat)}V");
            }

            if (showCurrent)
            {
                float current = electricalComponent.Current;
                string currentText = FormatCurrent(current);
                lines.Add($"I: {currentText}");
            }

            if (showResistance)
            {
                float resistance = electricalComponent.Resistance;
                string resistanceText = FormatResistance(resistance);
                lines.Add($"R: {resistanceText}");
            }

            if (showPower)
            {
                float power = electricalComponent.GetPower();
                lines.Add($"P: {power.ToString(powerFormat)}W");
            }

            // Educational indicators
            if (electricalComponent.IsSwitch)
            {
                string switchState = electricalComponent.SwitchState ? "CLOSED" : "OPEN";
                lines.Add($"<color=yellow>[{switchState}]</color>");
            }

            if (electricalComponent.IsVoltageSource)
            {
                lines.Add("<color=red>[SOURCE]</color>");
            }

            return string.Join("\n", lines);
        }

        private string FormatCurrent(float current)
        {
            if (Mathf.Abs(current) < 0.001f)
                return "0.000A";

            if (Mathf.Abs(current) < 1f)
                return $"{(current * 1000f).ToString(currentFormat)}mA";

            return $"{current.ToString(currentFormat)}A";
        }

        private string FormatResistance(float resistance)
        {
            if (float.IsPositiveInfinity(resistance))
                return "∞Ω";

            if (resistance < 0.001f)
                return "0Ω";

            if (resistance >= 1000000f)
                return $"{(resistance / 1000000f).ToString(resistanceFormat)}MΩ";

            if (resistance >= 1000f)
                return $"{(resistance / 1000f).ToString(resistanceFormat)}kΩ";

            return $"{resistance.ToString(resistanceFormat)}Ω";
        }

        private void SetLabelText(string text)
        {
            if (useScreenSpace && canvasText != null)
            {
                canvasText.text = text;
            }
            else if (textMesh != null)
            {
                textMesh.text = text;
            }
        }

        private void UpdateCameraFacing()
        {
            if (labelObject != null && mainCamera != null)
            {
                labelObject.transform.LookAt(mainCamera.transform);
                labelObject.transform.Rotate(0, 180, 0); // Flip to face camera correctly
            }
        }

        #endregion

        #region Public Interface

        /// <summary>
        /// Configure which values to display
        /// </summary>
        public void ConfigureDisplay(bool voltage = true, bool current = true,
                                   bool resistance = true, bool power = false, bool componentName = true)
        {
            showVoltage = voltage;
            showCurrent = current;
            showResistance = resistance;
            showPower = power;
            showComponentName = componentName;

            UpdateDisplay();
        }

        /// <summary>
        /// Set label visibility
        /// </summary>
        public void SetVisible(bool visible)
        {
            if (labelObject != null)
                labelObject.SetActive(visible);
        }

        /// <summary>
        /// Update label position offset
        /// </summary>
        public void SetLabelOffset(Vector3 offset)
        {
            labelOffset = offset;
            if (labelObject != null)
                labelObject.transform.localPosition = offset;
        }

        /// <summary>
        /// Apply display settings from ComponentDefinition (ScriptableObjects)
        /// </summary>
        public void ApplyDefinition(CircuitSimulator.ComponentDefinition definition)
        {
            if (definition == null) return;

            var visual = definition.visualProperties;
            showVoltage = visual.showsVoltageIndicator;

            // Customize based on component type
            switch (definition.baseType)
            {
                case BaseComponentType.Battery:
                    ConfigureDisplay(voltage: true, current: true, resistance: false, power: true);
                    break;

                case BaseComponentType.Resistor:
                    ConfigureDisplay(voltage: true, current: true, resistance: true, power: false);
                    break;

                case BaseComponentType.Bulb:
                    ConfigureDisplay(voltage: true, current: true, resistance: false, power: true);
                    break;

                case BaseComponentType.Switch:
                    ConfigureDisplay(voltage: false, current: true, resistance: false, power: false);
                    break;

                case BaseComponentType.Junction:
                    ConfigureDisplay(voltage: false, current: true, resistance: false, power: false);
                    break;
            }

            Debug.Log($"[LabelComponent] Applied definition display settings to {gameObject.name}");
        }

        /// <summary>
        /// Apply display settings from ThemedComponentDefinition
        /// </summary>
        public void ApplyDefinition(CircuitSimulator.Components.ThemedComponentDefinition definition)
        {
            if (definition == null) return;

            var visual = definition.visualProperties;
            showVoltage = visual.showsVoltageIndicator;

            // Customize based on component type
            switch (definition.baseType)
            {
                case ThemedBaseComponentType.Battery:
                    ConfigureDisplay(voltage: true, current: true, resistance: false, power: true);
                    break;

                case ThemedBaseComponentType.Resistor:
                    ConfigureDisplay(voltage: true, current: true, resistance: true, power: false);
                    break;

                case ThemedBaseComponentType.Bulb:
                    ConfigureDisplay(voltage: true, current: true, resistance: false, power: true);
                    break;

                case ThemedBaseComponentType.Switch:
                    ConfigureDisplay(voltage: false, current: true, resistance: false, power: false);
                    break;

                case ThemedBaseComponentType.Junction:
                    ConfigureDisplay(voltage: false, current: true, resistance: false, power: false);
                    break;
            }

            Debug.Log($"[LabelComponent] Applied themed definition display settings to {gameObject.name}");
        }

        /// <summary>
        /// Show educational information
        /// </summary>
        public void ShowEducationalInfo(string info)
        {
            if (!string.IsNullOrEmpty(info))
            {
                string educationalText = $"<color=cyan>[INFO]</color>\n{info}";
                SetLabelText(currentDisplayText + "\n" + educationalText);

                // Auto-hide after delay
                StartCoroutine(HideEducationalInfoAfterDelay(5f));
            }
        }

        private System.Collections.IEnumerator HideEducationalInfoAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            UpdateDisplay(); // Refresh to normal display
        }

        /// <summary>
        /// Highlight specific values (for educational emphasis)
        /// </summary>
        public void HighlightValue(string valueType, bool highlight)
        {
            // Could implement value highlighting for educational purposes
            UpdateDisplay();
        }

        #endregion

        [ContextMenu("Debug Label State")]
        public void DebugLabelState()
        {
            Debug.Log($"[LabelComponent] {gameObject.name}:\n" +
                     $"  Display Text: {currentDisplayText}\n" +
                     $"  Show Voltage: {showVoltage}\n" +
                     $"  Show Current: {showCurrent}\n" +
                     $"  Show Resistance: {showResistance}\n" +
                     $"  Use Screen Space: {useScreenSpace}\n" +
                     $"  Face Camera: {faceCamera}");
        }

        [ContextMenu("Force Update Display")]
        public void ForceUpdateDisplay()
        {
            currentDisplayText = ""; // Force refresh
            UpdateDisplay();
        }
    }
}