using System;
using System.Collections;
using UnityEngine;

namespace steph.Unity.Curve.Runtime
{

    public class CurveFollower
    {
        public Curve path;
        public Transform walker;

        public event Action OnFinishedWalking;

        public float NextNodeActivationDistance = .5f;
        public float LastNodeActivationDistance = .5f;
        int _lastPoint;
        public int LastPoint
        {
            get => Math.Clamp(_lastPoint, 0, path.points.Count-1);
            set => _lastPoint = value;
        }
        public float pathLerpTime = 10f;
        public float MoveSpeed = 4f;
        public bool changeRotation = true;

        private int _currentPathNode;
        private bool _move;
        Vector3 _lastDir;

        public CurveFollower(Curve path, Transform walker, float nextNodeActivationDistance, float lastNodeActivationDistance, bool changeRotation = true)
        {
            this.path = path;
            this.walker = walker;
            NextNodeActivationDistance = nextNodeActivationDistance;
            LastNodeActivationDistance = lastNodeActivationDistance;
            LastPoint = path != null ? path.points.Count: 0;

            this.changeRotation = changeRotation;
        }

        public CurveFollower(CurveFollowData data, Transform walker, bool changeRotation = true)
        {
            path = data.path;
            this.walker = walker;
            NextNodeActivationDistance = data.nextNodeActivationDistance;
            LastNodeActivationDistance = data.lastNodeActivationDistance;
            MoveSpeed = data.MoveSpeed;
            pathLerpTime = data.pathLerpTime;
            LastPoint = data.path != null ? data.path.points.Count - 1 : 0;
            this.changeRotation = changeRotation;
        }

        public CurveFollower(CurveFollowData data, Transform walker, Curve path, bool changeRotation = true)
        {
            this.path = path;
            this.walker = walker;
            NextNodeActivationDistance = data.nextNodeActivationDistance;
            LastNodeActivationDistance = data.lastNodeActivationDistance;
            MoveSpeed = data.MoveSpeed;
            pathLerpTime = data.pathLerpTime;

            LastPoint = data.path != null ? data.path.points.Count - 1 : 0;
            this.changeRotation = changeRotation;
        }

        public void GetCurrentNode()
        {
            if (_currentPathNode - 1 == LastPoint) return;
            _currentPathNode = Math.Clamp(_currentPathNode, 0, path.points.Count - 1);
            Vector3 currentTarget = path.GetGlobalSpace(_currentPathNode);

            float distanceToNode = Vector3.Distance(currentTarget, walker.position);
            bool nodeIsBehind = distanceToNode < NextNodeActivationDistance;
            if (!nodeIsBehind) return;
            //was last node so stop
            if (_currentPathNode == LastPoint)
            {
                if (distanceToNode < LastNodeActivationDistance)
                {
                    _move = false;
                }
            }
            else
            {
                _currentPathNode++;
            }
        }

        public void MoveBody()
        {
            if (_currentPathNode - 1 == LastPoint) return;

            GetCurrentNode();
            float deltaTime = Time.deltaTime;
            Vector3 currentTarget = path.GetGlobalSpace(_currentPathNode);
            Vector3 forward = walker.forward;
            Vector3 currentPos = walker.position;

            if (changeRotation)
            {
                forward = Vector3.Lerp(forward, (currentTarget - currentPos).normalized, pathLerpTime * deltaTime);
                Quaternion targetRotation = Quaternion.LookRotation(forward, Vector3.up);
                walker.SetPositionAndRotation(walker.position += MoveSpeed * deltaTime * forward, targetRotation);
            }
            else
            {
                if(_lastDir == Vector3.zero)
                {
                    _lastDir = (currentTarget - currentPos).normalized;
                }
                _lastDir = Vector3.Lerp(_lastDir, (currentTarget - currentPos).normalized, pathLerpTime * deltaTime);

                walker.position += MoveSpeed * deltaTime * _lastDir;
            }
        }

        public IEnumerator LerpToTransform(Transform tr, float time)
        {
            float currentTime = 0;
            float t = 0;
            walker.GetPositionAndRotation(out Vector3 startPos, out Quaternion startRot);

            while (t < 1)
            {
                currentTime += Time.deltaTime;
                t = currentTime / time;

                walker.SetPositionAndRotation
                (
                    Vector3.Slerp(startPos, tr.position, t),
                    Quaternion.Slerp(startRot, tr.rotation, t)
                );
                yield return null;
            }
            Transform prevParent = walker.parent;
            walker.parent = tr;
            walker.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            walker.parent = prevParent;
        }

        public void RemoveSubscriptions()
        {
            OnFinishedWalking = null;
        }

        public IEnumerator WalkLoop(Transform finalPosition = null, float finalPositionTime = 0)
        {
            _move = true;
            while (_move)
            {
                MoveBody();
                yield return null;
            }

            if(finalPosition != null)
            {
                yield return LerpToTransform(finalPosition, finalPositionTime);
            }
            OnFinishedWalking?.Invoke();

        }
    }

}
