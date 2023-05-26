using UnityEngine;
using UnityEngine.Events;


public class MenuButton : MonoBehaviour
{
    public Material materialIdle;
    public Material materialHover;

    private MeshRenderer _meshRenderer;

    public UnityEvent onClick;

    private void Awake()
    {
        _meshRenderer = GetComponent<MeshRenderer>();
    }

    public void OnHoverEnter()
    {
        _meshRenderer.material = materialHover;

        var position = transform.position;
        position.z += 0.25f;
        transform.position = position;
    }

    public void OnHoverExit()
    {
        _meshRenderer.material = materialIdle;

        var position = transform.position;
        position.z -= 0.25f;
        transform.position = position;
    }
}
