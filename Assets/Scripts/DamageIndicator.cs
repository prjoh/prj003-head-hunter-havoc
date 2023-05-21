using System;
using System.Collections;
using UnityEngine;


public class DamageIndicator : MonoBehaviour
{
    private const float MaxTimer = 4.0f;
    private float _timer = MaxTimer;

    private CanvasGroup _canvasGroup = null;
    protected CanvasGroup CanvasGroup
    {
        get
        {
            if (_canvasGroup != null) return _canvasGroup;

            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
            {
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            return _canvasGroup;
        }
    }

    private RectTransform _rect = null;
    protected RectTransform Rect
    {
        get
        {
            if (_rect != null) return _rect;

            _rect = GetComponent<RectTransform>();
            if (_rect == null)
            {
                _rect = gameObject.AddComponent<RectTransform>();
            }

            return _rect;
        }
    }

    public Transform Target { get; protected set; } = null;
    private Transform _player = null;
    private Camera _camera = null;

    private IEnumerator IE_Countdown = null;
    private Action Unregister = null;

    private Quaternion _tRot = Quaternion.identity;
    private Vector3 _tPos = Vector3.zero;

    public void Register(Transform target, Transform player, Action unregister)
    {
        Target = target;
        _player = player;
        Unregister = unregister;

        _camera = _player.GetComponent<Camera>();

        StartCoroutine(RotateToTarget());
        StartTimer();
    }

    public void Restart()
    {
        _timer = MaxTimer;
        StartTimer();
    }

    private void StartTimer()
    {
        if (IE_Countdown != null) { StopCoroutine(IE_Countdown); }

        IE_Countdown = Countdown();
        StartCoroutine(IE_Countdown);
    }
    
    private IEnumerator RotateToTarget()
    {
        while (enabled)
        {
            if (Target)
            {
                _tPos = Target.position;
                _tRot = Target.rotation;
            }

            Vector2 targetScreenPoint = _camera.WorldToScreenPoint(_tPos);
            var screenCenter = new Vector2(_camera.pixelWidth * 0.5f, _camera.pixelHeight * 0.5f);

            var diff = targetScreenPoint - screenCenter;
            var angle = Mathf.Atan2(diff.y, diff.x);
            var angleDegrees = angle * 180 / Mathf.PI + 90.0f;

            var eulerRotation = new Vector3(0.0f, 0.0f, angleDegrees);
            Rect.rotation = Quaternion.Euler(eulerRotation);

            yield return null;
        }
    }

    private IEnumerator Countdown()
    {
        while (CanvasGroup.alpha < 1.0f)
        {
            CanvasGroup.alpha += 4 * Time.deltaTime;
            yield return null;
        }

        while (_timer > 0)
        {
            _timer--;
            yield return new WaitForSeconds(1);
        }

        while (CanvasGroup.alpha > 0.0f)
        {
            CanvasGroup.alpha -= 2 * Time.deltaTime;
            yield return null;
        }

        Unregister();
        Destroy(gameObject);
    }
}
