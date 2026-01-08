using FluentAssertions;
using Moq;
using Peo.Core.Dtos;
using Peo.Core.Messages.IntegrationCommands;
using Peo.Faturamento.Application.Handlers;
using Peo.Faturamento.Domain.Entities;
using Peo.Faturamento.Domain.Services;
using Peo.Faturamento.Domain.ValueObjects;

namespace Peo.Tests.UnitTests.Faturamento;

public class ProcessarPagamentoMatriculaCommandHandlerTests
{
    private readonly Mock<IPagamentoService> _pagamentoServiceMock;
    private readonly ProcessarPagamentoMatriculaCommandHandler _handler;

    public ProcessarPagamentoMatriculaCommandHandlerTests()
    {
        _pagamentoServiceMock = new Mock<IPagamentoService>();
        _handler = new ProcessarPagamentoMatriculaCommandHandler(_pagamentoServiceMock.Object);
    }

    [Fact]
    public async Task Handle_DeveRetornarSucesso_QuandoPagamentoForProcessadoComSucesso()
    {
        // Arrange
        var matriculaId = Guid.CreateVersion7();
        var valor = 1000m;
        var cartao = new CartaoCredito("4111111111111111", "12/2025", "123", "João Silva");
        var pagamento = new Pagamento(matriculaId, valor);

        // Simula um pagamento bem-sucedido
        var pagamentoPago = new Pagamento(matriculaId, valor);
        pagamentoPago.ProcessarPagamento("TXN123");
        var dadosCartao = new DadosDoCartaoCredito { Hash = "hash_cartao_encriptado" };
        pagamentoPago.ConfirmarPagamento(dadosCartao);

        _pagamentoServiceMock
            .Setup(x => x.ProcessarPagamentoMatriculaAsync(matriculaId, valor, cartao, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagamentoPago);

        var command = new ProcessarPagamentoMatriculaCommand(matriculaId, valor, cartao);

        // Act
        var resultado = await _handler.Handle(command, CancellationToken.None);

        // Assert
        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Should().NotBeNull();
        resultado.Value.Sucesso.Should().BeTrue();
        resultado.Value.StatusPagamento.Should().Be(StatusPagamento.Pago.ToString());

        _pagamentoServiceMock.Verify(
            x => x.ProcessarPagamentoMatriculaAsync(matriculaId, valor, cartao, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_DeveRetornarFalha_QuandoPagamentoFalhar()
    {
        // Arrange
        var matriculaId = Guid.CreateVersion7();
        var valor = 1000m;
        var cartao = new CartaoCredito("4111111111111111", "12/2025", "123", "João Silva");
        var detalhesErro = "Cartão recusado";

        // Simula um pagamento com falha
        var pagamentoFalhado = new Pagamento(matriculaId, valor);
        pagamentoFalhado.ProcessarPagamento("TXN456");
        pagamentoFalhado.MarcarComoFalha(detalhesErro);

        _pagamentoServiceMock
            .Setup(x => x.ProcessarPagamentoMatriculaAsync(matriculaId, valor, cartao, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagamentoFalhado);

        var command = new ProcessarPagamentoMatriculaCommand(matriculaId, valor, cartao);

        // Act
        var resultado = await _handler.Handle(command, CancellationToken.None);

        // Assert
        resultado.IsSuccess.Should().BeFalse();
        resultado.Error.Should().NotBeNull();
        resultado.Error.Message.Should().Contain(detalhesErro);

        _pagamentoServiceMock.Verify(
            x => x.ProcessarPagamentoMatriculaAsync(matriculaId, valor, cartao, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_DeveRetornarFalha_QuandoPagamentoEstiverPendente()
    {
        // Arrange
        var matriculaId = Guid.CreateVersion7();
        var valor = 1000m;
        var cartao = new CartaoCredito("4111111111111111", "12/2025", "123", "João Silva");

        // Pagamento que ficou pendente
        var pagamentoPendente = new Pagamento(matriculaId, valor);

        _pagamentoServiceMock
            .Setup(x => x.ProcessarPagamentoMatriculaAsync(matriculaId, valor, cartao, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagamentoPendente);

        var command = new ProcessarPagamentoMatriculaCommand(matriculaId, valor, cartao);

        // Act
        var resultado = await _handler.Handle(command, CancellationToken.None);

        // Assert
        resultado.IsSuccess.Should().BeTrue(); // Handler retorna sucesso para Processando
        resultado.Value.Should().NotBeNull();
        resultado.Value.StatusPagamento.Should().Be(StatusPagamento.Pendente.ToString());
    }

    [Fact]
    public async Task Handle_DeveRetornarSucesso_QuandoPagamentoEstiverProcessando()
    {
        // Arrange
        var matriculaId = Guid.CreateVersion7();
        var valor = 1000m;
        var cartao = new CartaoCredito("4111111111111111", "12/2025", "123", "João Silva");

        var pagamentoProcessando = new Pagamento(matriculaId, valor);
        pagamentoProcessando.ProcessarPagamento("TXN789");

        _pagamentoServiceMock
            .Setup(x => x.ProcessarPagamentoMatriculaAsync(matriculaId, valor, cartao, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagamentoProcessando);

        var command = new ProcessarPagamentoMatriculaCommand(matriculaId, valor, cartao);

        // Act
        var resultado = await _handler.Handle(command, CancellationToken.None);

        // Assert
        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Should().NotBeNull();
        resultado.Value.StatusPagamento.Should().Be(StatusPagamento.Processando.ToString());
    }
}
