using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Peo.Core.DomainObjects;
using Peo.Core.Entities.Base;
using Peo.Core.Infra.Data.Repositories;
using Peo.Core.Interfaces.Data;

namespace Peo.Tests.UnitTests.Infrastructure;

public class GenericRepositoryTests : IDisposable
{
    private readonly TestDbContext _dbContext;
    private readonly GenericRepository<TestEntity, TestDbContext> _repository;

    public GenericRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
            .Options;

        _dbContext = new TestDbContext(options);
        _repository = new GenericRepository<TestEntity, TestDbContext>(_dbContext);
    }

    [Fact]
    public async Task GetAllAsync_DeveRetornarTodasEntidades()
    {
        // Arrange
        var entity1 = new TestEntity { Name = "Entity 1" };
        var entity2 = new TestEntity { Name = "Entity 2" };

        await _dbContext.TestEntities.AddRangeAsync(entity1, entity2);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _repository.GetAllAsync(CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(e => e.Name == "Entity 1");
        result.Should().Contain(e => e.Name == "Entity 2");
    }

    [Fact]
    public async Task GetAsync_DeveRetornarEntidadePorId()
    {
        // Arrange
        var entity = new TestEntity { Name = "Test Entity" };
        await _dbContext.TestEntities.AddAsync(entity);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _repository.GetAsync(entity.Id, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(entity.Id);
        result.Name.Should().Be("Test Entity");
    }

    [Fact]
    public async Task GetAsync_DeveRetornarNulo_QuandoEntidadeNaoExistir()
    {
        // Arrange
        var nonExistentId = Guid.CreateVersion7();

        // Act
        var result = await _repository.GetAsync(nonExistentId, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAsync_ComPredicate_DeveRetornarEntidadesFiltradas()
    {
        // Arrange
        var entity1 = new TestEntity { Name = "Active Entity" };
        var entity2 = new TestEntity { Name = "Inactive Entity" };

        await _dbContext.TestEntities.AddRangeAsync(entity1, entity2);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _repository.GetAsync(e => e.Name.Contains("Active"), CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result!.First().Name.Should().Be("Active Entity");
    }

    [Fact]
    public void Insert_DeveAdicionarEntidade()
    {
        // Arrange
        var entity = new TestEntity { Name = "New Entity" };

        // Act
        _repository.Insert(entity, CancellationToken.None);
        _dbContext.SaveChanges();

        // Assert
        var savedEntity = _dbContext.TestEntities.Find(entity.Id);
        savedEntity.Should().NotBeNull();
        savedEntity!.Name.Should().Be("New Entity");
    }

    [Fact]
    public void Update_DeveAtualizarEntidade()
    {
        // Arrange
        var entity = new TestEntity { Name = "Original Name" };
        _dbContext.TestEntities.Add(entity);
        _dbContext.SaveChanges();

        // Detach para simular uma nova referência
        _dbContext.Entry(entity).State = EntityState.Detached;

        entity.Name = "Updated Name";

        // Act
        _repository.Update(entity, CancellationToken.None);
        _dbContext.SaveChanges();

        // Assert
        var updatedEntity = _dbContext.TestEntities.Find(entity.Id);
        updatedEntity.Should().NotBeNull();
        updatedEntity!.Name.Should().Be("Updated Name");
    }

    [Fact]
    public async Task AnyAsync_DeveRetornarTrue_QuandoExistirEntidadeComPredicate()
    {
        // Arrange
        var entity = new TestEntity { Name = "Specific Entity" };
        await _dbContext.TestEntities.AddAsync(entity);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _repository.AnyAsync(e => e.Name == "Specific Entity", CancellationToken.None);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task AnyAsync_DeveRetornarFalse_QuandoNaoExistirEntidadeComPredicate()
    {
        // Arrange & Act
        var result = await _repository.AnyAsync(e => e.Name == "Non-existent", CancellationToken.None);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task AddRangeAsync_DeveAdicionarMultiplasEntidades()
    {
        // Arrange
        var entities = new List<TestEntity>
        {
            new TestEntity { Name = "Entity 1" },
            new TestEntity { Name = "Entity 2" },
            new TestEntity { Name = "Entity 3" }
        };

        // Act
        await _repository.AddRangeAsync(entities, CancellationToken.None);
        await _dbContext.SaveChangesAsync();

        // Assert
        var count = await _dbContext.TestEntities.CountAsync();
        count.Should().Be(3);
    }

    [Fact]
    public void WithTracking_DeveHabilitarTracking()
    {
        // Act
        var result = _repository.WithTracking();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeAssignableTo<IRepository<TestEntity>>();
    }

    [Fact]
    public void WithoutTracking_DeveDesabilitarTracking()
    {
        // Act
        var result = _repository.WithoutTracking();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeAssignableTo<IRepository<TestEntity>>();
    }

    [Fact]
    public async Task GetAllAsync_SemTracking_DeveRetornarEntidadesDesanexadas()
    {
        // Arrange
        var entity = new TestEntity { Name = "Test" };
        await _dbContext.TestEntities.AddAsync(entity);
        await _dbContext.SaveChangesAsync();

        _repository.WithoutTracking();

        // Act
        var result = await _repository.GetAllAsync(CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        var retrievedEntity = result.First();
        _dbContext.Entry(retrievedEntity).State.Should().Be(EntityState.Detached);
    }

    public void Dispose()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
    }

    // Test Entity
    public class TestEntity : EntityBase, IAggregateRoot
    {
        public string Name { get; set; } = string.Empty;
    }

    // Test DbContext
    public class TestDbContext : DbContext, IUnitOfWork
    {
        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }

        public DbSet<TestEntity> TestEntities { get; set; } = null!;

        public async Task<int> CommitAsync(CancellationToken cancellationToken = default)
        {
            return await SaveChangesAsync(cancellationToken);
        }
    }
}
