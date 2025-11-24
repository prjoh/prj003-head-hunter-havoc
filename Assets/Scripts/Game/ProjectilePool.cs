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

    public void Shoot(Vector3 from, Vector3 direction, float speed, float size, Color color, string target)
    {
        var obj = Create(from, Quaternion.identity);
        var projectile = obj.GetComponent<Projectile>();
        projectile.SetStyle(size, color);
        projectile.AddTarget(target);
        projectile.AddForce(direction * speed);
        _liveProjectiles.Add(projectile);
    }

    public void Shoot(Vector3 from, Vector3 direction, float speed, float size, Color color, IEnumerable<string> targets)
    {
        var obj = Create(from, Quaternion.identity);
        var projectile = obj.GetComponent<Projectile>();
        projectile.SetStyle(size, color);
        projectile.AddTargets(targets);
        projectile.AddForce(direction * speed);
        _liveProjectiles.Add(projectile);
    }
}
