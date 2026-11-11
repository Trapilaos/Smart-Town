using System.ComponentModel.DataAnnotations;
using API.Entities;

namespace API.DTOs
{
    public class WasteBinDTO
    {
        public int Id { get; set; }
        public string Location { get; set; }
        /// <summary>General, Recycling, Glass, Organic or Paper.</summary>
        public string Type { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public int Capacity { get; set; }
        public int CurrentFillLevel { get; set; }
        /// <summary>CurrentFillLevel as a percentage of Capacity, rounded to one decimal place.</summary>
        public double FillPercent { get; set; }
        public DateTime LastEmptied { get; set; }
        public bool IsActive { get; set; }

        public static double FillPercentOf(int capacity, int currentFillLevel) =>
            capacity > 0 ? Math.Round(100.0 * currentFillLevel / capacity, 1) : 0;

        public static WasteBinDTO From(WasteBin bin) => new()
        {
            Id = bin.Id,
            Location = bin.Location,
            Type = bin.Type.ToString(),
            Latitude = bin.Latitude,
            Longitude = bin.Longitude,
            Capacity = bin.Capacity,
            CurrentFillLevel = bin.CurrentFillLevel,
            FillPercent = FillPercentOf(bin.Capacity, bin.CurrentFillLevel),
            LastEmptied = bin.LastEmptied,
            IsActive = bin.IsActive,
        };
    }

    /// <summary>Create and update payload for an admin bin.</summary>
    public class WasteBinInputDTO
    {
        [Required]
        [StringLength(200, MinimumLength = 1)]
        public string Location { get; set; }

        [Required]
        [RegularExpression("^(General|Recycling|Glass|Organic|Paper)$", ErrorMessage = "Type must be General, Recycling, Glass, Organic or Paper")]
        public string Type { get; set; }

        [Range(-90, 90)]
        public double Latitude { get; set; }

        [Range(-180, 180)]
        public double Longitude { get; set; }

        [Range(1, 100000)]
        public int Capacity { get; set; }

        [Range(0, 100000)]
        public int CurrentFillLevel { get; set; }
    }
}
