namespace Restaurant.Core.Models;

public sealed class DishPopularityIncrement
{
    public string DishId { get; init; } = null!;
    public int Quantity { get; init; }
}

public sealed class FinishReservationOutcome
{
    public bool IsSuccess { get; init; }
    public bool OrderWasCompleted { get; init; }
    public IReadOnlyList<DishPopularityIncrement> PopularityIncrements { get; init; } = [];
}
