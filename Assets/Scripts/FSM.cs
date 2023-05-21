using System.Collections.Generic;


public abstract class State
{
    public readonly string name;

    private FiniteStateMachine _fsm;


    public State(string name)
    {
        this.name = name;
    }

    public void SetStateMachine(FiniteStateMachine fsm)
    {
        _fsm = fsm;
    }

    public virtual void Enter() {}
    public virtual void Update() {}
    public virtual void Exit() {}
}

public class FiniteStateMachine
{
    private State _currentState;
    private Dictionary<string, State> _states;

    public FiniteStateMachine()
    {
        _currentState = null;
        _states = new Dictionary<string, State>();
    }

    public void AddState(State state)
    {
        _states.Add(state.name, state);

        state.SetStateMachine(this);
    }

    public void SwitchState(string name)
    {
        _currentState?.Exit();
        _currentState = _states[name]; 
        _currentState.Enter();
    }

    public void Update()
    {
        _currentState?.Update();
    }

    public string CurrentState()
    {
        return _currentState is null ? "null" : _currentState.name;
    }
}