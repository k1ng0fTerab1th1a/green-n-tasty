using Amazon.DynamoDBv2.DataModel;
using FluentAssertions;
using Restaurant.Core.Models;
using Restaurant.Infrastructure.Repositories;

namespace Restaurant.IntegrationTests.Infrastructure;

public sealed class DishRepositoryIntegrationTests : IClassFixture<DynamoDbFixture>
{
    private readonly DynamoDBContext _context;
    private readonly DishRepository _repo;
 
    public DishRepositoryIntegrationTests(DynamoDbFixture fixture)
    {
        _context = fixture.Context;
        _repo    = new DishRepository(fixture.Context, fixture.Client);
    }
    private static string DishId() => $"dish-{Guid.NewGuid():N}";
    
    [Fact]
    public async Task GetDishByIdAsync_WhenExists_ShouldReturnAllFields()
    {
        var id = DishId();
        var saved = new Dish
        {
            Id            = id,
            Name          = "Truffle Risotto",
            DishType      = "MAIN",
            Price         = 28.5f,
            State         = "ON",
            Description   = "Creamy risotto",
            ImageUrl      = "http://img/truffle",
            Weight        = 350,
            Calories      = 680,
            Proteins      = 12.0f,
            Fats          = 18.0f,
            Carbohydrates = 70.0f,
            Vitamins      = "B1, B6"
        };
 
        await _context.SaveAsync(saved);
 
        var result = await _repo.GetDishByIdAsync(id);
 
        result.Should().NotBeNull();
        result!.Id.Should().Be(id);
        result.Name.Should().Be("Truffle Risotto");
        result.DishType.Should().Be("MAIN");
        result.Price.Should().BeApproximately(28.5f, 0.001f);
        result.State.Should().Be("ON");
        result.Description.Should().Be("Creamy risotto");
        result.ImageUrl.Should().Be("http://img/truffle");
        result.Weight.Should().Be(350);
        result.Calories.Should().Be(680);
        result.Proteins.Should().BeApproximately(12.0f, 0.001f);
        result.Fats.Should().BeApproximately(18.0f, 0.001f);
        result.Carbohydrates.Should().BeApproximately(70.0f, 0.001f);
        result.Vitamins.Should().Be("B1, B6");
    }
 
    [Fact]
    public async Task GetDishByIdAsync_WhenNotFound_ShouldReturnNull()
    {
        var result = await _repo.GetDishByIdAsync($"dish-{Guid.NewGuid():N}");
 
        result.Should().BeNull();
    }
    
    [Fact]
    public async Task GetPopularDishesAsync_ShouldReturnOnlyDishesWithPopularityFlagTrue()
    {
        var popularId    = DishId();
        var nonPopularId = DishId();
 
        await _context.SaveAsync(new Dish
        {
            Id             = popularId,
            Name           = "Popular Burger",
            Price          = 14.0f,
            PopularityFlag = "true"   // indexed value the repo queries with
        });
 
        await _context.SaveAsync(new Dish
        {
            Id             = nonPopularId,
            Name           = "Unknown Soup",
            Price          = 6.0f,
            PopularityFlag = null     // not in index
        });
 
        var result = await _repo.GetPopularDishesAsync();
 
        result.Should().Contain(d => d.Id == popularId,
            "dish with popularityFlag='true' must be returned");
 
        result.Should().NotContain(d => d.Id == nonPopularId,
            "dish without popularityFlag must not be returned");
    }
 
    [Fact]
    public async Task GetPopularDishesAsync_ShouldReturnMultiplePopularDishes()
    {
        var id1 = DishId();
        var id2 = DishId();
 
        await _context.SaveAsync(new Dish { Id = id1, Name = "Pop A", Price = 10f, PopularityFlag = "true" });
        await _context.SaveAsync(new Dish { Id = id2, Name = "Pop B", Price = 12f, PopularityFlag = "true" });
 
        var result = await _repo.GetPopularDishesAsync();
 
        result.Should().Contain(d => d.Id == id1);
        result.Should().Contain(d => d.Id == id2);
    }
    
