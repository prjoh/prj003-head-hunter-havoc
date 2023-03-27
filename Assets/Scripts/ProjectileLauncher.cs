using System.Collections.Generic;
using UnityEngine;


public class ProjectileLauncher : MonoBehaviour
{
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

    private List<ParticleCollisionEvent> _collisionEvents;

    private void Awake()
    {
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
        }
    }
}
