using UnityEngine;


public class ExclusiveColliderZone : MonoBehaviour
{
    private Collider _colliderZone;
    private Collider _blockingCollider;

    private void Awake()
    {
        _colliderZone = GetComponent<Collider>();
        if (_colliderZone is null)
        {
            Debug.LogError("No Collider found in ExclusiveColliderZone!");
            return;
        }
        _colliderZone.isTrigger = true;

        var rb = gameObject.AddComponent<Rigidbody>();
        rb.isKinematic = true;
    }

    public void Allocate(Collider blockingCollider)
    {
        _blockingCollider = blockingCollider;
    }

    public void Free()
    {
        _blockingCollider = null;
    }

    public bool IsFree()
    {
        return _blockingCollider is null;
    }
    
    private void OnTriggerExit(Collider other)
    {
        if (other == _blockingCollider)
        {
            Free();
        }
    }
}
