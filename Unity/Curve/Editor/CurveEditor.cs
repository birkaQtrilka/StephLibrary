
namespace steph.Unity.Curve.Editor
{
    using steph.Unity.Curve.Runtime;
    using UnityEditor;
    using UnityEngine;
    using static Codice.Client.Commands.WkTree.WorkspaceTreeNode;

    [CustomEditor(typeof(Curve))]
    public class CurveEditor : Editor
    {
        private Curve curve;
        int _lastModifiedPointIndex = -1;

        private void OnEnable()
        {
            curve = (Curve)target;
        }


        // This method is called by Unity whenever it renders the scene view.
        // We use it to draw gizmos, and deal with changes (dragging objects)
        void OnSceneGUI()
        {
            if (curve.points == null)
                return;

            bool dirty = false;

            // Add new points if needed:
            Event e = Event.current;
            if (e.type == EventType.KeyDown)
            {
                switch (e.keyCode)
                {

                    case KeyCode.Space:
                        dirty |= AddPoint();
                        break;
                    case KeyCode.Backspace:
                        dirty |= RemovePoint();
                        break;
                }

            }
            dirty |= ShowAndMovePoints();

            if (dirty)
            {
                curve.OnChange?.Invoke(curve);
            }
        }

        // Tries to add a point to the curve, where the mouse is in the scene view.
        // Returns true if a change was made.
        bool AddPoint()
        {
            Transform handleTransform = curve.transform;

            Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);

            if (curve.AddOnMouse && Physics.Raycast(ray, out RaycastHit hit))
            {
                Undo.RecordObject(curve, "Add Spline Point");

                CurvePoint newPoint = new CurvePoint()
                {
                    position = handleTransform.InverseTransformPoint(hit.point),
                    bezierTangent = Vector3.zero
                };

                curve.points.Add(newPoint);
                EditorUtility.SetDirty(curve);
                return true;
            }

            Undo.RecordObject(curve, "Add Spline Point");

            InsertPointOnLastModified();

            EditorUtility.SetDirty(curve);

            return true;
        }

        void InsertPointOnLastModified()
        {
            Vector3 direction;
            _lastModifiedPointIndex = Mathf.Clamp(_lastModifiedPointIndex, 0, curve.points.Count - 1);
            if (curve.points.Count < 2)
            {
                direction = (curve.points.Count + 1) * .5f * curve.transform.forward;
                _lastModifiedPointIndex++;
                curve.points.Add(new CurvePoint()
                {
                    position = direction,
                    bezierTangent = direction
                });
                return;
            }
            else if (_lastModifiedPointIndex == curve.points.Count - 1)
            {
                direction = curve.GetPoint(_lastModifiedPointIndex - 1, _lastModifiedPointIndex, 0.5f);
            }
            else
            {
                direction = curve.GetPoint(_lastModifiedPointIndex, _lastModifiedPointIndex + 1, 0.5f);
            }

            _lastModifiedPointIndex++;
            curve.points.Insert(_lastModifiedPointIndex, new CurvePoint()
            {
                position = direction,
                bezierTangent = direction
            });
        }

        bool RemovePoint()
        {
            bool dirty = false;

            if (curve.points == null || curve.points.Count == 0) return false;

            if (_lastModifiedPointIndex != -1 && _lastModifiedPointIndex < curve.points.Count)
            {
                Undo.RecordObject(curve, "Removed Spline Point");
                curve.points.RemoveAt(_lastModifiedPointIndex);
                if (_lastModifiedPointIndex == curve.points.Count && _lastModifiedPointIndex > -1) _lastModifiedPointIndex--;

                EditorUtility.SetDirty(curve);
                dirty = true;
            }


            return dirty;
        }

        // Show points in scene view, and check if they're changed:
        bool ShowAndMovePoints()
        {
            bool dirty = false;
            Transform handleTransform = curve.transform;
            SceneView sceneView = SceneView.currentDrawingSceneView;
            Vector3 previousPoint = Vector3.zero;

            if (sceneView == null) return dirty;

            for (int i = 0; i < curve.points.Count; i++)
            {
                Vector3 currentPoint = handleTransform.TransformPoint(curve.points[i].position);
                
                if (i > 0)
                {
                    Vector3 currentTangent = handleTransform.TransformPoint(curve.points[i].bezierTangent);
                    if(curve.ShowHandles)
                        dirty |= DoBezierHandles(handleTransform, currentPoint, previousPoint, i, ref currentTangent);

                    Handles.color = Color.white;
                    Vector3 p1 = Vector3.Lerp(previousPoint, currentTangent, 0.6666667f);
                    Vector3 p2 = Vector3.Lerp(currentPoint, currentTangent, 0.6666667f);
                    Handles.DrawBezier(previousPoint, currentPoint, p1, p2, Color.wheat, null, 1);
                    
                    Camera cam = sceneView.camera;
                    float distance = Vector3.Distance(cam.transform.position, currentPoint);
                    float screenSizeFactor = 0.06f * CurveDebugWindow.Settings.ArrowSize;
                    float capSize = distance * screenSizeFactor;

                    Handles.color = CurveDebugWindow.Settings.ArrowColor;
                    Vector3 dir = (currentPoint - handleTransform.TransformPoint(curve.GetPoint(i - 1, i, 0.9f))).normalized;
                    if(dir != Vector3.zero )
                        Handles.ConeHandleCap(i, currentPoint - capSize * 0.5f * dir, Quaternion.LookRotation(dir), capSize, EventType.Repaint);
                }

                DrawPoint(currentPoint);

                if (!curve.ShowHandles)
                {
                    previousPoint = currentPoint;
                    continue;
                }
                

                DoPointHandles(handleTransform, i, ref currentPoint);
                previousPoint = currentPoint;

            }
            return dirty;
        }

