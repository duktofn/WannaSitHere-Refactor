using System;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using PrimeTween;
using Cysharp.Threading.Tasks;

namespace Game.View.UI
{
    public class TransitionController : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Canvas transitionCanvas;
        [SerializeField] private Image transitionImage;
        [SerializeField] private Camera targetCamera;

        [Header("Transition Settings")]
        [Tooltip("Thời gian mở màn hình (giây)")]
        [FormerlySerializedAs("defaultDuration")]
        [SerializeField] private float openDuration = 0.8f;
        [Tooltip("Thời gian đóng màn hình (giây)")]
        [SerializeField] private float closeDuration = 0.5f;

        public float OpenDuration => openDuration;
        public float CloseDuration => closeDuration;
        [Tooltip("Bật nếu muốn dùng AnimationCurve để mở (mở hé -> dừng -> mở hẳn). Tắt nếu muốn dùng Open Ease thông thường.")]
        [SerializeField] private bool useOpenCurve = true;
        [Tooltip("Đường cong mở màn hình: 0 -> 0.35 (mở hé), 0.35 -> 0.55 (dừng lại), 0.55 -> 1.0 (mở bung hoàn toàn)")]
        [SerializeField] private AnimationCurve openCurve = new AnimationCurve(
            new Keyframe(0f, 0f, 0f, 2f),
            new Keyframe(0.35f, 0.35f, 0f, 0f),
            new Keyframe(0.55f, 0.35f, 0f, 0f),
            new Keyframe(1f, 1f, 2f, 0f)
        );
        [SerializeField] private Ease openEase = Ease.OutQuad;
        [SerializeField] private Ease closeEase = Ease.InQuad;
        [SerializeField] private float maxRadius = 1.25f;
        [Tooltip("Khoảng thời gian dừng lại (giữ màn hình đen) giữa tween đóng và tween mở.")]
        [SerializeField] private float delayBetweenTransitions = 0.2f;
        [SerializeField] private bool playOpenOnStart = false;

        public float DelayBetweenTransitions => delayBetweenTransitions;

        [Header("Debug / Testing")]
        [Tooltip("Kéo đối tượng muốn làm tâm vào đây để test. Để trống sẽ tự động lấy chính giữa màn hình.")]
        [SerializeField] private Transform testTarget;

        private Material _materialInstance;
        private RectTransform _imageRectTransform;
        private RectTransform _canvasRectTransform;
        private Tween _currentTween;

        private static readonly int RadiusProperty = Shader.PropertyToID("_Radius");
        private static readonly int CenterXProperty = Shader.PropertyToID("_CenterX");
        private static readonly int CenterYProperty = Shader.PropertyToID("_CenterY");

        private void Awake()
        {
            ResolveReferences();
            InitMaterial();

            if (!playOpenOnStart && transitionCanvas != null)
            {
                transitionCanvas.gameObject.SetActive(false);
            }
        }

        private void Start()
        {
            if (playOpenOnStart)
            {
                OpenAsync().Forget();
            }
        }

