using UnityEngine;

namespace WizzardsCauldron.Presentation
{
    [DisallowMultipleComponent]
    public sealed class AstralPortalAnimator : MonoBehaviour
    {
        [Header("Counter-Rotating Rings")]
        [SerializeField]
        private Transform _outerRing;

        [SerializeField]
        private Transform _innerRing;

        [SerializeField, Range(0f, 10f)]
        private float _outerRingDegreesPerSecond = 2.2f;

        [SerializeField, Range(0f, 10f)]
        private float _innerRingDegreesPerSecond = 3.1f;

        [Header("Gentle Portal Pulse")]
        [SerializeField]
        private Light _portalLight;

        [SerializeField, Min(0f)]
        private float _minimumLightIntensity = 0.08f;

        [SerializeField, Min(0f)]
        private float _maximumLightIntensity = 0.22f;

        [SerializeField, Range(0.02f, 0.5f)]
        private float _pulseCyclesPerSecond = 0.1f;

        [Header("Optional Core Emission")]
        [SerializeField]
        private Renderer _coreRenderer;

        [SerializeField]
        private string _emissionColorProperty =
            "_EmissionColor";

        [SerializeField]
        private Color _coreEmissionColor =
            new Color(0.12f, 0.55f, 0.9f, 1f);

        [SerializeField, Min(0f)]
        private float _minimumEmissionMultiplier = 0.45f;

        [SerializeField, Min(0f)]
        private float _maximumEmissionMultiplier = 0.8f;

        private MaterialPropertyBlock _propertyBlock;

        private bool _baselineCaptured;
        private bool _hasCoreEmissionProperty;
        private int _emissionColorPropertyId;
        private float _baseLightIntensity;
        private Color _baseLightColor = Color.white;
        private Color _baseCoreEmissionColor = Color.black;

        private void Awake()
        {
            _propertyBlock = new MaterialPropertyBlock();
            CaptureVisualBaseline();
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;

            if (_outerRing != null)
            {
                _outerRing.Rotate(
                    0f,
                    0f,
                    _outerRingDegreesPerSecond * deltaTime,
                    Space.Self);
            }

            if (_innerRing != null)
            {
                _innerRing.Rotate(
                    0f,
                    0f,
                    -_innerRingDegreesPerSecond * deltaTime,
                    Space.Self);
            }

            ApplyGentlePulse();
        }

        private void OnDisable()
        {
            RestoreVisualBaseline();
        }

        private void OnValidate()
        {
            _outerRingDegreesPerSecond = Mathf.Clamp(
                Mathf.Abs(_outerRingDegreesPerSecond),
                0f,
                10f);

            _innerRingDegreesPerSecond = Mathf.Clamp(
                Mathf.Abs(_innerRingDegreesPerSecond),
                0f,
                10f);

            _minimumLightIntensity = Mathf.Max(
                0f,
                _minimumLightIntensity);

            _maximumLightIntensity = Mathf.Max(
                _minimumLightIntensity,
                _maximumLightIntensity);

            _pulseCyclesPerSecond = Mathf.Clamp(
                _pulseCyclesPerSecond,
                0.02f,
                0.5f);

            _minimumEmissionMultiplier = Mathf.Max(
                0f,
                _minimumEmissionMultiplier);

            _maximumEmissionMultiplier = Mathf.Max(
                _minimumEmissionMultiplier,
                _maximumEmissionMultiplier);

            if (string.IsNullOrWhiteSpace(
                _emissionColorProperty))
            {
                _emissionColorProperty =
                    "_EmissionColor";
            }
        }

        private void ApplyGentlePulse()
        {
            float phase = Time.time *
                _pulseCyclesPerSecond *
                Mathf.PI * 2f;

            float pulse = 0.5f +
                0.5f * Mathf.Sin(phase);

            if (_portalLight != null)
            {
                _portalLight.intensity = Mathf.Lerp(
                    _minimumLightIntensity,
                    _maximumLightIntensity,
                    pulse);
            }

            if (!_hasCoreEmissionProperty ||
                _coreRenderer == null)
            {
                return;
            }

            float emissionMultiplier = Mathf.Lerp(
                _minimumEmissionMultiplier,
                _maximumEmissionMultiplier,
                pulse);

            SetCoreEmission(
                _coreEmissionColor * emissionMultiplier);
        }

        private void CaptureVisualBaseline()
        {
            EnsurePropertyBlock();
            _emissionColorPropertyId = Shader.PropertyToID(
                _emissionColorProperty);

            if (_portalLight != null)
            {
                _baseLightIntensity =
                    _portalLight.intensity;

                _baseLightColor =
                    _portalLight.color;
            }

            _hasCoreEmissionProperty = false;

            if (_coreRenderer != null &&
                _coreRenderer.sharedMaterial != null)
            {
                if (_coreRenderer.sharedMaterial.HasProperty(
                    _emissionColorPropertyId))
                {
                    _hasCoreEmissionProperty = true;
                    _coreRenderer.GetPropertyBlock(
                        _propertyBlock);

                    _baseCoreEmissionColor =
                        _propertyBlock.HasColor(
                            _emissionColorPropertyId)
                            ? _propertyBlock.GetColor(
                                _emissionColorPropertyId)
                            : _coreRenderer.sharedMaterial
                                .GetColor(
                                    _emissionColorPropertyId);
                }
            }

            _baselineCaptured = true;
        }

        private void RestoreVisualBaseline()
        {
            if (!_baselineCaptured)
            {
                return;
            }

            if (_portalLight != null)
            {
                _portalLight.color = _baseLightColor;
                _portalLight.intensity =
                    _baseLightIntensity;
            }

            if (_hasCoreEmissionProperty)
            {
                SetCoreEmission(
                    _baseCoreEmissionColor);
            }
        }

        private void SetCoreEmission(Color color)
        {
            if (_coreRenderer == null)
            {
                return;
            }

            EnsurePropertyBlock();
            _coreRenderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetColor(
                _emissionColorPropertyId,
                color);
            _coreRenderer.SetPropertyBlock(_propertyBlock);
        }

        private void EnsurePropertyBlock()
        {
            if (_propertyBlock == null)
            {
                _propertyBlock = new MaterialPropertyBlock();
            }
        }
    }
}
