using System.Collections.Generic;
using Game.App;
using Game.App.Tutorial;
using Game.Core.Board;
using Game.Core.Levels;
using Game.Core.People;
using GameLogger = Game.Logging.Logger;
using Game.View.Input;
using Game.View.Board;
using Game.View.People;
using Game.View.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.View.Tutorial
{
    public sealed class FirstTimeTutorialStep : TutorialStep
    {
        private enum Stage
        {
            None,
            TapPerson,
            HoldTooltip,
            ChooseBurger,
            WaitAfterBurgerTap,
            SeatPerson
        }

        private const float TooltipHoldDuration = 2.5f;
        private const float BurgerTooltipHoldDuration = 1f;
        private const float TutorialArtboardWidth = 600f;
        private const float TutorialArtboardHeight = 1213f;

        // Bounds are top-left pixel coordinates from the corresponding Figma tutorial artboards.
        private static readonly Rect PersonInstructionBounds = new Rect(41f, 792f, 480f, 29f);
        private static readonly Rect FoodInstructionBounds = new Rect(114f, 469f, 400f, 58f);
        private static readonly Rect SeatInstructionBounds = new Rect(73f, 752f, 380f, 87f);

        private GameManager _gameManager;
        private GridManager _gridManager;
        private LevelView _levelView;
        private Transform _overlayParent;
        private TMP_FontAsset _tutorialFont;
        private Sprite _tutorialHandSprite;
        private TutorialOverlayView _overlay;
        private LevelManager _activeLevelManager;
        private PersonView _personView;
        private PersonDragManager _personDragManager;
        private PersonRuntimeData _person;
        private PersonTooltip _personTooltip;
        private CellView _hamburgerCell;
        private FoodTooltips _hamburgerTooltip;
        private readonly List<CellView> _seatCells = new List<CellView>();
        private readonly List<FoodTooltips> _foodTooltips = new List<FoodTooltips>();
        private bool _isActive;
        private bool _isPersonSelected;
        private bool _gameplayInputLocked;
        private bool _boostersStateCaptured;
        private bool _previousGameplayInputEnabled;
        private bool _previousBoostersEnabled;
        private float _remainingTime;
        private Stage _stage;

        public void Configure(
            GameManager gameManager,
            GridManager gridManager,
            LevelView levelView,
            Transform overlayParent,
            TMP_FontAsset tutorialFont,
            Sprite tutorialHandSprite)
        {
            _gameManager = gameManager;
            _gridManager = gridManager;
            _levelView = levelView;
            _tutorialFont = tutorialFont;
            _tutorialHandSprite = tutorialHandSprite;
            _overlayParent = overlayParent != null
                ? overlayParent
                : levelView != null ? levelView.transform : null;
        }

        public override void StartStep()
        {
            EndStep();
            _isActive = true;

            _activeLevelManager = _gameManager != null ? _gameManager.ActiveLevelManager : null;
            LevelRuntimeData level = _activeLevelManager != null ? _activeLevelManager.CurrentLevel : null;
            if (_gridManager == null || level == null)
            {
                Skip("Level 1 tutorial could not find the active level or grid.");
                return;
            }

            _personView = FindPersonView(level.WaitGrid) ?? FindPersonView(level.MainGrid);
            _person = _personView != null ? _personView.RuntimeData : null;
            _personDragManager = _personView != null ? _personView.GetComponent<PersonDragManager>() : null;
            _personTooltip = _personView != null ? _personView.Tooltip : null;
            _hamburgerCell = FindFoodCellView(level.MainGrid, Food.Hamburger);
            _hamburgerTooltip = _hamburgerCell != null ? _hamburgerCell.FoodTooltips : null;

            if (_person == null || _personTooltip == null || _hamburgerCell == null || _hamburgerTooltip == null)
            {
                Skip("Level 1 tutorial needs a person tooltip and a hamburger food tooltip.");
                return;
            }

            _personTooltip.Tapped += HandlePersonTapped;
            if (_personDragManager != null)
                _personDragManager.DragStarted += HandlePersonDragStarted;
            _hamburgerTooltip.Tapped += HandleFoodTapped;
            SubscribeToFoodTaps(level.MainGrid);

            if (_overlay == null && _overlayParent != null)
                _overlay = new TutorialOverlayView(_overlayParent, _tutorialFont, _tutorialHandSprite);

            if (_overlay == null)
                GameLogger.LogWarning("FirstTimeTutorialStep: tutorial overlay parent is missing.");

            if (_levelView != null)
            {
                _previousBoostersEnabled = _levelView.BoostersEnabled;
                _boostersStateCaptured = true;
                _levelView.SetBoostersEnabled(false);
            }

            StartPersonStep();
            GameLogger.Log("FirstTimeTutorialStep: tutorial started.");
        }

        public override void UpdateStep()
        {
            if (!_isActive)
                return;

            _overlay?.UpdatePointer();

            if (_stage == Stage.HoldTooltip)
            {
                _remainingTime -= Time.unscaledDeltaTime;
                if (_remainingTime <= 0f)
                {
                    RestoreGameplayInput();
                    StartFoodStep();
                }
            }
            else if (_stage == Stage.SeatPerson && _person.State == PersonState.Happy)
            {
                CompleteStep();
            }
            else if (_stage == Stage.WaitAfterBurgerTap)
            {
                _remainingTime -= Time.unscaledDeltaTime;
                if (_remainingTime <= 0f)
                    StartSeatStep();
            }
        }

        public override void EndStep()
        {
            for (int i = 0; i < _seatCells.Count; i++)
                _seatCells[i].Tapped -= HandleSeatTapped;
            _seatCells.Clear();

            if (_personTooltip != null)
                _personTooltip.Tapped -= HandlePersonTapped;

            if (_personDragManager != null)
                _personDragManager.DragStarted -= HandlePersonDragStarted;

            if (_hamburgerTooltip != null)
                _hamburgerTooltip.Tapped -= HandleFoodTapped;

            for (int i = 0; i < _foodTooltips.Count; i++)
                _foodTooltips[i].Tapped -= HandleFoodTapped;
            _foodTooltips.Clear();

            if (_activeLevelManager != null)
                _activeLevelManager.MoveSucceeded -= HandleMoveSucceeded;

            RestoreGameplayInput();

            if (_boostersStateCaptured && _levelView != null)
                _levelView.SetBoostersEnabled(_previousBoostersEnabled);

            _overlay?.Hide();
            _stage = Stage.None;
            _isActive = false;
            _isPersonSelected = false;
            _personTooltip = null;
            _personDragManager = null;
            _hamburgerTooltip = null;
            _personView = null;
            _person = null;
            _hamburgerCell = null;
            _activeLevelManager = null;
            _boostersStateCaptured = false;
        }

        private void StartPersonStep()
        {
            _stage = Stage.TapPerson;
            _overlay?.Show(
                "Try tap on this guy to see what he want",
                PersonInstructionBounds,
                _personView.transform,
                null);
        }

        private void HandlePersonTapped()
        {
            if (_stage == Stage.TapPerson && _personTooltip.IsShowingTooltip)
            {
                _stage = Stage.HoldTooltip;
                _remainingTime = TooltipHoldDuration;
                _previousGameplayInputEnabled = GamePresentation == null || GamePresentation.IsGameplayInputEnabled;
                GamePresentation?.SetGameplayInputEnabled(false);
                _gameplayInputLocked = GamePresentation != null;
                _overlay?.ShowHold();
                return;
            }

            if (_stage == Stage.SeatPerson)
            {
                _isPersonSelected = true;
            }
        }

        private void HandlePersonDragStarted()
        {
            if (_stage == Stage.SeatPerson)
                _overlay?.Hide();
        }

        private void StartFoodStep()
        {
            LevelRuntimeData level = _activeLevelManager != null ? _activeLevelManager.CurrentLevel : null;
            CellView friesCell = level != null ? FindFoodCellView(level.MainGrid, Food.FrenchFries) : null;
            Transform firstFood = friesCell != null ? friesCell.transform : _hamburgerCell.transform;

            _stage = Stage.ChooseBurger;
            _overlay?.Show(
                "Try press on these foods to see\nwhich one is the burger",
                FoodInstructionBounds,
                firstFood,
                _hamburgerCell.transform);
        }

        private void HandleFoodTapped(FoodTooltips tappedTooltip)
        {
            if (_stage != Stage.ChooseBurger)
                return;

            _overlay?.Hide();
            if (tappedTooltip != _hamburgerTooltip)
                return;

            _stage = Stage.WaitAfterBurgerTap;
            _remainingTime = BurgerTooltipHoldDuration;
            GameLogger.Log("FirstTimeTutorialStep: burger tapped; waiting before the drag step.");
        }

        private void StartSeatStep()
        {
            Transform seatTarget = FindSuitableSeat(_hamburgerCell.RuntimeData);
            _stage = Stage.SeatPerson;
            SubscribeToSeatTaps();
            _activeLevelManager.MoveSucceeded += HandleMoveSucceeded;
            _overlay?.Show(
                "Maybe he want this burger,\ndrag him to the somewhere\nhe can sit to eat this burger",
                SeatInstructionBounds,
                _personView.transform,
                seatTarget);
            GameLogger.Log("FirstTimeTutorialStep: drag step started.");

            if (_person.State == PersonState.Happy)
                CompleteStep();
        }

        private void HandleMoveSucceeded()
        {
            if (_stage == Stage.SeatPerson && _person != null && _person.State == PersonState.Happy)
                CompleteStep();
        }

        private void HandleSeatTapped(CellView tappedCell)
        {
            if (_stage != Stage.SeatPerson || !_isPersonSelected || tappedCell == null ||
                tappedCell.RuntimeData == null || tappedCell.RuntimeData.Type != CellType.Seat ||
                tappedCell.RuntimeData.CurrentPerson != null ||
                !IsSeatAdjacentToFood(tappedCell.RuntimeData, _hamburgerCell.RuntimeData))
            {
                return;
            }

            PersonDragManager dragManager = _personView.GetComponent<PersonDragManager>();
            if (dragManager != null && dragManager.TryMoveToCell(tappedCell))
                _isPersonSelected = false;
        }

        private void SubscribeToSeatTaps()
        {
            LevelRuntimeData level = _activeLevelManager != null ? _activeLevelManager.CurrentLevel : null;
            Grid<CellRuntimeData> mainGrid = level != null ? level.MainGrid : null;
            if (mainGrid == null || mainGrid.GridContent == null)
                return;

            for (int i = 0; i < mainGrid.GridContent.Length; i++)
            {
                CellRuntimeData cell = mainGrid.GridContent[i];
                if (cell == null || cell.Type != CellType.Seat)
                    continue;

                CellView cellView = _gridManager.FindCellView(cell);
                if (cellView == null)
                    continue;

                cellView.Tapped += HandleSeatTapped;
                _seatCells.Add(cellView);
            }
        }

        private void SubscribeToFoodTaps(Grid<CellRuntimeData> grid)
        {
            if (grid == null || grid.GridContent == null)
                return;

            for (int i = 0; i < grid.GridContent.Length; i++)
            {
                CellRuntimeData cell = grid.GridContent[i];
                if (cell == null || cell.Type != CellType.Food)
                    continue;

                CellView cellView = _gridManager.FindCellView(cell);
                FoodTooltips foodTooltip = cellView != null ? cellView.FoodTooltips : null;
                if (foodTooltip == null || foodTooltip == _hamburgerTooltip)
                    continue;

                foodTooltip.Tapped += HandleFoodTapped;
                _foodTooltips.Add(foodTooltip);
            }
        }

        private PersonView FindPersonView(Grid<CellRuntimeData> grid)
        {
            if (grid == null || grid.GridContent == null)
                return null;

            for (int i = 0; i < grid.GridContent.Length; i++)
            {
                CellRuntimeData cell = grid.GridContent[i];
                if (cell == null || cell.CurrentPerson == null)
                    continue;

                PersonView view = _gridManager.FindPersonView(cell.CurrentPerson);
                if (view != null)
                    return view;
            }

            return null;
        }

        private CellView FindFoodCellView(Grid<CellRuntimeData> grid, Food food)
        {
            if (grid == null || grid.GridContent == null)
                return null;

            for (int i = 0; i < grid.GridContent.Length; i++)
            {
                CellRuntimeData cell = grid.GridContent[i];
                if (cell == null || cell.Type != CellType.Food || cell.Food != food)
                    continue;

                return _gridManager.FindCellView(cell);
            }

            return null;
        }

        private Transform FindSuitableSeat(CellRuntimeData foodCell)
        {
            LevelRuntimeData level = _activeLevelManager != null ? _activeLevelManager.CurrentLevel : null;
            Grid<CellRuntimeData> mainGrid = level != null ? level.MainGrid : null;
            if (foodCell == null || mainGrid == null || mainGrid.GridContent == null)
                return null;

            for (int i = 0; i < mainGrid.GridContent.Length; i++)
            {
                CellRuntimeData candidate = mainGrid.GridContent[i];
                if (candidate == null || candidate.Type != CellType.Seat || candidate.CurrentPerson != null ||
                    !IsSeatAdjacentToFood(candidate, foodCell))
                    continue;

                CellView seatView = _gridManager.FindCellView(candidate);
                if (seatView != null)
                    return seatView.transform;
            }

            return null;
        }

        private bool IsSeatAdjacentToFood(CellRuntimeData seat, CellRuntimeData food)
        {
            if (seat == null || food == null || _gridManager.AdjacentOffsets == null)
                return false;

            for (int i = 0; i < _gridManager.AdjacentOffsets.Count; i++)
            {
                if (seat.Index + _gridManager.AdjacentOffsets[i] == food.Index)
                    return true;
            }

            return false;
        }

        private void RestoreGameplayInput()
        {
            if (_gameplayInputLocked)
                GamePresentation?.SetGameplayInputEnabled(_previousGameplayInputEnabled);

            _gameplayInputLocked = false;
            _overlay?.SetInputBlocked(false);
        }

        private void Skip(string reason)
        {
            GameLogger.LogError("FirstTimeTutorialStep: " + reason);
            EndStep();
            CompleteStep();
        }

        private sealed class TutorialOverlayView
        {
            private const float HandSize = 164f;
            // The index fingertip is offset from the sprite's top-left corner by these Figma pixels.
            private const float HandTipOffsetX = 75f;
            private const float HandTipOffsetY = 23f;
            private const float HandShiftRight = 20f;
            private const float TextBackgroundPaddingX = 12f;
            private const float TextBackgroundPaddingY = 8f;

            private readonly GameObject _root;
            private readonly RectTransform _rootRect;
            private readonly CanvasGroup _canvasGroup;
            private readonly Image _background;
            private readonly RectTransform _pointerRect;
            private readonly Image _pointerImage;
            private readonly GameObject _messageBackgroundObject;
            private readonly RectTransform _messageBackgroundRect;
            private readonly GameObject _messageObject;
            private readonly RectTransform _messageRect;
            private readonly TextMeshProUGUI _message;
            private readonly Canvas _canvas;
            private readonly TMP_FontAsset _font;
            private Transform _firstTarget;
            private Transform _secondTarget;

            public TutorialOverlayView(Transform parent, TMP_FontAsset font, Sprite handSprite)
            {
                _canvas = parent.GetComponentInParent<Canvas>();
                _font = font;
                _root = new GameObject("TutorialOverlay", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
                _rootRect = _root.GetComponent<RectTransform>();
                _rootRect.SetParent(parent, false);
                _rootRect.anchorMin = Vector2.zero;
                _rootRect.anchorMax = Vector2.one;
                _rootRect.offsetMin = Vector2.zero;
                _rootRect.offsetMax = Vector2.zero;
                _rootRect.SetAsLastSibling();

                _canvasGroup = _root.GetComponent<CanvasGroup>();
                _canvasGroup.blocksRaycasts = false;
                _canvasGroup.interactable = false;
                _background = _root.GetComponent<Image>();
                _background.color = Color.clear;
                _background.raycastTarget = false;

                _messageBackgroundObject = new GameObject(
                    "InstructionBackground",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image));
                _messageBackgroundRect = _messageBackgroundObject.GetComponent<RectTransform>();
                _messageBackgroundRect.SetParent(_rootRect, false);
                Image messageBackground = _messageBackgroundObject.GetComponent<Image>();
                messageBackground.color = Color.black;
                messageBackground.raycastTarget = false;

                _messageObject = new GameObject(
                    "InstructionText",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(TextMeshProUGUI));
                _messageRect = _messageObject.GetComponent<RectTransform>();
                _messageRect.SetParent(_rootRect, false);
                _message = _messageObject.GetComponent<TextMeshProUGUI>();
                _message.font = _font;
                _message.color = Color.white;
                _message.enableAutoSizing = false;
                _message.enableWordWrapping = true;
                _message.margin = Vector4.zero;
                _message.alignment = TextAlignmentOptions.Center;
                _message.raycastTarget = false;

                GameObject pointerObject = new GameObject(
                    "TutorialPointer",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image));
                _pointerRect = pointerObject.GetComponent<RectTransform>();
                _pointerRect.SetParent(_rootRect, false);
                _pointerRect.anchorMin = new Vector2(0.5f, 0.5f);
                _pointerRect.anchorMax = new Vector2(0.5f, 0.5f);
                _pointerRect.pivot = new Vector2(0f, 1f);
                _pointerImage = pointerObject.GetComponent<Image>();
                _pointerImage.sprite = handSprite;
                _pointerImage.preserveAspect = true;
                _pointerImage.raycastTarget = false;

                _root.SetActive(false);
            }

            public void Show(string message, Rect messageBounds, Transform firstTarget, Transform secondTarget)
            {
                _message.text = message;
                _messageBackgroundObject.SetActive(true);
                _messageObject.SetActive(true);
                _background.color = new Color(0f, 0f, 0f, 0.72f);
                _background.raycastTarget = false;
                SetInputBlocked(false);
                Rect backgroundBounds = new Rect(
                    messageBounds.x - TextBackgroundPaddingX,
                    messageBounds.y - TextBackgroundPaddingY,
                    messageBounds.width + TextBackgroundPaddingX * 2f,
                    messageBounds.height + TextBackgroundPaddingY * 2f);
                PositionFromDesign(_messageBackgroundRect, backgroundBounds);
                PositionFromDesign(_messageRect, messageBounds);
                _message.fontSize = 24f * (_rootRect.rect.height / TutorialArtboardHeight);
                _firstTarget = firstTarget;
                _secondTarget = secondTarget;
                _root.SetActive(true);
                _rootRect.SetAsLastSibling();
                _pointerRect.gameObject.SetActive(_pointerImage.sprite != null && firstTarget != null);
                UpdatePointer();
            }

            public void ShowHold()
            {
                _messageBackgroundObject.SetActive(false);
                _messageObject.SetActive(false);
                _pointerRect.gameObject.SetActive(false);
                _firstTarget = null;
                _secondTarget = null;
                _background.color = Color.clear;
                _root.SetActive(true);
                _rootRect.SetAsLastSibling();
                SetInputBlocked(true);
            }

            public void SetInputBlocked(bool blocked)
            {
                _canvasGroup.blocksRaycasts = blocked;
                _canvasGroup.interactable = blocked;
                _background.raycastTarget = blocked;
            }

            public void UpdatePointer()
            {
                if (!_root.activeSelf)
                    return;

                if (!_pointerRect.gameObject.activeSelf || _firstTarget == null)
                {
                    _pointerRect.gameObject.SetActive(false);
                    return;
                }

                _pointerRect.gameObject.SetActive(true);
                Vector3 targetPosition = _firstTarget.position;
                if (_secondTarget != null)
                {
                    float blend = Mathf.PingPong(Time.unscaledTime * 0.8125f, 1f);
                    targetPosition = Vector3.Lerp(targetPosition, _secondTarget.position, blend);
                }

                Camera eventCamera = _canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay
                    ? _canvas.worldCamera
                    : null;
                Camera worldCamera = Camera.main;
                if (worldCamera == null)
                    return;

                Vector2 screenPosition = worldCamera.WorldToScreenPoint(targetPosition);
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _rootRect,
                    screenPosition,
                    eventCamera,
                    out Vector2 localPosition))
                {
                    float horizontalScale = _rootRect.rect.width / TutorialArtboardWidth;
                    float verticalScale = _rootRect.rect.height / TutorialArtboardHeight;
                    float handScale = Mathf.Min(horizontalScale, verticalScale);
                    float tapOffset = Mathf.PingPong(Time.unscaledTime * 24f, 8f * verticalScale);
                    _pointerRect.sizeDelta = new Vector2(HandSize * handScale, HandSize * handScale);
                    _pointerRect.anchoredPosition = localPosition + new Vector2(
                        (-HandTipOffsetX + HandShiftRight) * horizontalScale,
                        HandTipOffsetY * verticalScale - tapOffset);
                }
            }

            private void PositionFromDesign(RectTransform rectTransform, Rect bounds)
            {
                Vector2 topLeftAnchor = new Vector2(
                    bounds.x / TutorialArtboardWidth,
                    1f - bounds.y / TutorialArtboardHeight);
                float horizontalScale = _rootRect.rect.width / TutorialArtboardWidth;
                float verticalScale = _rootRect.rect.height / TutorialArtboardHeight;

                rectTransform.anchorMin = topLeftAnchor;
                rectTransform.anchorMax = topLeftAnchor;
                rectTransform.pivot = new Vector2(0f, 1f);
                rectTransform.anchoredPosition = Vector2.zero;
                rectTransform.sizeDelta = new Vector2(
                    bounds.width * horizontalScale,
                    bounds.height * verticalScale);
            }

            public void Hide()
            {
                SetInputBlocked(false);
                _root.SetActive(false);
                _firstTarget = null;
                _secondTarget = null;
            }
        }
    }
}
