using DefaultNamespace.UI;
using UnityEditor;
using UnityEngine;

namespace DefaultNamespace.Editor
{
    [CustomEditor(typeof(ArcLayoutGroup))]
    [CanEditMultipleObjects]
    public class ArcLayoutGroupEditor : UnityEditor.Editor
    {
        private const float GuideSegmentAngle = 5f;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawPropertiesExcluding(serializedObject, "m_Padding", "m_ChildAlignment");
            serializedObject.ApplyModifiedProperties();
        }

        private void OnSceneGUI()
        {
            foreach (Object selectedTarget in targets) DrawArcGuidance((ArcLayoutGroup)selectedTarget);
        }

        private static void DrawArcGuidance(ArcLayoutGroup layout)
        {
            SerializedObject layoutObject = new SerializedObject(layout);
            RectTransform arcBounds = (RectTransform)layoutObject.FindProperty("arcBounds").objectReferenceValue;
            float arcCenterAngle = layoutObject.FindProperty("arcCenterAngle").floatValue;
            float preferredAngleSpacing = layoutObject.FindProperty("preferredAngleSpacing").floatValue;
            float arcSpan = layoutObject.FindProperty("arcSpan").floatValue;
            float direction = Mathf.Sign(preferredAngleSpacing);
            float startAngle = arcCenterAngle - direction * arcSpan * 0.5f;
            float endAngle = arcCenterAngle + direction * arcSpan * 0.5f;
            Vector3 center = arcBounds.TransformPoint(arcBounds.rect.center);
            Vector3 start = GetWorldPoint(arcBounds, startAngle);
            Vector3 arcCenter = GetWorldPoint(arcBounds, arcCenterAngle);
            Vector3 end = GetWorldPoint(arcBounds, endAngle);
            Color previousColor = Handles.color;

            Handles.color = new Color(1f, 1f, 1f, 0.2f);
            Handles.DrawAAPolyLine(2f, CreateArcPoints(arcBounds, 0f, 360f));
            Handles.color = new Color(0.2f, 0.9f, 1f, 1f);
            Handles.DrawAAPolyLine(4f, CreateArcPoints(arcBounds, startAngle, endAngle));
            Handles.DrawLine(center, start);
            Handles.DrawLine(center, arcCenter);
            Handles.DrawLine(center, end);
            DrawMarker(start, "Start");
            DrawMarker(arcCenter, "Arc Center");
            DrawMarker(end, "End");
            Handles.color = previousColor;
        }

        private static Vector3[] CreateArcPoints(RectTransform arcBounds, float startAngle, float endAngle)
        {
            int segmentCount = Mathf.Max(2, Mathf.CeilToInt(Mathf.Abs(endAngle - startAngle) / GuideSegmentAngle));
            Vector3[] points = new Vector3[segmentCount + 1];
            for (int i = 0; i <= segmentCount; i++) points[i] = GetWorldPoint(arcBounds, Mathf.Lerp(startAngle, endAngle, i / (float)segmentCount));
            return points;
        }

        private static Vector3 GetWorldPoint(RectTransform arcBounds, float angle)
        {
            float radians = angle * Mathf.Deg2Rad;
            Rect bounds = arcBounds.rect;
            Vector2 localPoint = bounds.center + new Vector2(Mathf.Cos(radians) * bounds.width * 0.5f, Mathf.Sin(radians) * bounds.height * 0.5f);
            return arcBounds.TransformPoint(localPoint);
        }

        private static void DrawMarker(Vector3 position, string label)
        {
            float size = HandleUtility.GetHandleSize(position) * 0.04f;
            Handles.DotHandleCap(0, position, Quaternion.identity, size, EventType.Repaint);
            Handles.Label(position, label);
        }
    }
}
