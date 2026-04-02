using System.Net.Mail;
using Microsoft.Extensions.Logging;
using SportsLeague.Domain.Entities;
using SportsLeague.Domain.Interfaces.Repositories;
using SportsLeague.Domain.Interfaces.Services;

namespace SportsLeague.Domain.Services;

public class SponsorService : ISponsorService
{
    private readonly ISponsorRepository _sponsorRepository;
    private readonly ITournamentRepository _tournamentRepository;
    private readonly ITournamentSponsorRepository _tournamentSponsorRepository;
    private readonly ILogger<SponsorService> _logger;

    public SponsorService(
        ISponsorRepository sponsorRepository,
        ITournamentRepository tournamentRepository,
        ITournamentSponsorRepository tournamentSponsorRepository,
        ILogger<SponsorService> logger)
    {
        _sponsorRepository = sponsorRepository;
        _tournamentRepository = tournamentRepository;
        _tournamentSponsorRepository = tournamentSponsorRepository;
        _logger = logger;
    }

    public async Task<IEnumerable<Sponsor>> GetAllAsync()
    {
        _logger.LogInformation("Retrieving all sponsors");
        return await _sponsorRepository.GetAllAsync();
    }

    public async Task<Sponsor?> GetByIdAsync(int id)
    {
        _logger.LogInformation("Retrieving sponsor with ID: {SponsorId}", id);

        var sponsor = await _sponsorRepository.GetByIdAsync(id);

        if (sponsor == null)
            _logger.LogWarning("Sponsor with ID {SponsorId} not found", id);

        return sponsor;
    }

    public async Task<Sponsor> CreateAsync(Sponsor sponsor)
    {
        await ValidateSponsorAsync(sponsor);

        var duplicatedName = await _sponsorRepository.ExistsByNameAsync(sponsor.Name);
        if (duplicatedName)
        {
            _logger.LogWarning("Sponsor with name '{SponsorName}' already exists", sponsor.Name);
            throw new InvalidOperationException(
                $"Ya existe un sponsor con el nombre '{sponsor.Name}'");
        }

        _logger.LogInformation("Creating sponsor: {SponsorName}", sponsor.Name);
        return await _sponsorRepository.CreateAsync(sponsor);
    }

    public async Task UpdateAsync(int id, Sponsor sponsor)
    {
        var existingSponsor = await _sponsorRepository.GetByIdAsync(id);
        if (existingSponsor == null)
        {
            throw new KeyNotFoundException(
                $"No se encontró el sponsor con ID {id}");
        }

        await ValidateSponsorAsync(sponsor);

        if (!existingSponsor.Name.Equals(sponsor.Name, StringComparison.OrdinalIgnoreCase))
        {
            var duplicatedName = await _sponsorRepository.ExistsByNameAsync(sponsor.Name);
            if (duplicatedName)
            {
                throw new InvalidOperationException(
                    $"Ya existe un sponsor con el nombre '{sponsor.Name}'");
            }
        }

        existingSponsor.Name = sponsor.Name;
        existingSponsor.ContactEmail = sponsor.ContactEmail;
        existingSponsor.Phone = sponsor.Phone;
        existingSponsor.WebsiteUrl = sponsor.WebsiteUrl;
        existingSponsor.Category = sponsor.Category;

        _logger.LogInformation("Updating sponsor with ID: {SponsorId}", id);
        await _sponsorRepository.UpdateAsync(existingSponsor);
    }

    public async Task DeleteAsync(int id)
    {
        var exists = await _sponsorRepository.ExistsAsync(id);
        if (!exists)
        {
            throw new KeyNotFoundException(
                $"No se encontró el sponsor con ID {id}");
        }

        _logger.LogInformation("Deleting sponsor with ID: {SponsorId}", id);
        await _sponsorRepository.DeleteAsync(id);
    }

    public async Task<TournamentSponsor> LinkToTournamentAsync(int sponsorId, int tournamentId, decimal contractAmount)
    {
        var sponsor = await _sponsorRepository.GetByIdAsync(sponsorId);
        if (sponsor == null)
        {
            throw new KeyNotFoundException(
                $"No se encontró el sponsor con ID {sponsorId}");
        }

        var tournament = await _tournamentRepository.GetByIdAsync(tournamentId);
        if (tournament == null)
        {
            throw new KeyNotFoundException(
                $"No se encontró el torneo con ID {tournamentId}");
        }

        if (contractAmount <= 0)
        {
            throw new InvalidOperationException(
                "El ContractAmount debe ser mayor a 0");
        }

        var existingLink = await _tournamentSponsorRepository
            .GetByTournamentAndSponsorAsync(tournamentId, sponsorId);

        if (existingLink != null)
        {
            throw new InvalidOperationException(
                "Este sponsor ya está vinculado a este torneo");
        }

        var tournamentSponsor = new TournamentSponsor
        {
            SponsorId = sponsorId,
            TournamentId = tournamentId,
            ContractAmount = contractAmount,
            JoinedAt = DateTime.UtcNow
        };

        _logger.LogInformation(
            "Linking sponsor {SponsorId} to tournament {TournamentId}",
            sponsorId, tournamentId);

        var createdLink = await _tournamentSponsorRepository.CreateAsync(tournamentSponsor);

        return await _tournamentSponsorRepository.GetByIdWithDetailsAsync(createdLink.Id)
               ?? throw new InvalidOperationException("No fue posible recuperar la vinculación creada");
    }

    public async Task<IEnumerable<TournamentSponsor>> GetTournamentsBySponsorAsync(int sponsorId)
    {
        var sponsor = await _sponsorRepository.GetByIdAsync(sponsorId);
        if (sponsor == null)
        {
            throw new KeyNotFoundException(
                $"No se encontró el sponsor con ID {sponsorId}");
        }

        return await _tournamentSponsorRepository.GetBySponsorIdAsync(sponsorId);
    }

    public async Task UnlinkFromTournamentAsync(int sponsorId, int tournamentId)
    {
        var sponsor = await _sponsorRepository.GetByIdAsync(sponsorId);
        if (sponsor == null)
        {
            throw new KeyNotFoundException(
                $"No se encontró el sponsor con ID {sponsorId}");
        }

        var tournament = await _tournamentRepository.GetByIdAsync(tournamentId);
        if (tournament == null)
        {
            throw new KeyNotFoundException(
                $"No se encontró el torneo con ID {tournamentId}");
        }

        var existingLink = await _tournamentSponsorRepository
            .GetByTournamentAndSponsorAsync(tournamentId, sponsorId);

        if (existingLink == null)
        {
            throw new KeyNotFoundException(
                $"No existe una vinculación entre el sponsor {sponsorId} y el torneo {tournamentId}");
        }

        _logger.LogInformation(
            "Unlinking sponsor {SponsorId} from tournament {TournamentId}",
            sponsorId, tournamentId);

        await _tournamentSponsorRepository.DeleteAsync(existingLink.Id);
    }

    private static Task ValidateSponsorAsync(Sponsor sponsor)
    {
        if (string.IsNullOrWhiteSpace(sponsor.Name))
        {
            throw new InvalidOperationException("El nombre del sponsor es obligatorio");
        }

        if (string.IsNullOrWhiteSpace(sponsor.ContactEmail))
        {
            throw new InvalidOperationException("El email de contacto es obligatorio");
        }

        if (!IsValidEmail(sponsor.ContactEmail))
        {
            throw new InvalidOperationException("El ContactEmail no tiene un formato válido");
        }

        return Task.CompletedTask;
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var mailAddress = new MailAddress(email);
            return mailAddress.Address == email;
        }
        catch
        {
            return false;
        }
    }
}