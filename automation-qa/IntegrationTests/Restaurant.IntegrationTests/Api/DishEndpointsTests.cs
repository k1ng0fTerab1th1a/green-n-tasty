using System.Net;
using System.Text.Json;
using FluentAssertions;
using Restaurant.Api.Tests;
using Restaurant.Core.DTOs;
using Restaurant.Core.Models;

namespace Restaurant.IntegrationTests.Api;

public class DishEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;
 
    public DishEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client  = factory.CreateClient();
    } 
    
    [Fact]
    public async Task GetPopularDishes_ShouldReturn200_AndMapAllFields()
    {
        _factory.DishService.Reset();
        _factory.DishService.PopularDishes.Add(new Dish
        {
            Id       = "dish-99",
            Name     = "Truffle Pasta",
            ImageUrl = "http://img/dish-99",
            Price    = 38.0m,
            State    = "ON",
            Weight   = 280
        });
 
        var res = await _client.GetAsync("/dishes/popular");
 
        res.StatusCode.Should().Be(HttpStatusCode.OK);
 
        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
        doc.RootElement.GetPropertyIgnoreCase("isSuccess").GetBoolean().Should().BeTrue();
 
        var data = doc.RootElement.GetPropertyIgnoreCase("data");
        data.ValueKind.Should().Be(JsonValueKind.Array);
        data.GetArrayLength().Should().Be(1);
 
        var item = data[0];
        item.GetPropertyIgnoreCase("id").GetString().Should().Be("dish-99");
        item.GetPropertyIgnoreCase("name").GetString().Should().Be("Truffle Pasta");
        item.GetPropertyIgnoreCase("previewImageUrl").GetString().Should().Be("http://img/dish-99");
        item.GetPropertyIgnoreCase("price").GetDouble().Should().BeApproximately(38.0, 0.001);
        item.GetPropertyIgnoreCase("state").GetString().Should().Be("ON");
        item.GetPropertyIgnoreCase("weight").GetInt32().Should().Be(280);
    }
 
    [Fact]
    public async Task GetPopularDishes_WhenNoDishes_ShouldReturn200_WithEmptyArray()
    {
        _factory.DishService.Reset();
        // PopularDishes is already empty after Reset()
 
        var res = await _client.GetAsync("/dishes/popular");
 
        res.StatusCode.Should().Be(HttpStatusCode.OK);
 
        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
        doc.RootElement.GetPropertyIgnoreCase("data")
            .GetArrayLength().Should().Be(0);
    }
    
    
        [Fact]
    public async Task GetDishById_WhenFound_ShouldReturn200_AndMapAllFields()
    {
        _factory.DishService.Reset();
        _factory.DishService.DishesById["dish-7"] = new Dish
        {
            Id             = "dish-7",
            Name           = "Beef Burger",
            DishType       = "MAIN",
            Price          = 15.9m,
            State          = "ON",
            Description    = "Juicy beef burger",
            ImageUrl       = "http://img/dish-7",
            Weight         = 350,
            Calories       = 620,
            Proteins       = 38.5f,
            Fats           = 22.0f,
            Carbohydrates  = 45.0f,
            Vitamins       = "B12, C"
        };
 
        var res = await _client.GetAsync("/dishes/dish-7");
 
        res.StatusCode.Should().Be(HttpStatusCode.OK);
        _factory.DishService.LastDishId.Should().Be("dish-7");
 
        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
        doc.RootElement.GetPropertyIgnoreCase("isSuccess").GetBoolean().Should().BeTrue();
 
        var data = doc.RootElement.GetPropertyIgnoreCase("data");
        data.GetPropertyIgnoreCase("id").GetString().Should().Be("dish-7");
        data.GetPropertyIgnoreCase("name").GetString().Should().Be("Beef Burger");
        data.GetPropertyIgnoreCase("dishType").GetString().Should().Be("MAIN");
        data.GetPropertyIgnoreCase("price").GetDouble().Should().BeApproximately(15.9, 0.001);
        data.GetPropertyIgnoreCase("state").GetString().Should().Be("ON");
        data.GetPropertyIgnoreCase("description").GetString().Should().Be("Juicy beef burger");
        data.GetPropertyIgnoreCase("imageUrl").GetString().Should().Be("http://img/dish-7");
        data.GetPropertyIgnoreCase("weight").GetInt32().Should().Be(350);
        data.GetPropertyIgnoreCase("calories").GetInt32().Should().Be(620);
        data.GetPropertyIgnoreCase("proteins").GetDouble().Should().BeApproximately(38.5, 0.01);
        data.GetPropertyIgnoreCase("fats").GetDouble().Should().BeApproximately(22.0, 0.01);
        data.GetPropertyIgnoreCase("carbohydrates").GetDouble().Should().BeApproximately(45.0, 0.01);
        data.GetPropertyIgnoreCase("vitamins").GetString().Should().Be("B12, C");
    }
 
    [Fact]
    public async Task GetDishById_WhenNotFound_ShouldReturn404()
    {
        _factory.DishService.Reset();
 
        var res = await _client.GetAsync("/dishes/nonexistent-id");
 
        res.StatusCode.Should().Be(HttpStatusCode.NotFound);
        _factory.DishService.LastDishId.Should().Be("nonexistent-id");
    }
    
    
        [Fact]
    public async Task GetMenuDishes_WithDefaultParams_ShouldReturn200_AndUseDefaultSort()
    {
        _factory.DishService.Reset();
        _factory.DishService.MenuDishes.Add(new DishBriefDTO
        {
            Id       = "dish-m1",
            Name     = "Caesar Salad",
            DishType = "SALAD",
            Price    = 9.5m,
            ImageUrl = "http://img/dish-m1",
            Weight   = 200
        });
 
        var res = await _client.GetAsync("/dishes/menu");
 
        res.StatusCode.Should().Be(HttpStatusCode.OK);
        _factory.DishService.LastMenuType.Should().BeNull();
        _factory.DishService.LastMenuSort.Should().Be("price,asc");
 
        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
        doc.RootElement.GetPropertyIgnoreCase("isSuccess").GetBoolean().Should().BeTrue();
 
        var data = doc.RootElement.GetPropertyIgnoreCase("data");
        data.ValueKind.Should().Be(JsonValueKind.Array);
        data.GetArrayLength().Should().Be(1);
 
        var item = data[0];
        item.GetPropertyIgnoreCase("id").GetString().Should().Be("dish-m1");
        item.GetPropertyIgnoreCase("name").GetString().Should().Be("Caesar Salad");
        item.GetPropertyIgnoreCase("dishType").GetString().Should().Be("SALAD");
        item.GetPropertyIgnoreCase("price").GetDouble().Should().BeApproximately(9.5, 0.001);
        item.GetPropertyIgnoreCase("imageUrl").GetString().Should().Be("http://img/dish-m1");
        item.GetPropertyIgnoreCase("weight").GetInt32().Should().Be(200);
    }
 
    [Fact]
    public async Task GetMenuDishes_WithTypeFilter_ShouldPassTypeToService()
    {
        _factory.DishService.Reset();
 
        var res = await _client.GetAsync("/dishes/menu?type=SALAD");
 
        res.StatusCode.Should().Be(HttpStatusCode.OK);
        _factory.DishService.LastMenuType.Should().Be("SALAD");
        _factory.DishService.LastMenuSort.Should().Be("price,asc");
 
        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
        doc.RootElement.GetPropertyIgnoreCase("data")
            .GetArrayLength().Should().Be(0);
    }
 
    [Fact]
    public async Task GetMenuDishes_WithExplicitSort_ShouldPassSortToService()
    {
        _factory.DishService.Reset();
 
        var res = await _client.GetAsync("/dishes/menu?sort=name,desc");
 
        res.StatusCode.Should().Be(HttpStatusCode.OK);
        _factory.DishService.LastMenuSort.Should().Be("name,desc");
        _factory.DishService.LastMenuType.Should().BeNull();
    }
 
    [Fact]
    public async Task GetMenuDishes_WithTypeAndSort_ShouldPassBothToService()
    {
        _factory.DishService.Reset();
        _factory.DishService.MenuDishes.AddRange(new[]
        {
            new DishBriefDTO { Id = "dish-a", Name = "Zucchini", DishType = "MAIN", Price = 12.0m },
            new DishBriefDTO { Id = "dish-b", Name = "Avocado Toast", DishType = "MAIN", Price = 8.5m }
        });
 
        var res = await _client.GetAsync("/dishes/menu?type=MAIN&sort=price,desc");
 
        res.StatusCode.Should().Be(HttpStatusCode.OK);
        _factory.DishService.LastMenuType.Should().Be("MAIN");
        _factory.DishService.LastMenuSort.Should().Be("price,desc");
 
        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
        doc.RootElement.GetPropertyIgnoreCase("data")
            .GetArrayLength().Should().Be(2);
    }
 
    [Fact]
    public async Task GetMenuDishes_WhenEmpty_ShouldReturn200_WithEmptyArray()
    {
        _factory.DishService.Reset();
 
        var res = await _client.GetAsync("/dishes/menu");
 
        res.StatusCode.Should().Be(HttpStatusCode.OK);
 
        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
        doc.RootElement.GetPropertyIgnoreCase("data")
            .GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task SearchDishes_WithoutUserHeader_ShouldReturn401()
    {
        _factory.DishService.Reset();

        var res = await _client.GetAsync("/dishes/search?query=селед");

        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        _factory.DishService.LastSearchQuery.Should().BeNull();
    }

    [Fact]
    public async Task SearchDishes_AsWaiter_ShouldReturn200_AndPassParameters()
    {
        _factory.DishService.Reset();
        _factory.DishService.SearchResults.AddRange(new[]
        {
        new DishSearchResultDTO
        {
            Id = "dish-1",
            Name = "Селедка під шубою",
            DishType = "MAIN"
        }
    });

        var req = new HttpRequestMessage(HttpMethod.Get, "/dishes/search?query=селед&type=MAIN&limit=15");
        req.Headers.Add("X-User-Id", "waiter-1");
        req.Headers.Add("X-Role", "WAITER");

        var res = await _client.SendAsync(req);

        res.StatusCode.Should().Be(HttpStatusCode.OK);
        _factory.DishService.LastSearchQuery.Should().Be("селед");
        _factory.DishService.LastSearchType.Should().Be("MAIN");
        _factory.DishService.LastSearchLimit.Should().Be(15);
    }

    [Fact]
    public async Task SearchDishes_AsNonWaiter_ShouldReturn403()
    {
        _factory.DishService.Reset();

        var req = new HttpRequestMessage(HttpMethod.Get, "/dishes/search?query=селед");
        req.Headers.Add("X-User-Id", "customer-1");
        req.Headers.Add("X-Role", "CUSTOMER");

        var res = await _client.SendAsync(req);

        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        _factory.DishService.LastSearchQuery.Should().BeNull();
    }

    [Fact]
    public async Task GetMenuDishes_WithPopularitySort_ShouldPassSortToService()
    {
        _factory.DishService.Reset();

        var res = await _client.GetAsync("/dishes/menu?sort=popularity,desc");

        res.StatusCode.Should().Be(HttpStatusCode.OK);
        _factory.DishService.LastMenuSort.Should().Be("popularity,desc");
    }

    [Fact]
    public async Task SearchDishes_WhenLimitOmitted_ShouldUseDefaultLimit()
    {
        _factory.DishService.Reset();

        var req = new HttpRequestMessage(HttpMethod.Get, "/dishes/search?query=селед");
        req.Headers.Add("X-User-Id", "waiter-1");
        req.Headers.Add("X-Role", "WAITER");

        var res = await _client.SendAsync(req);

        res.StatusCode.Should().Be(HttpStatusCode.OK);
        _factory.DishService.LastSearchQuery.Should().Be("селед");
        _factory.DishService.LastSearchLimit.Should().Be(20);
        _factory.DishService.LastSearchType.Should().BeNull();
    }
}