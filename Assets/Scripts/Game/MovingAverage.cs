using System.Linq;


public class MovingAverage
{
    private float _movingAverage;
    private int _windowSize;

    public MovingAverage(int windowSize)
    {
        _windowSize = windowSize;
        _movingAverage = 0.0f;
    }

    public void Add(float newValue)
    {
        var alpha = 2.0f / (_windowSize + 1.0f);
        _movingAverage = alpha * newValue + (1.0f - alpha) * _movingAverage;
    }

    public float GetAverage => _movingAverage;
}