        [Fact]
    public async Task GetSpecialityDishesByLocationIdAsync_ShouldReturnOnlyDishesForThatLocation()
    {
        var locationId      = $"loc-{Guid.NewGuid():N}";
        var otherLocationId = $"loc-{Guid.NewGuid():N}";
 
        var specialityId = DishId();
        var otherId      = DishId();
 
        await _context.SaveAsync(new Dish
        {
            Id                    = specialityId,
            Name                  = "House Steak",
            Price                 = 35.0f,
            SpecialityForLocation = locationId
        });
 
        await _context.SaveAsync(new Dish
        {
            Id                    = otherId,
            Name                  = "Other Soup",
            Price                 = 8.0f,
            SpecialityForLocation = otherLocationId
        });
 
        var result = await _repo.GetSpecialityDishesByLocationIdAsync(locationId);
 
        result.Should().ContainSingle(d => d.Id == specialityId,
            "only the dish assigned to this location should be returned");
 
        result.Should().NotContain(d => d.Id == otherId);
    }
 
    [Fact]
    public async Task GetSpecialityDishesByLocationIdAsync_WhenNoSpecialities_ShouldReturnEmpty()
    {
        var locationId = $"loc-{Guid.NewGuid():N}";
 
        var result = await _repo.GetSpecialityDishesByLocationIdAsync(locationId);
 
        result.Should().BeEmpty();
    }
 
    [Fact]
    public async Task GetSpecialityDishesByLocationIdAsync_ShouldReturnMultipleDishesForSameLocation()
    {
        var locationId = $"loc-{Guid.NewGuid():N}";
 
        var id1 = DishId();
        var id2 = DishId();
 
        await _context.SaveAsync(new Dish { Id = id1, Name = "Dish Alpha", Price = 20f, SpecialityForLocation = locationId });
        await _context.SaveAsync(new Dish { Id = id2, Name = "Dish Beta",  Price = 22f, SpecialityForLocation = locationId });
 
        var result = await _repo.GetSpecialityDishesByLocationIdAsync(locationId);
 
        result.Should().HaveCount(2);
        result.Select(d => d.Id).Should().BeEquivalentTo(new[] { id1, id2 });
    }
    
    [Fact]
    public async Task GetShortenedDishesAsync_ShouldMapAllProjectedFields()
    {
        var id = DishId();
        await _context.SaveAsync(new Dish
        {
            Id       = id,
            Name     = "Mapped Dish",
            DishType = "DESSERT",
            Price    = 7.5f,
            State    = "ON",
            ImageUrl = "http://img/mapped",
            Weight   = 150
        });
 
        var result = await _repo.GetShortenedDishesAsync(type: null, sort: "price,asc");
 
        var item = result.FirstOrDefault(d => d.Id == id);
        item.Should().NotBeNull();
        item!.Name.Should().Be("Mapped Dish");
        item.DishType.Should().Be("DESSERT");
        item.Price.Should().BeApproximately(7.5f, 0.001f);
        item.State.Should().Be("ON");
        item.ImageUrl.Should().Be("http://img/mapped");
        item.Weight.Should().Be(150);
    }
 
    [Fact]
    public async Task GetShortenedDishesAsync_WhenImageUrlAndWeightAreNull_ShouldReturnNulls()
    {
        var id = DishId();
        await _context.SaveAsync(new Dish
        {
            Id       = id,
            Name     = "Minimal Dish",
            DishType = "STARTER",
            Price    = 4.0f,
            ImageUrl = null,
            Weight   = null
        });
 
        var result = await _repo.GetShortenedDishesAsync(type: null, sort: "price,asc");
 
        var item = result.FirstOrDefault(d => d.Id == id);
        item.Should().NotBeNull();
        item!.ImageUrl.Should().BeNull();
        item.Weight.Should().BeNull();
    }
    
    [Fact]
    public async Task GetShortenedDishesAsync_WithTypeFilter_ShouldReturnOnlyMatchingType()
    {
        var mainId    = DishId();
        var dessertId = DishId();
 
        await _context.SaveAsync(new Dish { Id = mainId,    Name = "Steak",      DishType = "MAIN",    Price = 30f });
        await _context.SaveAsync(new Dish { Id = dessertId, Name = "Ice Cream",  DishType = "DESSERT", Price = 6f  });
 
        var result = await _repo.GetShortenedDishesAsync(type: "MAIN", sort: "price,asc");
 
        result.Should().Contain(d => d.Id == mainId,
            "MAIN dish must be included");
 
        result.Should().NotContain(d => d.Id == dessertId,
            "DESSERT dish must be excluded by the type filter");
    }
 
