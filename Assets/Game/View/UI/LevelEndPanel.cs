using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;
using PrimeTween;
using Cysharp.Threading.Tasks;
using Game.Events;

namespace Game.View.UI
{
    public class LevelEndPanel : MonoBehaviour
    {
        [FormerlySerializedAs("images")]
        [SerializeField] private List<Graphic> graphics;
        [SerializeField] private GameObject navigation;
        [SerializeField] private GameObject reward;
        [SerializeField] private float revealDuration;
        [SerializeField] private Ease revealEase;
        [SerializeField] private float revealDelay;
        [SerializeField] private VoidEventChannelSO onNavigateRequest;

        [Header("Transition Settings")]
        [SerializeField] private float fadeOutDuration;
        [SerializeField] private Ease fadeOutEase;
        [SerializeField] private float fadeInDuration;
        [SerializeField] private Ease fadeInEase;

        private readonly EventListener _eventListener = new();
        private CanvasGroup _rewardCanvasGroup;
        private CanvasGroup _navigationCanvasGroup;

        private void Awake()
        {
            ResetAlpha();
            InitCanvasGroups();
        }

        private void InitCanvasGroups()
        {
            if (reward != null && _rewardCanvasGroup == null)
            {
                if (!reward.TryGetComponent(out _rewardCanvasGroup))
                {
                    _rewardCanvasGroup = reward.AddComponent<CanvasGroup>();
                }
            }

            if (navigation != null && _navigationCanvasGroup == null)
            {
                if (!navigation.TryGetComponent(out _navigationCanvasGroup))
                {
                    _navigationCanvasGroup = navigation.AddComponent<CanvasGroup>();
                }
            }
        }

        private void OnEnable()
        {
            Revealing().Forget();

            InitCanvasGroups();

            if (_rewardCanvasGroup != null)
            {
                _rewardCanvasGroup.alpha = 1f;
                _rewardCanvasGroup.interactable = true;
                _rewardCanvasGroup.blocksRaycasts = true;
            }
            if (reward != null) reward.SetActive(true);

            if (_navigationCanvasGroup != null)
            {
                _navigationCanvasGroup.alpha = 0f;
                _navigationCanvasGroup.interactable = false;
                _navigationCanvasGroup.blocksRaycasts = false;
            }
            if (navigation != null) navigation.SetActive(false);

            _eventListener.Listen(onNavigateRequest, Navigate);
        }

        private void OnDisable()
        {
            _eventListener.UnbindAll();

            if (_rewardCanvasGroup != null)
            {
                Tween.StopAll(_rewardCanvasGroup);
            }

            if (_navigationCanvasGroup != null)
            {
                Tween.StopAll(_navigationCanvasGroup);
            }

            if (graphics == null) return;
            foreach (Graphic graphic in graphics)
            {
                if (graphic != null)
                {
                    Tween.StopAll(graphic);
                }
            }
        }

        public void Navigate()
        {
            NavigateSequenceAsync().Forget();
        }

        private async UniTaskVoid NavigateSequenceAsync()
        {
            if (_rewardCanvasGroup != null)
            {
                _rewardCanvasGroup.interactable = false;
                _rewardCanvasGroup.blocksRaycasts = false;
                if (fadeOutDuration > 0f)
                {
                    await _rewardCanvasGroup.TweenAlpha(0f, fadeOutDuration, fadeOutEase);
                }
                else
                {
                    _rewardCanvasGroup.alpha = 0f;
                }
            }

            if (!gameObject.activeInHierarchy) return;

            if (reward != null)
            {
                reward.SetActive(false);
            }

            if (navigation != null)
            {
                if (_navigationCanvasGroup != null)
                {
                    _navigationCanvasGroup.alpha = 0f;
                    _navigationCanvasGroup.interactable = false;
                    _navigationCanvasGroup.blocksRaycasts = false;
                }

                navigation.SetActive(true);

                if (_navigationCanvasGroup != null)
                {
                    if (fadeInDuration > 0f)
                    {
                        await _navigationCanvasGroup.TweenAlpha(1f, fadeInDuration, fadeInEase);
                    }
                    else
                    {
                        _navigationCanvasGroup.alpha = 1f;
                    }

                    _navigationCanvasGroup.interactable = true;
                    _navigationCanvasGroup.blocksRaycasts = true;
                }
            }
        }

        public void ResetAlpha()
        {
            if (graphics == null) return;
            foreach (Graphic graphic in graphics)
            {
                if (graphic != null)
                {
                    Tween.StopAll(graphic);
                    graphic.SetAlpha(0f);
                }
            }
        }

        [ContextMenu("Revealing")]
        public async UniTask Revealing()
        {
            if (graphics == null || graphics.Count == 0) return;

            ResetAlpha();

            foreach (Graphic graphic in graphics)
            {
                if (!gameObject.activeInHierarchy) return;

                if (revealDelay > 0f)
                {
                    await UniTask.WaitForSeconds(revealDelay);
                }

                if (!gameObject.activeInHierarchy) return;

                if (graphic != null)
                {
                    _ = graphic.TweenAlpha(0f, 1f, revealDuration, revealEase);
                }
            }
        }
    }
}