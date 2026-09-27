using System.Collections.Generic;
using Game.App;
using Game.App.Tutorial;
using Game.Core.Board;
using Game.Core.Levels;
using Game.Core.People;
using Game.View.Board;
using Game.View.Input;
using Game.View.People;
using GameLogger = Game.Logging.Logger;
using TMPro;
using UnityEngine;

namespace Game.View.Tutorial
{
    public sealed class LevelTwoTutorialStep : TutorialStep
    {
        private enum Stage
        {
            None,
            TapCoughy,
            HoldCoughyTooltip,
            TapCooly,
            HoldWrongPersonTooltip,
            HoldCoolyTooltip,
            PlacePeople,
            RevealAngry
        }

        private const float TooltipHoldDuration = 2.5f;
        private const float TutorialArtboardWidth = 600f;
        private const float TutorialArtboardHeight = 1213f;

        private static readonly Rect NewGuyInstructionBounds = new Rect(58f, 790f, 484f, 43f);
        private static readonly Rect CoolyQuestionBounds = new Rect(70f, 792f, 460f, 63f);
        private static readonly Rect CoolyExplanationBounds = new Rect(42f, 570f, 516f, 108f);
        private static readonly Rect DragInstructionBounds = new Rect(55f, 393f, 490f, 47f);
        private static readonly Rect AngryInstructionBounds = new Rect(43f, 365f, 514f, 58f);

        private GameManager _gameManager;
        private GridManager _gridManager;
        private Transform _overlayParent;
        private TMP_FontAsset _tutorialFont;
        private Material _tutorialFontMaterial;
        private Sprite _tutorialHandSprite;
        private FirstTimeTutorialStep.TutorialOverlayView _overlay;
        private LevelManager _activeLevelManager;
        private CellView _hamburgerCell;
        private PersonRuntimeData _coughy;
        private PersonRuntimeData _cooly;
        private PersonView _coughyView;
        private PersonView _coolyView;
        private PersonTooltip _coughyTooltip;
        private PersonTooltip _coolyTooltip;
        private PersonDragManager _coughyDragManager;
        private PersonDragManager _coolyDragManager;
        private float _remainingTime;
        private bool _isActive;
        private bool _isDraggingPerson;
        private bool _gameplayInputLocked;
        private bool _previousGameplayInputEnabled;
        private Stage _stage;

        public void Configure(
            GameManager gameManager,
            GridManager gridManager,
            Transform overlayParent,
            TMP_FontAsset tutorialFont,
            Material tutorialFontMaterial,
            Sprite tutorialHandSprite)
        {
            _gameManager = gameManager;
            _gridManager = gridManager;
            _overlayParent = overlayParent;
            _tutorialFont = tutorialFont;
            _tutorialFontMaterial = tutorialFontMaterial;
            _tutorialHandSprite = tutorialHandSprite;
        }

