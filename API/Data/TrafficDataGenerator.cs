using API.Entities;

namespace API.Data
{
    public static class TrafficDataGenerator
    {
        /// <summary>
        /// Generates hourly traffic data for multiple locations.
        /// </summary>
        /// <returns>A list of traffic data entities.</returns>
        public static List<TrafficData> GenerateTrafficData()
        {
            var trafficDataList = new List<TrafficData>();
            var random = new Random();
            var locations = new[] { "3is Septemvriou", "Alexandras", "Stadiou", "Vouliagmenis" };
            var currentTime = DateTime.Now;
            int id = 1; // Initialize id to 1

            var peakHours = new[] { 7, 8, 9, 16, 17, 18 };

            // Generate hourly traffic data for all streets
            foreach (var location in locations)
            {
                for (int i = 0; i < 24; i++)
                {
                    int trafficFlow;

                    // Generate different traffic flow values for peak and non-peak hours
                    if (peakHours.Contains(i))
                    {
                        // Generate higher traffic flow during peak hours (between 60 and 100)
                        trafficFlow = random.Next(60, 101);
                    }
                    else
                    {
                        // Generate lower traffic flow during non-peak hours (between 0 and 59)
                        trafficFlow = random.Next(0, 60);
                    }

                    // Create a new TrafficData entity with the generated data
                    trafficDataList.Add(new TrafficData
                    {
                        Id = id++, // Assign a unique ID to each TrafficData entity
                        Location = location,
                        TrafficFlow = trafficFlow, // Traffic flow percentage between 0 and 100
                        Timestamp = currentTime.AddHours(i)
                    });
                }
            }

            return trafficDataList;
        }
    }
}
