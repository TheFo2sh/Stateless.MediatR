using Autofac;
using MediatR;
using NEventStore;

namespace Stateless.MediatR;

public static class AutofacExtensions
{
    public static ContainerBuilder RegisterStateMachine<TState,TTrigger>(this ContainerBuilder containerBuilder,Type stateMachine)
    {
        containerBuilder.RegisterGeneric(stateMachine).As(typeof(INotificationHandler<>));
        containerBuilder.RegisterType(stateMachine.MakeGenericType(typeof(TTrigger))).As(typeof(IRequestHandler<RequestState<TState>,TState>));

        return containerBuilder;
    }
}