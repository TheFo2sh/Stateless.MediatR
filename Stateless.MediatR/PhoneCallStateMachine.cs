using NEventStore;

namespace Stateless.MediatR;

public class PhoneCallStateMachine<T> : 
    StateMachineRegistration<PhoneState, PhoneTriggers,T> 
    where T:PhoneTriggers
{
    protected override void Configure(FiniteStateMachine<PhoneState, PhoneTriggers> stateMachine)
    {
        stateMachine.Configure<CallNumber>(new PhoneState(PhoneAvailabilityState.Free),
            callNumber => new PhoneState(PhoneAvailabilityState.Busy) { Number = callNumber.PhoneNumber });
    
    }

    protected override string CorrelateBy(T notification) => notification.PhoneId;
    protected override string Mapper(PhoneState arg) => arg.AvailabilityState.ToString();

    public PhoneCallStateMachine(IStoreEvents store) : base(store, new PhoneState(PhoneAvailabilityState.Free){Number = "s"})
    { }
}