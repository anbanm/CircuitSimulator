using UnityEngine;
using CircuitSimulator; // Explicitly import root namespace

namespace CircuitSimulator.Components
{
    /// <summary>
    /// Aspect component responsible for visual representation and effects.
    /// Handles 3D models, materials, colors, and visual feedback.
    /// </summary>
    public class VisualComponent : MonoBehaviour
    {
        [Header("Visual Elements")]
        [SerializeField] private Renderer componentRenderer;
        [SerializeField] private Material defaultMaterial;
        [SerializeField] private Material activeMaterial;
        [SerializeField] private Material errorMaterial;

        [Header("Colors")]
        [SerializeField] private Color defaultColor = Color.gray;
        [SerializeField] private Color activeColor = Color.green;
        [SerializeField] private Color errorColor = Color.red;
        [SerializeField] private Color currentColor;

        [Header("Effects")]
        [SerializeField] private bool hasGlowEffect = false;
        [SerializeField] private bool hasCurrentAnimation = false;
        [SerializeField] private bool showsVoltageIndicator = true;
        [SerializeField] private GameObject glowEffect;
        [SerializeField] private ParticleSystem currentFlowEffect;

        [Header("Scale and Transform")]
        [SerializeField] private Vector3 originalScale;
        [SerializeField] private bool allowScaling = true;

        // Visual state
        private VisualState currentState = VisualState.Default;
        private ElectricalComponent electricalComponent;

        // Events
        public System.Action<VisualState> OnVisualStateChanged;

        public enum VisualState
        {
            Default,    // Normal appearance
            Active,     // Has current flowing through it
            Error,      // Error state (disconnected, etc.)
            Highlighted // Selected or highlighted
        }

        void Start()
        {
            InitializeVisual();
            FindElectricalComponent();
            SubscribeToElectricalEvents();
        }

        void OnDestroy()
        {
            UnsubscribeFromElectricalEvents();
        }

        private void InitializeVisual()
        {
            // Get renderer if not assigned
            if (componentRenderer == null)
                componentRenderer = GetComponent<Renderer>();

            // Store original scale
            originalScale = transform.localScale;

            // Set initial color
            currentColor = defaultColor;
            UpdateVisualAppearance();

            // Initialize effects
            if (hasGlowEffect && glowEffect == null)
                CreateGlowEffect();

            if (hasCurrentAnimation && currentFlowEffect == null)
                CreateCurrentFlowEffect();
        }

        private void FindElectricalComponent()
        {
            electricalComponent = GetComponent<ElectricalComponent>();
            if (electricalComponent == null)
            {
                Debug.LogWarning($"[VisualComponent] No ElectricalComponent found on {gameObject.name}");
            }
        }

        private void SubscribeToElectricalEvents()
        {
            if (electricalComponent != null)
            {
                electricalComponent.OnCurrentChanged += OnCurrentChanged;
                electricalComponent.OnVoltageChanged += OnVoltageChanged;
                electricalComponent.OnSwitchStateChanged += OnSwitchStateChanged;
            }
        }

        private void UnsubscribeFromElectricalEvents()
        {
            if (electricalComponent != null)
            {
                electricalComponent.OnCurrentChanged -= OnCurrentChanged;
                electricalComponent.OnVoltageChanged -= OnVoltageChanged;
                electricalComponent.OnSwitchStateChanged -= OnSwitchStateChanged;
            }
        }

        #region Visual State Management

        public void SetVisualState(VisualState newState)
        {
            if (currentState != newState)
            {
                currentState = newState;
                UpdateVisualAppearance();
                OnVisualStateChanged?.Invoke(currentState);
            }
        }

        private void UpdateVisualAppearance()
        {
            switch (currentState)
            {
                case VisualState.Default:
                    ApplyDefaultAppearance();
                    break;
                case VisualState.Active:
                    ApplyActiveAppearance();
                    break;
                case VisualState.Error:
                    ApplyErrorAppearance();
                    break;
                case VisualState.Highlighted:
                    ApplyHighlightedAppearance();
                    break;
            }

            UpdateEffects();
        }

        private void ApplyDefaultAppearance()
        {
            currentColor = defaultColor;
            SetRendererColor(currentColor);
            SetGlowIntensity(0f);
        }

        private void ApplyActiveAppearance()
        {
            currentColor = activeColor;
            SetRendererColor(currentColor);

            if (hasGlowEffect)
                SetGlowIntensity(0.5f);
        }

        private void ApplyErrorAppearance()
        {
            currentColor = errorColor;
            SetRendererColor(currentColor);

            if (hasGlowEffect)
                SetGlowIntensity(0.8f);
        }

