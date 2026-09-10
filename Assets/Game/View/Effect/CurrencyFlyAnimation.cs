using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using PrimeTween;
using UnityEngine;
using UnityEngine.UI;

namespace Game.View.Effect
{
    public class CurrencyFlyAnimation : MonoBehaviour
    {
        [Header("Visual Settings")]
        [SerializeField] private Sprite coinSprite;
        [SerializeField] private Vector2 coinSize;
        [SerializeField] private RectTransform container;
        [SerializeField] private RectTransform defaultTarget;

        [Header("Burst Settings")]
        [SerializeField] private int coinCount;
        [SerializeField] private float burstRadius;
        [SerializeField] private float burstDuration;
        [SerializeField] private Ease burstEase;

        [Header("Fly Settings")]
        [SerializeField] private float startFlyDelay;
        [SerializeField] private float flyDuration;
        [SerializeField] private float staggerInterval;
        [SerializeField] private Ease flyEase;

        [Header("Impact Settings")]
        [SerializeField] private float targetPunchScale;
        [SerializeField] private float targetPunchDuration;

        private readonly Queue<RectTransform> _pool = new();
        private bool _isInitialized;

        public event Action OnCoinArrived;
        public event Action OnAllCoinsArrived;

        private void Awake()
        {
            EnsureInitialized();
            PrewarmPool();
        }

        private void EnsureInitialized()
        {
            if (_isInitialized && container != null) return;

            if (container == null)
            {
                // Find top-level root Canvas so coins render freely on top of everything
                Canvas rootCanvas = GetComponentInParent<Canvas>()?.rootCanvas;
                if (rootCanvas != null)
                {
                    container = rootCanvas.GetComponent<RectTransform>();
                }
                else
                {
                    container = GetComponent<RectTransform>();
                }
            }

            _isInitialized = true;
        }

        private void PrewarmPool()
        {
            for (int i = 0; i < coinCount; i++)
            {
                _pool.Enqueue(CreateNewCoin());
            }
        }

        private Vector2 GetLocalPosition(Vector3 screenOrWorldPos, bool isScreenPos)
        {
            Canvas canvas = container != null ? container.GetComponentInParent<Canvas>() : null;
            Camera cam = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                ? canvas.worldCamera
                : null;

            Vector2 screenPoint = isScreenPos
                ? (Vector2)screenOrWorldPos
                : (Vector2)RectTransformUtility.WorldToScreenPoint(cam, screenOrWorldPos);

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                container,
                screenPoint,
                cam,
                out Vector2 localPoint
            );

            return localPoint;
        }

        /// <summary>
        /// Play coin fly animation from screen center to defaultTarget.
        /// </summary>
        public void PlayFromScreenCenter()
        {
            Vector3 center = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f);
            PlayAsync(center, null, isScreenPos: true).Forget();
        }

        /// <summary>
        /// Play coin fly animation from a source transform position.
        /// </summary>
        public void PlayFromTransform(Transform source)
        {
            if (source == null)
            {
                PlayFromScreenCenter();
                return;
            }

            PlayAsync(source.position, null, isScreenPos: false).Forget();
        }

        /// <summary>
        /// Fire and forget method.
        /// </summary>
        public void Play(Vector3 startPos, RectTransform target = null, bool isScreenPos = false)
        {
            PlayAsync(startPos, target, isScreenPos).Forget();
        }

        /// <summary>
        /// Async method allowing callers to await full completion.
        /// </summary>
        public async UniTask PlayAsync(Vector3 startPos, RectTransform target = null, bool isScreenPos = false)
        {
            EnsureInitialized();

            RectTransform targetTransform = target != null ? target : defaultTarget;
            if (targetTransform == null)
            {
                Debug.LogWarning("[CurrencyFlyAnimation] Missing target RectTransform.");
                return;
            }

            Vector2 startLocalPos = GetLocalPosition(startPos, isScreenPos);
            Vector2 targetLocalPos = GetLocalPosition(targetTransform.position, false);

            List<RectTransform> activeCoins = new List<RectTransform>(coinCount);

            // Phase 1: Spawn and burst outward in 360-degree distribution
            for (int i = 0; i < coinCount; i++)
            {
                RectTransform coin = GetCoin();
                coin.anchoredPosition = startLocalPos;
                coin.localScale = Vector3.zero;
                coin.gameObject.SetActive(true);
                activeCoins.Add(coin);

                // Distribute angles evenly around full 360 degrees with slight random jitter
                float angle = (i / (float)coinCount) * Mathf.PI * 2f + UnityEngine.Random.Range(-0.25f, 0.25f);
                Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                float distance = UnityEngine.Random.Range(burstRadius * 0.5f, burstRadius);
                Vector2 burstLocalTarget = startLocalPos + dir * distance;

                // Pop-in scale and burst position
                _ = Tween.Scale(coin, Vector3.one, burstDuration * 0.5f, Ease.OutQuad);
                _ = Tween.UIAnchoredPosition(coin, burstLocalTarget, burstDuration, burstEase);
            }

            // Wait for burst phase to finish + slight hang
            await UniTask.Delay((int)((burstDuration + startFlyDelay) * 1000));

            // Phase 2: Fly to target with staggered intervals
            List<UniTask> flyTasks = new List<UniTask>(activeCoins.Count);

            for (int i = 0; i < activeCoins.Count; i++)
            {
                RectTransform coin = activeCoins[i];
                if (coin == null || !coin.gameObject.activeSelf) continue;

                flyTasks.Add(FlyCoinToTarget(coin, targetLocalPos, targetTransform));

                if (staggerInterval > 0f)
                {
                    await UniTask.Delay((int)(staggerInterval * 1000));
                }
            }

            await UniTask.WhenAll(flyTasks);

            OnAllCoinsArrived?.Invoke();
        }

        private async UniTask FlyCoinToTarget(RectTransform coin, Vector2 targetLocalPos, RectTransform targetTransform)
        {
            // Fly to target local position using UIAnchoredPosition
            await Tween.UIAnchoredPosition(coin, targetLocalPos, flyDuration, flyEase);

            // Arrival: Impact effect on target icon and release to pool
            if (targetTransform != null && targetTransform.gameObject.activeInHierarchy)
            {
                _ = Tween.PunchScale(targetTransform, Vector3.one * targetPunchScale, targetPunchDuration);
            }

            OnCoinArrived?.Invoke();
            ReleaseCoin(coin);
        }

        private RectTransform GetCoin()
        {
            while (_pool.Count > 0)
            {
                RectTransform pooled = _pool.Dequeue();
                if (pooled != null)
                {
                    pooled.SetAsLastSibling();
                    return pooled;
                }
            }

            // Fallback nếu số lượng coin đang bay vượt quá số lượng prewarm trong pool
            return CreateNewCoin();
        }

        private RectTransform CreateNewCoin()
        {
            GameObject go = new GameObject("CoinFx", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.SetParent(container != null ? container : transform, false);
            rect.sizeDelta = coinSize;

            Image img = go.GetComponent<Image>();
            img.sprite = coinSprite;
            img.raycastTarget = false;

            go.SetActive(false);
            return rect;
        }

        private void ReleaseCoin(RectTransform coin)
        {
            if (coin == null) return;
            coin.gameObject.SetActive(false);
            _pool.Enqueue(coin);
        }
    }
}
