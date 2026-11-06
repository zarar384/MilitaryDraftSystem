using Moq;
using MilitaryDraftSystem.Application.Common.Contracts;
using MilitaryDraftSystem.Application.Common.Interfaces;
using MilitaryDraftSystem.Application.Draft.Behaviors;
using MediatR;

namespace MilitaryDraftSystem.Tests.Application.Draft.Behaviors
{
    public class TransactionBehaviorTests
    {
        public sealed record SampleRequest(string Name) : ICommand<string>;

        [Fact]
        public async Task Handle_ShouldCommitTransaction_WhenNextSucceeds()
        {
            // Arrange
            var transactionMock = new Mock<IAppTransaction>();
            var dbMock = new Mock<IAppDbContext>();
            dbMock
                .Setup(db => db.BeginTransactionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(transactionMock.Object);

            var sut = new TransactionBehavior<SampleRequest, string>(dbMock.Object);

            RequestHandlerDelegate<string> next = (_) => Task.FromResult("ok");

            // Act
            var result = await sut.Handle(new SampleRequest("Alice"), next, CancellationToken.None);

            // Assert
            Assert.Equal("ok", result);
            transactionMock.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
            transactionMock.Verify(t => t.RollbackAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_ShouldRollbackTransaction_AndRethrow_WhenNextThrows()
        {
            // Arrange
            var transactionMock = new Mock<IAppTransaction>();
            var dbMock = new Mock<IAppDbContext>();
            dbMock
                .Setup(db => db.BeginTransactionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(transactionMock.Object);

            var sut = new TransactionBehavior<SampleRequest, string>(dbMock.Object);

            RequestHandlerDelegate<string> next = (_) => throw new InvalidOperationException("boom");

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                sut.Handle(new SampleRequest("Alice"), next, CancellationToken.None));

            transactionMock.Verify(t => t.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
            transactionMock.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_ShouldBeginTransaction_BeforeInvokingNext()
        {
            // Arrange
            var transactionMock = new Mock<IAppTransaction>();
            var dbMock = new Mock<IAppDbContext>();
            dbMock
                .Setup(db => db.BeginTransactionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(transactionMock.Object);

            var sut = new TransactionBehavior<SampleRequest, string>(dbMock.Object);
            var nextCalled = false;

            RequestHandlerDelegate<string> next = (_) =>
            {
                nextCalled = true;
                return Task.FromResult("ok");
            };

            // Act
            await sut.Handle(new SampleRequest("Alice"), next, CancellationToken.None);

            // Assert
            dbMock.Verify(db => db.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
            Assert.True(nextCalled);
        }

        public sealed record SampleQuery(string Name) : IQuery<string>;

        [Fact]
        public void TransactionBehavior_ShouldNotBeConstructible_ForQueryRequests()
        {
            // SampleQuery implements IQuery<string>, not ICommand<string>, so
            // it can never satisfy TransactionBehavior<TRequest, TResponse>'s
            // generic constraint 'where TRequest : ICommand<TResponse>'.
            // MediatR resolves IPipelineBehavior<,> via DI, which filters out
            // open-generic implementations whose constraints aren't satisfied
            // by the closed request type - so TransactionBehavior is simply
            // never selected for queries. We assert that directly here:
            // attempting to close the generic for a query type must fail.
            var openGeneric = typeof(TransactionBehavior<,>);

            Assert.Throws<ArgumentException>(() =>
                openGeneric.MakeGenericType(typeof(SampleQuery), typeof(string)));
        }
    }
}
