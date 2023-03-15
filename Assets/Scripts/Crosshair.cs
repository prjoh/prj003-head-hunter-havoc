using UnityEngine;


public class Crosshair : MonoBehaviour
{
    public Collider screenCollider;
    public Transform sphere;
    public Transform sphere2;

    public LayerMask mask = -1;

    public OffAxisPerspectiveProjection projection;

    public LineRenderer line1;
    public LineRenderer line2;
    
    private Transform _camera;
    private Ray _ray;

    private void Start()
    {
        _camera = Camera.main.transform;
        _ray = new Ray(_camera.position, _camera.forward);
    }

    private void Update()
    {
        _ray.origin = _camera.position;
        _ray.direction = _camera.forward;

        if (screenCollider.Raycast(_ray, out var hitScreen, 200.0f))
        {
            var position = sphere.position;
            position.x = Mathf.Min(Mathf.Max(projection.left, -hitScreen.point.x), projection.right);
            position.y = Mathf.Min(Mathf.Max(projection.bottom, -hitScreen.point.y), projection.top);
            sphere.position = position;
        }

        _ray.direction = (sphere.position - _camera.position).normalized;
        if (Physics.Raycast(_ray, out var hitObject, 200.0f, mask.value))
        {
            Debug.Log($"Hit something: {hitObject.collider.gameObject.name}");
            sphere2.position = hitObject.point;

            var l1p0 = line1.GetPosition(0);
            var l1p1 = l1p0 + (line1.gameObject.transform.InverseTransformPoint(sphere2.position) - l1p0).normalized * 25.0f;
            var l2p0 = line2.GetPosition(0);
            var l2p1 = l2p0 + (line2.gameObject.transform.InverseTransformPoint(sphere2.position) - l2p0).normalized * 25.0f;
            line1.SetPosition(1, l1p1);
            line2.SetPosition(1, l2p1);
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawRay(_camera.position, _camera.forward * 100.0f);

        Gizmos.color = Color.green;
        Gizmos.DrawRay(_camera.position, (sphere.position - _camera.position).normalized * 100.0f);
    }
}
