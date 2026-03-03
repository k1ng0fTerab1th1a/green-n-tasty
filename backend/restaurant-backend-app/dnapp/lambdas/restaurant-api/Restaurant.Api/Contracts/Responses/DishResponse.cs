namespace Restaurant.Api.Contracts.Responses
{
    public class DishShortResponse
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string PreviewImageUrl { get; set; }
        public float Price { get; set; }
        public string State { get; set; }
        public int? Weight { get; set; }
    }
}
