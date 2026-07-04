// Version 2023
//  (Update: student labture version, with TODO's)

using UnityEngine;
using UnityEditor;
using static Codice.Client.Commands.WkTree.WorkspaceTreeNode;

[CustomEditor(typeof(Curve))]
public class CurveEditor : Editor
{
	private Curve curve;

    private void OnEnable()
    {
		curve = (Curve)target;
    }


    // This method is called by Unity whenever it renders the scene view.
    // We use it to draw gizmos, and deal with changes (dragging objects)
    void OnSceneGUI() {
		if (curve.points==null)
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

		if (dirty) {
			curve.OnChange?.Invoke(curve);
		}
 	}

	// Tries to add a point to the curve, where the mouse is in the scene view.
	// Returns true if a change was made.
	bool AddPoint() {
		Transform handleTransform = curve.transform;

		Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);

        if(curve.AddOnMouse && Physics.Raycast(ray, out RaycastHit hit))
        {
            Undo.RecordObject(curve, "Add Spline Point");
            curve.points.Add(handleTransform.InverseTransformPoint(hit.point));
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

        if (curve.points.Count < 2)
        {
            direction = (curve.points.Count + 1) * .5f * curve.transform.forward;
        }
        else if (_lastModifiedPointIndex == curve.points.Count - 1)
        {
            Vector3 neighbour = curve.points[_lastModifiedPointIndex - 1];
            Vector3 lastModified = curve.points[_lastModifiedPointIndex];
            direction = lastModified - neighbour;
            //direction = direction.normalized;
            direction += lastModified;
        }
        else 
        {
            Vector3 neighbour = curve.points[_lastModifiedPointIndex + 1];
            direction = Vector3.Lerp(neighbour, curve.points[_lastModifiedPointIndex], .4f);

        }

        _lastModifiedPointIndex++;
        curve.points.Insert(_lastModifiedPointIndex, direction);
    }

    bool RemovePoint()
    {
        bool dirty = false;

        if(curve.points == null || curve.points.Count == 0) return false;

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

	int _lastModifiedPointIndex = -1;
	// Show points in scene view, and check if they're changed:
	bool ShowAndMovePoints() {
		bool dirty = false;
		Transform handleTransform = curve.transform;

		Vector3 previousPoint = Vector3.zero;
		for (int i = 0; i < curve.points.Count; i++) {
			Vector3 currentPoint = handleTransform.TransformPoint(curve.points[i]);

            if (i > 0)
            {
                SceneView sceneView = SceneView.currentDrawingSceneView;
                if (sceneView != null)
                {
                    Camera cam = sceneView.camera;
                    float distance = Vector3.Distance(cam.transform.position, currentPoint);

                    // This multiplier controls how big the cone appears on screen
                    float screenSizeFactor = 0.06f * curve.PointSize; // Adjust this as needed
                    float capSize = distance * screenSizeFactor;

                    Handles.color = Color.white;
                    Handles.DrawLine(previousPoint, currentPoint);

                    Handles.color = Color.blue;
                    Vector3 dir = (currentPoint - previousPoint).normalized;
                    Handles.ConeHandleCap(i, currentPoint - capSize * 0.5f * dir, Quaternion.LookRotation(dir), capSize, EventType.Repaint);
                }
            }

            previousPoint =currentPoint;

			EditorGUI.BeginChangeCheck();
			currentPoint = Handles.DoPositionHandle(currentPoint, Quaternion.identity);
			if (EditorGUI.EndChangeCheck()) {
				Undo.RecordObject(curve, "moved");
				curve.points[i] = handleTransform.InverseTransformPoint(currentPoint);
                EditorUtility.SetDirty(curve);
                _lastModifiedPointIndex = i;
				dirty = true;
			}

		}
		return dirty;
	}
}
