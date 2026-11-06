using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using MilitaryDraftSystem.Application.Common.Contracts;
using MilitaryDraftSystem.Application.Common.Interfaces;
using MilitaryDraftSystem.Application.Draft.Behaviors;

namespace MilitaryDraftSystem.Tests.Application.Draft.Behaviors
{
    /// <summary>
    /// Verifies the actual MediatR pipeline wiring (the same open-generic
    /// registrations used by the API): a command request opens an explicit
    /// EF Core transaction, while a query request never does, because
    /// <see cref="TransactionBehavior{TRequest, TResponse}"/> is constrained
    /// to <c>ICommand&lt;TResponse&gt;</c> requests only.
    /// </summary>
    public class TransactionBehaviorPipelineTests
    {
        public sealed record SampleCommand(string Name) : ICommand<string>;

        public sealed class SampleCommandHandler : IRequestHandler<SampleCommand, string>
        {
            public Task<string> Handle(SampleCommand request, CancellationToken cancellationToken)
                => Task.FromResult($"handled:{request.Name}");
        }

        public sealed record SampleQuery(string Name) : IQuery<string>;

        public sealed class SampleQueryHandler : IRequestHandler<SampleQuery, string>
        {
            public Task<string> Handle(SampleQuery request, CancellationToken cancellationToken)
                => Task.FromResult($"queried:{request.Name}");
        }

        private static ServiceProvider BuildServiceProvider(Mock<IAppDbContext> dbMock)
        {
            var services = new ServiceCollection();

            services.AddLogging();
            services.AddSingleton(dbMock.Object);

            services.AddMediatR(cfg =>
                cfg.RegisterServicesFromAssemblyContaining<SampleCommand>());

            services.AddTransient(
                typeof(IPipelineBehavior<,>),
                typeof(TransactionBehavior<,>));

            return services.BuildServiceProvider();
        }

        [Fact]
        public async Task Send_Command_ShouldOpenAndCommitTransaction()
        {
            // Arrange
            var transactionMock = new Mock<IAppTransaction>();
            var dbMock = new Mock<IAppDbContext>();
            dbMock
                .Setup(db => db.BeginTransactionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(transactionMock.Object);

            await using var provider = BuildServiceProvider(dbMock);
            var mediator = provider.GetRequiredService<IMediator>();

            // Act
            var result = await mediator.Send(new SampleCommand("Alice"));

            // Assert
            Assert.Equal("handled:Alice", result);
            dbMock.Verify(db => db.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
            transactionMock.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Send_Query_ShouldNeverOpenTransaction()
        {
            // Arrange
            var dbMock = new Mock<IAppDbContext>();

            await using var provider = BuildServiceProvider(dbMock);
            var mediator = provider.GetRequiredService<IMediator>();

            // Act
            var result = await mediator.Send(new SampleQuery("Bob"));

            // Assert
            Assert.Equal("queried:Bob", result);
            dbMock.Verify(db => db.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Send_Command_ShouldRollback_WhenHandlerThrows()
        {
            // Arrange
            var transactionMock = new Mock<IAppTransaction>();
            var dbMock = new Mock<IAppDbContext>();
            dbMock
                .Setup(db => db.BeginTransactionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(transactionMock.Object);

            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton(dbMock.Object);
            services.AddMediatR(cfg =>
                cfg.RegisterServicesFromAssemblyContaining<SampleCommand>());
            services.AddTransient(
                typeof(IPipelineBehavior<,>),
                typeof(TransactionBehavior<,>));

            // Replace the normal handler with one that throws.
            services.AddTransient<IRequestHandler<ThrowingCommand, string>, ThrowingCommandHandler>();

            await using var provider = services.BuildServiceProvider();
            var mediator = provider.GetRequiredService<IMediator>();

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                mediator.Send(new ThrowingCommand("Carol")));

            dbMock.Verify(db => db.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
            transactionMock.Verify(t => t.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
            transactionMock.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        public sealed record ThrowingCommand(string Name) : ICommand<string>;

        public sealed class ThrowingCommandHandler : IRequestHandler<ThrowingCommand, string>
        {
            public Task<string> Handle(ThrowingCommand request, CancellationToken cancellationToken)
                => throw new InvalidOperationException("boom");
        }
    }
}
