using System.ComponentModel.DataAnnotations;

namespace SportsLeague.API.DTOs.Request;

public class CreateMatchLineupDto
{
    [Required(ErrorMessage = "El ID del jugador es obligatorio")]
    [Range(1, int.MaxValue, ErrorMessage = "El ID del jugador debe ser mayor que cero")]
    public int PlayerId { get; set; }

    [Required(ErrorMessage = "Debe indicar si el jugador es titular o suplente")]
    public bool IsStarter { get; set; }

    [Required(ErrorMessage = "La posición es obligatoria")]
    [StringLength(20, ErrorMessage = "La posición no puede superar los 20 caracteres")]
    public string Position { get; set; } = string.Empty;
}