using Game.Domain.Person;
using Game.Shared;
using UnityEngine;

namespace Game.View
{
    public class PersonView : MonoBehaviour
    {
        [Header("Visuals")]
        [SerializeField] private SpriteRenderer PersonBody;
        [SerializeField] private SpriteRenderer PersonFace;

        [Header("Faces")]
        [SerializeField] private Sprite HappyFace;
        [SerializeField] private Sprite NormalFace;
        [SerializeField] private Sprite AngryFace;
        [SerializeField] private PersonRuntimeData _person;

        private void Awake()
        {
            GetComponent<SpriteRenderer>().sprite = _person.BaseSprite;
        }

        private void OnEnable()
        {
            _person.OnPersonStateChanged += UpdateState;
        }

        private void OnDisable()
        {
            _person.OnPersonStateChanged -= UpdateState;
        }

        private void UpdateState(PersonState state)
        {
            if (state == PersonState.Normal) 
                PersonFace.sprite = NormalFace;
            else if (state == PersonState.Happy) 
                PersonFace.sprite = HappyFace;
            else if (state == PersonState.Angry)
                PersonFace.sprite = AngryFace;
        }
    }
}