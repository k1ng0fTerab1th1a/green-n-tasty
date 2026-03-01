using System.Linq.Dynamic.Core;
using Restaurant.Core.ServiceDTOs;

namespace Restaurant.Core.Helpers;

public static class SortHelper
{
    public static List<FeedbackDTO> SortFeedbackDynamic(
        List<FeedbackDTO> items,
        List<string> sortCriteria)
    {
        if (items.Count == 0)
            return [];

        var sortParts = new List<string>();

        foreach (var criterion in sortCriteria)
        {
            if (string.IsNullOrWhiteSpace(criterion))
                continue;

            // Split "date,asc" into property and direction
            var parts = criterion.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length == 0)
                continue;

            string property = parts[0];
            // Default to ascending if direction is missing or invalid
            string direction = (parts.Length > 1) ? parts[1].ToLowerInvariant() : "asc";

            sortParts.Add($"{property} {direction}");
        }

        if (sortParts.Count == 0)
            return items;

        string sortString = string.Join(", ", sortParts);

        return items.AsQueryable().OrderBy(sortString).ToList();

    }
    
}