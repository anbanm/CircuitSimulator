using UnityEngine;

namespace CircuitSimulator.Effects
{
    /// <summary>
    /// Makes a Light component pulse in intensity.
    /// Attach to a GameObject with a Light component.
    /// </summary>
    [RequireComponent(typeof(Light))]
    public class PulsingLight : MonoBehaviour
    {
        [Header("Pulse Settings")]
        [SerializeField] private float pulseSpeed = 2f;
        [SerializeField] private float minIntensity = 0.5f;
        [SerializeField] private float maxIntensity = 2f;

        [Header("State")]
        [SerializeField] private bool isActive = true;

        private Light lightComponent;
        private float pulseTimer;

        private void Awake()
        {
            lightComponent = GetComponent<Light>();
        }

        private void Update()
        {
            if (!isActive || lightComponent == null) return;

            pulseTimer += Time.deltaTime * pulseSpeed;

            float pulse = (Mathf.Sin(pulseTimer) + 1f) / 2f;
            lightComponent.intensity = Mathf.Lerp(minIntensity, maxIntensity, pulse);
        }

        public void SetActive(bool active)
        {
            isActive = active;
            if (lightComponent != null)
            {
                lightComponent.enabled = active;
            }
        }
    }
}
