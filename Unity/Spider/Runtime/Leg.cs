using System;
using System.Linq;
using UnityEngine;

public class Leg
{
    protected class GizmosInfo
    {
        public Vector3 current;
        public Vector3 last;
        public Vector3 live;
    }


    protected readonly Member[] members;
    protected SpiderController data;

    protected GizmosInfo _debug;
    Vector3 _nextTarget;
    Vector3 _lastTarget;

    float _currLerpTime;
    readonly float _angleX, _angleY;

    Leg[] _adjacentLegs;
    public bool IsGrounded => _currLerpTime > data.StepSpeed;
    public Vector3 CurrentGroundPosition { get; private set; }
    public Vector3 CurrentGroundNormal { get; private set; }

    public event Action<Leg> OnStep;

    public float StepDistance;
    public float ForwardReach;
    LayerMask _groundMask;

    public Leg(float angleX, float angleY, BoxCollider legPrefab, int jointCount, SpiderController data, float forwardReach, LayerMask groundMask)
    {
        this.data = data;
        _angleX = angleX;
        _angleY = angleY;
        ForwardReach = forwardReach;
        StepDistance = data.StepDistance;
        _groundMask = groundMask;
        members = new Member[jointCount];
        for (int i = 0; i < jointCount; i++)
            members[i] = new Member(GameObject.Instantiate(legPrefab, data.transform));
        
        //initial positioning
        if (!GetGroundTarget(out Vector3 groundPoint, out Vector3 groundNormal))
        {
            Vector3 direction = GetYAngle() + data.ForwardReach * GetScale() * data.transform.forward + GetLegReach() * -data.transform.up / 2f;
            groundPoint = data.transform.position + direction;
        }
        CurrentGroundPosition = groundPoint;
        CurrentGroundNormal = groundNormal;

        _lastTarget = groundPoint;
        _nextTarget = groundPoint;
        SetLegStartPosition();
        //
        InverseKinematics(_lastTarget);
    }

    float GetScale()
    {
        return data.transform.lossyScale.x;
    }

    public void ClearEvents()
    {
        OnStep = null;
    }

    public virtual void Update()
    {
        if (!GetGroundTarget(out Vector3 currentTarget, out Vector3 groundNormal))
        {
            _debug = null;
            return;
        }
        CurrentGroundPosition = currentTarget;
        CurrentGroundNormal = groundNormal;
        if(currentTarget == Vector3.zero)
        {
            currentTarget = data.DistanceFromBody * GetScale() * GetYAngle() - data.GroundOffset * GetScale() * data.transform.up;
        }
        Vector3 target = InterpolateToTarget(currentTarget);

        _debug = new() { last = _lastTarget, current = _nextTarget, live = currentTarget };

        SetLegStartPosition();
        
        Member foot = members[0];
        int tries = 0;
        while (Vector3.Distance(foot.GetEndPosition(), target) > data.AcceptableDistance * GetScale() && tries < data.CalibrationAttempts)
        {
            InverseKinematics(target);
            tries++;
        }

    }

    Vector3 InterpolateToTarget(Vector3 currentTarget)
    {
        //current target is where the static position of the legs are
        _currLerpTime += Time.deltaTime;

        bool interpolationEnded = _currLerpTime >= data.StepSpeed;
        float triggerDistance = (!data.IsMoving && interpolationEnded) ? data.RestStepDistance * GetScale() : StepDistance * GetScale();
        float distanceBetweenCurrentAndNextTarget = Vector3.Distance(_nextTarget, currentTarget);

        if (distanceBetweenCurrentAndNextTarget > triggerDistance && AdjacentLegsAreGrounded())
        {
            //setting the next target
            _lastTarget = _nextTarget;
            _currLerpTime = 0;
            _nextTarget = currentTarget;
            
            OnStep?.Invoke(this);
        }
        else if (distanceBetweenCurrentAndNextTarget > triggerDistance + .5f * GetScale())
        {
            _lastTarget = _nextTarget;
            _currLerpTime = 0;
            _nextTarget = currentTarget;
            OnStep?.Invoke(this);

        }
        //else if (!data.IsMoving)
        //{
        //    //returns to the original place
        //    _lastTarget = Vector3.Slerp(_lastTarget, _nextTarget, _currLerpTime / data.StepSpeed);
        //    _currLerpTime = 0;
        //    _nextTarget = currentTarget;
        //}

        return Vector3.Slerp(_lastTarget, _nextTarget, _currLerpTime / data.StepSpeed);
    }

