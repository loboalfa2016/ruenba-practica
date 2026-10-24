using FrameRent.Domain.Enums;

namespace FrameRent.Domain.Entities;

public class Rental
{
    public Guid Id { get; private set; }
    public Guid EquipmentId { get; private set; }
    public string ClientId { get; private set; } = string.Empty; // Relacionado con ASP.NET Core Identity User
    public int Quantity { get; private set; }
    public DateTime StartDate { get; private set; }
    public DateTime EndDate { get; private set; }
    public decimal TotalCost { get; private set; }
    public RentalStatus Status { get; private set; }
    public decimal OverduePenalty { get; private set; }

    // Propiedad de navegación
    public Equipment Equipment { get; private set; } = null!;

    // Constructor requerido por EF Core
    private Rental() { }

    public Rental(Guid equipmentId, string clientId, int quantity, DateTime startDate, DateTime endDate, decimal dailyRate)
    {
        if (startDate.Date < DateTime.UtcNow.Date)
            throw new ArgumentException("Start date cannot be in the past.");

        if (endDate <= startDate)
            throw new ArgumentException("End date must be after the start date.");

        int rentalDays = (endDate.Date - startDate.Date).Days;
        if (rentalDays > 15)
            throw new ArgumentException("A rental can last a maximum of 15 days.");

        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than zero.");

        Id = Guid.NewGuid();
        EquipmentId = equipmentId;
        ClientId = clientId;
        Quantity = quantity;
        StartDate = DateTime.SpecifyKind(startDate.Date, DateTimeKind.Utc);
        EndDate = DateTime.SpecifyKind(endDate.Date, DateTimeKind.Utc);
        Status = RentalStatus.Pending;
        
        // El costo total lo calcula el sistema automáticamente ignorando lo que mande el cliente
        TotalCost = rentalDays * dailyRate * quantity;
        OverduePenalty = 0;
    }

    public void Approve()
    {
        if (Status != RentalStatus.Pending)
            throw new InvalidOperationException("Only pending rentals can be approved.");

        Status = RentalStatus.Approved;
    }

    public void Reject()
    {
        if (Status != RentalStatus.Pending)
            throw new InvalidOperationException("Only pending rentals can be rejected.");

        Status = RentalStatus.Rejected;
    }

    public void Deliver()
    {
        if (Status != RentalStatus.Approved)
            throw new InvalidOperationException("Cannot deliver a rental that is not approved.");

        Status = RentalStatus.Delivered;
    }

    public void Return(DateTime returnDate, decimal dailyRate)
    {
        if (Status != RentalStatus.Delivered)
            throw new InvalidOperationException("Cannot return an equipment that has not been delivered.");

        Status = RentalStatus.Returned;

        // Regla de negocio 9: Si la devolución ocurre después de la fecha de fin, se cobra multa
        if (returnDate.Date > EndDate.Date)
            {
            int overdueDays = (returnDate.Date - EndDate.Date).Days;
            // Fórmula obligatoria: días de retraso × tarifa diaria × cantidad × 1.5
            OverduePenalty = overdueDays * dailyRate * Quantity * 1.5m;
        }
    }

    public void Cancel()
    {
        if (Status != RentalStatus.Pending && Status != RentalStatus.Approved)
            throw new InvalidOperationException("Only pending or approved rentals can be canceled.");

        if (DateTime.UtcNow.Date >= StartDate.Date)
            throw new InvalidOperationException("Cannot cancel a rental on or after its start date.");

        Status = RentalStatus.Canceled;
    }
}
