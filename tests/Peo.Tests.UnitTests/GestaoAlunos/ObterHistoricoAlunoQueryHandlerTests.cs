using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Peo.Core.Interfaces.Services;
using Peo.GestaoAlunos.Application.Queries.ObterHistoricoAluno;
using Peo.GestaoAlunos.Domain.Entities;
using Peo.GestaoAlunos.Domain.Services;

namespace Peo.Tests.UnitTests.GestaoAlunos;

public class ObterHistoricoAlunoQueryHandlerTests
{
    private readonly Mock<IAlunoService> _alunoServiceMock;
    private readonly Mock<ILogger<ObterHistoricoAlunoQueryHandler>> _loggerMock;
    private readonly Mock<IAppIdentityUser> _appIdentityUserMock;
    private readonly ObterHistoricoAlunoQueryHandler _handler;

    public ObterHistoricoAlunoQueryHandlerTests()
    {
        _alunoServiceMock = new Mock<IAlunoService>();
        _loggerMock = new Mock<ILogger<ObterHistoricoAlunoQueryHandler>>();
        _appIdentityUserMock = new Mock<IAppIdentityUser>();
        _handler = new ObterHistoricoAlunoQueryHandler(
            _alunoServiceMock.Object,
            _loggerMock.Object,
            _appIdentityUserMock.Object);
    }

    [Fact]
    public async Task Handle_DeveRetornarTodoHistorico_QuandoApenasConcluidasIsFalse()
    {
        // Arrange
        var usuarioId = Guid.CreateVersion7();
        var cursoId1 = Guid.CreateVersion7();
        var cursoId2 = Guid.CreateVersion7();

        var matricula1 = new Matricula(usuarioId, cursoId1);
        matricula1.ConfirmarPagamento();

        var matricula2 = new Matricula(usuarioId, cursoId2);
        matricula2.Concluir();

        var matriculas = new List<Matricula> { matricula1, matricula2 };

        _appIdentityUserMock.Setup(x => x.GetUserId())
            .Returns(usuarioId);

        _alunoServiceMock
            .Setup(x => x.ObterMatriculas(usuarioId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(matriculas);

        var query = new ObterHistoricoAlunoQuery { ApenasConcluidas = false };

        // Act
        var resultado = await _handler.Handle(query, CancellationToken.None);

        // Assert
        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Should().NotBeNull();
        resultado.Value.Should().HaveCount(2);

        _alunoServiceMock.Verify(
            x => x.ObterMatriculas(usuarioId, false, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_DeveRetornarApenasMatriculasConcluidas_QuandoApenasConcluidasIsTrue()
    {
        // Arrange
        var usuarioId = Guid.CreateVersion7();
        var cursoId1 = Guid.CreateVersion7();
        var cursoId2 = Guid.CreateVersion7();

        var matriculaAtiva = new Matricula(usuarioId, cursoId1);
        matriculaAtiva.ConfirmarPagamento();

        var matriculaConcluida = new Matricula(usuarioId, cursoId2);
        matriculaConcluida.Concluir();

        // Service retorna todas, mas handler filtra
        var matriculas = new List<Matricula> { matriculaAtiva, matriculaConcluida };

        _appIdentityUserMock.Setup(x => x.GetUserId())
            .Returns(usuarioId);

        _alunoServiceMock
            .Setup(x => x.ObterMatriculas(usuarioId, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(matriculas);

        var query = new ObterHistoricoAlunoQuery { ApenasConcluidas = true };

        // Act
        var resultado = await _handler.Handle(query, CancellationToken.None);

        // Assert
        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Should().NotBeNull();
        resultado.Value.Should().HaveCount(1);
        resultado.Value.First().DataConclusao.Should().NotBeNull();

        _alunoServiceMock.Verify(
            x => x.ObterMatriculas(usuarioId, true, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_DeveRetornarListaVazia_QuandoNaoTiverHistorico()
    {
        // Arrange
        var usuarioId = Guid.CreateVersion7();
        var matriculas = new List<Matricula>();

        _appIdentityUserMock.Setup(x => x.GetUserId())
            .Returns(usuarioId);

        _alunoServiceMock
            .Setup(x => x.ObterMatriculas(usuarioId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(matriculas);

        var query = new ObterHistoricoAlunoQuery { ApenasConcluidas = false };

        // Act
        var resultado = await _handler.Handle(query, CancellationToken.None);

        // Assert
        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Should().NotBeNull();
        resultado.Value.Should().BeEmpty();

        _alunoServiceMock.Verify(
            x => x.ObterMatriculas(usuarioId, false, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_DeveRetornarFalha_QuandoOcorreErro()
    {
        // Arrange
        var usuarioId = Guid.CreateVersion7();
        var mensagemErro = "Erro ao buscar histórico do aluno";

        _appIdentityUserMock.Setup(x => x.GetUserId())
            .Returns(usuarioId);

        _alunoServiceMock
            .Setup(x => x.ObterMatriculas(usuarioId, false, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception(mensagemErro));

        var query = new ObterHistoricoAlunoQuery { ApenasConcluidas = false };

        // Act
        var resultado = await _handler.Handle(query, CancellationToken.None);

        // Assert
        resultado.IsSuccess.Should().BeFalse();
        resultado.Error.Should().NotBeNull();
        resultado.Error.Message.Should().Be(mensagemErro);

        _alunoServiceMock.Verify(
            x => x.ObterMatriculas(usuarioId, false, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_DeveFiltrarApenasMatriculasComDataConclusao_QuandoApenasConcluidasIsTrue()
    {
        // Arrange
        var usuarioId = Guid.CreateVersion7();
        var cursoId1 = Guid.CreateVersion7();
        var cursoId2 = Guid.CreateVersion7();
        var cursoId3 = Guid.CreateVersion7();

        var matriculaAtiva = new Matricula(usuarioId, cursoId1);
        var matriculaConcluida1 = new Matricula(usuarioId, cursoId2);
        matriculaConcluida1.Concluir();
        var matriculaConcluida2 = new Matricula(usuarioId, cursoId3);
        matriculaConcluida2.Concluir();

        // Service retorna todas incluindo as sem DataConclusao
        var matriculas = new List<Matricula> { matriculaAtiva, matriculaConcluida1, matriculaConcluida2 };

        _appIdentityUserMock.Setup(x => x.GetUserId())
            .Returns(usuarioId);

        _alunoServiceMock
            .Setup(x => x.ObterMatriculas(usuarioId, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(matriculas);

        var query = new ObterHistoricoAlunoQuery { ApenasConcluidas = true };

        // Act
        var resultado = await _handler.Handle(query, CancellationToken.None);

        // Assert
        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Should().NotBeNull();
        resultado.Value.Should().HaveCount(2); // Apenas as concluídas
        resultado.Value.All(h => h.DataConclusao.HasValue).Should().BeTrue();

        _alunoServiceMock.Verify(
            x => x.ObterMatriculas(usuarioId, true, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
