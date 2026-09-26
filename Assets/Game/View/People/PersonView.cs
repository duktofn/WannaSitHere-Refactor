using System;
using UnityEngine;
using Game.Core.People;
using Game.Events;
using Game.View.Audio;
using Game.View.VFX;
using Game.View.Input;

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

        [Header("Tooltip")]
        [SerializeField] private PersonTooltip personTooltip;

        private PersonRuntimeData _person;
        private VfxPlayer _vfxPlayer;
        private AudioPlayer _audioPlayer;
        private bool _isSubscribed;

        public PersonRuntimeData RuntimeData => _person;
        public PersonTooltip Tooltip => personTooltip;

        private void Awake()
        {
            personTooltip = GetComponent<PersonTooltip>();
        }

        private void Start()
        {
            if (_person != null)
                personBody.sprite = _person.BaseSprite;
        }

        private void OnEnable()
        {
            SubscribeToStateChanges();

            if (_person != null)
                ApplyState(_person.State);
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
            ApplyState(_person.State);

            if (personTooltip != null)
                personTooltip.BindData(_person);
        }

        public void BindVfxPlayer(VfxPlayer vfxPlayer)
        {
            if (_vfxPlayer == vfxPlayer)
                return;

            _vfxPlayer = vfxPlayer;

            if (_person != null && _person.State == PersonState.Happy)
                _vfxPlayer?.PlayAtWorld(VfxId.Happy, transform);
        }

        public void BindAudioPlayer(AudioPlayer audioPlayer)
        {
            _audioPlayer = audioPlayer;
        }

        public void SetInputEnabled(bool enabled)
        {
            GetComponent<PersonDragManager>()?.SetInputEnabled(enabled);
            personTooltip?.SetInputEnabled(enabled);
        }

        private void SubscribeToStateChanges()
        {
            if (_person == null || _isSubscribed)
                return;

            _person.OnPersonStateChanged += HandleStateChanged;
            _isSubscribed = true;
        }

        private void UnsubscribeFromStateChanges()
        {
            if (_person == null || !_isSubscribed)
                return;

            _person.OnPersonStateChanged -= HandleStateChanged;
            _isSubscribed = false;
        }

        private void HandleStateChanged(PersonState state)
        {
            ApplyState(state);

            if (state == PersonState.Happy)
                _audioPlayer?.Play(AudioCueId.PersonHappy);
        }

        private void ApplyState(PersonState state)
        {
            if (state == PersonState.Normal) 
                personFace.sprite = normalFace;
            else if (state == PersonState.Happy) 
                personFace.sprite = happyFace;
            else if (state == PersonState.Angry)
                personFace.sprite = angryFace;

            if (state == PersonState.Happy)
                _vfxPlayer?.PlayAtWorld(VfxId.Happy, transform);
        }
    }
}
