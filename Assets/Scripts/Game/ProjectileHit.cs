using UnityEngine;


public class ProjectileHit : MonoBehaviour
{
    public ParticleSystem hitVFX;
    public ParticleSystem hitBeamVFX;
    public ParticleSystem hitParticlesVFX;

    public void EmitAtLocation(Vector3 position)
    {
        hitVFX.transform.position = position;
        hitBeamVFX.transform.position = position;
        hitParticlesVFX.transform.position = position;

        hitVFX.Emit(1);
        hitBeamVFX.Emit(1);
        hitParticlesVFX.Play();
    }
}
