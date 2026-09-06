using System;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Game.View.UI
{
    public class TransitionController : MonoBehaviour
    {
        [Header("Objects & Material")]
        [SerializeField] private Image circleOverlay;
        [SerializeField] private Material circleCutoutMaterial;

        [Header("Circle Hide (Close)")]
        [SerializeField] private float hideDuration = 0.5f;
        [SerializeField] private AnimationCurve hideCurve = AnimationCurve.EaseInOut(0f, 1.8f, 1f, 0f);

        [Header("Circle Reveal (Open)")]
        [FormerlySerializedAs("circleDuration")]
        [SerializeField] private float revealDuration = 0.8f;
        [FormerlySerializedAs("radiusCurve")]
        [SerializeField] private AnimationCurve revealCurve = new AnimationCurve(
            new Keyframe(0f, 0f),
            new Keyframe(0.55f, 0.85f),
            new Keyframe(0.7f, 0.68f),
            new Keyframe(1f, 1.8f)
        );

        private const float DefaultOpenRadius = 1.8f;

        private static readonly int RadiusId = Shader.PropertyToID("_Radius");
        private static readonly int AspectRatioId = Shader.PropertyToID("_AspectRatio");

        private Material circleMaterial;
        private Material circleSourceMaterial;
        private Material originalOverlayMaterial;
        private Image materialOverlay;
        private bool useAutomaticAspectRatio;
        private bool isPlaying;

        public float TotalDuration => Mathf.Max(0f, hideDuration) + Mathf.Max(0f, revealDuration);
        public bool IsPlaying => isPlaying;

        private void Awake()
        {
            EnsureCircleMaterial();
            SetCircleRadius(EvaluateRadius(hideCurve, 0f, DefaultOpenRadius, 0f));
            SetOverlayVisible(false);
        }

        private void OnDestroy()
        {
            ReleaseCircleMaterial();
        }

        public async Awaitable PlayAsync(Action onCovered)
        {
            if (isPlaying || !EnsureCircleMaterial())
                return;

            isPlaying = true;
            SetOverlayVisible(true);

            try
            {
                // 1. Close the circle until the overlay fully covers the screen.
                await PlayCircleHideInternalAsync();

                // 2. The screen is covered; update the game state now.
                onCovered?.Invoke();

                await Awaitable.NextFrameAsync();

                // 3. Open the circle to reveal the updated screen.
                await PlayCircleRevealInternalAsync();
            }
            finally
            {
                isPlaying = false;
                SetOverlayVisible(false);
            }
        }

        [ContextMenu("Hide")]
        public async Awaitable PlayCircleHideAsync()
        {
            if (isPlaying || !EnsureCircleMaterial())
                return;

            isPlaying = true;
            SetOverlayVisible(true);

            try
            {
                await PlayCircleHideInternalAsync();
            }
            finally
            {
                isPlaying = false;
            }
        }

        [ContextMenu("Reveal")]
        public async Awaitable PlayCircleRevealAsync()
        {
            if (isPlaying || !EnsureCircleMaterial())
                return;

            isPlaying = true;
            SetOverlayVisible(true);

            try
            {
                await PlayCircleRevealInternalAsync();
            }
            finally
            {
                isPlaying = false;
                SetOverlayVisible(false);
            }
        }

        public void Preview(float normalizedTime)
        {
            if (!EnsureCircleMaterial())
                return;

            normalizedTime = Mathf.Clamp01(normalizedTime);
            SetOverlayVisible(true);

            float safeHideDuration = Mathf.Max(0f, hideDuration);
            float safeRevealDuration = Mathf.Max(0f, revealDuration);
            float total = safeHideDuration + safeRevealDuration;

            if (total <= 0f)
            {
                SetCircleRadius(EvaluateRadius(revealCurve, 1f, 0f, DefaultOpenRadius));
                return;
            }

            float hideRatio = safeHideDuration / total;

            if (normalizedTime <= hideRatio && safeHideDuration > 0f)
            {
                float hideT = normalizedTime / hideRatio;
                SetCircleRadius(EvaluateRadius(hideCurve, hideT, DefaultOpenRadius, 0f));
            }
            else
            {
                float revealT = Mathf.InverseLerp(hideRatio, 1f, normalizedTime);
                SetCircleRadius(EvaluateRadius(revealCurve, revealT, 0f, DefaultOpenRadius));
            }
        }

        public void ResetPreview()
        {
            if (!EnsureCircleMaterial())
                return;

            SetCircleRadius(EvaluateRadius(hideCurve, 0f, DefaultOpenRadius, 0f));
            SetOverlayVisible(false);
        }

        public bool SetCircleRadius(float radius)
        {
            if (!EnsureCircleMaterial())
                return false;

            UpdateAspectRatio();
            circleMaterial.SetFloat(RadiusId, Mathf.Max(0f, radius));
            return true;
        }

        private async Awaitable PlayCircleHideInternalAsync()
        {
            await PlayRadiusCurveAsync(
                Mathf.Max(0f, hideDuration),
                hideCurve,
                DefaultOpenRadius,
                0f);
        }

        private async Awaitable PlayCircleRevealInternalAsync()
        {
            await PlayRadiusCurveAsync(
                Mathf.Max(0f, revealDuration),
                revealCurve,
                0f,
                DefaultOpenRadius);
        }

        private async Awaitable PlayRadiusCurveAsync(
            float duration,
            AnimationCurve curve,
            float fallbackStart,
            float fallbackEnd)
        {
            float startTime = Time.unscaledTime;

            while (true)
            {
                float t = duration <= 0f
                    ? 1f
                    : Mathf.Clamp01((Time.unscaledTime - startTime) / duration);

                SetCircleRadius(EvaluateRadius(curve, t, fallbackStart, fallbackEnd));

                if (t >= 1f)
                    break;

                await Awaitable.NextFrameAsync();
            }
        }

        private static float EvaluateRadius(
            AnimationCurve curve,
            float normalizedTime,
            float fallbackStart,
            float fallbackEnd)
        {
            float radius = curve != null
                ? curve.Evaluate(normalizedTime)
                : Mathf.Lerp(fallbackStart, fallbackEnd, normalizedTime);

            return Mathf.Max(0f, radius);
        }

        private void SetOverlayVisible(bool visible)
        {
            if (circleOverlay != null && circleOverlay.gameObject.activeSelf != visible)
                circleOverlay.gameObject.SetActive(visible);
        }

        private bool EnsureCircleMaterial()
        {
            if (circleOverlay == null)
                circleOverlay = GetComponentInChildren<Image>(true);

            if (!circleOverlay)
                return false;

            Material sourceMaterial = circleCutoutMaterial;
            if (!sourceMaterial)
            {
                sourceMaterial = materialOverlay == circleOverlay && circleSourceMaterial
                    ? circleSourceMaterial
                    : circleOverlay.material;
            }
            if (!sourceMaterial)
            {
                Debug.LogError(
                    "TransitionController is missing the circle cutout material. Assign Mat_CircleCut to Circle Cutout Material.",
                    this);
                return false;
            }

            if (!sourceMaterial.HasProperty(RadiusId))
            {
                Debug.LogError(
                    "TransitionController needs a circle cutout material with a _Radius property. " +
                    "Assign Mat_CircleCut to Circle Cutout Material or to the Circle Overlay Image.",
                    this);
                return false;
            }

            if (circleMaterial && materialOverlay == circleOverlay &&
                circleSourceMaterial == sourceMaterial && circleMaterial.HasProperty(RadiusId))
            {
                return true;
            }

            ReleaseCircleMaterial();

            originalOverlayMaterial = circleOverlay.material;
            circleMaterial = Instantiate(sourceMaterial);
            circleSourceMaterial = sourceMaterial;
            materialOverlay = circleOverlay;
            useAutomaticAspectRatio = circleMaterial.HasProperty(AspectRatioId) &&
                                      sourceMaterial.GetFloat(AspectRatioId) <= 0f;
            circleMaterial.name = $"{sourceMaterial.name} (Canvas Transition Instance)";
            circleMaterial.hideFlags = HideFlags.DontSave;
            circleOverlay.material = circleMaterial;

            UpdateAspectRatio();
            return true;
        }

        private void UpdateAspectRatio()
        {
            if (!useAutomaticAspectRatio || circleMaterial == null || circleOverlay == null ||
                !circleMaterial.HasProperty(AspectRatioId))
            {
                return;
            }

            Rect rect = circleOverlay.rectTransform.rect;
            float aspectRatio = rect.height > Mathf.Epsilon ? rect.width / rect.height : 0f;
            circleMaterial.SetFloat(AspectRatioId, aspectRatio > Mathf.Epsilon ? aspectRatio : 0f);
        }

        private void ReleaseCircleMaterial()
        {
            if (materialOverlay != null && materialOverlay.material == circleMaterial)
                materialOverlay.material = originalOverlayMaterial;

            if (circleMaterial != null)
            {
                if (Application.isPlaying)
                    Destroy(circleMaterial);
                else
                    DestroyImmediate(circleMaterial);
            }

            circleMaterial = null;
            circleSourceMaterial = null;
            originalOverlayMaterial = null;
            materialOverlay = null;
            useAutomaticAspectRatio = false;
        }
    }
}
