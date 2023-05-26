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

    private MenuButton _hoveredButton;
    private GameManager _gameManager;

    private void Awake()
    {
        _gameManager = FindObjectOfType<GameManager>();
    }

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
            // Debug.Log($"Hit something: {hitObject.collider.gameObject.name}");
            // sphere2.position = hitObject.point;
            sphere2.position = hitObject.point - _ray.direction * 2.5f;

            var obj = hitObject.transform.gameObject;
            if (obj.CompareTag("UIButton"))
            {
                if (_hoveredButton is null || _hoveredButton.gameObject != obj)
                {
                    if (_hoveredButton is not null)
                        _hoveredButton.OnHoverExit();

                    _hoveredButton = obj.GetComponent<MenuButton>();
                    _hoveredButton.OnHoverEnter();
                }
            }
            else if (_hoveredButton is not null)
            {
                _hoveredButton.OnHoverExit();
                _hoveredButton = null;
            }

            // var toCamera = (_camera.position - hitObject.point).normalized;
            // Debug.DrawRay(hitObject.point, toCamera * 10.0f, Color.magenta, 3.0f);
            // Debug.DrawRay(hitObject.point, hitObject.normal.normalized * 10.0f, Color.green, 3.0f);
            // // sphere2.rotation = Quaternion.LookRotation(toCamera, Vector3.up);
            // sphere2.rotation = Quaternion.LookRotation(hitObject.normal, Vector3.up);

            // var l1p0 = line1.GetPosition(0);
            // var l1p1 = l1p0 + (line1.gameObject.transform.InverseTransformPoint(sphere2.position) - l1p0).normalized * 25.0f;
            // var l2p0 = line2.GetPosition(0);
            // var l2p1 = l2p0 + (line2.gameObject.transform.InverseTransformPoint(sphere2.position) - l2p0).normalized * 25.0f;
            // line1.SetPosition(1, l1p1);
            // line2.SetPosition(1, l2p1);
        }

        if (Input.GetMouseButtonDown(0) && _hoveredButton is not null && _gameManager.fsm.CurrentState() != "GameState")
        {
            _hoveredButton.onClick?.Invoke();
        }
    }

    public Vector3 GetTargetPosition()
    {
        return sphere2.position;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawRay(_camera.position, _camera.forward * 100.0f);

        Gizmos.color = Color.green;
        Gizmos.DrawRay(_camera.position, (sphere.position - _camera.position).normalized * 100.0f);
    }
}
