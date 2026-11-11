using System.ComponentModel.DataAnnotations;
using API.Entities;

namespace API.DTOs
{
    /// <summary>A published event as shown to everyone. It never includes user ids.</summary>
    public class EventDTO
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string PhotoUrl { get; set; }
        public string Location { get; set; }
        public double? Lat { get; set; }
        public double? Lng { get; set; }
        public DateTime Date { get; set; }
        public DateTime? EndDate { get; set; }
        public string Category { get; set; }
        public int InterestedCount { get; set; }
        /// <summary>True only when the caller is signed in and has marked interest.</summary>
        public bool IsInterested { get; set; }

        public static EventDTO From(Event ev, int interestedCount, bool isInterested) => new()
        {
            Id = ev.Id,
            Title = ev.Title,
            Description = ev.Description,
            PhotoUrl = ev.PhotoUrl,
            Location = ev.Location,
            Lat = ev.Latitude,
            Lng = ev.Longitude,
            Date = ev.Date,
            EndDate = ev.EndDate,
            Category = ev.Category.ToString(),
            InterestedCount = interestedCount,
            IsInterested = isInterested,
        };
    }

    /// <summary>An event as the admin sees it, including unpublished events.</summary>
    public class AdminEventDTO
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string PhotoUrl { get; set; }
        public string Location { get; set; }
        public double? Lat { get; set; }
        public double? Lng { get; set; }
        public DateTime Date { get; set; }
        public DateTime? EndDate { get; set; }
        public string Category { get; set; }
        public int InterestedCount { get; set; }
        public bool IsPublished { get; set; }
        public DateTime CreatedAt { get; set; }

        public static AdminEventDTO From(Event ev, int interestedCount) => new()
        {
            Id = ev.Id,
            Title = ev.Title,
            Description = ev.Description,
            PhotoUrl = ev.PhotoUrl,
            Location = ev.Location,
            Lat = ev.Latitude,
            Lng = ev.Longitude,
            Date = ev.Date,
            EndDate = ev.EndDate,
            Category = ev.Category.ToString(),
            InterestedCount = interestedCount,
            IsPublished = ev.IsPublished,
            CreatedAt = ev.CreatedAt,
        };
    }

    /// <summary>Admin create and update payload. An update replaces every field.</summary>
    public class EventInputDTO : IValidatableObject
    {
        [Required]
        [StringLength(200, MinimumLength = 1)]
        public string Title { get; set; }

        [StringLength(4000)]
        public string Description { get; set; }

        /// <summary>Optional. Must be an https URL.</summary>
        [StringLength(2048)]
        public string PhotoUrl { get; set; }

        [StringLength(200)]
        public string Location { get; set; }

        [Range(-90, 90)]
        public double? Lat { get; set; }

        [Range(-180, 180)]
        public double? Lng { get; set; }

        [Required]
        public DateTime Date { get; set; }

        public DateTime? EndDate { get; set; }

        [Required]
        [RegularExpression("^(Culture|Sports|Council|Environment|Other)$", ErrorMessage = "Category must be Culture, Sports, Council, Environment or Other")]
        public string Category { get; set; }

        public bool IsPublished { get; set; } = true;

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (Date == default)
                yield return new ValidationResult("Date is required", new[] { nameof(Date) });

            if (EndDate.HasValue && EndDate.Value < Date)
                yield return new ValidationResult("End date must not be before the start date", new[] { nameof(EndDate) });

            if (Lat.HasValue != Lng.HasValue)
                yield return new ValidationResult("Latitude and longitude must be given together", new[] { nameof(Lat), nameof(Lng) });

            if (!string.IsNullOrWhiteSpace(PhotoUrl) &&
                !(Uri.TryCreate(PhotoUrl, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps))
                yield return new ValidationResult("Photo must be an https URL", new[] { nameof(PhotoUrl) });
        }
    }
}
