

public class CountdownTimer
{
    private float _intervalS;
    private float _countS;
    private bool _active;

    public delegate void OnTimeout();
    public event OnTimeout Timeout;

    public CountdownTimer(float intervalS = 10.0f)
    {
        _intervalS = intervalS;
        _countS = 0.0f;
        _active = false;
    }

    public void Start()
    {
        _active = true;
    }

    public void Start(float intervalS)
    {
        _intervalS = intervalS;
        _active = true;
    }
    
    public void Stop()
    {
        _active = false;
        _countS = 0.0f;
    }

    public void Update(float deltaTimeS)
    {
        if (!_active)
            return;

        _countS += deltaTimeS;

        if (_countS >= _intervalS)
        {
            Timeout?.Invoke();
            _countS = 0.0f;
        }
    }
}
