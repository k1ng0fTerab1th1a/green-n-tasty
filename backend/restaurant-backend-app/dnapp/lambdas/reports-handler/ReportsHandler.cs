using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using Restaurant.Core.Models;
using Restaurant.Reports;
using Restaurant.Reports.Messaging;
using Restaurant.Reports.Models;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace Restaurant.ReportsHandler;

public class ReportsHandler
{
    private readonly Repository _repository;

    public ReportsHandler()
    {
        _repository = new Repository();
    }
    public async Task FunctionHandler(SQSEvent sqsEvent, ILambdaContext context)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(context.RemainingTime.TotalSeconds - 1));
        var ct = cts.Token;

        foreach (var message in sqsEvent.Records)
        {
            var baseEvent = JsonSerializer.Deserialize<SqsEvent>(message.Body);

            switch (baseEvent.EventType)
            {

                case EventTypes.ReservationCompleted:
                {
                        var reservationDto = baseEvent.Payload.Deserialize<ReservationCompletedDTO>();
                        var reservation = await _repository.GetReservationAsync(reservationDto.ReservationId, ct);
                        if (reservation == null)
                        {
                            context.Logger.LogWarning($"Reservation not found: {reservationDto.ReservationId}");
                            break;
                        }

                        var order = await _repository.GetOrderByReservationAsync(reservation.Id, ct);

                        var feedbacks = await _repository.GetFeedbacksByReservationAsync(reservation.Id, ct);
                        var serviceFeedback = feedbacks.FirstOrDefault(f => f.Type == "waiter")?.Rate;
                        var cuisineFeedback = feedbacks.FirstOrDefault(f => f.Type == "kitchen")?.Rate;

                        var reportEntry = new ReportEntry(reservation, order, serviceFeedback, cuisineFeedback);

                        await _repository.SaveReportEntryAsync(reportEntry, ct);
                        break;
                }
                case EventTypes.FeedbackCreated:
                {
                        var feedbackDto = baseEvent.Payload.Deserialize<FeedbackCreatedDTO>();
                        context.Logger.LogInformation($"Received feedback message: {message.Body}");
                        await _repository.UpdateReportFeedbackAsync(feedbackDto.ReservationId, feedbackDto.ServiceFeedback, feedbackDto.CuisineFeedback, ct);

                        break;
                }
            }
            
        }
    }
}