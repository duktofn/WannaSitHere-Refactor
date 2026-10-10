#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using Game.App;
using Game.Core.Board;
using Game.Core.Levels;
using Game.Core.People;
using Game.View.Board;
using Game.View.People;
using NUnit.Framework;
using PrimeTween;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Tests.PlayMode
{
    public class PersonFeedbackTests
    {
        private readonly List<GameObject> _objects = new();

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            foreach (GameObject obj in _objects)
                if (obj != null) Object.Destroy(obj);
            _objects.Clear();
            yield return null;
        }

        [UnityTest]
        public IEnumerator HappyPlacement_DefersFeedbackThenHopsWithoutMovingCollider()
        {
            PersonView view = CreatePerson();
            Transform body = view.transform.Find("PersonBody");
            Vector3 originalScale = body.localScale;
            view.PrepareForPlacement();
            view.RuntimeData.SetState(PersonState.Happy);
            yield return null;
            AssertVector(originalScale, body.localScale);
            Vector3 target = Vector3.right * 2f;
            view.MoveToSeat(target, 0.08f, Ease.Linear);
            yield return new WaitForSeconds(0.14f);
            Assert.Less(body.localScale.y, originalScale.y, "Landing should squash the visual.");
            yield return new WaitForSeconds(0.17f);
            Assert.Greater(body.localPosition.y, 0.02f, "Happy should hop after landing.");
            AssertVector(target, view.transform.position);
            AssertVector(Vector3.zero, view.GetComponent<Collider2D>().offset);
            yield return new WaitForSeconds(0.4f);
            AssertVisualsRestored(view);

            // A second seat still gets feedback when the emotion does not change.
            view.PrepareForPlacement();
            view.MoveToSeat(Vector3.right * 3f, 0.08f, Ease.Linear);
            yield return new WaitForSeconds(0.3f);
            Assert.Greater(body.localPosition.y, 0.02f);
            yield return new WaitForSeconds(0.4f);
            AssertVisualsRestored(view);
        }

        [UnityTest]
        public IEnumerator AngryState_ShakesAndNormalStateRestoresVisuals()
        {
            PersonView view = CreatePerson();
            view.RuntimeData.SetState(PersonState.Angry);
            yield return new WaitForSeconds(0.04f);
            Transform body = view.transform.Find("PersonBody");
            Assert.Greater(Mathf.Abs(body.localPosition.x), 0.005f);
            Assert.Greater(Quaternion.Angle(Quaternion.identity, body.localRotation), 0.1f);
            AssertVector(Vector3.zero, view.transform.position);
            view.RuntimeData.SetState(PersonState.Normal);
            AssertVisualsRestored(view);
            yield return new WaitForSeconds(0.5f);
            AssertVisualsRestored(view);
        }

        [UnityTest]
        public IEnumerator AngryPlacement_DefersShakeUntilAfterLanding()
        {
            PersonView view = CreatePerson();
            view.PrepareForPlacement();
            view.RuntimeData.SetState(PersonState.Angry);
            yield return null;
            AssertVisualsRestored(view);
            Vector3 target = Vector3.right * 2f;
            view.MoveToSeat(target, 0.08f, Ease.Linear);
            yield return new WaitForSeconds(0.25f);
            Transform body = view.transform.Find("PersonBody");
            Assert.Greater(Mathf.Abs(body.localPosition.x), 0.005f);
            Assert.Greater(Quaternion.Angle(Quaternion.identity, body.localRotation), 0.1f);
            AssertVector(target, view.transform.position);
            yield return new WaitForSeconds(0.4f);
            AssertVisualsRestored(view);
        }

        [UnityTest]
        public IEnumerator CustomizedHappyFeedback_UsesConfiguredHeightAndDuration()
        {
            PersonView view = CreatePerson();
            var settings = new SerializedObject(view);
            settings.FindProperty("_landingFeedbackDuration").floatValue = 0f;
            settings.FindProperty("_happyFeedbackDuration").floatValue = 0.8f;
            settings.FindProperty("_happyHopHeight").floatValue = 0.6f;
            settings.FindProperty("_happySquash").floatValue = 0f;
            settings.FindProperty("_happyStretch").floatValue = 0f;
            settings.FindProperty("_happyTiltAngle").floatValue = 0f;
            settings.ApplyModifiedPropertiesWithoutUndo();
            view.PrepareForPlacement();
            view.RuntimeData.SetState(PersonState.Happy);
            view.MoveToSeat(Vector3.right, 0.08f, Ease.Linear);
            yield return new WaitForSeconds(0.3f);
            Transform body = view.transform.Find("PersonBody");
            Assert.Greater(body.localPosition.y, 0.3f);
            AssertVector(Vector3.one, body.localScale);
            Assert.Less(Quaternion.Angle(Quaternion.identity, body.localRotation), 0.001f);
            yield return new WaitForSeconds(0.3f);
            Assert.Greater(body.localPosition.y, 0.3f, "The configured longer hop should still be playing.");
            yield return new WaitForSeconds(0.4f);
            AssertVisualsRestored(view);
        }

        [UnityTest]
        public IEnumerator ZeroDurations_DisableMotionAndKeepVisualsValid()
        {
            PersonView view = CreatePerson();
            var settings = new SerializedObject(view);
            settings.FindProperty("_landingFeedbackDuration").floatValue = 0f;
            settings.FindProperty("_happyFeedbackDuration").floatValue = 0f;
            settings.FindProperty("_angryFeedbackDuration").floatValue = 0f;
            settings.ApplyModifiedPropertiesWithoutUndo();
            view.PrepareForPlacement();
            view.RuntimeData.SetState(PersonState.Happy);
            view.MoveToSeat(Vector3.right, 0.08f, Ease.Linear);
            yield return new WaitForSeconds(0.2f);
            AssertVector(Vector3.right, view.transform.position);
            AssertVisualsRestored(view);
            view.RuntimeData.SetState(PersonState.Angry);
            yield return null;
            AssertVisualsRestored(view);
        }

        [UnityTest]
        public IEnumerator Disable_CancelsArrivalAndResetsFeedbackOnReenable()
        {
            PersonView view = CreatePerson();
            view.PrepareForPlacement();
            view.RuntimeData.SetState(PersonState.Happy);
            view.MoveToSeat(Vector3.right, 0.3f, Ease.Linear);
            yield return new WaitForSeconds(0.04f);
            view.gameObject.SetActive(false);
            Vector3 stoppedPosition = view.transform.position;
            yield return new WaitForSeconds(0.4f);
            AssertVector(stoppedPosition, view.transform.position);
            view.gameObject.SetActive(true);
            AssertVisualsRestored(view);
            yield return new WaitForSeconds(0.1f);
            AssertVisualsRestored(view);
        }

        [UnityTest]
        public IEnumerator BeginMove_InterruptsReactionAndPendingArrival()
        {
            PersonView view = CreatePerson();
            PersonMover mover = CreateObject("Mover").AddComponent<PersonMover>();
            view.RuntimeData.SetState(PersonState.Angry);
            yield return new WaitForSeconds(0.04f);
            mover.BeginMove(view.transform, view.GetComponent<Collider2D>(), view.transform.position);
            AssertVisualsRestored(view);
            view.PrepareForPlacement();
            view.MoveToSeat(Vector3.right, 0.3f, Ease.Linear);
            yield return new WaitForSeconds(0.04f);
            mover.BeginMove(view.transform, view.GetComponent<Collider2D>(), view.transform.position);
            Vector3 stoppedPosition = view.transform.position;
            yield return new WaitForSeconds(0.4f);
            AssertVector(stoppedPosition, view.transform.position);
            AssertVisualsRestored(view);
        }

        [UnityTest]
        public IEnumerator SwapAndUndo_KeepViewsAtSeatsAndRestoreVisuals()
        {
            PersonView first = CreatePerson();
            PersonView second = CreatePerson();
            CellView source = CreateSeat(0, first);
            CellView target = CreateSeat(1, second);
            second.transform.position = target.transform.position;
            var main = new Grid<CellRuntimeData>(new Vector2Int(2, 1));
            main.Set(0, 0, source.RuntimeData);
            main.Set(1, 0, target.RuntimeData);
            var level = new LevelRuntimeData(10, main, new Grid<CellRuntimeData>(Vector2Int.one));
            GameObject owner = CreateObject("Grid");
            GridManager grid = owner.AddComponent<GridManager>();
            PersonMover mover = owner.AddComponent<PersonMover>();
            var settings = new SerializedObject(mover);
            settings.FindProperty("gridManager").objectReferenceValue = grid;
            settings.FindProperty("snapTime").floatValue = 0.08f;
            settings.ApplyModifiedPropertiesWithoutUndo();
            grid.Initialize(level, new LevelManager(level, new List<Vector2Int> { Vector2Int.left, Vector2Int.right }));
            mover.BeginMove(first.transform, first.GetComponent<Collider2D>(), first.transform.position, source);
            Assert.IsTrue(mover.MoveToCell(first.transform, first.RuntimeData, target));
            Assert.AreSame(first, target.CurrentPersonView);
            Assert.AreSame(second, source.CurrentPersonView);
            yield return new WaitForSeconds(0.7f);
            AssertVector(target.transform.position, first.transform.position);
            AssertVector(source.transform.position, second.transform.position);
            AssertVisualsRestored(first);
            AssertVisualsRestored(second);
            mover.RevertMove(source, target);
            yield return new WaitForSeconds(0.7f);
            AssertVector(source.transform.position, first.transform.position);
            AssertVector(target.transform.position, second.transform.position);
            AssertVisualsRestored(first);
            AssertVisualsRestored(second);

            // A same-seat drop settles after landing feedback.
            mover.BeginMove(first.transform, first.GetComponent<Collider2D>(), first.transform.position, source);
            source.RuntimeData.SetPerson(first.RuntimeData);
            Assert.IsTrue(mover.MoveToCell(first.transform, first.RuntimeData, source));
            yield return new WaitForSeconds(0.7f);
            AssertVisualsRestored(first);

            int movesBefore = level.CurrentMove;
            mover.BeginMove(first.transform, first.GetComponent<Collider2D>(), first.transform.position, source);
            first.transform.position += Vector3.up;
            LogAssert.Expect(LogType.Warning, "[MoveToCell] FAIL: targetCell is null — no overlapping cell found.");
            Assert.IsFalse(mover.MoveToCell(first.transform, first.RuntimeData, null));
            yield return new WaitForSeconds(0.2f);
            AssertVector(source.transform.position, first.transform.position);
            AssertVisualsRestored(first);
            Assert.AreEqual(movesBefore, level.CurrentMove);
        }

        private PersonView CreatePerson()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/InGame/PersonViewPrefab.prefab");
            Assert.IsNotNull(prefab);
            GameObject instance = Object.Instantiate(prefab);
            _objects.Add(instance);
            PersonView view = instance.GetComponent<PersonView>();
            view.BindData(new PersonRuntimeData("Test", PersonTrait.Cool, null, null));
            return view;
        }

        private CellView CreateSeat(int index, PersonView person)
        {
            GameObject obj = CreateObject("Seat" + index);
            obj.transform.position = Vector3.right * index * 2f;
            CellView view = obj.AddComponent<CellView>();
            view.BindData(new CellRuntimeData(new Vector2Int(index, 0), CellType.Seat,
                Vector2.one, person.RuntimeData, Food.Any, null, GridId.MainGrid), null);
            view.SetPersonView(person);
            return view;
        }

        private GameObject CreateObject(string name)
        {
            var obj = new GameObject(name);
            _objects.Add(obj);
            return obj;
        }

        private static void AssertVisualsRestored(PersonView view)
        {
            foreach (string name in new[] { "PersonBody", "PersonFace" })
            {
                Transform visual = view.transform.Find(name);
                AssertVector(Vector3.zero, visual.localPosition);
                AssertVector(Vector3.one, visual.localScale);
                Assert.Less(Quaternion.Angle(Quaternion.identity, visual.localRotation), 0.001f);
            }
        }

        private static void AssertVector(Vector3 expected, Vector3 actual)
        {
            Assert.Less(Vector3.Distance(expected, actual), 0.001f);
        }
    }
}
#endif