        private void ApplyHighlightedAppearance()
        {
            // Highlighted state with pulsing effect
            currentColor = Color.Lerp(defaultColor, Color.yellow, 0.7f);
            SetRendererColor(currentColor);

            if (hasGlowEffect)
                SetGlowIntensity(0.3f);
        }

        #endregion

        #region Electrical Event Handlers

        private void OnCurrentChanged(float newCurrent)
        {
            // Update visual state based on current flow
            if (newCurrent > 0.001f)
            {
                SetVisualState(VisualState.Active);

                if (hasCurrentAnimation)
                    StartCurrentAnimation(newCurrent);
            }
            else
            {
                SetVisualState(VisualState.Default);

                if (hasCurrentAnimation)
                    StopCurrentAnimation();
            }
        }

        private void OnVoltageChanged(float newVoltage)
        {
            // Could adjust visual intensity based on voltage
            if (hasGlowEffect && currentState == VisualState.Active)
            {
                float intensity = Mathf.Clamp01(newVoltage / 12f); // Normalize to 12V
                SetGlowIntensity(intensity * 0.5f);
            }
        }

        private void OnSwitchStateChanged(bool switchState)
        {
            // Visual feedback for switch state
            if (electricalComponent != null && electricalComponent.IsSwitch)
            {
                if (switchState) // Closed
                {
                    SetVisualState(VisualState.Active);
                }
                else // Open
                {
                    SetVisualState(VisualState.Default);
                }
            }
        }

        #endregion

        #region Material and Color Management

        private void SetRendererColor(Color color)
        {
            if (componentRenderer == null) return;

            // Clone material to avoid shared material modification
            if (componentRenderer.material != null)
            {
                var material = new Material(componentRenderer.material);
                material.color = color;
                componentRenderer.material = material;
            }
        }

        public void SetMaterial(Material material)
        {
            if (componentRenderer != null && material != null)
            {
                componentRenderer.material = material;
            }
        }

        #endregion

        #region Effects Management

        private void CreateGlowEffect()
        {
            // Create simple glow effect using additional renderer
            glowEffect = new GameObject("GlowEffect");
            glowEffect.transform.SetParent(transform);
            glowEffect.transform.localPosition = Vector3.zero;
            glowEffect.transform.localScale = Vector3.one * 1.1f;

            var glowRenderer = glowEffect.AddComponent<MeshRenderer>();
            var meshFilter = glowEffect.AddComponent<MeshFilter>();

            // Copy mesh from parent
            if (componentRenderer != null)
            {
                var parentMeshFilter = componentRenderer.GetComponent<MeshFilter>();
                if (parentMeshFilter != null)
                    meshFilter.mesh = parentMeshFilter.mesh;
            }

            // Create glow material
            var standardShader = Shader.Find("Standard");
            if (standardShader == null)
            {
                Debug.LogWarning("[VisualComponent] Standard shader not found, using default material");
                standardShader = Shader.Find("Legacy Shaders/Diffuse");
            }

            if (standardShader != null)
            {
                var glowMaterial = new Material(standardShader);
                glowMaterial.SetFloat("_Mode", 3f); // Transparent mode
                glowMaterial.color = new Color(activeColor.r, activeColor.g, activeColor.b, 0f);
                glowMaterial.EnableKeyword("_ALPHABLEND_ON");
                glowRenderer.material = glowMaterial;
            }
            else
            {
                Debug.LogError("[VisualComponent] Could not find any suitable shader for glow effect");
            }

            glowEffect.SetActive(false);
        }

        private void CreateCurrentFlowEffect()
        {
            // Create particle system for current flow visualization
            var effectGO = new GameObject("CurrentFlowEffect");
            effectGO.transform.SetParent(transform);
            effectGO.transform.localPosition = Vector3.zero;

            currentFlowEffect = effectGO.AddComponent<ParticleSystem>();
            var main = currentFlowEffect.main;
            main.startLifetime = 2f;
            main.startSpeed = 1f;
            main.startColor = Color.cyan;
            main.maxParticles = 10;

            var emission = currentFlowEffect.emission;
            emission.enabled = false;

            var shape = currentFlowEffect.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.5f;
        }

        private void SetGlowIntensity(float intensity)
        {
            if (glowEffect == null) return;

            bool shouldGlow = intensity > 0.01f;
            glowEffect.SetActive(shouldGlow);

            if (shouldGlow)
            {
                var glowRenderer = glowEffect.GetComponent<Renderer>();
                if (glowRenderer != null)
                {
                    var material = glowRenderer.material;
                    var color = material.color;
                    color.a = intensity;
                    material.color = color;
                }
            }
        }

