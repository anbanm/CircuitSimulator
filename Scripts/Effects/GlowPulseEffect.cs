using UnityEngine;

namespace CircuitSimulator.Effects
{
    /// <summary>
    /// Adds a pulsing glow effect to a component.
    /// Attach to any prefab to make it glow and pulse when active.
    /// </summary>
    public class GlowPulseEffect : MonoBehaviour
    {
        [Header("Pulse Settings")]
        [SerializeField] private float pulseSpeed = 2f;
        [SerializeField] private float minIntensity = 0.5f;
        [SerializeField] private float maxIntensity = 1.5f;

        [Header("Glow Color")]
        [SerializeField] private Color glowColor = new Color(1f, 0.9f, 0.3f, 1f); // Warm yellow
        [SerializeField] private bool useEmission = true;

        [Header("Scale Pulse (Optional)")]
        [SerializeField] private bool pulseScale = false;
        [SerializeField] private float scaleAmount = 0.05f;

        [Header("State")]
        [SerializeField] private bool isActive = false;  // Start inactive - circuit system activates when connected
        [SerializeField] private bool activeOnStart = false;  // Set true for always-on effects

        private Renderer[] renderers;
        private MaterialPropertyBlock propertyBlock;
        private Vector3 originalScale;
        private float pulseTimer;

        // Material property IDs for performance
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        private void Awake()
        {
            renderers = GetComponentsInChildren<Renderer>();
            propertyBlock = new MaterialPropertyBlock();
            originalScale = transform.localScale;

            // Enable emission on all materials
            if (useEmission)
            {
                EnableEmission();
            }
        }

        private void Start()
        {
            if (activeOnStart)
            {
                SetActive(true);
            }
        }

        private void EnableEmission()
        {
            foreach (var rend in renderers)
            {
                foreach (var mat in rend.materials)
                {
                    mat.EnableKeyword("_EMISSION");
                }
            }
        }

        private void Update()
        {
            if (!isActive) return;

            pulseTimer += Time.deltaTime * pulseSpeed;

            // Calculate pulse value (0 to 1, using sine wave)
            float pulse = (Mathf.Sin(pulseTimer) + 1f) / 2f;
            float intensity = Mathf.Lerp(minIntensity, maxIntensity, pulse);

            // Apply glow to all renderers
            Color emissionColor = glowColor * intensity;

            foreach (var rend in renderers)
            {
                rend.GetPropertyBlock(propertyBlock);
                propertyBlock.SetColor(EmissionColorId, emissionColor);
                rend.SetPropertyBlock(propertyBlock);
            }

            // Optional scale pulse
            if (pulseScale)
            {
                float scalePulse = 1f + (pulse * scaleAmount);
                transform.localScale = originalScale * scalePulse;
            }
        }

        /// <summary>
        /// Turn the glow effect on/off (e.g., when circuit is powered)
        /// </summary>
        public void SetActive(bool active)
        {
            isActive = active;

            if (!active)
            {
                // Reset to no glow
                foreach (var rend in renderers)
                {
                    rend.GetPropertyBlock(propertyBlock);
                    propertyBlock.SetColor(EmissionColorId, Color.black);
                    rend.SetPropertyBlock(propertyBlock);
                }

                if (pulseScale)
                {
                    transform.localScale = originalScale;
                }
            }
        }

        /// <summary>
        /// Set the glow color at runtime
        /// </summary>
        public void SetGlowColor(Color color)
        {
            glowColor = color;
        }

        /// <summary>
        /// Set pulse speed at runtime
        /// </summary>
        public void SetPulseSpeed(float speed)
        {
            pulseSpeed = speed;
        }
    }
}