    bool GetGroundTarget(out Vector3 target, out Vector3 normal)
    {
        bool hasHit = GetGroundTarget(out RaycastHit hit);
        target = hit.point;
        normal = hit.normal;
        return hasHit;
    }

    bool GetGroundTarget(out RaycastHit hit)
    {
        Vector3 direction = data.DistanceFromBody * GetScale() * GetYAngle() + data.transform.forward * (data.IsMoving ? ForwardReach * GetScale() : 0);
        //in case the ground is higher than the body position, so the ray doesn't ignore the mesh 
        Vector3 abovePoint = data.AbovePointHeight * GetScale() * data.transform.up;

        bool hasHit = Physics.SphereCast
        (
            data.transform.position + abovePoint + direction,
            data.TouchRaySize * GetScale(),
            -data.transform.up,
            out hit,
            GetLegReach() + abovePoint.magnitude,
            _groundMask
        );

        //if(!hasHit)
        //{
        //    hasHit = Physics.SphereCast
        //    (
        //        data.transform.position + abovePoint + direction,
        //        .5f,
        //        -data.transform.up,
        //        out hit,
        //        GetLegReach() + abovePoint.magnitude,
        //        _groundMask
        //    );
        //}

        return hasHit;
    }


    bool AdjacentLegsAreGrounded()
    {
        return _adjacentLegs.All(l => l.IsGrounded);
    }

    public void SetAdjacentLegs(Leg[] adjacentLegs)
    {
        _adjacentLegs = adjacentLegs;
    }
    
    public void OnDrawGizmos()
    {
        if (_debug == null) return;

        Gizmos.color = Color.white;
        Vector3 direction = data.DistanceFromBody * GetScale() * GetYAngle() + data.transform.forward * (data.IsMoving ? ForwardReach * GetScale() : 0);
        //in case the ground is higher than the body position, so the ray doesn't ignore the mesh 
        Vector3 abovePoint = data.AbovePointHeight * GetScale() * data.transform.up;
        Vector3 origin = data.transform.position + abovePoint + direction;
        Vector3 dir = -data.transform.up * (GetLegReach() + abovePoint.magnitude);
        Gizmos.DrawLine(origin, origin + dir);

        Gizmos.color = Color.yellow;

        Gizmos.DrawSphere(_debug.current, .1f * GetScale());
        Gizmos.color = Color.red;

        Gizmos.DrawSphere(_debug.last, .1f * GetScale());
        Gizmos.color = Color.blue;

       Gizmos.DrawLine(_debug.live, _debug.last);
    }

    float GetLegReach()
    {
        Member member = members[0];
        return member.GetSize() * 2 * members.Length;
    }

    //initial position of legs (pointing up to have an arch)
    void SetLegStartPosition()
    {
        Vector3 direction = GetYAngle();
        direction = GetXAngle(direction);

        Vector3 target = data.transform.position;

        for (int i = members.Length - 1; i >= 0; i--)
        {
            Member current = members[i];

            float l = current.GetSize();

            current.Box.transform.up = direction;
            current.Box.transform.position = target + direction * l;

            target = current.GetEndPosition();
        }
    }
    
    Vector3 GetYAngle()
    {
        return Vector3.RotateTowards(data.transform.right, data.transform.forward, data.AngleY + _angleY, 1);
    }

    Vector3 GetXAngle(Vector3 Yrotated)
    {
        return Vector3.RotateTowards(Yrotated, data.transform.up, data.AngleX + _angleX, 1);
    }

    void InverseKinematics(Vector3 target)
    {
        //arrange legs above target if possible
        Member foot = members[0];
        foot.SetBoxTransform(target);

        for (int i = 1; i < members.Length; i++)
        {
            Member current = members[i];
            Member previous = members[i - 1];
            current.SetBoxTransform(previous.GetStartPosition());
        }

        Member legBase = members[^1];
        legBase.SetStartPosition(data.transform.position);
        for (int i = members.Length - 2; i >= 0; i--)
        {
            Member current = members[i];
            Member next = members[i + 1];
            current.SetStartPosition(next.GetEndPosition());
        }
    }
    
}
