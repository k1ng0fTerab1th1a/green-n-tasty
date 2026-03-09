using Restaurant.Api.Contracts.Responses;
using Restaurant.Core.Models;

namespace Restaurant.Api.Mappers;

public static class DishMappers
{
    public static DishShortResponse ToShortResponse(this Dish dish)
    {
        if (dish == null) return null;

        return new DishShortResponse
        {
            Id = dish.Id,
            Name = dish.Name,
            PreviewImageUrl = dish.ImageUrl,
            Price = dish.Price,
            State = dish.State,
            Weight = dish.Weight
        };
    }
}
