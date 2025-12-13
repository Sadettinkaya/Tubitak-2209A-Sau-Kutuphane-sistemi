# RabbitMQ Event-Driven Architecture

## 📋 Genel Bakış

Kütüphane rezervasyon sistemi, mikroservisler arası iletişim için **RabbitMQ** message broker kullanır. Bu, servisler arasında gevşek bağlı (loosely coupled), asenkron ve güvenilir iletişim sağlar.

## 🏗️ Mimari

### Event Exchange Yapısı
- **Exchange Type**: Topic
- **Exchange Name**: `library_events`
- **Durable**: true (mesajlar kalıcı)

### Tanımlı Event'ler

#### 1. StudentEnteredEvent
**Routing Key**: `student.entered`

**Ne Zaman Tetiklenir**: Öğrenci turnike ile kütüphaneye giriş yaptığında

**Publisher**: TurnstileService  
**Consumer**: ReservationService

```csharp
public class StudentEnteredEvent
{
    public string StudentNumber { get; set; }
    public DateTime EntryTime { get; set; }
    public string? TurnstileId { get; set; }
}
```

**Kullanım Amacı**:
- Rezervasyon durumunu otomatik güncelleme (IsAttended = true)
- Giriş logları tutma
- Analytics ve raporlama

#### 2. ReservationCreatedEvent
**Routing Key**: `reservation.created`

**Ne Zaman Tetiklenir**: Yeni rezervasyon oluşturulduğunda

**Publisher**: ReservationService  
**Consumers**: (Gelecekte: Notification Service, Analytics Service)

