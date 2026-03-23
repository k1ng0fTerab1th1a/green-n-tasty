using FluentAssertions;
using Moq;
using Restaurant.Core.DTOs;
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
        _sut  = new DishService(_repo.Object);
    }



    [Fact]
    public async Task GetPopularDishesAsync_ShouldCallRepository_AndReturnDishes()
    {
        var dishes = new List<Dish>
        {
            new() { Id = "dish-1", Name = "Truffle Pasta", Price = 32.0f, State = "ON" },
            new() { Id = "dish-2", Name = "Beef Tartare",  Price = 27.5f, State = "ON" }
        };

        _repo.Setup(r => r.GetPopularDishesAsync(It.IsAny<CancellationToken>()))
             .ReturnsAsync(dishes);

        var result = await _sut.GetPopularDishesAsync();

        result.Should().HaveCount(2);
        result[0].Id.Should().Be("dish-1");
        result[1].Id.Should().Be("dish-2");

        _repo.Verify(r => r.GetPopularDishesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _repo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetPopularDishesAsync_WhenRepositoryReturnsEmpty_ShouldReturnEmptyList()
    {
        _repo.Setup(r => r.GetPopularDishesAsync(It.IsAny<CancellationToken>()))
             .ReturnsAsync(new List<Dish>());

        var result = await _sut.GetPopularDishesAsync();

        result.Should().BeEmpty();

        _repo.Verify(r => r.GetPopularDishesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _repo.VerifyNoOtherCalls();
    }



    [Fact]
    public async Task GetSpecialityDishesByLocationIdAsync_ShouldCallRepository_WithCorrectLocationId_AndReturnDishes()
    {
        var dishes = new List<Dish>
        {
            new() { Id = "dish-10", Name = "House Steak", Price = 45.0f, State = "ON" }
        };

        _repo.Setup(r => r.GetSpecialityDishesByLocationIdAsync("loc-5", It.IsAny<CancellationToken>()))
             .ReturnsAsync(dishes);

        var result = await _sut.GetSpecialityDishesByLocationIdAsync("loc-5");

        result.Should().HaveCount(1);
        result[0].Id.Should().Be("dish-10");
        result[0].Name.Should().Be("House Steak");

        _repo.Verify(r => r.GetSpecialityDishesByLocationIdAsync("loc-5", It.IsAny<CancellationToken>()), Times.Once);
        _repo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetSpecialityDishesByLocationIdAsync_WhenRepositoryReturnsEmpty_ShouldReturnEmptyList()
    {
        _repo.Setup(r => r.GetSpecialityDishesByLocationIdAsync("loc-empty", It.IsAny<CancellationToken>()))
             .ReturnsAsync(new List<Dish>());

        var result = await _sut.GetSpecialityDishesByLocationIdAsync("loc-empty");

        result.Should().BeEmpty();

        _repo.Verify(r => r.GetSpecialityDishesByLocationIdAsync("loc-empty", It.IsAny<CancellationToken>()), Times.Once);
        _repo.VerifyNoOtherCalls();
    }



    [Fact]
    public async Task GetDishByIdAsync_WhenFound_ShouldCallRepository_AndReturnDish()
    {
        var dish = new Dish
        {
            Id       = "dish-42",
            Name     = "Wagyu Burger",
            DishType = "MAIN",
            Price    = 55.0f,
            State    = "ON"
        };

        _repo.Setup(r => r.GetDishByIdAsync("dish-42", It.IsAny<CancellationToken>()))
             .ReturnsAsync(dish);

        var result = await _sut.GetDishByIdAsync("dish-42");

        result.Should().NotBeNull();
        result!.Id.Should().Be("dish-42");
        result.Name.Should().Be("Wagyu Burger");
        result.DishType.Should().Be("MAIN");
        result.Price.Should().BeApproximately(55.0f, 0.001f);

        _repo.Verify(r => r.GetDishByIdAsync("dish-42", It.IsAny<CancellationToken>()), Times.Once);
        _repo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetDishByIdAsync_WhenNotFound_ShouldReturnNull()
    {
        _repo.Setup(r => r.GetDishByIdAsync("nonexistent", It.IsAny<CancellationToken>()))
             .ReturnsAsync((Dish?)null);

        var result = await _sut.GetDishByIdAsync("nonexistent");

        result.Should().BeNull();

        _repo.Verify(r => r.GetDishByIdAsync("nonexistent", It.IsAny<CancellationToken>()), Times.Once);
        _repo.VerifyNoOtherCalls();
    }



    [Fact]
    public async Task GetMenuBriefDishesAsync_ShouldCallRepository_WithCorrectParams_AndReturnDtos()
    {
        var dtos = new List<DishBriefDTO>
        {
            new() { Id = "dish-m1", Name = "Caesar Salad", DishType = "SALAD",  Price = 9.5f  },
            new() { Id = "dish-m2", Name = "Margherita",   DishType = "PIZZA",  Price = 12.0f }
        };

        _repo.Setup(r => r.GetShortenedDishesAsync("SALAD", "price,asc", It.IsAny<CancellationToken>()))
             .ReturnsAsync(dtos);

        var result = await _sut.GetMenuBriefDishesAsync("SALAD", "price,asc");

        result.Should().HaveCount(2);
        result[0].Id.Should().Be("dish-m1");
        result[1].Id.Should().Be("dish-m2");

        _repo.Verify(r => r.GetShortenedDishesAsync("SALAD", "price,asc", It.IsAny<CancellationToken>()), Times.Once);
        _repo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetMenuBriefDishesAsync_WithNullType_ShouldForwardNullToRepository()
    {
        _repo.Setup(r => r.GetShortenedDishesAsync(null, "name,desc", It.IsAny<CancellationToken>()))
             .ReturnsAsync(new List<DishBriefDTO>());

        var result = await _sut.GetMenuBriefDishesAsync(null, "name,desc");

        result.Should().BeEmpty();

        _repo.Verify(r => r.GetShortenedDishesAsync(null, "name,desc", It.IsAny<CancellationToken>()), Times.Once);
        _repo.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetMenuBriefDishesAsync_WhenRepositoryReturnsEmpty_ShouldReturnEmptyList()
    {
        _repo.Setup(r => r.GetShortenedDishesAsync(It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync(new List<DishBriefDTO>());

        var result = await _sut.GetMenuBriefDishesAsync("MAIN", "price,asc");

        result.Should().BeEmpty();

        _repo.Verify(r => r.GetShortenedDishesAsync("MAIN", "price,asc", It.IsAny<CancellationToken>()), Times.Once);
        _repo.VerifyNoOtherCalls();
    }
}