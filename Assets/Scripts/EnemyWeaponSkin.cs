using UnityEngine;


public class EnemyWeaponSkin : MonoBehaviour
{
    public Transform anchor;
    public GameObject[] weapons;

    private GameObject currentWeapon;

    private void Awake()
    {
        var ndx = Random.Range(0, weapons.Length);
        for (var i = 0; i < weapons.Length; i++)
        {
            if (i == ndx)
                continue;
            weapons[i].SetActive(false);
        }

        weapons[ndx].transform.SetParent(anchor);

        currentWeapon = weapons[ndx];
    }

    public GameObject GetCurrentWeapon()
    {
        return currentWeapon;
    }
}