```csharp
public class ReservationCreatedEvent
{
    public int ReservationId { get; set; }
    public string StudentNumber { get; set; }
    public int TableId { get; set; }
    public DateOnly ReservationDate { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public string StudentType { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

#### 3. ReservationCancelledEvent
**Routing Key**: `reservation.cancelled`

**Ne Zaman Tetiklenir**: Rezervasyon iptal edildiğinde

**Publisher**: ReservationService  
**Consumers**: (Gelecekte: Notification Service)

```csharp
public class ReservationCancelledEvent
{
    public int ReservationId { get; set; }
    public string StudentNumber { get; set; }
    public int TableId { get; set; }
    public DateOnly ReservationDate { get; set; }
    public DateTime CancelledAt { get; set; }
}
```

#### 4. StudentProfileUpdatedEvent
**Routing Key**: `student.profile.updated`

**Ne Zaman Tetiklenir**: Öğrenci profili güncellendiğinde (ceza, ban vb.)

**Publisher**: ReservationService  
**Consumers**: (Gelecekte: Notification Service, Identity Service)

```csharp
public class StudentProfileUpdatedEvent
{
    public string StudentNumber { get; set; }
    public string StudentType { get; set; }
    public int PenaltyPoints { get; set; }
    public DateOnly? BanUntil { get; set; }
    public string? BanReason { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

## 🔧 Konfigürasyon

### Docker Compose

RabbitMQ servisi `docker-compose.yml` dosyasında tanımlıdır:

```yaml
rabbitmq:
  image: rabbitmq:3.13-management-alpine
  container_name: library_rabbitmq
  environment:
    - RABBITMQ_DEFAULT_USER=library
    - RABBITMQ_DEFAULT_PASS=library123
  ports:
    - "5672:5672"   # AMQP port
    - "15672:15672" # Management UI
  healthcheck:
    test: ["CMD", "rabbitmq-diagnostics", "ping"]
    interval: 10s
    timeout: 5s
    retries: 5
```

### Servis Konfigürasyonu

Her serviste `appsettings.json` dosyasına eklenmelidir:

```json
{
  "RabbitMQ": {
    "Host": "localhost",
    "Username": "library",
    "Password": "library123"
  }
}
```

Docker ortamında `docker-compose.yml` ile override edilir:

```yaml
environment:
  - RabbitMQ__Host=rabbitmq
  - RabbitMQ__Username=library
  - RabbitMQ__Password=library123
```

## 📊 RabbitMQ Management UI

**URL**: http://localhost:15672

**Credentials**:
- Username: `library`
- Password: `library123`

Management UI'dan şunları yapabilirsiniz:
- Queue'ları görüntüleme
- Message throughput izleme
- Connection'ları kontrol etme
- Manual message publish etme

## 🚀 Kullanım Örnekleri

### Event Publish Etme

```csharp
// Dependency Injection ile
private readonly RabbitMQPublisher _publisher;

public MyController(RabbitMQPublisher publisher)
{
    _publisher = publisher;
}

// Event gönderme
var entryEvent = new StudentEnteredEvent
{
    StudentNumber = "123456",
    EntryTime = DateTime.UtcNow,
    TurnstileId = "turnstile-1"
};

_publisher.Publish(entryEvent, "student.entered");
```

### Event Consume Etme

```csharp
public class MyEventConsumer : BackgroundService
{
    private RabbitMQConsumer? _consumer;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(5000, stoppingToken); // RabbitMQ'nun başlamasını bekle

        _consumer = new RabbitMQConsumer(
            hostName: "rabbitmq",
            userName: "library",
            password: "library123",
            queueName: "my_service_queue"
        );

        _consumer.Subscribe<StudentEnteredEvent>(
            routingKey: "student.entered",
            handler: HandleStudentEntry
        );

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }
    }

    private void HandleStudentEntry(StudentEnteredEvent eventData)
    {
        // Event işleme mantığı
        Console.WriteLine($"Student {eventData.StudentNumber} entered");
    }
}
```

## 🔍 Debugging ve Monitoring

### 1. Consumer Loglarını Kontrol Etme

```powershell
# ReservationService logları
docker logs reservation_service -f

# TurnstileService logları
docker logs turnstile_service -f
```

### 2. RabbitMQ Queue Durumunu Kontrol Etme

```powershell
# RabbitMQ container'a bağlan
docker exec -it library_rabbitmq rabbitmqctl list_queues

# Queue detayları
docker exec -it library_rabbitmq rabbitmqctl list_queues name messages consumers
```

### 3. Event Flow'u Test Etme

1. Turnstile endpoint'ini çağır:
```powershell
curl -X POST http://localhost:5003/api/Turnstile/enter `
  -H "Content-Type: application/json" `
  -d '{"studentNumber": "123456"}'
```

2. RabbitMQ Management UI'dan mesajları kontrol et

3. ReservationService loglarında event'in alındığını doğrula

## 📝 Yeni Event Ekleme Rehberi

1. **Event Sınıfı Oluştur** (`Backend/Shared.Events/`)
```csharp
namespace Shared.Events;

public class MyNewEvent
{
    public string Data { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

2. **Publisher Ekle** (Event'i tetikleyen serviste)
```csharp
_publisher.Publish(new MyNewEvent 
{ 
    Data = "example",
    CreatedAt = DateTime.UtcNow 
}, "my.new.event");
```

3. **Consumer Ekle** (Event'i dinleyen serviste)
```csharp
_consumer.Subscribe<MyNewEvent>("my.new.event", HandleMyNewEvent);

private void HandleMyNewEvent(MyNewEvent eventData)
{
    // İşleme mantığı
}
```

## 🎯 Best Practices

1. **Routing Key Naming Convention**: 
   - Format: `<entity>.<action>`
   - Örnek: `student.entered`, `reservation.created`

2. **Event Naming**:
   - Past tense kullan (StudentEnteredEvent, ReservationCreatedEvent)
   - Event'in ne olduğunu açıkça belirt

3. **Error Handling**:
   - Consumer'larda try-catch kullan
   - Hatalı mesajları log'la
   - BasicNack ile mesajı requeue et

4. **Idempotency**:
   - Aynı event birden fazla işlenebilir
   - Consumer logic'i idempotent olmalı

5. **Monitoring**:
   - Consumer'ların çalıştığını düzenli kontrol et
   - Queue depth'i izle
   - Dead letter queue'ları takip et

## 🔐 Güvenlik

**Üretim Ortamı İçin**:
- RabbitMQ şifresini değiştir
- TLS/SSL kullan
- User permissions ayarla
- Network segmentation uygula

## 📚 Kaynaklar

- [RabbitMQ Documentation](https://www.rabbitmq.com/documentation.html)
- [RabbitMQ .NET Client Guide](https://www.rabbitmq.com/tutorials/tutorial-one-dotnet.html)
- [Topic Exchange Tutorial](https://www.rabbitmq.com/tutorials/tutorial-five-dotnet.html)
