using MediatR;
using MilitaryDraftSystem.Application.Common.Contracts;
using MilitaryDraftSystem.Application.Common.Interfaces;

namespace MilitaryDraftSystem.Application.Draft.Behaviors
{
    /// <summary>
    /// Wraps request handling in an explicit EF Core transaction. Only
    /// applies to <see cref="ICommand{TResponse}"/> requests (state-changing
    /// operations); the generic constraint means MediatR never resolves
    /// this behavior for plain queries, so read-only requests never pay for
    /// (or risk) an open transaction.
    /// </summary>
    public class TransactionBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : ICommand<TResponse>
    {
        private readonly IAppDbContext _db;

        public TransactionBehavior(IAppDbContext db)
        {
            _db = db;
        }

        public async Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            // start db transaction
            await using var transaction = await _db.BeginTransactionAsync(cancellationToken);

            try
            {
                var response = await next();

                // commit transaction if all operations succeed
                await transaction.CommitAsync(cancellationToken);

                return response;
            }
            catch
            {
                // rollback transaction if any operation fails
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }
    }
}
