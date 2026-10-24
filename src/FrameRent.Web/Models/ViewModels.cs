using System.ComponentModel.DataAnnotations;
using FrameRent.Application.DTOs.Equipment;
using FrameRent.Application.DTOs.Rental;
using FrameRent.Application.DTOs.Report;
using FrameRent.Domain.Enums;

namespace FrameRent.Web.Models;

public class LoginViewModel
{
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email address format.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
}

public class RegisterViewModel
{
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email address format.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirm password is required.")]
    [Compare("Password", ErrorMessage = "Passwords do not match.")]
    [DataType(DataType.Password)]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public class CatalogViewModel
{
    public IEnumerable<EquipmentDto> Equipments { get; set; } = new List<EquipmentDto>();
    public IEnumerable<string> Categories { get; set; } = new List<string>();
    public string? SelectedCategory { get; set; }
    public string? SearchTerm { get; set; }
}

public class BookRentalViewModel
{
    public Guid EquipmentId { get; set; }
    public string EquipmentName { get; set; } = string.Empty;
    public decimal DailyRate { get; set; }

    [Required(ErrorMessage = "Quantity is required.")]
    [Range(1, 100, ErrorMessage = "Quantity must be at least 1.")]
    public int Quantity { get; set; } = 1;

    [Required(ErrorMessage = "Start date is required.")]
    [DataType(DataType.Date)]
    public DateTime StartDate { get; set; } = DateTime.UtcNow.Date.AddDays(1);

    [Required(ErrorMessage = "End date is required.")]
    [DataType(DataType.Date)]
    public DateTime EndDate { get; set; } = DateTime.UtcNow.Date.AddDays(3);
}

public class StaffRentalsViewModel
{
    public IEnumerable<RentalDto> Rentals { get; set; } = new List<RentalDto>();
    public RentalStatus? SelectedStatus { get; set; }
}
