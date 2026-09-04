using System.Collections;
using UnityEngine;
using WizzardsCauldron.Core;
using WizzardsCauldron.Interactions;

namespace WizzardsCauldron.Presentation
{
    [DisallowMultipleComponent]
    public sealed class PotionHintFeedback : MonoBehaviour
    {
        private static readonly int BaseColorProperty =
            Shader.PropertyToID("_BaseColor");
        private static readonly int ColorProperty =
            Shader.PropertyToID("_Color");
        private static readonly int EmissionColorProperty =
            Shader.PropertyToID("_EmissionColor");

        [SerializeField]
        private QuestCampaignController _campaign;

        [SerializeField, Min(1f)]
        private float _highlightDuration = 5f;

        [SerializeField, Min(0.5f)]
        private float _pulsesPerSecond = 2f;

        [SerializeField]
        private Color _highlightColor =
            new Color(1f, 0.72f, 0.15f, 1f);

        private Coroutine _highlightRoutine;
        private RendererState[] _activeStates;
        private bool _subscribed;

        public void Configure(
            QuestCampaignController campaign)
        {
            _campaign = campaign;
        }

        private void OnEnable()
        {
            if (!Application.isPlaying ||
                _subscribed ||
                _campaign == null)
            {
                return;
            }

            _campaign.HintRevealed += HandleHintRevealed;
            _campaign.RoundStarted += HandleRoundStarted;
            _subscribed = true;
        }

        private void OnDisable()
        {
            if (_campaign != null && _subscribed)
            {
                _campaign.HintRevealed -= HandleHintRevealed;
                _campaign.RoundStarted -= HandleRoundStarted;
            }

            _subscribed = false;
            StopHighlight();
        }

        private void OnValidate()
        {
            _highlightDuration = Mathf.Max(
                1f,
                _highlightDuration);
            _pulsesPerSecond = Mathf.Max(
                0.5f,
                _pulsesPerSecond);
        }

        private void HandleHintRevealed(
            PotionController potion)
        {
            StopHighlight();

            if (potion == null)
            {
                return;
            }

            _activeStates = CaptureRendererStates(potion);
            if (_activeStates.Length == 0)
            {
                return;
            }

            _highlightRoutine = StartCoroutine(
                RunHighlight());
        }

        private void HandleRoundStarted(
            QuestRoundInfo _)
        {
            StopHighlight();
        }

        private IEnumerator RunHighlight()
        {
            float elapsed = 0f;

            while (elapsed < _highlightDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float wave =
                    Mathf.Sin(
                        elapsed *
                        Mathf.PI * 2f *
                        _pulsesPerSecond) *
                    0.5f + 0.5f;

                ApplyHighlight(wave);
                yield return null;
            }

            RestoreRendererStates();
            _highlightRoutine = null;
            _activeStates = null;
        }

        private RendererState[] CaptureRendererStates(
            PotionController potion)
        {
            Renderer[] renderers =
                potion.GetComponentsInChildren<Renderer>(true);
            var states = new RendererState[renderers.Length];

            for (int index = 0;
                 index < renderers.Length;
                 index++)
            {
                Renderer renderer = renderers[index];
                var baselineBlock =
                    new MaterialPropertyBlock();
                renderer.GetPropertyBlock(baselineBlock);

                Material material = renderer.sharedMaterial;
                bool hasBaseColor =
                    material != null &&
                    material.HasProperty(BaseColorProperty);
                bool hasColor =
                    material != null &&
                    material.HasProperty(ColorProperty);
                bool hasEmission =
                    material != null &&
                    material.HasProperty(EmissionColorProperty);
                Color baseColor =
                    potion.Definition != null
                        ? potion.Definition.PresentationColor
                        : Color.white;

                if (material != null && hasBaseColor)
                {
                    baseColor = material.GetColor(
                        BaseColorProperty);
                }
                else if (material != null && hasColor)
                {
                    baseColor = material.GetColor(
                        ColorProperty);
                }

                states[index] = new RendererState(
                    renderer,
                    baselineBlock,
                    baseColor,
                    hasBaseColor,
                    hasColor,
                    hasEmission);
            }

            return states;
        }

        private void ApplyHighlight(float wave)
        {
            if (_activeStates == null)
            {
                return;
            }

            float brightness = Mathf.Lerp(
                0.25f,
                0.75f,
                wave);
            Color emission = _highlightColor *
                Mathf.Lerp(1.5f, 4f, wave);

            for (int index = 0;
                 index < _activeStates.Length;
                 index++)
            {
                RendererState state = _activeStates[index];
                if (state.Renderer == null)
                {
                    continue;
                }

                MaterialPropertyBlock block =
                    state.WorkingBlock;
                state.Renderer.GetPropertyBlock(block);
                Color brightColor = Color.Lerp(
                    state.BaseColor,
                    _highlightColor,
                    brightness);
                brightColor.a = state.BaseColor.a;

                if (state.HasBaseColor)
                {
                    block.SetColor(
                        BaseColorProperty,
                        brightColor);
                }

                if (state.HasColor)
                {
                    block.SetColor(
                        ColorProperty,
                        brightColor);
                }

                if (state.HasEmission)
                {
                    block.SetColor(
                        EmissionColorProperty,
                        emission);
                }

                state.Renderer.SetPropertyBlock(block);
            }
        }

        private void StopHighlight()
        {
            if (_highlightRoutine != null)
            {
                StopCoroutine(_highlightRoutine);
                _highlightRoutine = null;
            }

            RestoreRendererStates();
            _activeStates = null;
        }

        private void RestoreRendererStates()
        {
            if (_activeStates == null)
            {
                return;
            }

            for (int index = 0;
                 index < _activeStates.Length;
                 index++)
            {
                RendererState state = _activeStates[index];
                if (state.Renderer != null)
                {
                    state.Renderer.SetPropertyBlock(
                        state.BaselineBlock);
                }
            }
        }

        private sealed class RendererState
        {
            internal RendererState(
                Renderer renderer,
                MaterialPropertyBlock baselineBlock,
                Color baseColor,
                bool hasBaseColor,
                bool hasColor,
                bool hasEmission)
            {
                Renderer = renderer;
                BaselineBlock = baselineBlock;
                BaseColor = baseColor;
                HasBaseColor = hasBaseColor;
                HasColor = hasColor;
                HasEmission = hasEmission;
                WorkingBlock = new MaterialPropertyBlock();
            }

            internal Renderer Renderer { get; }
            internal MaterialPropertyBlock BaselineBlock { get; }
            internal Color BaseColor { get; }
            internal bool HasBaseColor { get; }
            internal bool HasColor { get; }
            internal bool HasEmission { get; }
            internal MaterialPropertyBlock WorkingBlock { get; }
        }
    }
}
