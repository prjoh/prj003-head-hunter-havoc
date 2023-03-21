

public class HealthComponent
{
    private const int MaxValue = 100;
    private int _value;

    public void Init()
    {
        _value = MaxValue;
    }

    public bool IsAlive()
    {
        return _value > 0;
    }

    public float GetPercentage()
    {
        return (float)_value / (float)MaxValue;
    }

    public void TakeDamage(float percentage)
    {
        _value -= (int)(MaxValue * percentage);
    }
}
