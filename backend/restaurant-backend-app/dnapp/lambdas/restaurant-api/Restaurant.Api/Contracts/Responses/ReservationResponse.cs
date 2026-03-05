namespace Restaurant.Api.Contracts.Responses
{
    public sealed class ReservationResponse
    {
        public string Id { get; set; } = null!;
        public string LocationId { get; set; } = null!;
        public int TableNumber { get; set; }
        public int GuestsCount { get; set; }

        public string StartDateTime { get; set; } = null!;
        public string EndDateTime { get; set; } = null!;

        public string Status { get; set; } = null!;
    }
}
