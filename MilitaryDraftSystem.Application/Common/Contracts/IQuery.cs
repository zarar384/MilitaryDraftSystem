using MediatR;

namespace MilitaryDraftSystem.Application.Common.Contracts
{
    /// <summary>
    /// Marks a MediatR request as a read-only operation that must not open
    /// an EF Core transaction. See <see cref="ICommand{TResponse}"/> for the
    /// state-changing equivalent.
    /// </summary>
    public interface IQuery<out TResponse> : IRequest<TResponse>
    {
    }
}
