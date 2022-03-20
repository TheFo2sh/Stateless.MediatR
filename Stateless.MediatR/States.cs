using MediatR;

namespace Stateless.MediatR;
public enum PhoneAvailabilityState {Free,Busy}

public record PhoneState(PhoneAvailabilityState AvailabilityState)
{
    public string Number { get; set; }
}

public record PhoneTriggers(string PhoneId) : INotification;
public record CallNumber(string PhoneId,string PhoneNumber) : PhoneTriggers(PhoneId);