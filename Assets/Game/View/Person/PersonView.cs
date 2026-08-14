using System;
using Game.Domain.Person;
using Game.Shared;
using UnityEngine;

namespace Game.View.Person
{
    public class PersonView : MonoBehaviour
    {
        [Header("Visuals")]
        [SerializeField] private SpriteRenderer personBody;
        [SerializeField] private SpriteRenderer personFace;

        [Header("Faces")]
        [SerializeField] private Sprite happyFace;
        [SerializeField] private Sprite normalFace;
        [SerializeField] private Sprite angryFace;

        private PersonRuntimeData _person;

        private void Start()
        {
            personBody.sprite = _person.BaseSprite;
        }

        private void OnEnable()
        {
            if (_person != null) 
                _person.OnPersonStateChanged += UpdateState;
        }

        private void OnDisable()
        {
            if (_person != null) 
                _person.OnPersonStateChanged -= UpdateState;
        }

        public void BindData(PersonRuntimeData person)
        {
            if (person == null) return;
            _person = person;
        }

        private void UpdateState(PersonState state)
        {
            if (state == PersonState.Normal) 
                personFace.sprite = normalFace;
            else if (state == PersonState.Happy) 
                personFace.sprite = happyFace;
            else if (state == PersonState.Angry)
                personFace.sprite = angryFace;
        }
    }
}
