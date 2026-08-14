using Game.Domain.Grid;
using Game.Domain.Person;
using Game.Domain.SaveAndLoad;
using Game.Shared;
using UnityEngine;
using System.Collections.Generic;
using Game.View.Person;

namespace Game.Manager
{
    public class GridManager : MonoBehaviour
    {
        private Grid<CellRuntimeData> _main;
        private Grid<CellRuntimeData> _wait;
        private LevelRuntimeData _currentLevel;
        private PersonMoveManager personMoveManager;
        [SerializeField] private GameObject cellPrefabs;
        [SerializeField] private List<Vector2> adjacent;

        private void Awake()
        {
            personMoveManager = GetComponent<PersonMoveManager>();
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

                GameObject tmpCell = Instantiate(cellPrefabs, cellOffset, Quaternion.identity);
                tmpCell.GetComponent<CellView>().BindData(c, personMoveManager);
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

                GameObject tmpCell = Instantiate(cellPrefabs, cellOffset, Quaternion.identity);
                tmpCell.GetComponent<CellView>().BindData(c, personMoveManager);
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
        
        public List<CellRuntimeData> GetAdjacentCells(Vector2 index, Grid<CellRuntimeData> grid)
        {
            List<CellRuntimeData> res = new();

            foreach(Vector2 v in adjacent) {
                foreach(CellRuntimeData c in grid.GridContent)
                {
                    if (c.Index == index + v) res.Add(c);
                }
            }

            return res;
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
            if (targetCell == null || person == null)
                return false;

            CellRuntimeData targetRuntimeCell = targetCell.RuntimeData;
            if (targetRuntimeCell == null || targetRuntimeCell.Type != CellType.Seat)
                return false;

            if (sourceCell == targetCell)
                return targetRuntimeCell.CurrentPerson == person;

            CellRuntimeData sourceRuntimeCell = sourceCell != null
                ? sourceCell.RuntimeData
                : null;

            if (sourceRuntimeCell != null && sourceRuntimeCell.CurrentPerson != person)
                return false;

            PersonRuntimeData targetPerson = targetRuntimeCell.CurrentPerson;

            if (targetPerson != null && sourceRuntimeCell == null)
                return false;

            targetRuntimeCell.SetPerson(person);
            sourceRuntimeCell?.SetPerson(targetPerson);
            return true;
        }

        public void Initialize(LevelRuntimeData level)
        {
            _currentLevel = level;
            _main = level?.MainGrid;
            _wait = level?.WaitGrid;
        }
    }
}
