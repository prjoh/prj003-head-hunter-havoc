using UnityEngine;


// TODO: Is it really broken? Because the Update function looks like a fix??? TEST THIS!!!
// TODO: THIS IS BROKEN!!
//   This works only when leaving spawn points (although it also breaks if we manage to kill an enemy inside the spawn point!!!)
public class ExclusiveColliderZone : MonoBehaviour
{
    private Collider _colliderZone;
    private Collider _blockingCollider;

    public bool available = true;
    
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

    private void Update()
    {
        if (_blockingCollider is not null && !_blockingCollider.gameObject.activeSelf)
            _blockingCollider = null;
    }

    public void Allocate(Collider blockingCollider)
    {
        _blockingCollider = blockingCollider;
        available = false;
    }

    public void Free()
    {
        _blockingCollider = null;
        available = true;
    }

    public bool IsFree()
    {
        return _blockingCollider is null;
    }
    
    // TODO: This should happen in AIBehavior! Right?
    private void OnTriggerExit(Collider other)
    {
        Debug.Log($"############# >>{GetType().Name}.OnTriggerExit {other.gameObject.name}");
        if (other == _blockingCollider)
        {
            Free();
        }
    }
}
