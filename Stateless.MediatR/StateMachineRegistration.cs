using MediatR;
using NEventStore;

namespace Stateless.MediatR;

public abstract class StateMachineRegistration<TState,TTrigger,TCurrentTrigger>:
    IRequestHandler<RequestState<TState>,TState>
    ,INotificationHandler<TCurrentTrigger> where TTrigger:INotification where TCurrentTrigger : TTrigger
{
    private readonly IStoreEvents _store;
    private readonly TState _state;

    protected StateMachineRegistration(IStoreEvents store, TState state)
    {
        _store = store;
        _state = state;
    }
    

    public Task<TState> Handle(RequestState<TState> request, CancellationToken cancellationToken)
    {
        var stateMachine = FiniteStateMachine<TState,TTrigger>.Create<TState,TTrigger>(_store, request.Id,_state, Mapper);
        return Task.FromResult(stateMachine.GetFullState());
        
    }



    public async Task Handle(TCurrentTrigger notification, CancellationToken cancellationToken)
    {
        var stateMachine = FiniteStateMachine<TState,TTrigger>.Create<TState,TTrigger>(_store, CorrelateBy(notification),_state, Mapper);
        Configure(stateMachine);
        await stateMachine.FireAsync(notification);    
    }

    protected abstract void Configure(FiniteStateMachine<TState,TTrigger> stateMachine);

    protected abstract string CorrelateBy(TCurrentTrigger notification);
    
    protected abstract string Mapper(TState arg);


}