        private void OnDestroy()
        {
            _currentTween.Stop();
            if (_materialInstance != null)
            {
                Destroy(_materialInstance);
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            ResolveReferences();
        }
#endif

        /// <summary>
        /// Tự động tìm và liên kết các thành phần nếu chưa kéo thả vào Inspector
        /// </summary>
        public void ResolveReferences()
        {
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }

            if (transitionCanvas == null)
            {
                transitionCanvas = GetComponentInChildren<Canvas>(true);
                if (transitionCanvas == null)
                {
                    var allCanvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                    foreach (var c in allCanvases)
                    {
                        if (c.name.IndexOf("transition", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            transitionCanvas = c;
                            break;
                        }
                    }
                }
            }

            if (transitionCanvas != null)
            {
                _canvasRectTransform = transitionCanvas.GetComponent<RectTransform>();

                if (transitionImage == null)
                {
                    transitionImage = transitionCanvas.GetComponentInChildren<Image>(true);
                }
            }

            if (transitionImage != null)
            {
                _imageRectTransform = transitionImage.rectTransform;
            }
        }

        private void InitMaterial()
        {
            if (transitionImage != null && transitionImage.material != null)
            {
                // Tạo Material clone riêng để không can thiệp trực tiếp vào file asset gốc
                _materialInstance = new Material(transitionImage.material);
                transitionImage.material = _materialInstance;
            }
        }

        private Material GetActiveMaterial()
        {
            if (Application.isPlaying)
            {
                if (_materialInstance == null) InitMaterial();
                return _materialInstance;
            }
            return transitionImage != null ? transitionImage.material : null;
        }

        // ─────────────────────────────────────────────────────────────
        // Public API
        // ─────────────────────────────────────────────────────────────

        public void OpenBlackScreen() => OpenAsync(testTarget).Forget();
        public void CloseBlackScreen() => CloseAsync(testTarget).Forget();

        private Easing GetDefaultOpenEasing()
        {
            if (useOpenCurve && openCurve != null && openCurve.length >= 2)
            {
                return Easing.Curve(openCurve);
            }
            return Easing.Standard(openEase);
        }

        /// <summary>
        /// Mở màn hình (vòng tròn mở rộng từ 0 -> maxRadius hé lộ game)
        /// </summary>
        public async UniTask OpenAsync(Transform target = null, float? duration = null, Easing? easing = null)
        {
            Vector2 uvCenter = target != null ? CalculateCenterUV(target.position) : CalculateCenterUV();
            Easing activeEasing = easing ?? GetDefaultOpenEasing();
            await PlayTransitionAsync(0f, maxRadius, uvCenter, duration ?? openDuration, activeEasing, false);
        }

        public async UniTask OpenAsync(Vector3 worldPosition, float? duration = null, Easing? easing = null)
        {
            Vector2 uvCenter = CalculateCenterUV(worldPosition);
            Easing activeEasing = easing ?? GetDefaultOpenEasing();
            await PlayTransitionAsync(0f, maxRadius, uvCenter, duration ?? openDuration, activeEasing, false);
        }

        /// <summary>
        /// Đóng màn hình (vòng tròn thu hẹp từ maxRadius -> 0 che đen game)
        /// </summary>
        public async UniTask CloseAsync(Transform target = null, float? duration = null, Easing? easing = null)
        {
            Vector2 uvCenter = target != null ? CalculateCenterUV(target.position) : CalculateCenterUV();
            Easing activeEasing = easing ?? Easing.Standard(closeEase);
            await PlayTransitionAsync(maxRadius, 0f, uvCenter, duration ?? closeDuration, activeEasing, true);
        }

        public async UniTask CloseAsync(Vector3 worldPosition, float? duration = null, Easing? easing = null)
        {
            Vector2 uvCenter = CalculateCenterUV(worldPosition);
            Easing activeEasing = easing ?? Easing.Standard(closeEase);
            await PlayTransitionAsync(maxRadius, 0f, uvCenter, duration ?? closeDuration, activeEasing, true);
        }

        /// <summary>
        /// Thực hiện trọn vẹn chu kỳ: Đóng màn hình -> Chờ delay -> Gọi action -> Mở màn hình
        /// </summary>
        public async UniTask DoTransitionAsync(Action onCovered = null, Transform target = null, float? closeDurationOverride = null, float? openDurationOverride = null, float? delayBetween = null)
        {
            await CloseAsync(target, closeDurationOverride);

            onCovered?.Invoke();

            float wait = delayBetween ?? delayBetweenTransitions;
            if (wait > 0f)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(wait));
            }

