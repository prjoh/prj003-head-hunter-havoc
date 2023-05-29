

public class HealthComponent
{
    private int _maxValue;
    private int _value;

    public HealthComponent(int maxValue = 100)
    {
        _maxValue = maxValue;
        _value = _maxValue;
    }

    public void Init(int maxValue = 100)
    {
        _maxValue = maxValue;
        _value = _maxValue;
    }

    public bool IsAlive()
    {
        return _value > 0;
    }

    public float GetPercentage()
    {
        return (float)_value / (float)_maxValue;
    }

    public void TakeDamage(float percentage)
    {
        _value -= (int)(_maxValue * percentage);
    }
}
