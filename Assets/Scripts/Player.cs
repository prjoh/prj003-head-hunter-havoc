using UnityEngine;
using UnityEngine.UI;


public class Player : MonoBehaviour
{
    public Slider healthBar;

    private HealthComponent _health;

    private void Awake()
    {
        _health = new HealthComponent();
        _health.Init();
        healthBar.value = 1.0f;
    }

    public void TakeDamage(float percentage)
    {
        _health.TakeDamage(percentage);
        healthBar.value = _health.GetPercentage();
        Debug.Log($"Player Health: {_health.GetPercentage()}");
    }
}
