using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[SelectionBase]
public class SpiderController : MonoBehaviour
{
    public bool IsMoving { get; private set; }
    [SerializeField] bool _move;
    [field: SerializeField] public bool Gizmos { get; private set; } = false;
    [field: Space]

    [field: Header("Pathing")]
    [SerializeField] Curve _path;
    [Tooltip("How close do you have to be to the following node to then move on to the next one")]
    [SerializeField] float _nextNodeActivationDistance = .5f;
    [Tooltip("colliders taht legs detect and can walk on")]
    [SerializeField] LayerMask _groundMask;
    [Tooltip("How fast does the spider change axis forward direction when following a new node")]
    [SerializeField] float _pathLerpTime = 10;
    [Tooltip("How fast does the spider change axis up direction when following a new node")]
    [SerializeField] float _upLerpTime = 10;
    [field: Space]
    
    [field: Header("Leg positioning")]
    [Tooltip("How far up do legs initially point")]
    [field: SerializeField] public float AngleX { get; private set; } = .31f;
    [field: SerializeField] public float AngleY { get; private set; } = .2f;
    [field: Tooltip("The radius of the circle in which the legs are touching the ground")]
    [field: SerializeField] public float DistanceFromBody { get; private set; } = .5f;
    [field: Tooltip("How far forward do the leg extend when walking")]
    [field: SerializeField] public float ForwardReach { get; private set; } = .8f;
    [field: Space]

    [field: Header("Movement")]
    [field: Tooltip("Interpolation speed between the current step and next step")]
    [field: SerializeField] public float StepSpeed { get; private set; } = .5f;
    [field: Tooltip("Distance betweeen last step and current step default position as a threshhold for the next step")]
    [field: SerializeField] public float StepDistance { get; private set; } = 1f;
    [field: Tooltip("Distance betweeen last step and current step default position as a threshhold for the next step")]
    [field: SerializeField] public float RestStepDistance { get; private set; } = .1f;
    [field: SerializeField] public float GroundOffset { get; private set; } = .8f;
    [field: Space]

    [field: Header("Move Variance")]
    [Tooltip("How often does the movement speed change per frame")]
    [field: SerializeField] public float MoveVarianceSpeed { get; private set; } = 5;
    [field: SerializeField] public float MoveSpeed { get; private set; } = 5;
    [Tooltip("How fast and slow can the spider Move")]
    [SerializeField] AnimationCurve _varianceRange;
    [field: Space]

    [field: Header("Leg models")]
    [SerializeField, Range(1, 20)] int _jointCount = 3;
    [SerializeField] BoxCollider _legPrefab;
    [SerializeField] BoxCollider firstLeg;
    [SerializeField] BoxCollider firstSecondLeg;
    [field: Space]

    [field: Header("Advanced")]
    [field: Tooltip("How far above does the ground checking raycast start")]
    [field: SerializeField] public float AbovePointHeight { get; private set; } = 1f;
    [field: Tooltip("Size of raycast that is searching for ground")]
    [field: SerializeField] public float TouchRaySize { get; private set; } = 1f;
    [field: Tooltip("How close can leg joints be")]
    [field: SerializeField] public float AcceptableDistance { get; private set; } = .05f;
    [field: Tooltip("How many attempts to connect leg joints to each other before giving up")]
    [field: SerializeField] public int CalibrationAttempts { get; private set; } = 5;

    public event Action OnFinishedWalking;

    Leg[] _legs;
    bool _startedMoving;
    bool _waitForFirstLegs;
    int _currentPathNode;
    float _currMoveVal;
    readonly Collider[] _contactPoints = new Collider[10];
    Vector3 _lastUp;

    //for debug
    Vector3 _closestContactPoint;

    void Start()
    {
        _lastUp = transform.up;
        _legs = new Leg[]
        {
           new (AngleX, 0, firstLeg, _jointCount, this                    , ForwardReach, _groundMask),
           new (AngleX, -AngleY*.5f, _legPrefab, _jointCount, this            , ForwardReach, _groundMask),
           new (AngleX, -AngleY, _legPrefab, _jointCount, this            , ForwardReach, _groundMask),
           new (AngleX, -AngleY*2, _legPrefab, _jointCount, this          , ForwardReach, _groundMask),

           new (AngleX, -AngleY*2 - 135, firstSecondLeg, _jointCount, this, ForwardReach, _groundMask),
           new (AngleX, -AngleY-135, _legPrefab, _jointCount, this        , ForwardReach, _groundMask),
           new (AngleX, -AngleY*.5f-135, _legPrefab, _jointCount, this        , ForwardReach, _groundMask),
           new (AngleX, - 135, _legPrefab, _jointCount, this              , ForwardReach, _groundMask),

        };

        SetAdjacentLegs(_legs);
    }

    public void StartMove()
    {
        _move = true;
    }

