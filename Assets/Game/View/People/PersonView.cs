using System;
using PrimeTween;
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

        [Header("Landing Feedback")]
        [SerializeField, Min(0f)] private float _landingFeedbackDuration = 0.12f;
        [SerializeField, Range(0f, 0.9f)] private float _landingStretch = 0.18f;
        [SerializeField, Range(0f, 0.9f)] private float _landingSquash = 0.16f;

        [Header("Happy Feedback")]
        [SerializeField, Min(0f)] private float _happyFeedbackDuration = 0.4f;
        [SerializeField, Min(0f)] private float _happyHopHeight = 0.18f;
        [SerializeField, Range(0f, 0.9f)] private float _happySquash = 0.08f;
        [SerializeField, Range(0f, 0.9f)] private float _happyStretch = 0.1f;
        [SerializeField, Range(0f, 30f)] private float _happyTiltAngle = 5f;

        [Header("Angry Feedback")]
        [SerializeField, Min(0f)] private float _angryFeedbackDuration = 0.4f;
        [SerializeField, Min(0f)] private float _angryShakeDistance = 0.07f;
        [SerializeField, Range(0f, 30f)] private float _angryShakeAngle = 8f;
        [SerializeField, Min(1)] private int _angryShakeCycles = 3;

        private PersonRuntimeData _person;
        private VfxPlayer _vfxPlayer;
        private AudioPlayer _audioPlayer;
        private bool _isSubscribed;
        private Tween _placementTween;
        private bool _playPlacementReaction;
        private Tween _feedbackTween;
        private bool _isPlacing;
        private Transform[] _visuals;
        private Vector3[] _visualPositions;
        private Vector3[] _visualScales;
        private Quaternion[] _visualRotations;
        private PersonState _feedbackState;
        private float _landingDuration;
        private float _reactionDuration;

        public PersonRuntimeData RuntimeData => _person;
        public PersonTooltip Tooltip => personTooltip;

        private void Awake()
        {
            personTooltip = GetComponent<PersonTooltip>();
            _visuals = new[] { personBody.transform, personFace.transform };
            _visualPositions = new Vector3[_visuals.Length];
            _visualScales = new Vector3[_visuals.Length];
            _visualRotations = new Quaternion[_visuals.Length];
            for (int i = 0; i < _visuals.Length; i++)
            {
                _visualPositions[i] = _visuals[i].localPosition;
                _visualScales[i] = _visuals[i].localScale;
                _visualRotations[i] = _visuals[i].localRotation;
            }
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
            CancelFeedback();
            UnsubscribeFromStateChanges();
        }

        public void BindData(PersonRuntimeData person)
        {
            CancelFeedback();
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

        public void PrepareForPlacement()
        {
            CancelFeedback();
            _isPlacing = true;
        }

        public void MoveToSeat(Vector3 position, float duration, Ease ease, bool playReaction = true)
        {
            _placementTween.Stop();
            _playPlacementReaction = playReaction;
            _placementTween = Tween.Position(transform, position, duration, ease)
                .OnComplete(this, view =>
                {
                    view._isPlacing = false;
                    if (view.isActiveAndEnabled && view._person != null)
                        view.PlayFeedback(true, view._playPlacementReaction);
                });
        }

        public void CancelFeedback()
        {
            _isPlacing = false;
            _placementTween.Stop();
            _feedbackTween.Stop();
            if (_visuals != null)
                ApplyVisualOffset(Vector3.zero, Vector3.one, 0f);
        }

        private void PlayFeedback(bool landing, bool playReaction = true)
        {
            _feedbackTween.Stop();
            _feedbackState = playReaction ? _person.State : PersonState.Normal;
            _landingDuration = landing ? Mathf.Max(0f, _landingFeedbackDuration) : 0f;
            _reactionDuration = _feedbackState == PersonState.Happy
                ? Mathf.Max(0f, _happyFeedbackDuration)
                : _feedbackState == PersonState.Angry ? Mathf.Max(0f, _angryFeedbackDuration) : 0f;
            float duration = _landingDuration + _reactionDuration;
            ApplyVisualOffset(Vector3.zero, Vector3.one, 0f);
            if (duration > 0f)
            {
                _feedbackTween = Tween.Custom(this, 0f, duration, duration,
                    (view, elapsed) => view.UpdateFeedback(elapsed), Ease.Linear);
            }

            if (_feedbackState == PersonState.Happy)
            {
                _vfxPlayer?.PlayAtWorld(VfxId.Happy, transform);
                _audioPlayer?.Play(AudioCueId.PersonHappy);
            }
        }

        private void UpdateFeedback(float elapsed)
        {
            Vector3 offset = Vector3.zero;
            Vector3 scale = Vector3.one;
            float angle = 0f;
            if (elapsed < _landingDuration)
            {
                float squash = Mathf.Sin(Mathf.PI * elapsed / _landingDuration);
                scale = new Vector3(1f + _landingStretch * squash, 1f - _landingSquash * squash, 1f);
            }
            else
            {
                float progress = _reactionDuration > 0f
                    ? Mathf.Clamp01((elapsed - _landingDuration) / _reactionDuration)
                    : 1f;
                float pulse = Mathf.Sin(Mathf.PI * progress);
                if (_feedbackState == PersonState.Happy)
                {
                    offset.y = _happyHopHeight * pulse;
                    scale = new Vector3(1f - _happySquash * pulse, 1f + _happyStretch * pulse, 1f);
                    angle = _happyTiltAngle * Mathf.Sin(2f * Mathf.PI * progress) * (1f - progress);
                }
                else if (_feedbackState == PersonState.Angry)
                {
                    float shake = Mathf.Sin(2f * Mathf.PI * _angryShakeCycles * progress) * (1f - progress);
                    offset.x = _angryShakeDistance * shake;
                    angle = _angryShakeAngle * shake;
                }
            }
            ApplyVisualOffset(offset, scale, angle);
        }

        private void ApplyVisualOffset(Vector3 offset, Vector3 scale, float angle)
        {
            Quaternion rotation = Quaternion.Euler(0f, 0f, angle);
            for (int i = 0; i < _visuals.Length; i++)
            {
                _visuals[i].localPosition = rotation * Vector3.Scale(_visualPositions[i], scale) + offset;
                _visuals[i].localScale = Vector3.Scale(_visualScales[i], scale);
                _visuals[i].localRotation = rotation * _visualRotations[i];
            }
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

            // Moving people react at their destination; neighbours react immediately.
            if (!_isPlacing)
                PlayFeedback(false);
        }

        private void ApplyState(PersonState state)
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
