using FluentAssertions;
using MediatR;
using Moq;
using Peo.Core.Interfaces.Data;
using Peo.GestaoConteudo.Application.UseCases.Curso.Atualizar;
using Peo.GestaoConteudo.Domain.Entities;
using Peo.GestaoConteudo.Domain.ValueObjects;

namespace Peo.Tests.UnitTests.GestaoConteudo.Curso;

public class AtualizarCursoCommandHandlerTests
{
    private readonly Mock<IRepository<Peo.GestaoConteudo.Domain.Entities.Curso>> _cursoRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly AtualizarCursoCommandHandler _handler;

    public AtualizarCursoCommandHandlerTests()
    {
        _cursoRepositoryMock = new Mock<IRepository<Peo.GestaoConteudo.Domain.Entities.Curso>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _handler = new AtualizarCursoCommandHandler(_cursoRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_DeveAtualizarCurso_QuandoCursoExistir()
    {
        // Arrange
        var cursoId = Guid.CreateVersion7();
        var tituloOriginal = "Curso Original";
        var descricaoOriginal = "Descrição Original";
        var novoTitulo = "Curso Atualizado";
        var novaDescricao = "Descrição Atualizada";

        var curso = new Peo.GestaoConteudo.Domain.Entities.Curso(
            tituloOriginal,
            descricaoOriginal,
            "Instrutor Teste",
            new ConteudoProgramatico("Ementa completa do curso"),
            100m,
            true,
            DateTime.Now,
            new List<string> { "Tag1" },
            new List<Peo.GestaoConteudo.Domain.Entities.Aula>()
        );

        _cursoRepositoryMock
            .Setup(x => x.WithTracking())
            .Returns(_cursoRepositoryMock.Object);

        _cursoRepositoryMock
            .Setup(x => x.GetAsync(cursoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(curso);

        _cursoRepositoryMock
            .Setup(x => x.UnitOfWork)
            .Returns(_unitOfWorkMock.Object);

        _unitOfWorkMock
            .Setup(x => x.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new AtualizarCursoCommand
        {
            Id = cursoId,
            Titulo = novoTitulo,
            Descricao = novaDescricao,
            Preco = 150m,
            EstaPublicado = true,
            Tags = new List<string> { "Tag2" }
        };

        // Act
        var resultado = await _handler.Handle(command, CancellationToken.None);

        // Assert
        resultado.Should().Be(Unit.Value);
        curso.Titulo.Should().Be(novoTitulo);
        curso.Descricao.Should().Be(novaDescricao);

        _cursoRepositoryMock.Verify(
            x => x.GetAsync(cursoId, It.IsAny<CancellationToken>()),
            Times.Once);

        _cursoRepositoryMock.Verify(
            x => x.Update(curso, It.IsAny<CancellationToken>()),
            Times.Once);

        _unitOfWorkMock.Verify(
            x => x.CommitAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_DeveRetornarUnit_QuandoCursoNaoExistir()
    {
        // Arrange
        var cursoId = Guid.CreateVersion7();

        _cursoRepositoryMock
            .Setup(x => x.WithTracking())
            .Returns(_cursoRepositoryMock.Object);

        _cursoRepositoryMock
            .Setup(x => x.GetAsync(cursoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Peo.GestaoConteudo.Domain.Entities.Curso?)null);

        var command = new AtualizarCursoCommand
        {
            Id = cursoId,
            Titulo = "Novo Título",
            Descricao = "Nova Descrição"
        };

        // Act
        var resultado = await _handler.Handle(command, CancellationToken.None);

        // Assert
        resultado.Should().Be(Unit.Value);

        _cursoRepositoryMock.Verify(
            x => x.GetAsync(cursoId, It.IsAny<CancellationToken>()),
            Times.Once);

        _cursoRepositoryMock.Verify(
            x => x.Update(It.IsAny<Peo.GestaoConteudo.Domain.Entities.Curso>(), It.IsAny<CancellationToken>()),
            Times.Never);

        _unitOfWorkMock.Verify(
            x => x.CommitAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_DeveAtualizarApenasTituloEDescricao_QuandoCursoExistir()
    {
        // Arrange
        var cursoId = Guid.CreateVersion7();
        var precoOriginal = 100m;
        var publicadoOriginal = true;
        var novoTitulo = "Título Atualizado";
        var novaDescricao = "Descrição Atualizada";

        var curso = new Peo.GestaoConteudo.Domain.Entities.Curso(
            "Título Original",
            "Descrição Original",
            "Instrutor Teste",
            new ConteudoProgramatico("Ementa completa"),
            precoOriginal,
            publicadoOriginal,
            DateTime.Now,
            new List<string> { "Tag1" },
            new List<Peo.GestaoConteudo.Domain.Entities.Aula>()
        );

        _cursoRepositoryMock
            .Setup(x => x.WithTracking())
            .Returns(_cursoRepositoryMock.Object);

        _cursoRepositoryMock
            .Setup(x => x.GetAsync(cursoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(curso);

        _cursoRepositoryMock
            .Setup(x => x.UnitOfWork)
            .Returns(_unitOfWorkMock.Object);

        _unitOfWorkMock
            .Setup(x => x.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new AtualizarCursoCommand
        {
            Id = cursoId,
            Titulo = novoTitulo,
            Descricao = novaDescricao,
            Preco = 200m, // Não será atualizado
            EstaPublicado = false, // Não será atualizado
            Tags = new List<string> { "Tag2" } // Não será atualizado
        };

        // Act
        var resultado = await _handler.Handle(command, CancellationToken.None);

        // Assert
        resultado.Should().Be(Unit.Value);
        curso.Titulo.Should().Be(novoTitulo);
        curso.Descricao.Should().Be(novaDescricao);
        curso.Preco.Should().Be(precoOriginal); // Mantém valor original
        curso.EstaPublicado.Should().Be(publicadoOriginal); // Mantém valor original

        _cursoRepositoryMock.Verify(
            x => x.Update(curso, It.IsAny<CancellationToken>()),
            Times.Once);

        _unitOfWorkMock.Verify(
            x => x.CommitAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
