using UnityEngine;


public class Projectile : PooledObject
{
    public float lifeTimeS = 6.0f;

    [HideInInspector] public bool alive = false;

    private Collider _collider;
    private Rigidbody _rb;

    protected void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.useGravity = false;
        
        _collider = GetComponent<Collider>();
        _collider.isTrigger = true;
    }

    private void OnEnable()
    {
        _rb.velocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero; 
        lifeTimeS = 10.0f;
        alive = true;
    }

    public void AddForce(Vector3 force)
    {
        _rb.AddForce(force);
    }

    private void Update()
    {
        lifeTimeS -= Time.deltaTime;
        alive = lifeTimeS > 0.0f;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            var player = other.gameObject.GetComponent<Player>();
            player.TakeDamage(0.05f);

            Debug.Log($"PROJECTILE COLLIDED WITH: {other.gameObject.name}");
            Destroy();
        }
    }
}
