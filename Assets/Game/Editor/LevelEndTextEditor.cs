using UnityEditor;
using UnityEngine;
using Game.View.UI;

namespace Game.Editor
{
    [CustomEditor(typeof(LevelEndText))]
    public class LevelEndTextEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space(10);
            if (GUILayout.Button("Play Revealing Effect", GUILayout.Height(30)))
            {
                var levelEndText = (LevelEndText)target;
                levelEndText.PlayRevealingEffect();
            }
        }
    }
}
