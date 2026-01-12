using UnityEngine;

[DisallowMultipleComponent]
public sealed class TracerLineVfx : MonoBehaviour
{
    [SerializeField] private LineRenderer line;

    private void Reset()
    {
        line = GetComponent<LineRenderer>();
    }

    private void Awake()
    {
        if (line == null)
            line = GetComponent<LineRenderer>();
    }

    public void SetPositions(Vector3 start, Vector3 end)
    {
        if (line == null) return;

        line.positionCount = 2;
        line.SetPosition(0, start);
        line.SetPosition(1, end);
    }

    public void SetWidth(float width)
    {
        if (line == null) return;

        line.startWidth = width;
        line.endWidth = width;
    }
}