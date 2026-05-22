#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public class LevelPositionExporter : EditorWindow
{
    private RectTransform content;

    [MenuItem("Tools/Level Position Exporter")]
    public static void ShowWindow()
    {
        GetWindow<LevelPositionExporter>("Level Position Exporter");
    }

    private void OnGUI()
    {
        content = (RectTransform)EditorGUILayout.ObjectField("Content", content, typeof(RectTransform), true);

        if (GUILayout.Button("Export Positions"))
            Export();
    }

    private void Export()
    {
        if (content == null) { Debug.LogError("Assign Content first"); return; }

        float halfWidth = content.rect.width * 0.5f;

        foreach (RectTransform child in content)
        {
            Vector2 normalized = new Vector2(
                (child.localPosition.x) / content.rect.width,
                child.localPosition.y / content.rect.height
            );
            Debug.Log($"{child.name}: ({normalized.x:F4}, {normalized.y:F4})");
        }
    }
}
#endif