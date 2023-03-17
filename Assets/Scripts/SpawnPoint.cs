using System.Collections.Generic;
using UnityEngine;


public class SpawnPoint : MonoBehaviour
{
    public Collider spawnZone;

    public bool IsFree(List<Collider> colliders)
    {
        foreach (var col in colliders)
        {
            if (spawnZone.bounds.Intersects(col.bounds))
                return false;
        }

        return true;
    }
}