        public override void StartStep()
        {
            EndStep();
            _isActive = true;

            if (_gameManager == null || _gameManager.ActiveLevelNumber != 2 || _gridManager == null)
            {
                Skip("Level 2 tutorial requires the active Level 2 board.");
                return;
            }

            _activeLevelManager = _gameManager.ActiveLevelManager;
            LevelRuntimeData level = _activeLevelManager != null ? _activeLevelManager.CurrentLevel : null;
            if (level == null)
            {
                Skip("Level 2 tutorial could not find the active level.");
                return;
            }

            _coughy = FindPerson(level.WaitGrid, PersonTrait.Sick) ?? FindPerson(level.MainGrid, PersonTrait.Sick);
            _cooly = FindPerson(level.WaitGrid, PersonTrait.Cool) ?? FindPerson(level.MainGrid, PersonTrait.Cool);
            _coughyView = _gridManager.FindPersonView(_coughy);
            _coolyView = _gridManager.FindPersonView(_cooly);
            _coughyTooltip = _coughyView != null ? _coughyView.Tooltip : null;
            _coolyTooltip = _coolyView != null ? _coolyView.Tooltip : null;
            _coughyDragManager = _coughyView != null ? _coughyView.GetComponent<PersonDragManager>() : null;
            _coolyDragManager = _coolyView != null ? _coolyView.GetComponent<PersonDragManager>() : null;
            _hamburgerCell = FindFoodCell(level.MainGrid, Food.Hamburger);

            if (_coughy == null || _cooly == null || _coughyTooltip == null ||
                _coolyTooltip == null || _hamburgerCell == null)
            {
                Skip("Level 2 tutorial needs Coughy, Cooly, and the hamburger cell.");
                return;
            }

            if (_overlay == null && _overlayParent != null)
            {
                _overlay = new FirstTimeTutorialStep.TutorialOverlayView(
                    _overlayParent,
                    _tutorialFont,
                    _tutorialFontMaterial,
                    _tutorialHandSprite);
            }

            if (_overlay == null)
            {
                Skip("Level 2 tutorial overlay parent is missing.");
                return;
            }

            _coughyTooltip.Tapped += HandleCoughyTapped;
            _coolyTooltip.Tapped += HandleCoolyTapped;
            if (_coughyDragManager != null)
            {
                _coughyDragManager.DragStarted += HandlePersonDragStarted;
                _coughyDragManager.DragEnded += HandlePersonDragEnded;
            }

            if (_coolyDragManager != null)
            {
                _coolyDragManager.DragStarted += HandlePersonDragStarted;
                _coolyDragManager.DragEnded += HandlePersonDragEnded;
            }

            _activeLevelManager.MoveSucceeded += HandleMoveSucceeded;
            StartTapCoughyStep();
            GameLogger.Log("LevelTwoTutorialStep: tutorial started.");
        }

        public override void UpdateStep()
        {
            if (!_isActive)
                return;

            _overlay?.UpdatePointer();
            if (_stage != Stage.HoldCoughyTooltip &&
                _stage != Stage.HoldWrongPersonTooltip &&
                _stage != Stage.HoldCoolyTooltip)
                return;

            _remainingTime -= Time.unscaledDeltaTime;
            if (_remainingTime > 0f)
                return;

            bool explainCooly = _stage == Stage.HoldCoolyTooltip;
            bool tappedWrongPerson = _stage == Stage.HoldWrongPersonTooltip;
            RestoreGameplayInput();
            if (explainCooly)
                StartPlacePeopleStep();
            else if (tappedWrongPerson)
            {
                _coughyTooltip.Hide();
                StartTapCoolyStep();
            }
            else
                StartTapCoolyStep();
        }

        public override void EndStep()
        {
            if (_coughyTooltip != null)
                _coughyTooltip.Tapped -= HandleCoughyTapped;
            if (_coolyTooltip != null)
                _coolyTooltip.Tapped -= HandleCoolyTapped;
            if (_coughyDragManager != null)
            {
                _coughyDragManager.DragStarted -= HandlePersonDragStarted;
                _coughyDragManager.DragEnded -= HandlePersonDragEnded;
            }

            if (_coolyDragManager != null)
            {
                _coolyDragManager.DragStarted -= HandlePersonDragStarted;
                _coolyDragManager.DragEnded -= HandlePersonDragEnded;
            }

            if (_activeLevelManager != null)
                _activeLevelManager.MoveSucceeded -= HandleMoveSucceeded;

            RestoreGameplayInput();
            _overlay?.Hide();
            _activeLevelManager = null;
            _coughy = null;
            _cooly = null;
            _coughyView = null;
            _coolyView = null;
            _coughyTooltip = null;
            _coolyTooltip = null;
            _coughyDragManager = null;
            _coolyDragManager = null;
            _hamburgerCell = null;
            _remainingTime = 0f;
            _isDraggingPerson = false;
            _stage = Stage.None;
            _isActive = false;
        }

        private void StartTapCoughyStep()
        {
            _stage = Stage.TapCoughy;
            _overlay.Show(
                "We have a new guy here, let's see",
                NewGuyInstructionBounds,
                _coughyView.transform,
                null,
                false,
                _coughyView.transform);
        }

