using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace steph.Unity.Curve.Runtime
{
    [System.Serializable]
    public struct CurvePoint
    {
        public Vector3 position;

        public Vector3 bezierTangent;
    }

    public class Curve : MonoBehaviour
    {
        public UnityEvent<Curve> OnChange { get; private set; }
        public List<CurvePoint> points;
        public bool AddOnMouse;

        public bool ShowHandles = true;

        //Gs means global space / world space
        public Vector3 start => points[0].position;
        public Vector3 end => points[^1].position;
        public Vector3 startGS => transform.TransformPoint(points[0].position);
        public Vector3 endGS => transform.TransformPoint(points[^1].position);

        public Vector3 GetGlobalSpace(int i)
        {
            return transform.TransformPoint(points[i].position);
        }

        public Vector3 GetPositionFromDistanceGS(float distance)
        {
            return transform.TransformPoint(GetPositionFromDistance(distance));
        }

        public Vector3 GetPositionFromDistance(float distance)
        {
            if (points.Count == 0) return Vector3.zero;
            if (points.Count == 1) return points[0].position;
            Vector3 pos1;
            Vector3 pos2;
            for (int i = 0; i < points.Count - 1; i++)
            {
                pos1 = points[i].position;
                pos2 = points[i + 1].position;
                float segLength = (pos2 - pos1).magnitude;
                if (segLength > distance) return Vector3.Lerp(pos1, pos2, distance / segLength);
                distance -= segLength;
            }
            return points[^1].position;
        }

        public float Getlength()
        {
            float res = 0;
            if (points.Count > 0)
            {
                for (int i = 0; i < points.Count - 1; i++)
                {
                    res += (points[i + 1].position - points[i].position).magnitude;
                }
            }
            return res;
        }
    }
}
