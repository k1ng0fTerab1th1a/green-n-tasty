using FluentAssertions;
using Moq;
using Restaurant.Core.DTOs;
using Restaurant.Core.Errors;
using Restaurant.Core.Interfaces.Repositories;
using Restaurant.Core.Models;
using Restaurant.Core.Services;

namespace Restaurant.UnitTests.Services;

public class DishServiceTests
{
    private readonly Mock<IDishRepository> _repo;
    private readonly DishService _sut;

    public DishServiceTests()
    {
        _repo = new Mock<IDishRepository>(MockBehavior.Strict);
        _sut = new DishService(_repo.Object);
    }

    [Fact]
    public async Task GetPopularDishesAsync_ShouldCallRepository_AndReturnDishes()
    {
        var dishes = new List<Dish>
        {
            new() { Id = "dish-1", Name = "Truffle Pasta", Price = 32.0m, State = "ON" },
            new() { Id = "dish-2", Name = "Beef Tartare",  Price = 27.5m, State = "ON" }
        };

        _repo.Setup(r => r.GetPopularDishesAsync(It.IsAny<CancellationToken>()))
             .ReturnsAsync(dishes);

        var result = await _sut.GetPopularDishesAsync();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value[0].Id.Should().Be("dish-1");
        result.Value[1].Id.Should().Be("dish-2");

        _repo.Verify(r => r.GetPopularDishesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _repo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetPopularDishesAsync_WhenRepositoryReturnsEmpty_ShouldReturnEmptyList()
    {
        _repo.Setup(r => r.GetPopularDishesAsync(It.IsAny<CancellationToken>()))
             .ReturnsAsync(new List<Dish>());

        var result = await _sut.GetPopularDishesAsync();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();

        _repo.Verify(r => r.GetPopularDishesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _repo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetSpecialityDishesByLocationIdAsync_ShouldCallRepository_WithCorrectLocationId_AndReturnDishes()
    {
        var dishes = new List<Dish>
    {
        new() { Id = "dish-10", Name = "House Steak", Price = 45.0m, State = "ON" }
    };

        _repo.Setup(r => r.GetSpecialityDishesByLocationIdAsync("loc-5", It.IsAny<CancellationToken>()))
             .ReturnsAsync(dishes);

        var result = await _sut.GetSpecialityDishesByLocationIdAsync("loc-5");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value[0].Id.Should().Be("dish-10");
        result.Value[0].Name.Should().Be("House Steak");

        _repo.Verify(r => r.GetSpecialityDishesByLocationIdAsync("loc-5", It.IsAny<CancellationToken>()), Times.Once);
        _repo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetSpecialityDishesByLocationIdAsync_WhenRepositoryReturnsEmpty_ShouldReturnEmptyList()
    {
        _repo.Setup(r => r.GetSpecialityDishesByLocationIdAsync("loc-empty", It.IsAny<CancellationToken>()))
             .ReturnsAsync(new List<Dish>());

        var result = await _sut.GetSpecialityDishesByLocationIdAsync("loc-empty");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();

        _repo.Verify(r => r.GetSpecialityDishesByLocationIdAsync("loc-empty", It.IsAny<CancellationToken>()), Times.Once);
        _repo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetDishByIdAsync_WhenFound_ShouldCallRepository_AndReturnDish()
    {
        var dish = new Dish
        {
            Id = "dish-42",
            Name = "Wagyu Burger",
            DishType = "MAIN",
            Price = 55.0m,
            State = "ON"
        };

        _repo.Setup(r => r.GetDishByIdAsync("dish-42", It.IsAny<CancellationToken>()))
             .ReturnsAsync(dish);

        var result = await _sut.GetDishByIdAsync("dish-42");

        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be("dish-42");
        result.Value.Name.Should().Be("Wagyu Burger");
        result.Value.DishType.Should().Be("MAIN");
        result.Value.Price.Should().BeApproximately(55.0m, 0.001m);

        _repo.Verify(r => r.GetDishByIdAsync("dish-42", It.IsAny<CancellationToken>()), Times.Once);
        _repo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetDishByIdAsync_WhenNotFound_ShouldReturnNotFoundError()
    {
        _repo.Setup(r => r.GetDishByIdAsync("nonexistent", It.IsAny<CancellationToken>()))
             .ReturnsAsync((Dish?)null);

        var result = await _sut.GetDishByIdAsync("nonexistent");

        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Should().BeEquivalentTo(DishErrors.NotFound);

        _repo.Verify(r => r.GetDishByIdAsync("nonexistent", It.IsAny<CancellationToken>()), Times.Once);
        _repo.VerifyNoOtherCalls();
    }



    [Fact]
    public async Task GetMenuBriefDishesAsync_ShouldCallRepository_WithCorrectParams_AndReturnDtos()
    {
        var dtos = new List<DishBriefDTO>
    {
        new() { Id = "dish-m1", Name = "Caesar Salad", DishType = "SALAD",  Price = 9.5m  },
        new() { Id = "dish-m2", Name = "Margherita",   DishType = "PIZZA",  Price = 12.0m }
    };

        _repo.Setup(r => r.GetShortenedDishesAsync("SALAD", "price,asc", It.IsAny<CancellationToken>()))
             .ReturnsAsync(dtos);

        var result = await _sut.GetMenuBriefDishesAsync("SALAD", "price,asc");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value[0].Id.Should().Be("dish-m1");
        result.Value[1].Id.Should().Be("dish-m2");

        _repo.Verify(r => r.GetShortenedDishesAsync("SALAD", "price,asc", It.IsAny<CancellationToken>()), Times.Once);
        _repo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetMenuBriefDishesAsync_WithNullType_ShouldForwardNullToRepository()
    {
        _repo.Setup(r => r.GetShortenedDishesAsync(null, "name,desc", It.IsAny<CancellationToken>()))
             .ReturnsAsync(new List<DishBriefDTO>());

        var result = await _sut.GetMenuBriefDishesAsync(null, "name,desc");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();

        _repo.Verify(r => r.GetShortenedDishesAsync(null, "name,desc", It.IsAny<CancellationToken>()), Times.Once);
        _repo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetMenuBriefDishesAsync_WhenRepositoryReturnsEmpty_ShouldReturnEmptyList()
    {
        _repo.Setup(r => r.GetShortenedDishesAsync(It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync(new List<DishBriefDTO>());

        var result = await _sut.GetMenuBriefDishesAsync("MAIN", "price,asc");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();

        _repo.Verify(r => r.GetShortenedDishesAsync("MAIN", "price,asc", It.IsAny<CancellationToken>()), Times.Once);
        _repo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetPopularDishesAsync_ShouldReturnRepositoryResult()
    {
        var dishes = new List<Dish>
        {
            new() { Id = "dish-1", Name = "Truffle Pasta", Price = 32.0m, State = "ON" },
            new() { Id = "dish-2", Name = "Beef Tartare", Price = 27.5m, State = "ON" }
        };

        _repo.Setup(r => r.GetPopularDishesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(dishes);

        var result = await _sut.GetPopularDishesAsync(CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);

        _repo.VerifyAll();
    }

    [Fact]
    public async Task GetDishByIdAsync_WhenNotFound_ShouldReturnNotFound()
    {
        _repo.Setup(r => r.GetDishByIdAsync("missing", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Dish?)null);

        var result = await _sut.GetDishByIdAsync("missing", CancellationToken.None);

        result.IsFailed.Should().BeTrue();
        result.Errors[0].Should().BeEquivalentTo(DishErrors.NotFound);

        _repo.VerifyAll();
    }

    [Fact]
    public async Task GetMenuBriefDishesAsync_ShouldReturnRepositoryResult()
    {
        var dtos = new List<DishBriefDTO>
        {
            new() { Id = "dish-a", Name = "Caesar", DishType = "SALAD", Price = 9.5m },
            new() { Id = "dish-b", Name = "Margherita", DishType = "PIZZA", Price = 12m }
        };

        _repo.Setup(r => r.GetShortenedDishesAsync("SALAD", "price,asc", It.IsAny<CancellationToken>()))
            .ReturnsAsync(dtos);

        var result = await _sut.GetMenuBriefDishesAsync("SALAD", "price,asc", CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);

        _repo.VerifyAll();
    }

    [Fact]
    public async Task SearchDishesAsync_WhenQueryBlank_ShouldReturnEmptyAndNotCallRepository()
    {
        var result = await _sut.SearchDishesAsync("   ", null, 20, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();

        _repo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task RebuildSearchIndexAsync_ShouldReturnCount()
    {
        _repo.Setup(r => r.RebuildSearchIndexAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(17);

        var result = await _sut.RebuildSearchIndexAsync(CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(17);

        _repo.VerifyAll();
    }

    [Fact]
    public async Task UpsertDishSearchIndexAsync_WhenDishMissing_ShouldReturnNotFound()
    {
        _repo.Setup(r => r.GetDishByIdAsync("dish-x", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Dish?)null);

        var result = await _sut.UpsertDishSearchIndexAsync("dish-x", CancellationToken.None);

        result.IsFailed.Should().BeTrue();
        result.Errors[0].Should().BeEquivalentTo(DishErrors.NotFound);

        _repo.VerifyAll();
    }

    [Fact]
    public async Task UpsertDishSearchIndexAsync_WhenDishExists_ShouldCallRepository()
    {
        var dish = new Dish
        {
            Id = "dish-1",
            Name = "Селедка",
            DishType = "MAIN",
            Price = 12m,
            State = "ON"
        };

        _repo.Setup(r => r.GetDishByIdAsync("dish-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(dish);

        _repo.Setup(r => r.UpsertDishSearchIndexAsync(dish, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _sut.UpsertDishSearchIndexAsync("dish-1", CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        _repo.VerifyAll();
    }

    [Fact]
    public async Task RemoveDishSearchIndexAsync_ShouldCallRepository()
    {
        _repo.Setup(r => r.RemoveDishSearchIndexAsync("dish-1", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _sut.RemoveDishSearchIndexAsync("dish-1", CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        _repo.VerifyAll();
    }
}