        private void HandleCoughyTapped()
        {
            if (!_isActive || !_coughyTooltip.IsShowingTooltip)
                return;

            if (_stage == Stage.TapCoughy)
                _stage = Stage.HoldCoughyTooltip;
            else if (_stage == Stage.TapCooly)
            {
                _stage = Stage.HoldWrongPersonTooltip;
                _overlay.ShowClear(
                    "Which one is the cool guy?\nGo find out",
                    CoolyQuestionBounds);
                _overlay.SetInputBlocked(true);
            }
            else
                return;

            _remainingTime = TooltipHoldDuration;
            LockGameplayInput();
            if (_stage == Stage.HoldCoughyTooltip)
                _overlay.ShowHold();
        }

        private void StartTapCoolyStep()
        {
            _coughyTooltip.Hide();
            _stage = Stage.TapCooly;
            _overlay.Show(
                "Which one is the cool guy?\nGo find out",
                CoolyQuestionBounds,
                _coughyView.transform,
                _coolyView.transform,
                false,
                _coughyView.transform,
                _coolyView.transform);
        }

        private void HandleCoolyTapped()
        {
            if (!_isActive || _stage != Stage.TapCooly || !_coolyTooltip.IsShowingTooltip)
                return;

            _stage = Stage.HoldCoolyTooltip;
            _remainingTime = TooltipHoldDuration;
            LockGameplayInput();
            _overlay.ShowDimmedClear(
                "Oh, this is the cool guy.\nEverybody have their own trait.\nLike this one is \"Cool\"",
                CoolyExplanationBounds);
            _overlay.SetInputBlocked(true);
        }

        private void StartPlacePeopleStep()
        {
            _coughyTooltip.Hide();
            _coolyTooltip.Hide();
            _stage = Stage.PlacePeople;
            ShowNextDragInstruction();
        }

        private void ShowNextDragInstruction()
        {
            LevelRuntimeData level = _activeLevelManager != null ? _activeLevelManager.CurrentLevel : null;
            CellRuntimeData coolyCell = FindPersonCell(level != null ? level.MainGrid : null, _cooly);
            CellRuntimeData coughyCell = FindPersonCell(level != null ? level.MainGrid : null, _coughy);

            if (coolyCell != null && coughyCell != null && _coughy.State == PersonState.Angry)
            {
                StartAngryRevealStep(level.MainGrid);
                return;
            }

            PersonView personView;
            CellView targetCell;
            if (coolyCell == null)
            {
                personView = _coolyView;
                targetCell = FindEmptySeatAdjacentTo(_hamburgerCell.RuntimeData);
                if (targetCell == null && coughyCell != null)
                    targetCell = FindEmptySeatAdjacentTo(coughyCell);
            }
            else if (coughyCell == null)
            {
                personView = _coughyView;
                targetCell = FindEmptySeatAdjacentTo(coolyCell);
            }
            else
            {
                personView = _coughyView;
                targetCell = FindEmptySeatAdjacentTo(coolyCell);
                if (targetCell == null)
                {
                    personView = _coolyView;
                    targetCell = FindEmptySeatAdjacentTo(coughyCell);
                }
            }

            if (targetCell == null)
            {
                GameLogger.LogWarning("LevelTwoTutorialStep: no suitable empty seat remains.");
                CompleteStep();
                return;
            }

            _overlay.Show(
                "Try drag these guy next to other",
                DragInstructionBounds,
                personView.transform,
                targetCell.transform,
                false,
                personView.transform,
                targetCell.transform);
        }

        private void StartAngryRevealStep(Grid<CellRuntimeData> mainGrid)
        {
            _stage = Stage.RevealAngry;
            LockGameplayInput();

            List<Transform> focusTargets = new List<Transform>();
            if (mainGrid != null && mainGrid.GridContent != null)
            {
                for (int i = 0; i < mainGrid.GridContent.Length; i++)
                {
                    CellRuntimeData cell = mainGrid.GridContent[i];
                    CellView cellView = cell != null ? _gridManager.FindCellView(cell) : null;
                    if (cellView != null)
                        focusTargets.Add(cellView.transform);
                }
            }

            focusTargets.Add(_coughyView.transform);
            focusTargets.Add(_coolyView.transform);
            _overlay.Show(
                "Oh, they're angry. Solve the puzzle can you?",
                AngryInstructionBounds,
                null,
                null,
                true,
                focusTargets.ToArray());
            _overlay.SetTapAnywhereCallback(CompleteStep);
        }

