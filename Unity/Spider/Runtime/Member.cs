using UnityEngine;

public class Member
{
    public readonly BoxCollider Box;

    public Member(BoxCollider box)
    {
        Box = box;
    }

    public void SetBoxTransform(Vector3 target)
    {
        Vector3 up = (target - GetStartPosition()).normalized;
        //up.z = 0;
        Box.transform.up = up;
        //Box.transform.localEulerAngles = new Vector3(Box.transform.localEulerAngles.x, 0, Box.transform.localEulerAngles.z);
        Box.transform.position = target - GetSizeAndDirection();
    }

    public Vector3 GetStartPosition()
    {
        return Box.transform.position - GetSizeAndDirection();
    }

    public void SetStartPosition(Vector3 pos)
    {
        Box.transform.position = pos + GetSizeAndDirection();
    }

    public Vector3 GetEndPosition()
    {
        return Box.transform.position + GetSizeAndDirection() ;
    }

    Vector3 GetSizeAndDirection()
    {
        return GetSize() * Box.transform.up;

    }

    public float GetSize()
    {
        return Box.transform.lossyScale.y * .5f;
    }
}
