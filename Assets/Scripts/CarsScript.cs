using UnityEngine;

public class CarsScript : MonoBehaviour
{
    public enum Axis
    {
        PosX,
        NegX,
        PosY,
        NegY,
        PosZ,
        NegZ
    }

    [Header("Movement Settings")]
    [SerializeField] private Axis moveAxis = Axis.PosZ;
    [SerializeField] private float speed = 1f;

    [Header("Wrapping Settings (along movement axis, in local space)")]
    [SerializeField] private float minPosition = -10f;
    [SerializeField] private float maxPosition = 10f;

    [Header("Material Settings")]
    [SerializeField] private Material[] materials;

    private void Awake()
    {
        // Assign a random material from the list to each direct child (if it has a Renderer)
        if (materials == null || materials.Length == 0)
        {
            return;
        }

        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            Renderer r = child.GetComponent<Renderer>();
            if (r == null)
            {
                continue;
            }

            int index = Random.Range(0, materials.Length); // upper bound is exclusive
            r.material = materials[index];
        }
    }

    private Vector3 GetAxisDirection()
    {
        switch (moveAxis)
        {
            case Axis.PosX: return Vector3.right;
            case Axis.NegX: return Vector3.left;
            case Axis.PosY: return Vector3.up;
            case Axis.NegY: return Vector3.down;
            case Axis.PosZ: return Vector3.forward;
            case Axis.NegZ: return Vector3.back;
            default:        return Vector3.forward;
        }
    }

    private void Update()
    {
        Vector3 axisDir = GetAxisDirection().normalized;
        float distance = speed * Time.deltaTime;

        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);

            // Move along local axis
            child.Translate(axisDir * distance, Space.Self);

            // Project local position onto axis
            Vector3 localPos = child.localPosition;
            float axisPos = Vector3.Dot(localPos, axisDir);

            // Wrap if out of bounds
            if (axisPos > maxPosition)
            {
                axisPos = minPosition;
            }
            else if (axisPos < minPosition)
            {
                axisPos = maxPosition;
            }
            else
            {
                continue; // still inside, no wrapping needed
            }

            // Apply wrapped position only along movement axis
            float currentAxisPos = Vector3.Dot(localPos, axisDir);
            float delta = axisPos - currentAxisPos;
            child.localPosition = localPos + axisDir * delta;
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Visualize the line segment of the wrap range in the Scene view
        if (!Application.isPlaying)
        {
            Vector3 axisDir = GetAxisDirection().normalized;
            Vector3 start = transform.TransformPoint(axisDir * minPosition);
            Vector3 end   = transform.TransformPoint(axisDir * maxPosition);

            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(start, end);
            Gizmos.DrawSphere(start, 0.1f);
            Gizmos.DrawSphere(end, 0.1f);
        }
    }
}