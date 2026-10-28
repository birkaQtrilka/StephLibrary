
namespace steph.Unity.Curve.Editor
{
    using steph.Unity.Curve.Runtime;
    using UnityEditor;
    using UnityEngine;

    public static class CurveGizmosDrawer
    {
        [DrawGizmo(GizmoType.Selected | GizmoType.NonSelected)]
        static void DrawCurve(Curve curve, GizmoType gizmoType)
        {
            if (curve.points == null || curve.points.Count < 2)
                return;
            Camera cam = SceneView.currentDrawingSceneView.camera;

            Transform t = curve.transform;
            DrawPoint(t.TransformPoint(curve.points[0].position));

            for (int i = 1; i < curve.points.Count; i++)
            {
                Vector3 previous = t.TransformPoint(curve.points[i - 1].position);
                Vector3 current = t.TransformPoint(curve.points[i].position);
                Vector3 tangent = t.TransformPoint(curve.points[i].bezierTangent);

                Vector3 p1 = Vector3.Lerp(previous, tangent, 0.66667f);
                Vector3 p2 = Vector3.Lerp(current, tangent, 0.66667f);

                Handles.color = CurveDebugWindow.Settings.BezierColor;
                Handles.DrawBezier(
                    previous,
                    current,
                    p1,
                    p2,
                    Color.white,
                    null,
                    2f);

                DrawPoint(current);

                float distance = Vector3.Distance(cam.transform.position, current);
                float screenSizeFactor = 0.06f * CurveDebugWindow.Settings.ArrowSize;
                float capSize = distance * screenSizeFactor;

                Handles.color = CurveDebugWindow.Settings.ArrowColor;
                Vector3 dir = (current - t.TransformPoint(curve.GetPoint(i - 1, i, 0.9f))).normalized;
                if (dir != Vector3.zero)
                    Handles.ConeHandleCap(i, current - capSize * 0.5f * dir, Quaternion.LookRotation(dir), capSize, EventType.Repaint);
                Handles.DrawDottedLine(previous, tangent, 3f);
                Handles.DrawDottedLine(current, tangent, 3f);
            }
        }

        static void DrawPoint(Vector3 p)
        {
            Color color = Handles.color;
            Gizmos.color = CurveDebugWindow.Settings.PointColor;
            Gizmos.DrawSphere(p, CurveDebugWindow.Settings.PointSize);
            Handles.color = color;
        }
    }
}