        private void HandleMoveSucceeded()
        {
            if (_stage == Stage.PlacePeople && !_isDraggingPerson)
                ShowNextDragInstruction();
        }

        private void HandlePersonDragStarted()
        {
            if (_stage != Stage.PlacePeople)
                return;

            _isDraggingPerson = true;
            _overlay.Hide();
        }

        private void HandlePersonDragEnded()
        {
            if (!_isDraggingPerson)
                return;

            _isDraggingPerson = false;
            if (_stage == Stage.PlacePeople)
                ShowNextDragInstruction();
        }

        private void LockGameplayInput()
        {
            if (_gameplayInputLocked)
                return;

            _previousGameplayInputEnabled = GamePresentation == null || GamePresentation.IsGameplayInputEnabled;
            GamePresentation?.SetGameplayInputEnabled(false);
            _gameplayInputLocked = GamePresentation != null;
        }

        private void RestoreGameplayInput()
        {
            if (_gameplayInputLocked)
                GamePresentation?.SetGameplayInputEnabled(_previousGameplayInputEnabled);

            _gameplayInputLocked = false;
            _overlay?.SetInputBlocked(false);
        }

        private PersonRuntimeData FindPerson(Grid<CellRuntimeData> grid, PersonTrait trait)
        {
            if (grid == null || grid.GridContent == null)
                return null;

            for (int i = 0; i < grid.GridContent.Length; i++)
            {
                PersonRuntimeData person = grid.GridContent[i] != null ? grid.GridContent[i].CurrentPerson : null;
                if (person != null && person.Trait == trait)
                    return person;
            }

            return null;
        }

        private CellRuntimeData FindPersonCell(Grid<CellRuntimeData> grid, PersonRuntimeData person)
        {
            if (grid == null || grid.GridContent == null || person == null)
                return null;

            for (int i = 0; i < grid.GridContent.Length; i++)
            {
                CellRuntimeData cell = grid.GridContent[i];
                if (cell != null && cell.CurrentPerson == person)
                    return cell;
            }

            return null;
        }

        private CellView FindFoodCell(Grid<CellRuntimeData> grid, Food food)
        {
            if (grid == null || grid.GridContent == null)
                return null;

            for (int i = 0; i < grid.GridContent.Length; i++)
            {
                CellRuntimeData cell = grid.GridContent[i];
                if (cell != null && cell.Type == CellType.Food && cell.Food == food)
                    return _gridManager.FindCellView(cell);
            }

            return null;
        }

        private CellView FindEmptySeatAdjacentTo(CellRuntimeData cell)
        {
            LevelRuntimeData level = _activeLevelManager != null ? _activeLevelManager.CurrentLevel : null;
            if (cell == null || level?.MainGrid == null || _gridManager.AdjacentOffsets == null)
                return null;

            Grid<CellRuntimeData> mainGrid = level.MainGrid;
            CellView bestCandidate = null;
            for (int i = 0; i < _gridManager.AdjacentOffsets.Count; i++)
            {
                Vector2Int adjacentIndex = cell.Index + _gridManager.AdjacentOffsets[i];
                CellRuntimeData candidate = mainGrid.Get(adjacentIndex.x, adjacentIndex.y);
                if (candidate == null || candidate.Type != CellType.Seat || candidate.CurrentPerson != null)
                    continue;

                CellView view = _gridManager.FindCellView(candidate);
                if (view != null && (bestCandidate == null || candidate.Index.y < bestCandidate.RuntimeData.Index.y))
                    bestCandidate = view;
            }

            return bestCandidate;
        }

        private void Skip(string reason)
        {
            GameLogger.LogWarning("LevelTwoTutorialStep: " + reason);
            EndStep();
            CompleteStep();
        }
    }
}
