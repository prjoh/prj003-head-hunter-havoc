using System.Collections.Generic;
using UnityEngine;


public class ProjectilePool : PooledObject.ObjectPool
{
    private List<Projectile> _liveProjectiles;

    protected override void Awake()
    {
        base.Awake();
        _liveProjectiles = new List<Projectile>();
    }

    private void Update()
    {
        for (var i = _liveProjectiles.Count - 1; i >= 0; --i)
        {
            var p = _liveProjectiles[i];
            if (!p.alive)
            {
                Debug.Log($"Destroyed {p.gameObject.name} due to timeout.");
                Destroy(p);
                _liveProjectiles.RemoveAt(i);
            }
        }
    }

    public void Shoot(Vector3 from, Vector3 direction, float speed)
    {
        var obj = Create(from, Quaternion.identity);
        var projectile = obj.GetComponent<Projectile>();
        projectile.AddForce(direction * speed);
        _liveProjectiles.Add(projectile);
    }
}
