using FeedbackService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IFeedbackRepository, FileFeedbackRepository>();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

app.MapGet("/", () => "Feedback Service is running...");

app.Run();
