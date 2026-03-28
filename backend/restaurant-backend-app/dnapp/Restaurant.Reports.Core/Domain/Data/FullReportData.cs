namespace Restaurant.Reports.Domain.Data;

public record FullReportData(
    List<LocationReportData> LocationReportData,
    List<WaiterReportData> WaiterReportData
);