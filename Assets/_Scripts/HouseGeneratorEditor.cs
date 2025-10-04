#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(HouseGenerator))]
public class HouseGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        HouseGenerator gen = (HouseGenerator)target;

        GUILayout.Space(8);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Generate"))
        {
            Undo.RecordObject(gen, "Generate Houses");
            gen.Generate();
            EditorUtility.SetDirty(gen);
        }
        if (GUILayout.Button("Clear"))
        {
            Undo.RecordObject(gen, "Clear Houses");
            gen.Clear();
            EditorUtility.SetDirty(gen);
        }
        GUILayout.EndHorizontal();
    }
}
#endif
