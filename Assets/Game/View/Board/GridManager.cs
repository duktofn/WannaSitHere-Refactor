using System.Collections.Generic;
using UnityEngine;
using Game.Core.Board;
using Game.Core.Conditions;
using Game.Core.Levels;
using Game.Core.People;
using Game.View.People;
using Game.Events;

namespace Game.View.Board
{
    public class GridManager : MonoBehaviour
    {
        private Grid<CellRuntimeData> _main;
        private Grid<CellRuntimeData> _wait;
        private LevelRuntimeData _currentLevel;
        private LevelConditionEvaluator _conditionEvaluator;

        [SerializeField] private PersonMover personMoveManager;
        [SerializeField] private GameObject cellPrefabs;
        [SerializeField] private List<Vector2> adjacent;
        [SerializeField] private Transform gridRoot;


        [Header("Events")]
        [SerializeField] private VoidEventChannelSO OnWinEvent;
        [SerializeField] private VoidEventChannelSO OnLoseEvent;

        private void Awake()
        {
            personMoveManager = GetComponent<PersonMover>();
            _conditionEvaluator = new LevelConditionEvaluator(adjacent);
        }
        
        public void CreateMainGrid()
        {
            _main = new Grid<CellRuntimeData>(_currentLevel.MainGrid);

            foreach (CellRuntimeData c in _main.GridContent)
            {
                Vector2 step = _main.CellDistance + _main.CellSize;
                Vector3 cellOffset = new Vector3((c.Index.x - (_main.GridSize.x - 1) / 2f) * step.x,
                                                 (c.Index.y - (_main.GridSize.y - 1) / 2f) * step.y,
                                                 0f)
                                    + GetGridWorldPos(_main);

                GameObject tmpCell = Instantiate(cellPrefabs, cellOffset, Quaternion.identity, gridRoot);
                tmpCell.GetComponent<CellView>().BindData(c, personMoveManager);

                if (c.CurrentPerson != null)
                    CheckPersonCondition(c, c.CurrentPerson, c.OwnGrid);
            }
        }

        public void CreateWaitGrid()
        {
            _wait = new Grid<CellRuntimeData>(_currentLevel.WaitGrid);
            
            foreach(CellRuntimeData c in _wait.GridContent) {
                Vector2 step = _wait.CellDistance + _wait.CellSize;
                Vector3 cellOffset = new Vector3((c.Index.x - (_wait.GridSize.x - 1) / 2f) * step.x,
                                                 (c.Index.y - (_wait.GridSize.y - 1) / 2f) * step.y,
                                                 0f)
                                    + GetGridWorldPos(_wait);

                GameObject tmpCell = Instantiate(cellPrefabs, cellOffset, Quaternion.identity, gridRoot);
                tmpCell.GetComponent<CellView>().BindData(c, personMoveManager);

                if (c.CurrentPerson != null)
                    CheckPersonCondition(c, c.CurrentPerson, c.OwnGrid);
            }
        }

        private Vector3 GetGridWorldPos(Grid<CellRuntimeData> grid)
        {
            Camera mainCam = Camera.main;
            if (mainCam == null) return Vector3.zero;

            float distanceToCam = Mathf.Abs(mainCam.transform.position.z);
            Vector3 worldPoint = mainCam.ViewportToWorldPoint(new Vector3(grid.PosX, grid.PosY, distanceToCam));
            worldPoint.z = 0;
            return worldPoint;
        }
        
        public List<CellRuntimeData> GetAdjacentCells(Vector2Int index, Grid<CellRuntimeData> grid)
        {
            return _conditionEvaluator.GetAdjacentCells(index, grid);
        }

        public bool IsConditionSatisfied(CellView cell, ConditionRuntimeData condition)
        {
            if (cell?.RuntimeData == null || _main == null || _conditionEvaluator == null)
                return false;

            return _conditionEvaluator.IsConditionSatisfied(
                cell.RuntimeData,
                condition,
                _main
            );
        }

        public bool TryAssignPerson(CellView targetCell, PersonRuntimeData person)
        {
            return TryMovePerson(null, targetCell, person);
        }

        public bool TryMovePerson(
            CellView sourceCell,
            CellView targetCell,
            PersonRuntimeData person)
        {
            if (_currentLevel == null || targetCell == null || person == null)
                return false;

            CellRuntimeData targetRuntimeCell = targetCell.RuntimeData;
            if (targetRuntimeCell == null || targetRuntimeCell.Type != CellType.Seat)
                return false;

            if (sourceCell == targetCell)
            {
                bool moveSucceeded = targetRuntimeCell.CurrentPerson == person;
                if (moveSucceeded)
                    _currentLevel.ModifyMove(-1);

                return moveSucceeded;
            }

            CellRuntimeData sourceRuntimeCell = sourceCell != null ? sourceCell.RuntimeData : null;

            if (sourceRuntimeCell != null && sourceRuntimeCell.CurrentPerson != person)
                return false;

            PersonRuntimeData targetPerson = targetRuntimeCell.CurrentPerson;

            if (targetPerson != null && sourceRuntimeCell == null)
                return false;

            targetRuntimeCell.SetPerson(person);
            sourceRuntimeCell?.SetPerson(targetPerson);

            // Consume one move after a successful TryMovePerson operation.
            _currentLevel.ModifyMove(-1);
            CheckAllPersonConditions();

            return true;
        }

        private void CheckAllPersonConditions()
        {
            if (_conditionEvaluator.AreAllPersonConditionsSatisfied(_main, _wait)) {
                OnWinEvent.Raise();
                return;
            }

            if (_currentLevel.IsOutOfMove)
                OnLoseEvent.Raise();
        }

        public void CheckPersonCondition(CellRuntimeData containCell, PersonRuntimeData person, GridId cellGrid)
        {
            _conditionEvaluator.CheckPersonCondition(containCell, person, cellGrid, _main);
        }

        public void Initialize(LevelRuntimeData level)
        {
            _currentLevel = level;
            _main = level?.MainGrid;
            _wait = level?.WaitGrid;
        }
    }
}
