using FluentAssertions;
using Moq;
using Peo.Core.Interfaces.Data;
using Peo.GestaoConteudo.Application.UseCases.Curso.ObterConteudoProgramatico;
using Peo.GestaoConteudo.Domain.Entities;
using Peo.GestaoConteudo.Domain.ValueObjects;

namespace Peo.Tests.UnitTests.GestaoConteudo.Curso;

public class ObterConteudoProgramaticoHandlerTests
{
    private readonly Mock<IRepository<Peo.GestaoConteudo.Domain.Entities.Curso>> _cursoRepositoryMock;
    private readonly Handler _handler;

    public ObterConteudoProgramaticoHandlerTests()
    {
        _cursoRepositoryMock = new Mock<IRepository<Peo.GestaoConteudo.Domain.Entities.Curso>>();
        _handler = new Handler(_cursoRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_DeveRetornarConteudoProgramatico_QuandoCursoExistir()
    {
        // Arrange
        var cursoId = Guid.CreateVersion7();
        var conteudo = "Ementa completa do curso de .NET com DDD e CQRS";
        var conteudoProgramatico = new ConteudoProgramatico(conteudo);

        var curso = new Peo.GestaoConteudo.Domain.Entities.Curso(
            "Curso de .NET Avançado",
            "Descrição do curso",
            "Instrutor Expert",
            conteudoProgramatico,
            200m,
            true,
            DateTime.Now,
            new List<string> { "DDD", "CQRS", ".NET" },
            new List<Peo.GestaoConteudo.Domain.Entities.Aula>()
        );

        _cursoRepositoryMock
            .Setup(x => x.GetAsync(cursoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(curso);

        var query = new Query(cursoId);

        // Act
        var resultado = await _handler.Handle(query, CancellationToken.None);

        // Assert
        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Should().NotBeNull();
        resultado.Value.ConteudoProgramatico.Should().NotBeNull();
        resultado.Value.ConteudoProgramatico!.ConteudoProgramatico.Should().NotBeNull();
        resultado.Value.ConteudoProgramatico!.ConteudoProgramatico!.Conteudo.Should().Be(conteudo);

        _cursoRepositoryMock.Verify(
            x => x.GetAsync(cursoId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_DeveRetornarConteudoNulo_QuandoCursoNaoTiverConteudoProgramatico()
    {
        // Arrange
        var cursoId = Guid.CreateVersion7();

        var curso = new Peo.GestaoConteudo.Domain.Entities.Curso(
            "Curso Básico",
            "Descrição",
            "Instrutor",
            null, // Sem conteúdo programático
            100m,
            false,
            null,
            new List<string>(),
            new List<Peo.GestaoConteudo.Domain.Entities.Aula>()
        );

        _cursoRepositoryMock
            .Setup(x => x.GetAsync(cursoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(curso);

        var query = new Query(cursoId);

        // Act
        var resultado = await _handler.Handle(query, CancellationToken.None);

        // Assert
        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Should().NotBeNull();
        resultado.Value.ConteudoProgramatico.Should().NotBeNull();
        resultado.Value.ConteudoProgramatico!.ConteudoProgramatico.Should().BeNull();

        _cursoRepositoryMock.Verify(
            x => x.GetAsync(cursoId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_DeveRetornarConteudoComTextoVazio_QuandoConteudoForVazio()
    {
        // Arrange
        var cursoId = Guid.CreateVersion7();
        var conteudoProgramatico = new ConteudoProgramatico(string.Empty);

        var curso = new Peo.GestaoConteudo.Domain.Entities.Curso(
            "Curso com Conteúdo Vazio",
            "Descrição",
            "Instrutor",
            conteudoProgramatico,
            100m,
            true,
            DateTime.Now,
            new List<string>(),
            new List<Peo.GestaoConteudo.Domain.Entities.Aula>()
        );

        _cursoRepositoryMock
            .Setup(x => x.GetAsync(cursoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(curso);

        var query = new Query(cursoId);

        // Act
        var resultado = await _handler.Handle(query, CancellationToken.None);

        // Assert
        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Should().NotBeNull();
        resultado.Value.ConteudoProgramatico.Should().NotBeNull();
        resultado.Value.ConteudoProgramatico!.ConteudoProgramatico.Should().NotBeNull();
        resultado.Value.ConteudoProgramatico!.ConteudoProgramatico!.Conteudo.Should().BeEmpty();

        _cursoRepositoryMock.Verify(
            x => x.GetAsync(cursoId, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
