using MediatR;

namespace MilitaryDraftSystem.Application.Common.Contracts
{
    /// <summary>
    /// Marks a MediatR request as an operation that changes application
    /// state (writes through <c>IAppDbContext</c>). Commands - and only
    /// commands - are wrapped in an explicit EF Core transaction by
    /// <see cref="MilitaryDraftSystem.Application.Draft.Behaviors.TransactionBehavior{TRequest, TResponse}"/>.
    /// Deliberately a plain marker interface (no members): whether a request
    /// is a command is an explicit contract decision made by its author, not
    /// something inferred from naming, namespace or reflection.
    /// </summary>
    public interface ICommand : IRequest, ICommand<Unit>
    {
    }

    /// <summary>
    /// Marks a MediatR request that changes application state and returns a
    /// <typeparamref name="TResponse"/>. See <see cref="ICommand"/> for the
    /// non-generic (void) equivalent.
    /// </summary>
    public interface ICommand<out TResponse> : IRequest<TResponse>
    {
    }
}
