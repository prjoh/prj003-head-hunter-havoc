using UnityEngine;
using UnityEngine.UI;


public class Player : MonoBehaviour
{
    public Slider healthBar;
    public Crosshair crosshair;

    private HealthComponent _health;
    // private Color _projectileColor;
    // private ProjectilePool _projectilePool;

    public ProjectileLauncher projectileLauncher; 
    public ProjectileLauncher projectileLauncher2;

    private void Awake()
    {
        _health = new HealthComponent();

        // _projectileColor = new Color(0, 48, 191) * 0.0085f;
        // _projectilePool = FindObjectOfType<ProjectilePool>();
        // if (_projectilePool == null)
            // Debug.LogError($"{GetType().Name}.Awake: No ProjectilePool found! Please make sure a ProjectilePool is in your Scene.");
    }

    public void Init()
    {
        _health.Init();
        healthBar.value = 1.0f;
    }

    public void TakeDamage(float percentage, Vector3 direction)
    {
        _health.TakeDamage(percentage);
        healthBar.value = _health.GetPercentage();
        Debug.Log($"Player Health: {_health.GetPercentage()}");
    }

    void Update()
    {
        if (!_health.IsAlive())
            return;

        var target = crosshair.GetTargetPosition();
        
        var leftFrom = projectileLauncher.transform.parent;
        var leftDirection = (target - leftFrom.position).normalized;
        var rightFrom = projectileLauncher2.transform.parent;
        var rightDirection = (target - rightFrom.position).normalized;

        leftFrom.rotation = Quaternion.LookRotation(leftDirection);
        rightFrom.rotation = Quaternion.LookRotation(rightDirection);

        if (Input.GetMouseButtonDown(0))
        {
            projectileLauncher.Shoot();
            projectileLauncher2.Shoot();
        }
    }

    public bool IsAlive()
    {
        return _health.IsAlive();
    }

    // public void Shoot()
    // {
    //     var target = crosshair.GetTargetPosition();
    //     var targets = new [] { "Enemy", "Environment" };
    //
    //     var leftFrom = transform.position + Vector3.left * 3.0f;
    //     var leftDirection = (target - leftFrom).normalized;
    //
    //     var rightFrom = transform.position + Vector3.right * 3.0f;
    //     var rightDirection = (target - rightFrom).normalized;
    //
    //     _projectilePool.Shoot(leftFrom, leftDirection, 2000.0f, 0.25f, _projectileColor, targets);
    //     _projectilePool.Shoot(rightFrom, rightDirection, 2000.0f, 0.25f, _projectileColor, targets);
    // }

    private void OnDrawGizmos()
    {
        var from = projectileLauncher.transform.parent;
        var target = crosshair.GetTargetPosition();
        
        Gizmos.color = Color.magenta;
        Gizmos.DrawLine(from.position, target);
    }
}
