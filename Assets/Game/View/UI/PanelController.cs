using System.Collections.Generic;
using Game.Events;
using PrimeTween;
using UnityEngine;

namespace Game.View.UI
{
    public enum PanelType
    {
        Shop,
        Main,
        Login
    }

    public class PanelController : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject mainPanel;
        [SerializeField] private GameObject loginPanel;
        [SerializeField] private GameObject shopPanel;

        [Header("Animation Settings")]
        [SerializeField] private float transitionDuration = 0.35f;
        [SerializeField] private Ease easeType = Ease.OutCubic;

        [Header("Events")]
        [SerializeField] private VoidEventChannelSO onMainPanelChange;
        [SerializeField] private VoidEventChannelSO onLoginPanelChange;
        [SerializeField] private VoidEventChannelSO onShopPanelChange;

        private readonly EventListener _listener = new();
        private List<Vector2> panelPos;

        private RectTransform _mainRect;
        private RectTransform _loginRect;
        private RectTransform _shopRect;

        private Tween _moveTween;

        private void Awake()
        {
            if (mainPanel != null) _mainRect = mainPanel.GetComponent<RectTransform>();
            if (loginPanel != null) _loginRect = loginPanel.GetComponent<RectTransform>();
            if (shopPanel != null) _shopRect = shopPanel.GetComponent<RectTransform>();
        }

        private void Start()
        {
            SetUpPanels();
        }

        private void OnEnable()
        {
            _listener.Listen(onMainPanelChange, GoToMainPanel);
            _listener.Listen(onLoginPanelChange, GoToLoginPanel);
            _listener.Listen(onShopPanelChange, GoToShopPanel);
        }

        private void OnDisable()
        {
            _listener.UnbindAll();
            _moveTween.Stop();
        }

        private float GetScreenWidth()
        {
            var canvas = GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                var rootCanvas = canvas.rootCanvas;
                if (rootCanvas != null && rootCanvas.transform is RectTransform rootRect && rootRect.rect.width > 0)
                    return rootRect.rect.width;

                if (canvas.transform is RectTransform canvasRect && canvasRect.rect.width > 0)
                    return canvasRect.rect.width;
            }

            if (transform is RectTransform selfRect && selfRect.rect.width > 0)
                return selfRect.rect.width;

            return Screen.width;
        }

        public List<Vector2> GetPanelPositions(PanelType target = PanelType.Main)
        {
            float w = GetScreenWidth();

            // Độ dời để đưa target panel về giữa (X = 0)
            float offset = target switch
            {
                PanelType.Shop => w,
                PanelType.Main => 0f,
                PanelType.Login => -w,
                _ => 0f
            };

            return new List<Vector2>
            {
                new Vector2(-w + offset, 0f), // 0: Shop Panel (bên trái)
                new Vector2(offset, 0f),       // 1: Main Panel (ở giữa)
                new Vector2(w + offset, 0f)    // 2: Login Panel (bên phải)
            };
        }

        private void SetUpPanels()
        {
            MoveTo(PanelType.Main, animate: false);
        }

        public void MoveTo(PanelType target, bool animate = true)
        {
            panelPos = GetPanelPositions(target);

            _moveTween.Stop();

            if (!animate || transitionDuration <= 0f)
            {
                ApplyPanelPositions(panelPos[0], panelPos[1], panelPos[2]);
                return;
            }

            Vector2 startShop = _shopRect != null ? _shopRect.anchoredPosition : panelPos[0];
            Vector2 startMain = _mainRect != null ? _mainRect.anchoredPosition : panelPos[1];
            Vector2 startLogin = _loginRect != null ? _loginRect.anchoredPosition : panelPos[2];

            Vector2 targetShop = panelPos[0];
            Vector2 targetMain = panelPos[1];
            Vector2 targetLogin = panelPos[2];

            _moveTween = Tween.Custom(
                target: this,
                startValue: 0f,
                endValue: 1f,
                duration: transitionDuration,
                ease: easeType,
                onValueChange: (self, t) =>
                {
                    self.ApplyPanelPositions(
                        Vector2.LerpUnclamped(startShop, targetShop, t),
                        Vector2.LerpUnclamped(startMain, targetMain, t),
                        Vector2.LerpUnclamped(startLogin, targetLogin, t)
                    );
                }
            );
        }

        private void ApplyPanelPositions(Vector2 shopPos, Vector2 mainPos, Vector2 loginPos)
        {
            if (_shopRect != null) _shopRect.anchoredPosition = shopPos;
            if (_mainRect != null) _mainRect.anchoredPosition = mainPos;
            if (_loginRect != null) _loginRect.anchoredPosition = loginPos;
        }

        private void GoToMainPanel() => MoveTo(PanelType.Main);
        private void GoToShopPanel() => MoveTo(PanelType.Shop);
        private void GoToLoginPanel() => MoveTo(PanelType.Login);
    }
}