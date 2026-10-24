namespace FrameRent.Domain.Entities;

public class Equipment
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Category { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public decimal DailyRate { get; private set; }
    public int TotalUnits { get; private set; }
    public bool IsActive { get; private set; }

    // Propiedad de navegación para validar los alquileres activos vinculados
    public ICollection<Rental> Rentals { get; private set; } = new List<Rental>();

    // Constructor requerido por EF Core
    private Equipment() { }

    public Equipment(string name, string category, string description, decimal dailyRate, int totalUnits)
    {
        Id = Guid.NewGuid();
        Update(name, category, description, dailyRate, totalUnits);
        IsActive = true;
    }

    public void Update(string name, string category, string description, decimal dailyRate, int totalUnits)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Equipment name cannot be empty.");
        if (dailyRate <= 0) throw new ArgumentException("Daily rate must be greater than zero.");
        if (totalUnits < 0) throw new ArgumentException("Total units cannot be negative.");

        Name = name;
        Category = category;
        Description = description;
        DailyRate = dailyRate;
        TotalUnits = totalUnits;
    }

    public void Deactivate()
    {
        // Validación de Dominio: No se puede desactivar si tiene alquileres cruzados activos
        bool hasActiveRentals = Rentals.Any(r => 
            r.Status == Enums.RentalStatus.Approved || 
            r.Status == Enums.RentalStatus.Delivered);

        if (hasActiveRentals)
        {
            throw new InvalidOperationException("Cannot deactivate equipment with approved or delivered rentals.");
        }

        IsActive = false;
    }

    public void Activate()
    {
        IsActive = true;
    }
}
