using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using PrimeTween;
using UnityEngine;

namespace Game.View.Effect
{
    /// <summary>
    /// Spawns pooled currency icons in a burst, holds them in place, then shrinks
    /// and releases them instead of sending them to a target.
    /// </summary>
    public class CurrencyScatterAnimation : MonoBehaviour
    {
        [Header("Layering (Always on top)")]
        [Tooltip("When true, coins are spawned under the same top-level overlay canvas used by CurrencyFlyAnimation.")]
        [SerializeField] private bool alwaysOnTop = true;
        [SerializeField] private int overlaySortingOrder = 32767;

        [Header("Visual Settings")]
        [SerializeField] private Sprite coinSprite;
        [SerializeField] private Vector2 coinSize;
        [SerializeField] private RectTransform container;

        [Header("Scatter Settings")]
        [Min(0)] [SerializeField] private int coinCount = 15;
        [Min(0f)] [SerializeField] private float scatterRadius = 160f;
        [Min(0f)] [SerializeField] private float scatterDuration = 0.4f;
        [SerializeField] private Ease scatterEase = Ease.OutQuad;

        [Header("Disappear Settings")]
        [Min(0f)] [SerializeField] private float delayBeforeShrink = 1f;
        [Min(0f)] [SerializeField] private float shrinkDuration = 0.25f;
        [Min(0f)] [SerializeField] private float shrinkStaggerInterval;
        [SerializeField] private Ease shrinkEase = Ease.InBack;

        private readonly Queue<RectTransform> _pool = new();
        private readonly HashSet<RectTransform> _activeCoins = new();
        private CancellationTokenSource _playCancellation;
        private bool _isInitialized;

        public event Action OnCoinDisappeared;
        public event Action OnAllCoinsDisappeared;

        private void Awake()
        {
            EnsureInitialized();
            PrewarmPool();
        }

        private void OnEnable()
        {
            if (_playCancellation == null)
            {
                _playCancellation = new CancellationTokenSource();
            }
        }

        private void OnDisable()
        {
            CancelAndReleaseActiveCoins();
        }

        private void OnDestroy()
        {
            CancelAndReleaseActiveCoins();
        }

        /// <summary>
        /// Plays the scatter animation from the center of the screen.
        /// </summary>
        public void PlayFromScreenCenter()
        {
            Vector3 center = new(Screen.width * 0.5f, Screen.height * 0.5f, 0f);
            PlayAsync(center, isScreenPos: true).Forget();
        }

        /// <summary>
        /// Plays the scatter animation from a transform position.
        /// </summary>
        public void PlayFromTransform(Transform source)
        {
            if (source == null)
            {
                PlayFromScreenCenter();
                return;
            }

            PlayAsync(source.position, isScreenPos: false, sourceTransform: source).Forget();
        }

        /// <summary>
        /// Fire-and-forget entry point for callers that already have a world or screen position.
        /// </summary>
        public void Play(Vector3 startPos, bool isScreenPos = false)
        {
            PlayAsync(startPos, isScreenPos).Forget();
        }

        /// <summary>
        /// Plays the full scatter, delay, shrink, and pool-release sequence.
        /// </summary>
        public async UniTask PlayAsync(
            Vector3 startPos,
            bool isScreenPos = false,
            Transform sourceTransform = null)
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            EnsureInitialized();
            if (container == null)
            {
                Debug.LogWarning("[CurrencyScatterAnimation] Missing container RectTransform.");
                return;
            }

            int count = Mathf.Max(0, coinCount);
            if (count == 0)
            {
                OnAllCoinsDisappeared?.Invoke();
                return;
            }

            CancellationToken cancellationToken = GetPlayCancellationToken();
            Vector2 startLocalPos = GetLocalPosition(startPos, isScreenPos, sourceTransform);
            List<RectTransform> activeCoins = new(count);

            try
            {
                // Phase 1: spawn at the source and scatter in a random radial distribution.
                for (int i = 0; i < count; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    RectTransform coin = GetCoin();
                    coin.anchoredPosition = startLocalPos;
                    coin.localScale = Vector3.zero;
                    coin.gameObject.SetActive(true);
                    _activeCoins.Add(coin);
                    activeCoins.Add(coin);

                    float angle = (i / (float)count) * Mathf.PI * 2f + UnityEngine.Random.Range(-0.25f, 0.25f);
                    Vector2 direction = new(Mathf.Cos(angle), Mathf.Sin(angle));
                    float distance = UnityEngine.Random.Range(scatterRadius * 0.5f, scatterRadius);
                    Vector2 scatterTarget = startLocalPos + direction * distance;

                    _ = Tween.Scale(coin, Vector3.one, scatterDuration * 0.5f, Ease.OutQuad);
                    _ = Tween.UIAnchoredPosition(coin, scatterTarget, scatterDuration, scatterEase);
                }

                await DelaySeconds(scatterDuration + delayBeforeShrink, cancellationToken);

                // Phase 2: shrink each coin and release it back to the pool.
                List<UniTask> disappearTasks = new(activeCoins.Count);
                for (int i = 0; i < activeCoins.Count; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    RectTransform coin = activeCoins[i];
                    if (coin == null || !coin.gameObject.activeSelf)
                    {
                        continue;
                    }

                    disappearTasks.Add(ShrinkAndRelease(coin, cancellationToken));

                    if (shrinkStaggerInterval > 0f && i < activeCoins.Count - 1)
                    {
                        await DelaySeconds(shrinkStaggerInterval, cancellationToken);
                    }
                }

                await UniTask.WhenAll(disappearTasks);

                if (!cancellationToken.IsCancellationRequested)
                {
                    OnAllCoinsDisappeared?.Invoke();
                }
            }
            catch (OperationCanceledException)
            {
                // Disable/destroy cleanup owns releasing the active coins.
            }
            finally
            {
                // Also covers a coin whose shrink task was interrupted before it could release.
                for (int i = 0; i < activeCoins.Count; i++)
                {
                    ReleaseCoin(activeCoins[i]);
                }
            }
        }

