using FluentAssertions;
using MassTransit;
using Microsoft.Extensions.Logging;
using Moq;
using Peo.Core.Infra.ServiceBus.Services;

namespace Peo.Tests.UnitTests.Infrastructure;

public class MassTransitMessageBusTests
{
    private readonly Mock<IPublishEndpoint> _publishEndpointMock;
    private readonly Mock<ILogger<MassTransitMessageBus>> _loggerMock;
    private readonly MassTransitMessageBus _messageBus;

    public MassTransitMessageBusTests()
    {
        _publishEndpointMock = new Mock<IPublishEndpoint>();
        _loggerMock = new Mock<ILogger<MassTransitMessageBus>>();
        _messageBus = new MassTransitMessageBus(_publishEndpointMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task PublishAsync_DevePublicarMensagemComSucesso()
    {
        // Arrange
        var message = new TestMessage { Content = "Test Content", Id = Guid.CreateVersion7() };

        _publishEndpointMock
            .Setup(x => x.Publish(message, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _messageBus.PublishAsync(message, CancellationToken.None);

        // Assert
        _publishEndpointMock.Verify(
            x => x.Publish(message, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task PublishAsync_DeveLogarInformacao_AntesDePublicar()
    {
        // Arrange
        var message = new TestMessage { Content = "Test", Id = Guid.CreateVersion7() };

        _publishEndpointMock
            .Setup(x => x.Publish(message, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _messageBus.PublishAsync(message, CancellationToken.None);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Publishing message")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task PublishAsync_DeveLogarSucesso_AposPublicar()
    {
        // Arrange
        var message = new TestMessage { Content = "Test", Id = Guid.CreateVersion7() };

        _publishEndpointMock
            .Setup(x => x.Publish(message, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _messageBus.PublishAsync(message, CancellationToken.None);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Successfully published")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task PublishAsync_DeveLogarErro_QuandoPublicacaoFalhar()
    {
        // Arrange
        var message = new TestMessage { Content = "Test", Id = Guid.CreateVersion7() };
        var expectedException = new Exception("Connection failed");

        _publishEndpointMock
            .Setup(x => x.Publish(message, It.IsAny<CancellationToken>()))
            .ThrowsAsync(expectedException);

        // Act
        Func<Task> act = async () => await _messageBus.PublishAsync(message, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<Exception>().WithMessage("Connection failed");

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error publishing")),
                expectedException,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task PublishAsync_ComTipo_DevePublicarMensagemComSucesso()
    {
        // Arrange
        var message = new TestMessage { Content = "Test Content", Id = Guid.CreateVersion7() };
        var messageType = typeof(TestMessage);

        _publishEndpointMock
            .Setup(x => x.Publish(message, messageType, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _messageBus.PublishAsync(message, messageType, CancellationToken.None);

        // Assert
        _publishEndpointMock.Verify(
            x => x.Publish(message, messageType, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task PublishAsync_ComTipo_DeveLogarInformacao_AntesDePublicar()
    {
        // Arrange
        var message = new TestMessage { Content = "Test", Id = Guid.CreateVersion7() };
        var messageType = typeof(TestMessage);

        _publishEndpointMock
            .Setup(x => x.Publish(message, messageType, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _messageBus.PublishAsync(message, messageType, CancellationToken.None);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Publishing message")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task PublishAsync_ComTipo_DeveLogarErro_QuandoPublicacaoFalhar()
    {
        // Arrange
        var message = new TestMessage { Content = "Test", Id = Guid.CreateVersion7() };
        var messageType = typeof(TestMessage);
        var expectedException = new Exception("RabbitMQ unavailable");

        _publishEndpointMock
            .Setup(x => x.Publish(message, messageType, It.IsAny<CancellationToken>()))
            .ThrowsAsync(expectedException);

        // Act
        Func<Task> act = async () => await _messageBus.PublishAsync(message, messageType, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<Exception>().WithMessage("RabbitMQ unavailable");

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error publishing")),
                expectedException,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task PublishAsync_DeveRespeitarCancellationToken()
    {
        // Arrange
        var message = new TestMessage { Content = "Test", Id = Guid.CreateVersion7() };
        var cts = new CancellationTokenSource();
        cts.Cancel();

        _publishEndpointMock
            .Setup(x => x.Publish(message, It.IsAny<CancellationToken>()))
            .Returns((TestMessage msg, CancellationToken ct) =>
            {
                ct.ThrowIfCancellationRequested();
                return Task.CompletedTask;
            });

        // Act
        Func<Task> act = async () => await _messageBus.PublishAsync(message, cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    // Test Message Class
    public class TestMessage
    {
        public Guid Id { get; set; }
        public string Content { get; set; } = string.Empty;
    }
}
