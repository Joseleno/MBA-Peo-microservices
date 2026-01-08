using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Peo.Core.Interfaces.Services;
using Peo.GestaoAlunos.Application.Queries.ObterAulasMatricula;
using Peo.GestaoAlunos.Application.Queries.ObterMatriculas;
using Peo.GestaoAlunos.Domain.Dtos;
using Peo.GestaoAlunos.Domain.Services;

namespace Peo.Tests.UnitTests.GestaoAlunos;

public class ObterAulasMatriculaQueryHandlerTests
{
    private readonly Mock<IAlunoService> _alunoServiceMock;
    private readonly Mock<ILogger<ObterAulasMatriculaQueryHandler>> _loggerMock;
    private readonly Mock<IAppIdentityUser> _appIdentityUserMock;
    private readonly ObterAulasMatriculaQueryHandler _handler;

    public ObterAulasMatriculaQueryHandlerTests()
    {
        _alunoServiceMock = new Mock<IAlunoService>();
        _loggerMock = new Mock<ILogger<ObterAulasMatriculaQueryHandler>>();
        _appIdentityUserMock = new Mock<IAppIdentityUser>();
        _handler = new ObterAulasMatriculaQueryHandler(
            _alunoServiceMock.Object,
            _loggerMock.Object,
            _appIdentityUserMock.Object);
    }

    [Fact]
    public async Task Handle_DeveRetornarAulas_QuandoMatriculaExistir()
    {
        // Arrange
        var usuarioId = Guid.CreateVersion7();
        var matriculaId = Guid.CreateVersion7();
        var cursoId = Guid.CreateVersion7();
        var aulaId1 = Guid.CreateVersion7();
        var aulaId2 = Guid.CreateVersion7();

        var aulasDto = new List<AulaMatriculaDto>
        {
            new AulaMatriculaDto(matriculaId, cursoId, aulaId1, DateTime.Now.AddDays(-2), DateTime.Now.AddDays(-1), "Concluída"),
            new AulaMatriculaDto(matriculaId, cursoId, aulaId2, null, null, "Não Iniciada")
        };

        _appIdentityUserMock.Setup(x => x.GetUserId())
            .Returns(usuarioId);

        _alunoServiceMock
            .Setup(x => x.ObterAulasMatricula(usuarioId, matriculaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(aulasDto);

        var query = new ObterAulasMatriculaQuery(matriculaId);

        // Act
        var resultado = await _handler.Handle(query, CancellationToken.None);

        // Assert
        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Should().NotBeNull();
        resultado.Value.Should().HaveCount(2);
        resultado.Value.First().AulaId.Should().Be(aulaId1);
        resultado.Value.Last().AulaId.Should().Be(aulaId2);

        _alunoServiceMock.Verify(
            x => x.ObterAulasMatricula(usuarioId, matriculaId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_DeveRetornarListaVazia_QuandoMatriculaNaoTiverAulas()
    {
        // Arrange
        var usuarioId = Guid.CreateVersion7();
        var matriculaId = Guid.CreateVersion7();
        var aulasDto = new List<AulaMatriculaDto>();

        _appIdentityUserMock.Setup(x => x.GetUserId())
            .Returns(usuarioId);

        _alunoServiceMock
            .Setup(x => x.ObterAulasMatricula(usuarioId, matriculaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(aulasDto);

        var query = new ObterAulasMatriculaQuery(matriculaId);

        // Act
        var resultado = await _handler.Handle(query, CancellationToken.None);

        // Assert
        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Should().NotBeNull();
        resultado.Value.Should().BeEmpty();

        _alunoServiceMock.Verify(
            x => x.ObterAulasMatricula(usuarioId, matriculaId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_DeveRetornarFalha_QuandoOcorreErro()
    {
        // Arrange
        var usuarioId = Guid.CreateVersion7();
        var matriculaId = Guid.CreateVersion7();
        var mensagemErro = "Erro ao buscar aulas da matrícula";

        _appIdentityUserMock.Setup(x => x.GetUserId())
            .Returns(usuarioId);

        _alunoServiceMock
            .Setup(x => x.ObterAulasMatricula(usuarioId, matriculaId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception(mensagemErro));

        var query = new ObterAulasMatriculaQuery(matriculaId);

        // Act
        var resultado = await _handler.Handle(query, CancellationToken.None);

        // Assert
        resultado.IsSuccess.Should().BeFalse();
        resultado.Error.Should().NotBeNull();
        resultado.Error.Message.Should().Be(mensagemErro);

        _alunoServiceMock.Verify(
            x => x.ObterAulasMatricula(usuarioId, matriculaId, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