            await OpenAsync(target, openDurationOverride);
        }

        public async UniTask DoTransitionAsync(Func<UniTask> onCovered, Transform target = null, float? closeDurationOverride = null, float? openDurationOverride = null, float? delayBetween = null)
        {
            await CloseAsync(target, closeDurationOverride);

            if (onCovered != null)
            {
                await onCovered();
            }

            float wait = delayBetween ?? delayBetweenTransitions;
            if (wait > 0f)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(wait));
            }

            await OpenAsync(target, openDurationOverride);
        }

        // ─────────────────────────────────────────────────────────────
        // Core Animation & Math
        // ─────────────────────────────────────────────────────────────

        private async UniTask PlayTransitionAsync(float startRadius, float endRadius, Vector2 centerUV, float duration, Easing easing, bool blockRaycastsAfter)
        {
            _currentTween.Stop();

            ResolveReferences();

            if (transitionCanvas != null)
            {
                transitionCanvas.gameObject.SetActive(true);
            }

            if (transitionImage != null)
            {
                transitionImage.gameObject.SetActive(true);
                transitionImage.raycastTarget = true; // Chặn input khi đang chuyển cảnh
            }

            ApplyCenterAndSize(centerUV);

            Material mat = GetActiveMaterial();
            if (mat != null)
            {
                mat.SetFloat(RadiusProperty, startRadius);

                _currentTween = Tween.Custom(startRadius, endRadius, duration: duration, ease: easing, onValueChange: val =>
                {
                    if (mat != null)
                    {
                        mat.SetFloat(RadiusProperty, val);
                    }
                });

                await _currentTween;
            }

            // Nếu đã mở hoàn toàn, tắt raycast để người chơi tương tác với game
            if (!blockRaycastsAfter)
            {
                if (transitionImage != null)
                {
                    transitionImage.raycastTarget = false;
                }
                if (transitionCanvas != null)
                {
                    transitionCanvas.gameObject.SetActive(false);
                }
            }
        }

        private Vector2 CalculateCenterUV()
        {
            return new Vector2(0.5f, 0.5f);
        }

        private Vector2 CalculateCenterUV(Vector3 worldPos)
        {
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }

            Vector3 screenPos = targetCamera != null
                ? targetCamera.WorldToScreenPoint(worldPos)
                : new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f);

            return CalculateCenterUVFromScreen(screenPos);
        }

        public Vector2 CalculateCenterUVFromScreen(Vector2 screenPos)
        {
            if (_canvasRectTransform == null)
            {
                ResolveReferences();
            }

            float screenWidth = Screen.width;
            float screenHeight = Screen.height;

            Rect canvasRect = _canvasRectTransform != null ? _canvasRectTransform.rect : new Rect(0, 0, screenWidth, screenHeight);
            float canvasWidth = canvasRect.width;
            float canvasHeight = canvasRect.height;

            Vector2 canvasPos = new Vector2(
                (screenPos.x / screenWidth) * canvasWidth,
                (screenPos.y / screenHeight) * canvasHeight
            );

            float squareValue;
            if (canvasWidth > canvasHeight)
            {
                // Landscape
                squareValue = canvasWidth;
                canvasPos.y += (canvasWidth - canvasHeight) * 0.5f;
            }
            else
            {
                // Portrait
                squareValue = canvasHeight;
                canvasPos.x += (canvasHeight - canvasWidth) * 0.5f;
            }

            return canvasPos / squareValue;
        }

        private void ApplyCenterAndSize(Vector2 centerUV)
        {
            if (_canvasRectTransform == null) return;

            Rect canvasRect = _canvasRectTransform.rect;
            float squareValue = Mathf.Max(canvasRect.width, canvasRect.height);

            if (_imageRectTransform != null)
            {
                _imageRectTransform.sizeDelta = new Vector2(squareValue, squareValue);
            }

            Material mat = GetActiveMaterial();
            if (mat != null)
            {
                mat.SetFloat(CenterXProperty, centerUV.x);
                mat.SetFloat(CenterYProperty, centerUV.y);
            }
        }

        private void OnDisable()
        {
#if UNITY_EDITOR
            if (_isEditorAnimating)
            {
                UnityEditor.EditorApplication.update -= OnEditorUpdate;
                _isEditorAnimating = false;
            }
#endif
        }

        // ─────────────────────────────────────────────────────────────
        // Context Menu Testing (Chỉ gồm Tween Mở và Tween Đóng)
        // ─────────────────────────────────────────────────────────────

