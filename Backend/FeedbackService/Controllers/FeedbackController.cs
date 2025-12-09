using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FeedbackService.Models;
using FeedbackService.Services;

namespace FeedbackService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FeedbackController : ControllerBase
{
    private readonly IFeedbackRepository _repository;

    public FeedbackController(IFeedbackRepository repository)
    {
        _repository = repository;
    }

    [HttpPost("Submit")]
    public async Task<IActionResult> SubmitFeedback([FromBody] Feedback feedback, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var saved = await _repository.AddAsync(feedback, cancellationToken);
        return Ok(new { message = "Geri bildiriminiz alındı.", id = saved.Id });
    }

    [HttpGet]
    public async Task<IActionResult> GetFeedbacks(CancellationToken cancellationToken)
    {
        var feedbacks = await _repository.GetAllAsync(cancellationToken);
        return Ok(feedbacks);
    }

    [HttpGet("Analysis")]
    public async Task<IActionResult> GetAnalysis(CancellationToken cancellationToken)
    {
        var feedbacks = await _repository.GetAllAsync(cancellationToken);
        var analysis = new
        {
            TotalFeedbacks = feedbacks.Count,
            Summary = "Kullanıcılar genel olarak rezervasyon sisteminden memnun, ancak masa bulma konusunda bazen zorluk yaşıyorlar.",
            Sentiment = "Positive",
            Suggestions = new List<string> { "Daha fazla masa eklenmeli", "Mobil uygulama geliştirilmeli" }
        };

        return Ok(analysis);
    }
}