        private async UniTask ShrinkAndRelease(RectTransform coin, CancellationToken cancellationToken)
        {
            try
            {
                await Tween.Scale(coin, Vector3.zero, shrinkDuration, shrinkEase);

                if (!cancellationToken.IsCancellationRequested)
                {
                    OnCoinDisappeared?.Invoke();
                }
            }
            catch (OperationCanceledException)
            {
                // The owner may be disabled while the animation is active.
            }
            finally
            {
                ReleaseCoin(coin);
            }
        }

        private void EnsureInitialized()
        {
            if (_isInitialized && container != null)
            {
                return;
            }

            if (alwaysOnTop)
            {
                container = CurrencyFlyAnimation.GetOrCreateOverlayContainer(overlaySortingOrder);
            }
            else if (container == null)
            {
                Canvas rootCanvas = GetComponentInParent<Canvas>()?.rootCanvas;
                container = rootCanvas != null
                    ? rootCanvas.GetComponent<RectTransform>()
                    : GetComponent<RectTransform>();
            }

            _isInitialized = true;
        }

        private void PrewarmPool()
        {
            for (int i = 0; i < Mathf.Max(0, coinCount); i++)
            {
                _pool.Enqueue(CreateNewCoin());
            }
        }

        private CancellationToken GetPlayCancellationToken()
        {
            if (_playCancellation == null)
            {
                _playCancellation = new CancellationTokenSource();
            }

            return _playCancellation.Token;
        }

        private static async UniTask DelaySeconds(float seconds, CancellationToken cancellationToken)
        {
            if (seconds <= 0f)
            {
                return;
            }

            await UniTask.Delay(
                Mathf.CeilToInt(seconds * 1000f),
                cancellationToken: cancellationToken);
        }

        private Vector2 GetEffectiveCoinSize()
        {
            if (coinSize.x > 0f && coinSize.y > 0f)
            {
                return coinSize;
            }

            if (coinSprite != null && coinSprite.rect.width > 0f && coinSprite.rect.height > 0f)
            {
                return new Vector2(coinSprite.rect.width, coinSprite.rect.height);
            }

            return new Vector2(35f, 35f);
        }

        private Vector2 GetLocalPosition(Vector3 screenOrWorldPos, bool isScreenPos, Transform sourceTransform = null)
        {
            Vector2 screenPoint;
            if (isScreenPos)
            {
                screenPoint = screenOrWorldPos;
            }
            else
            {
                Camera sourceCamera = null;
                if (sourceTransform != null)
                {
                    Canvas sourceCanvas = sourceTransform.GetComponentInParent<Canvas>();
                    if (sourceCanvas != null && sourceCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
                    {
                        sourceCamera = sourceCanvas.worldCamera;
                    }
                }

                if (sourceCamera == null)
                {
                    Canvas anyCanvas = GetComponentInParent<Canvas>();
                    if (anyCanvas != null && anyCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
                    {
                        sourceCamera = anyCanvas.worldCamera;
                    }
                }

                screenPoint = RectTransformUtility.WorldToScreenPoint(sourceCamera, screenOrWorldPos);
            }

            Canvas containerCanvas = container != null ? container.GetComponentInParent<Canvas>() : null;
            Camera containerCamera = containerCanvas != null && containerCanvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? containerCanvas.worldCamera
                : null;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                container,
                screenPoint,
                containerCamera,
                out Vector2 localPoint);

            return localPoint;
        }

        private RectTransform GetCoin()
        {
            while (_pool.Count > 0)
            {
                RectTransform pooled = _pool.Dequeue();
                if (pooled == null)
                {
                    continue;
                }

                if (pooled.parent != container)
                {
                    pooled.SetParent(container, false);
                }

                pooled.SetAsLastSibling();
                Tween.StopAll(pooled);
                return pooled;
            }

            return CreateNewCoin();
        }

        private RectTransform CreateNewCoin()
        {
            GameObject go = new("CoinScatterFx", typeof(RectTransform), typeof(CanvasRenderer), typeof(UnityEngine.UI.Image));
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.SetParent(container != null ? container : transform, false);
            rect.sizeDelta = GetEffectiveCoinSize();

            UnityEngine.UI.Image image = go.GetComponent<UnityEngine.UI.Image>();
            image.sprite = coinSprite;
            image.raycastTarget = false;

            go.SetActive(false);
            return rect;
        }

        private void CancelAndReleaseActiveCoins()
        {
            CancellationTokenSource cancellation = _playCancellation;
            _playCancellation = null;
            if (cancellation != null)
            {
                cancellation.Cancel();
                cancellation.Dispose();
            }

            if (_activeCoins.Count == 0)
            {
                return;
            }

            RectTransform[] activeCoins = new RectTransform[_activeCoins.Count];
            _activeCoins.CopyTo(activeCoins);
            for (int i = 0; i < activeCoins.Length; i++)
            {
                ReleaseCoin(activeCoins[i]);
            }
        }

        private void ReleaseCoin(RectTransform coin)
        {
            if (coin == null || !_activeCoins.Remove(coin))
            {
                return;
            }

            Tween.StopAll(coin);
            coin.localScale = Vector3.one;
            coin.gameObject.SetActive(false);
            _pool.Enqueue(coin);
        }
    }
}
