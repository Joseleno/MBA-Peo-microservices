using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Peo.Core.Interfaces.Services;
using Peo.GestaoAlunos.Application.Queries.ObterMatriculas;
using Peo.GestaoAlunos.Domain.Entities;
using Peo.GestaoAlunos.Domain.Services;

namespace Peo.Tests.UnitTests.GestaoAlunos;

public class ObterMatriculasQueryHandlerTests
{
    private readonly Mock<IAlunoService> _alunoServiceMock;
    private readonly Mock<ILogger<ObterMatriculasQueryHandler>> _loggerMock;
    private readonly Mock<IAppIdentityUser> _appIdentityUserMock;
    private readonly ObterMatriculasQueryHandler _handler;

    public ObterMatriculasQueryHandlerTests()
    {
        _alunoServiceMock = new Mock<IAlunoService>();
        _loggerMock = new Mock<ILogger<ObterMatriculasQueryHandler>>();
        _appIdentityUserMock = new Mock<IAppIdentityUser>();
        _handler = new ObterMatriculasQueryHandler(
            _alunoServiceMock.Object,
            _loggerMock.Object,
            _appIdentityUserMock.Object);
    }

    [Fact]
    public async Task Handle_DeveRetornarTodasMatriculas_QuandoApenasConcluidasIsFalse()
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

        var query = new ObterMatriculasQuery { ApenasConcluidas = false };

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

        var matriculaConcluida = new Matricula(usuarioId, cursoId1);
        matriculaConcluida.Concluir();

        var matriculas = new List<Matricula> { matriculaConcluida };

        _appIdentityUserMock.Setup(x => x.GetUserId())
            .Returns(usuarioId);

        _alunoServiceMock
            .Setup(x => x.ObterMatriculas(usuarioId, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(matriculas);

        var query = new ObterMatriculasQuery { ApenasConcluidas = true };

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
    public async Task Handle_DeveRetornarListaVazia_QuandoNaoTiverMatriculas()
    {
        // Arrange
        var usuarioId = Guid.CreateVersion7();
        var matriculas = new List<Matricula>();

        _appIdentityUserMock.Setup(x => x.GetUserId())
            .Returns(usuarioId);

        _alunoServiceMock
            .Setup(x => x.ObterMatriculas(usuarioId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(matriculas);

        var query = new ObterMatriculasQuery { ApenasConcluidas = false };

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
        var mensagemErro = "Erro ao buscar matrículas";

        _appIdentityUserMock.Setup(x => x.GetUserId())
            .Returns(usuarioId);

        _alunoServiceMock
            .Setup(x => x.ObterMatriculas(usuarioId, false, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception(mensagemErro));

        var query = new ObterMatriculasQuery { ApenasConcluidas = false };

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
}
