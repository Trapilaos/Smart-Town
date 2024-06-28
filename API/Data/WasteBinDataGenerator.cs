using API.Entities;
using Microsoft.EntityFrameworkCore;


namespace API.Data
{
    public class WasteBinDataGenerator
    {
        private readonly Random _random;
        private readonly string[] _locations;
        private readonly DataContext _context;

        public WasteBinDataGenerator(DataContext context)
        {
            _random = new Random();
            _locations = new[] { "3is Septemvriou", "Alexandras", "Stadiou", "Vouliagmenis" };
            _context = context;
        }

        public async Task CleanAndGenerateDataAsync(int numDays)
        {
            await CleanDataAsync();
            await GenerateDataAsync(numDays);
        }

        private async Task CleanDataAsync()
        {
            var wasteBins = await _context.WasteBins.ToListAsync();
            _context.WasteBins.RemoveRange(wasteBins);
            await _context.SaveChangesAsync();
        }

        private async Task GenerateDataAsync(int numDays)
        {
            var bins = new List<WasteBin>();

            // Generate data for each location
            foreach (var location in _locations)
            {
                var bin = new WasteBin
                {
                    Location = location,
                    Capacity = 100, // Set capacity to 100 liters
                    LastEmptied = DateTime.Now // Set last emptied time to current time
                };

                // Generate fill levels for each day
                for (int day = 0; day < numDays; day++)
                {
                    var currentDay = DateTime.Today.AddDays(day);
                    var currentFillLevel = 0;

                    // Generate fill levels for every 3-4 hours
                    for (int hour = 0; hour < 24; hour += _random.Next(3, 5))
                    {
                        var fillLevel = GetFillLevel(currentDay.AddHours(hour).Hour);
                        currentFillLevel += fillLevel;

                        // Ensure the fill level doesn't exceed the capacity
                        if (currentFillLevel > bin.Capacity)
                        {
                            currentFillLevel = bin.Capacity;
                        }
                    }

                    bin.CurrentFillLevel = currentFillLevel;
                }

                bins.Add(bin);
            }

            await _context.WasteBins.AddRangeAsync(bins);
            await _context.SaveChangesAsync();
        }

        private int GetFillLevel(int hour)
        {
            // Assume that the bins tend to fill up more during the day and less at night.
            var baseFillLevel = _random.Next(5, 15); // Base fill level per hour
            var dayFillIncrease = _random.Next(5, 10); // Additional fill level per hour during the day
            var nightFillDecrease = _random.Next(0, 5); // Decrease in fill level per hour at night

            if (hour >= 6 && hour <= 18) // Daytime
            {
                return baseFillLevel + dayFillIncrease;
            }
            else // Nighttime
            {
                return baseFillLevel - nightFillDecrease;
            }
        }

    }
}
