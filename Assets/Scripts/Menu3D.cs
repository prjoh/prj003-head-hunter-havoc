using System.Collections;
using UnityEngine;


public class Menu3D : MonoBehaviour
{
    [SerializeField] private Vector3 showPosition;
    [SerializeField] private Vector3 hidePosition;

    public void Show()
    {
        StartCoroutine(SetMenuPosition(showPosition));
    }

    public void Hide()
    {
        StartCoroutine(SetMenuPosition(hidePosition));
    }

    private IEnumerator SetMenuPosition(Vector3 goalPosition)
    {
        var startPosition = transform.position;
        const float time = 2.0f;
        var elapsed = 0.0f;

        while (elapsed < time && startPosition != goalPosition)
        {
            elapsed += Time.deltaTime;
            transform.position = Vector3.Lerp(startPosition, goalPosition, elapsed / time);
            yield return null;
        }

        transform.position = goalPosition;
    }
}
