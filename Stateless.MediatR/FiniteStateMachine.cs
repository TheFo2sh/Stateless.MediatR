using System;
using System.Collections.Concurrent;
using NEventLite.Core;
using NEventStore;
using NEventStore.Serialization.Json;

namespace Stateless.MediatR;

public class FiniteStateMachine<TState,TTrigger> : StateMachine<TState, Type>,IDisposable
{
  public static FiniteStateMachine<TState,TTrigger> Create<TState,TTrigger>(IStoreEvents store,string id,TState defaultState)
  {
    var stream = store.OpenStream(id);
    return new FiniteStateMachine<TState,TTrigger>(stream,defaultState);
  }
  
  private readonly IEventStream _stream;
  private readonly ConcurrentDictionary<Type, TriggerWithParameters> _triggerWithParametersMap;
  private FiniteStateMachine(IEventStream stream,TState defaultState)
    : base(() => GetState<TState>(stream)??defaultState, (state => SetState(stream, state)))
  {
    _stream = stream;
    _triggerWithParametersMap = new ConcurrentDictionary<Type, TriggerWithParameters>();
    this.OnUnhandledTrigger((state, type) =>
    {
      if(type==typeof(TTrigger))
        return;
      throw new InvalidOperationException($"trigger {type} is not allowed from {state}");
    } );
  }

  public void Configure<T>(TState state, Func<T, TState> func) where T : TTrigger
  {
    var stateMachineTrigger = (TriggerWithParameters<T>) _triggerWithParametersMap.GetOrAdd(typeof(T), SetTriggerParameters<T>(typeof(T)) );

     Configure(state).PermitDynamic(stateMachineTrigger, func);
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