        private void StartCurrentAnimation(float current)
        {
            if (currentFlowEffect == null) return;

            var emission = currentFlowEffect.emission;
            emission.enabled = true;
            emission.rateOverTime = Mathf.Clamp(current * 10f, 1f, 20f);
        }

        private void StopCurrentAnimation()
        {
            if (currentFlowEffect == null) return;

            var emission = currentFlowEffect.emission;
            emission.enabled = false;
        }

        private void UpdateEffects()
        {
            // Update any continuous effects based on current state
            if (hasCurrentAnimation && electricalComponent != null)
            {
                float current = electricalComponent.Current;
                if (current > 0.001f && currentState == VisualState.Active)
                {
                    StartCurrentAnimation(current);
                }
            }
        }

        #endregion

        #region Public Interface

        /// <summary>
        /// Apply visual properties from ComponentDefinition (ScriptableObjects)
        /// </summary>
        public void ApplyDefinition(CircuitSimulator.ComponentDefinition definition)
        {
            if (definition == null) return;

            var visual = definition.visualProperties;

            // Apply scale
            if (allowScaling)
                transform.localScale = Vector3.Scale(originalScale, visual.scale);

            // Apply colors from the root namespace ComponentDefinition
            defaultColor = visual.defaultColor;
            activeColor = visual.activeColor;
            errorColor = visual.errorColor;

            // Apply effects settings
            hasGlowEffect = visual.hasGlowEffect;
            hasCurrentAnimation = visual.hasCurrentAnimation;
            showsVoltageIndicator = visual.showsVoltageIndicator;

            // Apply custom material
            if (visual.defaultMaterial != null)
                SetMaterial(visual.defaultMaterial);

            // Reinitialize effects if settings changed
            if (hasGlowEffect && glowEffect == null)
                CreateGlowEffect();

            if (hasCurrentAnimation && currentFlowEffect == null)
                CreateCurrentFlowEffect();

            UpdateVisualAppearance();

            Debug.Log($"[VisualComponent] Applied definition visual properties to {gameObject.name}");
        }

        /// <summary>
        /// Apply visual properties from ThemedComponentDefinition
        /// </summary>
        public void ApplyDefinition(CircuitSimulator.Components.ThemedComponentDefinition definition)
        {
            if (definition == null) return;

            var visual = definition.visualProperties;

            // Apply scale
            if (allowScaling)
                transform.localScale = Vector3.Scale(originalScale, visual.scale);

            // Apply default color only (ThemedComponentDefinition doesn't have activeColor/errorColor)
            defaultColor = visual.defaultColor;

            // Apply custom material
            if (visual.defaultMaterial != null)
                SetMaterial(visual.defaultMaterial);

            // Reinitialize effects if settings changed
            if (hasGlowEffect && glowEffect == null)
                CreateGlowEffect();

            if (hasCurrentAnimation && currentFlowEffect == null)
                CreateCurrentFlowEffect();

            UpdateVisualAppearance();

            Debug.Log($"[VisualComponent] Applied themed definition visual properties to {gameObject.name} - Fixed");
        }

        /// <summary>
        /// Set highlight state (for selection/interaction)
        /// </summary>
        public void SetHighlighted(bool highlighted)
        {
            if (highlighted)
                SetVisualState(VisualState.Highlighted);
            else
                SetVisualState(VisualState.Default);
        }

        /// <summary>
        /// Set error state (for validation feedback)
        /// </summary>
        public void SetError(bool hasError)
        {
            if (hasError)
                SetVisualState(VisualState.Error);
            else
                SetVisualState(VisualState.Default);
        }

        /// <summary>
        /// Pulse effect for attention
        /// </summary>
        public void PulseEffect()
        {
            StartCoroutine(PulseCoroutine());
        }

        private System.Collections.IEnumerator PulseCoroutine()
        {
            var originalState = currentState;

            for (int i = 0; i < 3; i++)
            {
                SetVisualState(VisualState.Highlighted);
                yield return new WaitForSeconds(0.2f);
                SetVisualState(originalState);
                yield return new WaitForSeconds(0.2f);
            }
        }

        #endregion

        [ContextMenu("Debug Visual State")]
        public void DebugVisualState()
        {
            Debug.Log($"[VisualComponent] {gameObject.name}:\n" +
                     $"  Current State: {currentState}\n" +
                     $"  Current Color: {currentColor}\n" +
                     $"  Has Glow: {hasGlowEffect}\n" +
                     $"  Has Animation: {hasCurrentAnimation}\n" +
                     $"  Scale: {transform.localScale}");
        }
    }
}