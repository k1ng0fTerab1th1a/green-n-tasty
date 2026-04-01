using Moq;
using Restaurant.Core.DTOs;
using Restaurant.Core.Interfaces.Repositories;
using Restaurant.Core.Models;
using Restaurant.Core.Services;

namespace Restaurant.UnitTests.Services;

public sealed partial class ReservationServiceTests
{
    private readonly Mock<IReservationRepository> _repo;
    private readonly Mock<IWaiterScheduleRepository> _waiterScheduleRepo;
    private readonly Mock<ILocationRepository> _locationRepo;
    private readonly Mock<ITableRepository> _tableRepo;
    private readonly Mock<IUserRepository> _userRepo;
    private readonly Mock<IDishRepository> _dishRepo;
    private readonly ReservationService _sut;

    public ReservationServiceTests()
    {
        _repo = new Mock<IReservationRepository>(MockBehavior.Strict);
        _waiterScheduleRepo = new Mock<IWaiterScheduleRepository>(MockBehavior.Strict);
        _locationRepo = new Mock<ILocationRepository>(MockBehavior.Strict);
        _tableRepo = new Mock<ITableRepository>(MockBehavior.Strict);
        _userRepo = new Mock<IUserRepository>(MockBehavior.Strict);
        _dishRepo = new Mock<IDishRepository>(MockBehavior.Strict);

        _sut = new ReservationService(
            _repo.Object,
            _waiterScheduleRepo.Object,
            _locationRepo.Object,
            _tableRepo.Object,
            _userRepo.Object,
            _dishRepo.Object);
    }

    private static CreateReservationDTO BuildDto(DateOnly date, TimeOnly from, TimeOnly to)
        => new("loc-1", 3, date, from, to, 2);

    private static Location BuildLocation(string openTime = "10:00", string closeTime = "22:00")
        => new()
        {
            Id = "loc-1",
            Address = "Main street 1",
            Description = "Test location",
            TimeZone = "UTC",
            OpenTime = openTime,
            CloseTime = closeTime,
            ImageUrl = "http://img/loc-1",
            TotalCapacity = 120,
            AverageOccupancy = 0.35,
            TotalRating = 464,
            FeedbacksAmount = 100
        };

    private static User BuildWaiter(string userId)
        => new()
        {
            UserId = userId,
            FirstName = "Waiter",
            LastName = "User",
            Email = "waiter@example.com",
            Role = "WAITER"
        };

    private static User BuildCustomer(string userId)
        => new()
        {
            UserId = userId,
            FirstName = "Customer",
            LastName = "User",
            Email = "customer@example.com",
            Role = "CUSTOMER"
        };
}