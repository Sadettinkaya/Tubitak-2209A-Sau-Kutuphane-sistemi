using Shared.Events;

namespace ReservationService.Services;

public class StudentEntryEventConsumer : BackgroundService
{
    private readonly ILogger<StudentEntryEventConsumer> _logger;
    private readonly IConfiguration _configuration;
    private RabbitMQConsumer? _consumer;

    public StudentEntryEventConsumer(
        ILogger<StudentEntryEventConsumer> logger,
        IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(5000, stoppingToken); // RabbitMQ'nun başlamasını bekle

        var rabbitHost = _configuration.GetValue<string>("RabbitMQ:Host") ?? "localhost";
        var rabbitUser = _configuration.GetValue<string>("RabbitMQ:Username") ?? "library";
        var rabbitPass = _configuration.GetValue<string>("RabbitMQ:Password") ?? "library123";

        _consumer = new RabbitMQConsumer(rabbitHost, rabbitUser, rabbitPass, "reservation_service_queue");

        _consumer.Subscribe<StudentEnteredEvent>("student.entered", HandleStudentEntry);

        _logger.LogInformation("StudentEntryEventConsumer started and listening for events");

        // Keep the service running
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }
    }

    private void HandleStudentEntry(StudentEnteredEvent eventData)
    {
        _logger.LogInformation(
            "Student {StudentNumber} entered library at {EntryTime} via {TurnstileId}",
            eventData.StudentNumber,
            eventData.EntryTime,
            eventData.TurnstileId
        );

        // Burada gerekirse rezervasyon durumunu güncelleyebiliriz
        // Örneğin: IsAttended = true yapılabilir
    }

    public override void Dispose()
    {
        _consumer?.Dispose();
        base.Dispose();
    }
}