#if UNITY_EDITOR
        private float _editorAnimStartTime;
        private float _editorAnimDuration;
        private float _editorAnimStartRadius;
        private float _editorAnimEndRadius;
        private bool _isEditorAnimating;
        private bool _isEditorOpening;

        private Action _editorAnimOnComplete;

        [ContextMenu("Chạy Trọn Vẹn (Đóng -> Chờ -> Mở)")]
        public void TweenFullTransition()
        {
            if (Application.isPlaying)
            {
                DoTransitionAsync(target: testTarget).Forget();
            }
            else
            {
                StartEditorAnimation(maxRadius, 0f, closeDuration, isOpening: false, onComplete: () =>
                {
                    EditorDelayThenOpen().Forget();
                });
            }
        }

        private async UniTaskVoid EditorDelayThenOpen()
        {
            float wait = delayBetweenTransitions > 0f ? delayBetweenTransitions : 0.2f;
            await UniTask.Delay(TimeSpan.FromSeconds(wait));
            StartEditorAnimation(0f, maxRadius, openDuration, isOpening: true);
        }

        [ContextMenu("Tween Mở (Open)")]
        public void TweenOpen()
        {
            if (Application.isPlaying)
            {
                OpenAsync(testTarget).Forget();
            }
            else
            {
                StartEditorAnimation(0f, maxRadius, openDuration, isOpening: true);
            }
        }

        [ContextMenu("Tween Đóng (Close)")]
        public void TweenClose()
        {
            if (Application.isPlaying)
            {
                CloseAsync(testTarget).Forget();
            }
            else
            {
                StartEditorAnimation(maxRadius, 0f, closeDuration, isOpening: false);
            }
        }

        private void StartEditorAnimation(float startRadius, float endRadius, float duration, bool isOpening, Action onComplete = null)
        {
            ResolveReferences();
            if (transitionCanvas != null) transitionCanvas.gameObject.SetActive(true);
            if (transitionImage != null)
            {
                transitionImage.gameObject.SetActive(true);
                Vector2 uvCenter = testTarget != null ? CalculateCenterUV(testTarget.position) : CalculateCenterUV();
                ApplyCenterAndSize(uvCenter);
            }

            _editorAnimStartRadius = startRadius;
            _editorAnimEndRadius = endRadius;
            _editorAnimDuration = duration > 0f ? duration : 0.8f;
            _editorAnimStartTime = (float)UnityEditor.EditorApplication.timeSinceStartup;
            _isEditorOpening = isOpening;
            _editorAnimOnComplete = onComplete;

            if (!_isEditorAnimating)
            {
                _isEditorAnimating = true;
                UnityEditor.EditorApplication.update += OnEditorUpdate;
            }
        }

        private void OnEditorUpdate()
        {
            float elapsed = (float)UnityEditor.EditorApplication.timeSinceStartup - _editorAnimStartTime;
            float t = Mathf.Clamp01(elapsed / _editorAnimDuration);

            float radius;
            if (_isEditorOpening && useOpenCurve && openCurve != null && openCurve.length >= 2)
            {
                float progress = openCurve.Evaluate(t);
                radius = Mathf.LerpUnclamped(_editorAnimStartRadius, _editorAnimEndRadius, progress);
            }
            else
            {
                float smoothT = t * t * (3f - 2f * t);
                radius = Mathf.Lerp(_editorAnimStartRadius, _editorAnimEndRadius, smoothT);
            }

            if (transitionImage != null && transitionImage.material != null)
            {
                transitionImage.material.SetFloat(RadiusProperty, radius);
                UnityEditor.EditorUtility.SetDirty(transitionImage);
            }

            UnityEditor.SceneView.RepaintAll();

            if (t >= 1f)
            {
                UnityEditor.EditorApplication.update -= OnEditorUpdate;
                _isEditorAnimating = false;

                if (_editorAnimEndRadius > 0f)
                {
                    if (transitionImage != null) transitionImage.raycastTarget = false;
                    if (transitionCanvas != null) transitionCanvas.gameObject.SetActive(false);
                }

                var callback = _editorAnimOnComplete;
                _editorAnimOnComplete = null;
                callback?.Invoke();
            }
        }
#endif
    }
}
