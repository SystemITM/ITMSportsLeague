using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using SportsLeague.API.DTOs.Request;
using SportsLeague.API.DTOs.Response;
using SportsLeague.Domain.Entities;
using SportsLeague.Domain.Interfaces.Services;

namespace SportsLeague.API.Controllers;

[ApiController]
[Route("api/match/{matchId}/lineup")]
public class MatchLineupController : ControllerBase
{
    private readonly IMatchLineupService _matchLineupService;
    private readonly IMapper _mapper;

    public MatchLineupController(
        IMatchLineupService matchLineupService,
        IMapper mapper)
    {
        _matchLineupService = matchLineupService;
        _mapper = mapper;
    }

    [HttpPost]
    public async Task<ActionResult<MatchLineupDto>> Create(
        int matchId,
        CreateMatchLineupDto dto)
    {
        try
        {
            var lineup = _mapper.Map<MatchLineup>(dto);

            var createdLineup = await _matchLineupService
                .CreateAsync(matchId, lineup);

            var response = _mapper.Map<MatchLineupDto>(createdLineup);

            return CreatedAtAction(
                nameof(GetByMatch),
                new { matchId },
                response);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<MatchLineupDto>>> GetByMatch(
        int matchId)
    {
        try
        {
            var lineups = await _matchLineupService
                .GetByMatchAsync(matchId);

            var response = _mapper.Map<IEnumerable<MatchLineupDto>>(lineups);

            return Ok(response);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpGet("team/{teamId}")]
    public async Task<ActionResult<IEnumerable<MatchLineupDto>>> GetByMatchAndTeam(
        int matchId,
        int teamId)
    {
        try
        {
            var lineups = await _matchLineupService
                .GetByMatchAndTeamAsync(matchId, teamId);

            var response = _mapper.Map<IEnumerable<MatchLineupDto>>(lineups);

            return Ok(response);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(
        int matchId,
        int id)
    {
        try
        {
            await _matchLineupService.DeleteAsync(matchId, id);

            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }
}