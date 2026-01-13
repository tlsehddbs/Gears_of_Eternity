using UnityEngine;

public sealed class VfxFollow : MonoBehaviour
{
    private Transform target;
    private Vector3 offset;
    private bool followYaw;
    private Quaternion rotationOffset = Quaternion.identity;

    public void Bind(Transform t, Vector3 off, bool followYawOnly, Quaternion rotOffset)
    {
        target = t;
        offset = off;
        followYaw = followYawOnly;
        rotationOffset = rotOffset;
    }

    public void Unbind()
    {
        target = null;
        offset = Vector3.zero;
        followYaw = false;
        rotationOffset = Quaternion.identity;
    }

    private void LateUpdate()
    {
        if (target == null) return;

        transform.position = target.position + offset;

        if (followYaw)
        {
            float yaw = target.eulerAngles.y;
            Quaternion yawRot = Quaternion.Euler(0f, yaw, 0f);

            // 핵심: yaw만 따라가되 -90 오프셋은 항상 유지
            transform.rotation = yawRot * rotationOffset;
        }
    }
}