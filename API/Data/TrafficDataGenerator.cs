using API.Entities;


namespace API.Data
{
    public static class TrafficDataGenerator
    {
        public static List<TrafficData> GenerateTrafficData()
        {
            var trafficDataList = new List<TrafficData>();
            var random = new Random();
            var locations = new[] { "3is Septemvriou", "Alexandras", "Stadiou", "Vouliagmenis" };
            var currentTime = DateTime.Now;
            int id = 1; // Initialize id to 1

            var peakHours = new[] { 7, 8, 9, 16, 17, 18 };

            foreach (var location in locations)
            {
                for (int i = 0; i < 24; i++) // Generate hourly data for all streets
                {
                    int trafficFlow;
                    if (peakHours.Contains(i))
                    {
                        // Generate higher traffic flow during peak hours
                        trafficFlow = random.Next(60, 101);
                    }
                    else
                    {
                        // Generate lower traffic flow during non-peak hours
                        trafficFlow = random.Next(0, 60);
                    }

                    trafficDataList.Add(new TrafficData
                    {
                        Id = id++, // Assign unique id to each TrafficData entity
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