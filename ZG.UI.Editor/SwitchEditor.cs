using UnityEditor;
using UnityEditor.UI;

namespace ZG.UI
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(Switch), true)]
    public class SwitchEditor : ToggleEditor
    {
        public override void OnInspectorGUI()
        {
            var serializedObject = base.serializedObject;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("mode"));
            serializedObject.ApplyModifiedProperties();

            base.OnInspectorGUI();
        }
    }
}