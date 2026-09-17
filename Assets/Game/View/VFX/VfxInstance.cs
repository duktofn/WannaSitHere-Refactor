using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Game.View.VFX
{
    public sealed class VfxInstance : MonoBehaviour
    {
        private sealed class RendererState
        {
            public ParticleSystemRenderer Renderer;
            public MaterialPropertyBlock PropertyBlock;
            public Color BaseColor = Color.white;
            public bool HasBaseColor;
            public bool HasLegacyColor;
        }

        private ParticleSystem[] _particleSystems = Array.Empty<ParticleSystem>();
        private RendererState[] _rendererStates = Array.Empty<RendererState>();
        private CancellationTokenSource _lifecycleCts;
        private Action<VfxInstance> _onCompleted;
        private float _fadeDelay;
        private float _fadeDuration;
        private Vector3 _prefabLocalScale = Vector3.one;
        private bool _prefabScaleInitialized;
        private bool _isPlaying;

        public Vector3 PrefabLocalScale => _prefabLocalScale;

        private void Awake()
        {
            if (!_prefabScaleInitialized)
            {
                _prefabLocalScale = transform.localScale;
                _prefabScaleInitialized = true;
            }

            CacheComponents();
        }

        internal void SetPrefabLocalScale(Vector3 prefabLocalScale)
        {
            _prefabLocalScale = prefabLocalScale;
            _prefabScaleInitialized = true;
        }

        public void Play(float fadeDelay, float fadeDuration, Action<VfxInstance> onCompleted)
        {
            CancelLifecycle();
            CacheComponents();

            _fadeDelay = Mathf.Max(0f, fadeDelay);
            _fadeDuration = Mathf.Max(0f, fadeDuration);
            _onCompleted = onCompleted;
            _isPlaying = true;

            SetOpacity(1f);

            foreach (ParticleSystem particleSystem in _particleSystems)
            {
                if (particleSystem == null) continue;

                particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                particleSystem.Play(true);
            }

            _lifecycleCts = new CancellationTokenSource();
            RunLifecycleAsync(_lifecycleCts.Token).Forget();
        }

        public void StopImmediately()
        {
            _isPlaying = false;
            CancelLifecycle();
            _onCompleted = null;
            StopAndClearParticles();
            SetOpacity(1f);
        }

        public void ResetForPool()
        {
            StopImmediately();
        }

        private async UniTaskVoid RunLifecycleAsync(CancellationToken token)
        {
            try
            {
                if (_fadeDelay > 0f && _fadeDuration > 0f)
                {
                    await UniTask.Delay(
                        TimeSpan.FromSeconds(_fadeDelay),
                        DelayType.UnscaledDeltaTime,
                        PlayerLoopTiming.Update,
                        token);

                    await FadeOutAsync(token);
                    StopAndClearParticles();
                }

                await UniTask.WaitUntil(
                    () => !IsAnyParticleAlive(),
                    PlayerLoopTiming.Update,
                    token);

                Complete();
            }
            catch (OperationCanceledException)
            {
                // Cancellation is expected when an instance is stopped or returned to the pool.
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
                Complete();
            }
        }

        private async UniTask FadeOutAsync(CancellationToken token)
        {
            float elapsed = 0f;

            while (elapsed < _fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / _fadeDuration);
                SetOpacity(1f - progress);
                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }

            SetOpacity(0f);
        }

        private void Complete()
        {
            if (!_isPlaying) return;

            _isPlaying = false;
            Action<VfxInstance> callback = _onCompleted;
            _onCompleted = null;
            callback?.Invoke(this);
        }

        private void CacheComponents()
        {
            _particleSystems = GetComponentsInChildren<ParticleSystem>(true);
            ParticleSystemRenderer[] renderers = GetComponentsInChildren<ParticleSystemRenderer>(true);
            _rendererStates = new RendererState[renderers.Length];

            for (int i = 0; i < renderers.Length; i++)
            {
                ParticleSystemRenderer renderer = renderers[i];
                Material material = renderer != null ? renderer.sharedMaterial : null;
                RendererState state = new RendererState
                {
                    Renderer = renderer,
                    PropertyBlock = new MaterialPropertyBlock()
                };

                if (material != null)
                {
                    state.HasBaseColor = material.HasProperty("_BaseColor");
                    state.HasLegacyColor = material.HasProperty("_Color");

                    if (state.HasBaseColor)
                        state.BaseColor = material.GetColor("_BaseColor");
                    else if (state.HasLegacyColor)
                        state.BaseColor = material.GetColor("_Color");
                }

                _rendererStates[i] = state;
            }
        }

        private bool IsAnyParticleAlive()
        {
            foreach (ParticleSystem particleSystem in _particleSystems)
            {
                if (particleSystem != null && particleSystem.IsAlive(true))
                    return true;
            }

            return false;
        }

        private void StopAndClearParticles()
        {
            foreach (ParticleSystem particleSystem in _particleSystems)
            {
                if (particleSystem != null)
                    particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }

        private void SetOpacity(float opacity)
        {
            float clampedOpacity = Mathf.Clamp01(opacity);

            foreach (RendererState state in _rendererStates)
            {
                if (state?.Renderer == null) continue;
                if (!state.HasBaseColor && !state.HasLegacyColor) continue;

                Color color = state.BaseColor;
                color.a *= clampedOpacity;

                state.PropertyBlock.Clear();
                if (state.HasBaseColor)
                    state.PropertyBlock.SetColor("_BaseColor", color);
                if (state.HasLegacyColor)
                    state.PropertyBlock.SetColor("_Color", color);

                state.Renderer.SetPropertyBlock(state.PropertyBlock);
            }
        }

        private void CancelLifecycle()
        {
            if (_lifecycleCts == null) return;

            _lifecycleCts.Cancel();
            _lifecycleCts.Dispose();
            _lifecycleCts = null;
        }

        private void OnDisable()
        {
            _isPlaying = false;
            CancelLifecycle();
        }

        private void OnDestroy()
        {
            _isPlaying = false;
            CancelLifecycle();
            _onCompleted = null;
        }
    }
}
