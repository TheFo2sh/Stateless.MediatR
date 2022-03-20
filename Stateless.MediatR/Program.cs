// See https://aka.ms/new-console-template for more information

using Autofac;
using Autofac.Features.Variance;
using MediatR;
using NEventStore;
using NEventStore.Serialization.Json;
using Stateless.MediatR;


var store = Wireup.Init()
    .UsingInMemoryPersistence()
    .InitializeStorageEngine()
    .UsingJsonSerialization()
    .Compress()
    .Build();

var builder = new ContainerBuilder();
builder.Register(ctx => Wireup.Init()
    .UsingInMemoryPersistence()
    .InitializeStorageEngine()
    .UsingJsonSerialization()
    .Compress()
    .Build()).SingleInstance().AsImplementedInterfaces();

builder.RegisterSource(new ContravariantRegistrationSource());
builder.RegisterStateMachine<PhoneState,PhoneTriggers>(typeof(PhoneCallStateMachine<>));


builder.Register(ctx =>
{
    var context = ctx.Resolve<IComponentContext>();
    return new Mediator(type => context.Resolve(type));
}).AsImplementedInterfaces();

var container = builder.Build();

var mediator = container.Resolve<IMediator>();
var state=await mediator.Send(new RequestState<PhoneState>("phone1"));
Console.WriteLine(state);

await mediator.Publish(new CallNumber("phone1","1234"));
state=await mediator.Send(new RequestState<PhoneState>("phone1"));

Console.WriteLine(state);


