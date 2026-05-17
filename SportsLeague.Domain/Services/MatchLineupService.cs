using Microsoft.Extensions.Logging;
using SportsLeague.Domain.Entities;
using SportsLeague.Domain.Enums;
using SportsLeague.Domain.Helpers;
using SportsLeague.Domain.Interfaces.Repositories;
using SportsLeague.Domain.Interfaces.Services;

namespace SportsLeague.Domain.Services;

public class MatchLineupService : IMatchLineupService
{
    private readonly IMatchLineupRepository _matchLineupRepository;
    private readonly IMatchRepository _matchRepository;
    private readonly IPlayerRepository _playerRepository;
    private readonly MatchValidationHelper _validationHelper;
    private readonly ILogger<MatchLineupService> _logger;

    public MatchLineupService(
        IMatchLineupRepository matchLineupRepository,
        IMatchRepository matchRepository,
        IPlayerRepository playerRepository,
        MatchValidationHelper validationHelper,
        ILogger<MatchLineupService> logger)
    {
        _matchLineupRepository = matchLineupRepository;
        _matchRepository = matchRepository;
        _playerRepository = playerRepository;
        _validationHelper = validationHelper;
        _logger = logger;
    }

    public async Task<MatchLineup> CreateAsync(int matchId, MatchLineup lineup)
    {
        // V1: El partido debe existir
        var match = await _matchRepository.GetByIdAsync(matchId);

        if (match == null)
        {
            throw new KeyNotFoundException(
                $"No se encontró el partido con ID {matchId}");
        }

        // V6: Solo se pueden registrar alineaciones en partidos Scheduled
        if (match.Status != MatchStatus.Scheduled)
        {
            throw new InvalidOperationException(
                "Solo se pueden registrar alineaciones en partidos Scheduled");
        }

        // V2: El jugador debe existir
        var player = await _playerRepository.GetByIdAsync(lineup.PlayerId);

        if (player == null)
        {
            throw new KeyNotFoundException(
                $"No se encontró el jugador con ID {lineup.PlayerId}");
        }

        // V3: El jugador debe pertenecer al HomeTeam o AwayTeam del partido
        await _validationHelper.ValidatePlayerInMatchAsync(lineup.PlayerId, match);

        // V4: El jugador no puede estar registrado dos veces en el mismo partido
        var alreadyExists = await _matchLineupRepository
            .ExistsByMatchAndPlayerAsync(matchId, lineup.PlayerId);

        if (alreadyExists)
        {
            throw new InvalidOperationException(
                "El jugador ya está registrado en la alineación de este partido");
        }

        // V5: Máximo 11 titulares por equipo por partido
        if (lineup.IsStarter)
        {
            var startersCount = await _matchLineupRepository
                .CountStartersByMatchAndTeamAsync(matchId, player.TeamId);

            if (startersCount >= 11)
            {
                throw new InvalidOperationException(
                    "El equipo ya tiene 11 titulares registrados en este partido");
            }
        }

        lineup.MatchId = matchId;

        _logger.LogInformation(
            "Registering lineup: Match {MatchId}, Player {PlayerId}, IsStarter {IsStarter}, Position {Position}",
            matchId,
            lineup.PlayerId,
            lineup.IsStarter,
            lineup.Position);

        var createdLineup = await _matchLineupRepository.CreateAsync(lineup);

        var createdWithDetails = await _matchLineupRepository
            .GetByIdWithDetailsAsync(createdLineup.Id);

        return createdWithDetails ?? createdLineup;
    }

    public async Task<IEnumerable<MatchLineup>> GetByMatchAsync(int matchId)
    {
        var match = await _matchRepository.GetByIdAsync(matchId);

        if (match == null)
        {
            throw new KeyNotFoundException(
                $"No se encontró el partido con ID {matchId}");
        }

        return await _matchLineupRepository.GetByMatchAsync(matchId);
    }

    public async Task<IEnumerable<MatchLineup>> GetByMatchAndTeamAsync(int matchId, int teamId)
    {
        var match = await _matchRepository.GetByIdAsync(matchId);

        if (match == null)
        {
            throw new KeyNotFoundException(
                $"No se encontró el partido con ID {matchId}");
        }

        if (teamId != match.HomeTeamId && teamId != match.AwayTeamId)
        {
            throw new InvalidOperationException(
                "El equipo no pertenece al partido");
        }

        return await _matchLineupRepository.GetByMatchAndTeamAsync(matchId, teamId);
    }

    public async Task DeleteAsync(int matchId, int lineupId)
    {
        var match = await _matchRepository.GetByIdAsync(matchId);

        if (match == null)
        {
            throw new KeyNotFoundException(
                $"No se encontró el partido con ID {matchId}");
        }

        if (match.Status != MatchStatus.Scheduled)
        {
            throw new InvalidOperationException(
                "Solo se pueden eliminar alineaciones en partidos Scheduled");
        }

        var lineup = await _matchLineupRepository.GetByIdAsync(lineupId);

        if (lineup == null || lineup.MatchId != matchId)
        {
            throw new KeyNotFoundException(
                $"No se encontró la alineación con ID {lineupId} para el partido {matchId}");
        }

        await _matchLineupRepository.DeleteAsync(lineupId);

        _logger.LogInformation(
            "Deleted lineup: Match {MatchId}, Lineup {LineupId}",
            matchId,
            lineupId);
    }
}