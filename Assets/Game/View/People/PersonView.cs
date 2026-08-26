using System;
using UnityEngine;
using Game.Core.People;

namespace Game.View.People
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
        private bool _isSubscribed;

        private void Start()
        {
            if (_person != null)
                personBody.sprite = _person.BaseSprite;
        }

        private void OnEnable()
        {
            SubscribeToStateChanges();

            if (_person != null)
                UpdateState(_person.State);
        }

        private void OnDisable()
        {
            UnsubscribeFromStateChanges();
        }

        public void BindData(PersonRuntimeData person)
        {
            UnsubscribeFromStateChanges();
            _person = person;

            if (_person == null)
                return;

            personBody.sprite = _person.BaseSprite;
            SubscribeToStateChanges();
            UpdateState(_person.State);
        }

        private void SubscribeToStateChanges()
        {
            if (_person == null || _isSubscribed)
                return;

            _person.OnPersonStateChanged += UpdateState;
            _isSubscribed = true;
        }

        private void UnsubscribeFromStateChanges()
        {
            if (_person == null || !_isSubscribed)
                return;

            _person.OnPersonStateChanged -= UpdateState;
            _isSubscribed = false;
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
