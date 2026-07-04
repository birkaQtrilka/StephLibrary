using UnityEngine;

[System.Serializable]
public struct CurveFollowData
{
    [Tooltip("Path to be followed")]
    public Curve path;
    [Tooltip("How close do you have to be to the following node to then move on to the next one")]
    public float nextNodeActivationDistance;
    [Tooltip("How close do you have to be to the last node to then move on to the next one")]
    public float lastNodeActivationDistance;
    public float pathLerpTime;
    public float MoveSpeed;
}
