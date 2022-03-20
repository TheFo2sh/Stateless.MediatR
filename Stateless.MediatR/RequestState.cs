using MediatR;

namespace Stateless.MediatR;

public record RequestState<TState>(string Id): IRequest<TState>
{}