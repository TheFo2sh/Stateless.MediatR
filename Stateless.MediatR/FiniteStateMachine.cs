using System;
using System.Collections.Concurrent;
using NEventLite.Core;
using NEventStore;
using NEventStore.Serialization.Json;
using Stateless.Graph;

namespace Stateless.MediatR;

public class FiniteStateMachine<TState,TTrigger> : StateMachine<string, Type>,IDisposable
{
  public static FiniteStateMachine<TState, TTrigger> Create<TState, TTrigger>(IStoreEvents store, string id,
    TState defaultState, Func<TState,string> func)
  {
    var stream = store.OpenStream(id);
    return new FiniteStateMachine<TState,TTrigger>(stream,defaultState,func);
  }
  
  private readonly IEventStream _stream;
  private readonly ConcurrentDictionary<Type, TriggerWithParameters> _triggerWithParametersMap;
  private readonly Func<TState,string> _mapper;
  private TState _state;

  private FiniteStateMachine(IEventStream stream,TState defaultState,Func<TState,string>func)
    : base(() => func(GetState<TState>(stream)??defaultState),_=>{})
  {
    _state = GetState<TState>(stream) ?? defaultState;
    _stream = stream;
    _mapper = func;
    _triggerWithParametersMap = new ConcurrentDictionary<Type, TriggerWithParameters>();
    this.OnUnhandledTrigger((state, type) =>
    {
      if(type==typeof(TTrigger))
        return;
      throw new InvalidOperationException($"trigger {type} is not allowed from {state}");
    } );
  }

  public TState GetFullState() => _state;
  public void Configure<T>(TState state, Func<T, TState> func) where T : TTrigger
  {
    var stateMachineTrigger = (TriggerWithParameters<T>) _triggerWithParametersMap.GetOrAdd(typeof(T), SetTriggerParameters<T>(typeof(T)) );
    Configure(_mapper(state)).PermitDynamic(stateMachineTrigger,
      trigger =>
      {
        _state = func(trigger);
        SetState(_stream, _state);
        return _mapper(_state);
      });
  }
  public async Task FireAsync<T>(T trigger) where T : TTrigger
  {
    var stateMachineTrigger = (TriggerWithParameters<T>) _triggerWithParametersMap.GetOrAdd(typeof(T),(k)=>  SetTriggerParameters<T>(typeof(T)) );
    await FireAsync(stateMachineTrigger, trigger);
  }
 
  private static void SetState(IEventStream stream,object state)
  {
    stream.Add(new EventMessage() { Body = state  });
    stream.CommitChanges(Guid.NewGuid());
  }

  private static TState? GetState<TState>(IEventStream stream) 
  {
    var evt = stream.CommittedEvents.LastOrDefault();
    if (evt == null)
      return default;
    var state = (TState)evt.Body ;
    return state;
  }

  public void Dispose()
  {
    _stream.Dispose();
  }
  
}