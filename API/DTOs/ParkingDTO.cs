using System.ComponentModel.DataAnnotations;
using API.Entities;

namespace API.DTOs
{
    /// <summary>A parking space as shown to everyone. It never says who holds a reservation.</summary>
    public class ParkingSpaceDTO
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        /// <summary>Street, Lot, Disabled or EV.</summary>
        public string Type { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public decimal HourlyRate { get; set; }
        public int MaxVehicles { get; set; }
        /// <summary>Reservations held by the space, past starts included until they expire (kept for compatibility).</summary>
        public int CurrentVehicles { get; set; }
        /// <summary>MaxVehicles minus the reservations that have started and not yet ended, never below zero.</summary>
        public int FreeNow { get; set; }
        public bool IsActive { get; set; }

        public static ParkingSpaceDTO From(ParkingSpace space, int activeNow) => new()
        {
            Id = space.Id,
            Name = space.Name,
            Address = space.Address,
            Type = space.Type.ToString(),
            Latitude = space.Latitude,
            Longitude = space.Longitude,
            HourlyRate = space.HourlyRate,
            MaxVehicles = space.MaxVehicles,
            CurrentVehicles = space.CurrentVehicles,
            FreeNow = Math.Max(0, space.MaxVehicles - activeNow),
            IsActive = space.IsActive,
        };
    }

    /// <summary>Admin create and update payload. An update replaces every field.</summary>
    public class ParkingSpaceInputDTO : IValidatableObject
    {
        [Required]
        [StringLength(120, MinimumLength = 1)]
        public string Name { get; set; }

        [StringLength(200)]
        public string Address { get; set; }

        [Required]
        [RegularExpression("^(Street|Lot|Disabled|EV)$", ErrorMessage = "Type must be Street, Lot, Disabled or EV")]
        public string Type { get; set; }

        [Range(-90, 90)]
        public double Latitude { get; set; }

        [Range(-180, 180)]
        public double Longitude { get; set; }

        [Range(1, 1000)]
        public int MaxVehicles { get; set; }

        /// <summary>Between 0 and 1000 EUR. Checked in code: a decimal Range attribute parses with the server culture.</summary>
        public decimal HourlyRate { get; set; }

        public bool IsActive { get; set; } = true;

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (HourlyRate < 0m || HourlyRate > 1000m)
                yield return new ValidationResult("Hourly rate must be between 0 and 1000", new[] { nameof(HourlyRate) });
        }
    }

    /// <summary>A reservation as the admin sees it: the username only, never an email or phone number.</summary>
    public class ParkingReservationDTO
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        /// <summary>True once the start time has passed; false for upcoming reservations.</summary>
        public bool IsActive { get; set; }
    }
}