    void SetAdjacentLegs(Leg[] arr)
    {
        Debug.Assert(arr.Length % 2 == 0);

        int legsOnSide = (int)(arr.Length * .5f);
        for (int i = 0; i < arr.Length; i++)//attaches left right up down neighbours but assumes that the grid has only two rows
        {
            int left = i - 1;
            int right = i + 1;
            int up = i - legsOnSide;
            int down = i + legsOnSide;
            bool firstRow = i < legsOnSide;

            List<Leg> adjLegs = new(3);
            if (firstRow)//there are no up neighbours
            {
                if (left > -1) adjLegs.Add(arr[left]);
                if (right < legsOnSide) adjLegs.Add(arr[right]);
                adjLegs.Add(arr[down]);
            }
            else//there are no down neighbours
            {
                if (left > legsOnSide - 1) adjLegs.Add(arr[left]);
                if (right < arr.Length) adjLegs.Add(arr[right]);
                adjLegs.Add(arr[up]);
            }

            arr[i].SetAdjacentLegs(adjLegs.ToArray());
        }
    }

    void Update()
    {
        if (_move)
        {
            SetUpFirstStep();
            if (_waitForFirstLegs)
                MoveBody();
        }
        else
        {
            _waitForFirstLegs = false;
            _startedMoving = false;
        }

        IsMoving = _move;
        UpdateLegs();
    }

    void UpdateLegs()
    {
        foreach (Leg leg in _legs)
            leg.Update();
    }

    void GetCurrentNode()
    {
        if (_currentPathNode == _path.points.Count) return;

        Vector3 currentTarget = _path.GetGlobalSpace(_currentPathNode);

        bool nodeIsBehind = Vector3.Distance(currentTarget, transform.position) < _nextNodeActivationDistance * GetSize();
        if (nodeIsBehind)
        {
            _currentPathNode++;
            //was last node so stop
            if (_currentPathNode == _path.points.Count)
            {
                _move = false;
                OnFinishedWalking?.Invoke();
                _currentPathNode = 0;
            }
        }
    }

    void MoveBody()
    {
        if (_currentPathNode == _path.points.Count) return;

        GetCurrentNode();
        float deltaTime = Time.deltaTime;
        Vector3 currentTarget = _path.GetGlobalSpace(_currentPathNode);
        Vector3 forward = transform.forward;
        Vector3 currentPos = transform.position;

        int contacts = Physics.OverlapSphereNonAlloc(currentPos, (GroundOffset  + .7f) * GetSize(), _contactPoints, _groundMask);
        if (contacts > 0)
        {
            forward = Vector3.Lerp(forward, (currentTarget - currentPos).normalized, _pathLerpTime * deltaTime);

            Vector3 closestContactPoint = GetClosestContactPoint(contacts, currentPos);
            _closestContactPoint = closestContactPoint;
            Vector3 up = (currentPos - closestContactPoint).normalized;
            up = Vector3.Lerp(_lastUp, up, Time.deltaTime * _upLerpTime);
            _lastUp = up;

            Quaternion targetRotation = Quaternion.LookRotation(forward, up);
            transform.rotation = targetRotation;
        }
        _currMoveVal += MoveVarianceSpeed * deltaTime;
        transform.position += MoveSpeed * _varianceRange.Evaluate(Mathf.PerlinNoise1D(_currMoveVal)) * deltaTime * forward;
    }

    float GetSize()
    {
        return transform.lossyScale.y;
    }

    Vector3 GetClosestContactPoint(int contacts, Vector3 pos)
    {
        float min = int.MaxValue;
        Vector3 closestCol = Vector3.zero;
        foreach (Collider col in _contactPoints.Take(contacts))
        {
            Vector3 contactPoint = col.ClosestPoint(pos);
            float d = Vector3.Distance(pos, contactPoint);

            if (d < min)
            {
                min = d;
                closestCol = contactPoint;
            }
        }

        return closestCol;
    }

    void SetUpFirstStep()
    {
        if (_startedMoving) return;
        _startedMoving = true;

        // after the first stem, make step distance lower
        void legStepSizeReset(Leg l)
        {
            l.StepDistance = StepDistance;
            _waitForFirstLegs = true;
            l.OnStep -= legStepSizeReset;
        }
        _legs[0].StepDistance = RestStepDistance;
        _legs[_legs.Length / 2].StepDistance = RestStepDistance;
        _legs[0].OnStep += legStepSizeReset;
        _legs[_legs.Length / 2].OnStep += legStepSizeReset;

    }

    void OnDrawGizmos()
    {
        if (_legs == null || !Gizmos) return;

        if (_currentPathNode != _path.points.Count)
        {
            UnityEngine.Gizmos.color = Color.yellow;
            Vector3 currentTarget = _path.GetGlobalSpace(_currentPathNode);

            UnityEngine.Gizmos.DrawLine(transform.position, transform.position + (currentTarget - transform.position).normalized * GetSize());
            UnityEngine.Gizmos.DrawSphere(currentTarget, .1f * GetSize());
        }


        foreach (var leg in _legs)
            leg.OnDrawGizmos();

        UnityEngine.Gizmos.color = Color.black;
        //UnityEngine.Gizmos.DrawSphere(transform.position /*+ transform.up * GetSize() * .5f*/, (GroundOffset + .7f) * GetSize());
        UnityEngine.Gizmos.DrawSphere(_closestContactPoint, .2f * transform.lossyScale.y);
    }

    void OnDisable()
    {
        foreach (var leg in _legs) leg.ClearEvents();
    }
}
