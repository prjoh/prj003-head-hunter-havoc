using System.Collections.Generic;
using UnityEngine;


public class ProjectileLauncher : MonoBehaviour
{
    public string targetTag;

    [Header("Projectile")]
    public ParticleSystem projectileVFX;
    public ParticleSystem projectileBeamVFX;
    public LayerMask collisionMask;

    [Header("Muzzle")]
    public bool enableMuzzleVFX = true;
    public ParticleSystem muzzleVFX;
    public ParticleSystem muzzleBeamVFX;
    public ParticleSystem muzzleParticlesVFX;

    [Header("Hit Effect")]
    public bool enableHitVFX = true;
    private ProjectileHit _projectileHitVFX;

    private AudioSource _explosionSound;

    private List<ParticleCollisionEvent> _collisionEvents;

    public delegate void OnExplosion(Vector3 position, string targetTag);
    public static event OnExplosion Explosion;

    private void Awake()
    {
        _explosionSound = GetComponent<AudioSource>();

        _collisionEvents = new List<ParticleCollisionEvent>();

        var col = projectileVFX.collision;
        col.collidesWith = collisionMask;

        if (!enableMuzzleVFX)
        {
            muzzleVFX.gameObject.SetActive(false);
            muzzleBeamVFX.gameObject.SetActive(false);
            muzzleParticlesVFX.gameObject.SetActive(false);
        }

        if (enableHitVFX)
        {
            _projectileHitVFX = FindObjectOfType<ProjectileHit>();
        }
    }

    public void Shoot()
    {
        projectileVFX.Emit(1);
        projectileBeamVFX.Emit(1);

        if (enableMuzzleVFX)
        {
            muzzleVFX.Emit(1);
            muzzleBeamVFX.Emit(1);
            muzzleParticlesVFX.Play();
        }
    }

    private void OnParticleCollision(GameObject other)
    {
        projectileVFX.GetCollisionEvents(other, _collisionEvents);
        for (var i = 0; i < _collisionEvents.Count; ++i)
        {
            Debug.Log($"Particle collision: {other.name}");
            // TODO: EmitAtLocation(_collisionEvents[i]);
            if (enableHitVFX)
            {
                _projectileHitVFX.EmitAtLocation(_collisionEvents[i].intersection);
            }

            Explosion?.Invoke(_collisionEvents[i].intersection, targetTag);
            _explosionSound.Play();

            if (other.CompareTag(targetTag))
            {
                if (targetTag == "Enemy")
                {
                    // var ai = other.gameObject.GetComponent<AIBehavior>();
                    // ai.health.TakeDamage(0.5f);
                }
                // TODO: Create OnExplosion for Player?
                else if (targetTag.Equals("Player"))
                {
                    var player = other.gameObject.GetComponent<Player>();
                    player.TakeDamage(0.05f, -_collisionEvents[i].velocity.normalized);

                    var ai = transform.root.GetComponent<AIBehavior>();
                    if (ai.health.IsAlive() && !DamageIndicatorSystem.CheckIfObjectInSight(transform.parent))
                    {
                        DamageIndicatorSystem.CreateIndicator(transform.parent);
                    }
                }
            }
        }
    }
}
