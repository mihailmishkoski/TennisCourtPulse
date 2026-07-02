using CourtPulse.Application.Analytics;
using CourtPulse.Application.Features.Matches.Dtos;
using CourtPulse.Application.Features.Matches.Queries;
using CourtPulse.Application.Summary;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CourtPulse.Api.Controllers;

/// <summary>
/// Thin read API for the Angular client. Every action just builds a request and
/// hands it to MediatR — no logic here. A null result from a by-id query means
/// the match doesn't exist, which becomes a 404.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public sealed class MatchesController : ControllerBase
{
    private readonly IMediator _mediator;

    public MatchesController(IMediator mediator) => _mediator = mediator;

    /// <summary>All live and just-finished matches for the main list.</summary>
    [HttpGet("live")]
    public async Task<ActionResult<IReadOnlyList<LiveMatchSummaryDto>>> GetLive(CancellationToken ct)
    {
        IReadOnlyList<LiveMatchSummaryDto> result = await _mediator.Send(new GetLiveMatchesQuery(), ct);
        return Ok(result);
    }

    [HttpGet("{matchId:guid}")]
    public async Task<ActionResult<MatchDetailDto>> GetById(Guid matchId, CancellationToken ct)
    {
        MatchDetailDto? result = await _mediator.Send(new GetMatchByIdQuery(matchId), ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("{matchId:guid}/timeline")]
    public async Task<ActionResult<MatchTimelineDto>> GetTimeline(Guid matchId, CancellationToken ct)
    {
        MatchTimelineDto? result = await _mediator.Send(new GetMatchTimelineQuery(matchId), ct);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>Momentum points for the live graph and surge meter.</summary>
    [HttpGet("{matchId:guid}/momentum")]
    public async Task<ActionResult<IReadOnlyList<MomentumPointDto>>> GetMomentum(Guid matchId, CancellationToken ct)
    {
        IReadOnlyList<MomentumPointDto>? result = await _mediator.Send(new GetMatchMomentumQuery(matchId), ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("{matchId:guid}/statistics")]
    public async Task<ActionResult<IReadOnlyList<PlayerStatisticDto>>> GetStatistics(Guid matchId, CancellationToken ct)
    {
        IReadOnlyList<PlayerStatisticDto>? result = await _mediator.Send(new GetMatchStatisticsQuery(matchId), ct);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>Finished-match report: strengths, weaknesses, headlines.</summary>
    [HttpGet("{matchId:guid}/summary")]
    public async Task<ActionResult<MatchSummary>> GetSummary(Guid matchId, CancellationToken ct)
    {
        MatchSummary? result = await _mediator.Send(new GetMatchSummaryQuery(matchId), ct);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>Live win-probability estimate.</summary>
    [HttpGet("{matchId:guid}/win-probability")]
    public async Task<ActionResult<WinProbability>> GetWinProbability(Guid matchId, CancellationToken ct)
    {
        WinProbability? result = await _mediator.Send(new GetMatchWinProbabilityQuery(matchId), ct);
        return result is null ? NotFound() : Ok(result);
    }
}