        void DrawPoint(Vector3 p)
        {
            Color color = Handles.color;
            Handles.color = CurveDebugWindow.Settings.PointColor;
            Handles.SphereHandleCap(
                0,
                p,
                Quaternion.identity,
                CurveDebugWindow.Settings.PointSize,
                EventType.Repaint
            );
            Handles.color = color;
        }

        bool DoPointHandles(Transform handleTransform, int i, ref Vector3 currentPoint)
        {
            EditorGUI.BeginChangeCheck();
            currentPoint = CustomHandle(currentPoint, .5f);
            if (!EditorGUI.EndChangeCheck()) return false;
            Undo.RecordObject(curve, "moved");
            CurvePoint newPoint = new()
            {
                position = handleTransform.InverseTransformPoint(currentPoint),
                bezierTangent = curve.points[i].bezierTangent
            };
            curve.points[i] = newPoint;
            EditorUtility.SetDirty(curve);
            _lastModifiedPointIndex = i;
            return true;
        }

        bool DoBezierHandles(Transform handleTransform, Vector3 currentPoint, Vector3 previousPoint, int i, ref Vector3 currentTangent)
        {
            Handles.color = Color.yellow;

            Handles.SphereHandleCap(
                0,
                currentTangent,
                Quaternion.identity,
                CurveDebugWindow.Settings.PointSize * .5f,
                EventType.Repaint
            );
            EditorGUI.BeginChangeCheck();

            currentTangent = CustomHandle(currentTangent, .5f);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(curve, "moved tangent");
                CurvePoint newPoint = new()
                {
                    position = handleTransform.InverseTransformPoint(currentPoint),
                    bezierTangent = handleTransform.InverseTransformPoint(currentTangent)
                };
                curve.points[i] = newPoint;
                EditorUtility.SetDirty(curve);
                return true;
            }
            Handles.DrawDottedLine(previousPoint, currentTangent, 3f);
            Handles.DrawDottedLine(currentPoint, currentTangent, 3f);
            return false;
        }

        Vector3 CustomHandle(Vector3 position, float scale, float planeSize = .25f, float planeOffset = .25f)
        {
            float size = HandleUtility.GetHandleSize(position) * scale;
            planeOffset *= size;
            planeSize *= size;

            Color originalColor = Handles.color;

            // YZ Plane (Normal is X) - Red Square
            Handles.color = new Color(Handles.xAxisColor.r, Handles.xAxisColor.g, Handles.xAxisColor.b, 0.4f); // Transparent
            Vector3 offsetYZ = (Vector3.up + Vector3.forward) * planeOffset;
            Vector3 newYZ = Handles.Slider2D(position + offsetYZ, Vector3.right, Vector3.up, Vector3.forward, planeSize, Handles.RectangleHandleCap, Vector2.zero);
            position = newYZ - offsetYZ;

            // XZ Plane (Normal is Y) - Green Square
            Handles.color = new Color(Handles.yAxisColor.r, Handles.yAxisColor.g, Handles.yAxisColor.b, 0.4f);
            Vector3 offsetXZ = (Vector3.right + Vector3.forward) * planeOffset;
            Vector3 newXZ = Handles.Slider2D(position + offsetXZ, Vector3.up, Vector3.right, Vector3.forward, planeSize, Handles.RectangleHandleCap, Vector2.zero);
            position = newXZ - offsetXZ;

            // XY Plane (Normal is Z) - Blue Square
            Handles.color = new Color(Handles.zAxisColor.r, Handles.zAxisColor.g, Handles.zAxisColor.b, 0.4f);
            Vector3 offsetXY = (Vector3.right + Vector3.up) * planeOffset;
            Vector3 newXY = Handles.Slider2D(position + offsetXY, Vector3.forward, Vector3.right, Vector3.up, planeSize, Handles.RectangleHandleCap, Vector2.zero);
            position = newXY - offsetXY;


            Handles.color = Handles.xAxisColor;
            position = Handles.Slider(position, Vector3.right, size, Handles.ArrowHandleCap, 0f);

            Handles.color = Handles.yAxisColor;
            position = Handles.Slider(position, Vector3.up, size, Handles.ArrowHandleCap, 0f);

            Handles.color = Handles.zAxisColor;
            position = Handles.Slider(position, Vector3.forward, size, Handles.ArrowHandleCap, 0f);

            Handles.color = originalColor;
            return position;
        }
    }
}
