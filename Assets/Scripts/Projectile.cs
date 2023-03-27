using System.Collections.Generic;
using UnityEngine;


public class Projectile : PooledObject
{
    public float lifeTimeS = 6.0f;

    [HideInInspector] public bool alive = false;

    private Collider _collider;
    private Rigidbody _rb;
    private Material _material;
    private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");
    public List<string> _targetTags;

    public delegate void OnEnvironmentHit(Vector3 position);
    public static event OnEnvironmentHit EnvironmentHit;
 
    protected void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.useGravity = false;
        
        _collider = GetComponent<Collider>();
        _collider.isTrigger = true;

        _material = GetComponent<MeshRenderer>().material;
        _targetTags = new List<string>();
    }

    private void OnEnable()
    {
        _rb.velocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero; 
        lifeTimeS = 10.0f;
        alive = true;
        _targetTags.Clear();
    }

    public void SetStyle(float size, Color color)
    {
        gameObject.transform.localScale.Set(size, size, size);
        _material.SetColor(EmissionColor, color);
    }

    public void AddTarget(string targetTag)
    {
        _targetTags.Add(targetTag);
    }

    public void AddTargets(IEnumerable<string> targetTags)
    {
        _targetTags.AddRange(targetTags);
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
        foreach (var targetTag in _targetTags)
        {
            if (other.CompareTag(targetTag))
            {
                Debug.Log($"PROJECTILE COLLIDED WITH: {other.gameObject.name}");
                Destroy();

                if (targetTag == "Player")
                {
                    var player = other.gameObject.GetComponent<Player>();
                    player.TakeDamage(0.05f);
                }

                else if (targetTag == "Enemy")
                {
                    var ai = other.gameObject.GetComponent<AIBehavior>();
                    ai.health.TakeDamage(0.5f);
                }

                else
                {
                    // TODO: What is the performance implication of ClosestPoint???
                    EnvironmentHit?.Invoke(other.ClosestPoint(transform.position));
                }

                return;
            }
        }
    }
}
