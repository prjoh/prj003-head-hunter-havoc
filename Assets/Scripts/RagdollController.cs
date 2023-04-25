using UnityEngine;
using UnityEngine.UIElements;


public class RagdollController : MonoBehaviour
{
    public Animator animator;
    public Collider hitCollider;
    public Transform ragdollRoot;

    private Rigidbody[] _rigidbodies;
    private Collider[] _colliders;
    private CharacterJoint[] _joints;

    private void Awake()
    {
        _rigidbodies = ragdollRoot.GetComponentsInChildren<Rigidbody>();
        _colliders = ragdollRoot.GetComponentsInChildren<Collider>();
        _joints = ragdollRoot.GetComponentsInChildren<CharacterJoint>();
    }

    private void OnEnable()
    {
        ProjectileLauncher.Explosion += AddExplosionForce;
    }

    private void OnDisable()
    {
        ProjectileLauncher.Explosion -= AddExplosionForce;
    }
    
    public void AddExplosionForce(Vector3 position, string targetTag)
    {
        if (!gameObject.CompareTag(targetTag))
            return;

        foreach (var rb in _rigidbodies)
        {
            rb.AddExplosionForce(750, position, 5.0f);
        }
    }

    public void EnableRagdoll()
    {
        hitCollider.enabled = false;
        animator.enabled = false;
        foreach (var joint in _joints)
        {
            joint.enableCollision = true;
        }

        foreach (var col in _colliders)
        {
            col.enabled = true;
        }

        foreach (var rb in _rigidbodies)
        {
            rb.velocity = Vector3.zero;
            rb.isKinematic = false;
            rb.detectCollisions = true;
            rb.useGravity = true;
        }
    }

    public void EnableAnimator()
    {
        foreach (var joint in _joints)
        {
            joint.enableCollision = false;
        }

        foreach (var col in _colliders)
        {
            col.enabled = false;
        }

        foreach (var rb in _rigidbodies)
        {
            rb.isKinematic = true;
            rb.detectCollisions = false;
            rb.useGravity = false;
        }
        animator.enabled = true;
        hitCollider.enabled = true;
    }
}
