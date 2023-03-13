using UnityEngine;


public class RotateLight : MonoBehaviour
{
    public new GameObject light;
    public float speed;
    private bool _islightNull;

    private void Start()
    {
        _islightNull = light == null;
    }

    private void Update()
    {
        if (_islightNull)
            return;

        light.transform.RotateAround(Vector3.zero, Vector3.back, speed);        
    }
}
