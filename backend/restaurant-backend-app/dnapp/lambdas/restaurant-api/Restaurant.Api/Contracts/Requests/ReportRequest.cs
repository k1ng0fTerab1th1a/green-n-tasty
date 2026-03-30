using System.ComponentModel.DataAnnotations;

namespace Restaurant.Api.Contracts.Requests;

public class ReportRequest
{
    [Required]
    public DateTime From { get; set; }

    [Required]
    public DateTime To { get; set; }

    public DateTime? CompareFrom { get; set; }
    public DateTime? CompareTo { get; set; }
}