    [Fact]
    public async Task GetShortenedDishesAsync_WithNullType_ShouldReturnAllDishes()
    {
        var id1 = DishId();
        var id2 = DishId();
 
        await _context.SaveAsync(new Dish { Id = id1, Name = "Soup A",    DishType = "STARTER", Price = 5f });
        await _context.SaveAsync(new Dish { Id = id2, Name = "Chicken B", DishType = "MAIN",    Price = 18f });
 
        var result = await _repo.GetShortenedDishesAsync(type: null, sort: "price,asc");
 
        result.Should().Contain(d => d.Id == id1);
        result.Should().Contain(d => d.Id == id2);
    }
    
        [Fact]
    public async Task GetShortenedDishesAsync_WithPriceAsc_ShouldReturnDishesOrderedByPriceAscending()
    {
        var prefix = Guid.NewGuid().ToString("N")[..8];
 
        // Use a distinct type so we can filter to just these three dishes
        var uniqueType = $"TYPE_{prefix}";
 
        await _context.SaveAsync(new Dish { Id = DishId(), Name = $"{prefix}_Mid",  DishType = uniqueType, Price = 15f });
        await _context.SaveAsync(new Dish { Id = DishId(), Name = $"{prefix}_High", DishType = uniqueType, Price = 30f });
        await _context.SaveAsync(new Dish { Id = DishId(), Name = $"{prefix}_Low",  DishType = uniqueType, Price = 5f  });
 
        var result = await _repo.GetShortenedDishesAsync(type: uniqueType, sort: "price,asc");
 
        result.Should().HaveCount(3);
        result.Select(d => d.Price).Should().BeInAscendingOrder();
    }
 
    [Fact]
    public async Task GetShortenedDishesAsync_WithPriceDesc_ShouldReturnDishesOrderedByPriceDescending()
    {
        var prefix     = Guid.NewGuid().ToString("N")[..8];
        var uniqueType = $"TYPE_{prefix}";
 
        await _context.SaveAsync(new Dish { Id = DishId(), Name = $"{prefix}_A", DishType = uniqueType, Price = 10f });
        await _context.SaveAsync(new Dish { Id = DishId(), Name = $"{prefix}_B", DishType = uniqueType, Price = 25f });
        await _context.SaveAsync(new Dish { Id = DishId(), Name = $"{prefix}_C", DishType = uniqueType, Price = 3f  });
 
        var result = await _repo.GetShortenedDishesAsync(type: uniqueType, sort: "price,desc");
 
        result.Should().HaveCount(3);
        result.Select(d => d.Price).Should().BeInDescendingOrder();
    }
 
    [Fact]
    public async Task GetShortenedDishesAsync_WithNameAsc_ShouldReturnDishesOrderedByNameAscending()
    {
        var prefix     = Guid.NewGuid().ToString("N")[..8];
        var uniqueType = $"TYPE_{prefix}";
 
        await _context.SaveAsync(new Dish { Id = DishId(), Name = $"{prefix}_Zucchini",  DishType = uniqueType, Price = 10f });
        await _context.SaveAsync(new Dish { Id = DishId(), Name = $"{prefix}_Artichoke", DishType = uniqueType, Price = 12f });
        await _context.SaveAsync(new Dish { Id = DishId(), Name = $"{prefix}_Mango",     DishType = uniqueType, Price = 8f  });
 
        var result = await _repo.GetShortenedDishesAsync(type: uniqueType, sort: "name,asc");
 
        result.Should().HaveCount(3);
        result.Select(d => d.Name).Should().BeInAscendingOrder();
    }
 
    [Fact]
    public async Task GetShortenedDishesAsync_WithUnknownSortProperty_ShouldFallBackToNameAscending()
    {
        var prefix     = Guid.NewGuid().ToString("N")[..8];
        var uniqueType = $"TYPE_{prefix}";
 
        await _context.SaveAsync(new Dish { Id = DishId(), Name = $"{prefix}_Ziti",   DishType = uniqueType, Price = 9f });
        await _context.SaveAsync(new Dish { Id = DishId(), Name = $"{prefix}_Apple",  DishType = uniqueType, Price = 4f });
 
        var result = await _repo.GetShortenedDishesAsync(type: uniqueType, sort: "unknown,asc");
 
        result.Should().HaveCount(2);
        result.Select(d => d.Name).Should().BeInAscendingOrder(
            "unknown sort property should fall back to name ascending");
    }
}