using System;
using System.Collections.Generic;
using UnityEngine;


public class DamageIndicatorSystem : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private DamageIndicator _indicatorPrefab = null;
    [SerializeField] private RectTransform _holder = null;
    [SerializeField] private Camera _camera = null;
    [SerializeField] private Transform _player = null;

    private Dictionary<Transform, DamageIndicator> Indicators = new();

    #region Delegates
    public static Action<Transform> CreateIndicator = delegate {  };
    public static Func<Transform, bool> CheckIfObjectInSight = null;
    #endregion

    private void OnEnable()
    {
        CreateIndicator += Create;
        CheckIfObjectInSight += InSight;
    }

    private void OnDisable()
    {
        CreateIndicator -= Create;
        CheckIfObjectInSight -= InSight;
    }

    private void Create(Transform target)
    {
        if (Indicators.ContainsKey(target))
        {
            Indicators[target].Restart();
            return;
        }

        var newIndicator = Instantiate(_indicatorPrefab, _holder);
        newIndicator.Register(target, _player, () => { Indicators.Remove(target); });

        Indicators.Add(target, newIndicator);
    }

    private bool InSight(Transform t)
    {
        var screenPoint = _camera.WorldToViewportPoint(t.position);
        var inSight = screenPoint.z > 0 && screenPoint.x > 0 && screenPoint.x < 1 && screenPoint.y > 0 && screenPoint.y < 1;
        return inSight;
    }
}
