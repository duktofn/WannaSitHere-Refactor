using System.Collections.Generic;
using UnityEngine;
using Game.Core.Board;
using Game.Core.Booster;
using Game.Core.Conditions;
using Game.Core.Levels;
using Game.Core.People;
using Game.View.People;
using Game.Events;
using Game.App;
using Game.View.VFX;

namespace Game.View.Board
{
    public class GridManager : MonoBehaviour
    {
        private Grid<CellRuntimeData> _main;
        private Grid<CellRuntimeData> _wait;
        private LevelRuntimeData _currentLevel;
        private LevelManager _levelManager;
        private readonly Dictionary<CellRuntimeData, CellView> _cellViewMap = new();

        [SerializeField] private PersonMover personMoveManager;
        [SerializeField] private GameObject cellPrefabs;
        [SerializeField] private List<Vector2Int> adjacent = new()
        {
            Vector2Int.right,
            Vector2Int.left,
            Vector2Int.up,
            Vector2Int.down
        };
        [SerializeField] private Transform gridRoot;

        [Header("Visual Effects")]
        [SerializeField] private VfxPlayer _vfxPlayer;

        [Header("Events")]
        [SerializeField] private VoidEventChannelSO OnWinEvent;
        [SerializeField] private VoidEventChannelSO OnLoseEvent;

        public LevelManager LevelManager => _levelManager;

        private void Awake()
        {
            if (personMoveManager == null)
                personMoveManager = GetComponent<PersonMover>();
        }

        public void Initialize(LevelRuntimeData level)
        {
            _currentLevel = level;
            _main = level?.MainGrid;
            _wait = level?.WaitGrid;
            _levelManager = new LevelManager(level, adjacent, OnWinEvent, OnLoseEvent);
        }

        public void ClearGrids()
        {
            _vfxPlayer?.StopAll();
            _cellViewMap.Clear();
            if (gridRoot == null) return;
            for (int i = gridRoot.childCount - 1; i >= 0; i--)
            {
                Destroy(gridRoot.GetChild(i).gameObject);
            }
        }

        public void CreateMainGrid()
        {
            if (_main == null)
                _main = _currentLevel?.MainGrid;

            if (_main == null) return;

            foreach (CellRuntimeData c in _main.GridContent)
            {
                Vector2 step = _main.CellDistance + _main.CellSize;
                Vector3 cellOffset = new Vector3((c.Index.x - (_main.GridSize.x - 1) / 2f) * step.x,
                                                 (c.Index.y - (_main.GridSize.y - 1) / 2f) * step.y,
                                                 0f)
                                     + GetGridWorldPos(_main);

                GameObject tmpCell = Instantiate(cellPrefabs, cellOffset, Quaternion.identity, gridRoot);
                CellView cellView = tmpCell.GetComponent<CellView>();
                cellView.BindData(c, personMoveManager);
                cellView.CurrentPersonView?.BindVfxPlayer(_vfxPlayer);
                _cellViewMap[c] = cellView;

                if (c.CurrentPerson != null)
                    CheckPersonCondition(c, c.CurrentPerson, c.OwnGrid);
            }
        }

        public void CreateWaitGrid()
        {
            if (_wait == null)
                _wait = _currentLevel?.WaitGrid;

            if (_wait == null) return;

            foreach (CellRuntimeData c in _wait.GridContent)
            {
                Vector2 step = _wait.CellDistance + _wait.CellSize;
                Vector3 cellOffset = new Vector3((c.Index.x - (_wait.GridSize.x - 1) / 2f) * step.x,
                                                 (c.Index.y - (_wait.GridSize.y - 1) / 2f) * step.y,
                                                 0f)
                                     + GetGridWorldPos(_wait);

                GameObject tmpCell = Instantiate(cellPrefabs, cellOffset, Quaternion.identity, gridRoot);
                CellView cellView = tmpCell.GetComponent<CellView>();
                cellView.BindData(c, personMoveManager);
                cellView.CurrentPersonView?.BindVfxPlayer(_vfxPlayer);
                _cellViewMap[c] = cellView;

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
            return _levelManager?.GetAdjacentCells(index, grid) ?? new List<CellRuntimeData>();
        }

        public bool IsConditionSatisfied(CellView cell, ConditionRuntimeData condition)
        {
            return _levelManager?.IsConditionSatisfied(cell?.RuntimeData, condition) ?? false;
        }

        public bool TryMovePerson(
            CellView sourceCell,
            CellView targetCell,
            PersonRuntimeData person)
        {
            return _levelManager?.TryMovePerson(sourceCell?.RuntimeData, targetCell?.RuntimeData, person) ?? false;
        }

        public void CheckPersonCondition(CellRuntimeData containCell, PersonRuntimeData person, GridId cellGrid)
        {
            _levelManager?.CheckPersonCondition(containCell, person, cellGrid);
        }

        /// <summary>
        /// Visually reverts a move by looking up CellViews from domain data
        /// and delegating the animation to <see cref="PersonMover.RevertMove"/>.
        /// </summary>
        public void RevertMoveView(MoveRecord record)
        {
            if (!_cellViewMap.TryGetValue(record.SourceCell, out CellView sourceCellView)) return;
            if (!_cellViewMap.TryGetValue(record.TargetCell, out CellView targetCellView)) return;

            personMoveManager.RevertMove(sourceCellView, targetCellView);
        }

        /// <summary>
        /// Finds the <see cref="CellView"/> associated with a given <see cref="CellRuntimeData"/>.
        /// Returns null if not found (e.g. cell from a previous level).
        /// </summary>
        public CellView FindCellView(CellRuntimeData cellData)
        {
            if (cellData == null) return null;
            _cellViewMap.TryGetValue(cellData, out CellView view);
            return view;
        }

        /// <summary>
        /// Finds the current view for a person regardless of which grid cell contains them.
        /// </summary>
        public PersonView FindPersonView(PersonRuntimeData person)
        {
            if (person == null) return null;

            foreach (KeyValuePair<CellRuntimeData, CellView> pair in _cellViewMap)
            {
                if (pair.Key?.CurrentPerson == person)
                    return pair.Value?.CurrentPersonView;
            }

            return null;
        }
    }
}
