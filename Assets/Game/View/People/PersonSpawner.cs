using UnityEngine;
using Game.Core.People;
using Game.View.Board;
using Game.View.Input;

namespace Game.View.People
{
    public class PersonSpawner : MonoBehaviour
    {
        [SerializeField] private GameObject personViewPrefabs;

        public GameObject PersonViewPrefabs
        {
            get => personViewPrefabs;
            set => personViewPrefabs = value;
        }

        public PersonView SpawnPerson(
            PersonRuntimeData personData,
            CellView targetCell,
            PersonMover personMoveManager)
        {
            if (personData == null || targetCell == null)
                return null;

            if (personViewPrefabs == null)
            {
                Debug.LogError($"[PersonSpawner] personViewPrefabs chưa được gán trên {gameObject.name}! Vui lòng kéo Prefab vào Inspector.", this);
                return null;
            }

            GameObject tmp = Instantiate(personViewPrefabs, targetCell.transform.position, Quaternion.identity, targetCell.transform.root);
            PersonView personView = tmp.GetComponent<PersonView>();
            personView.BindData(personData);

            PersonDragManager dragManager = tmp.GetComponent<PersonDragManager>();
            if (dragManager != null)
            {
                dragManager.Initialize(personMoveManager, personData, targetCell);
            }

            targetCell.SetPersonView(personView);
            return personView;
        }
    }
}

