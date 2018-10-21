using UnityEngine;

public class ExperimentPlayer : MonoBehaviour {

    public float moveSpeed = 1f;
    public float rotateAngle = 90f;

    public void MoveForward()
    {
        Transform eyeTransform = this.transform.GetComponentInChildren<Camera>().transform;
        Vector3 lookDirection = eyeTransform.forward;
        lookDirection.y = 0;
        lookDirection = lookDirection.normalized;
        this.transform.position += lookDirection * moveSpeed;
    }

    public void RotateLeft()
    {
        this.transform.Rotate(new Vector3(0, -rotateAngle, 0));
    }

    public void RotateRight()
    {
        this.transform.Rotate(new Vector3(0, rotateAngle, 0));
    }
}
