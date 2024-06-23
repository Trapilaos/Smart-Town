using API.Entities;
using System;
using System.Collections.Generic;

namespace API.Data
{
    public static class TrafficDataGenerator
    {
        public static List<TrafficData> GenerateTrafficData()
        {
            var trafficDataList = new List<TrafficData>();
            var random = new Random();
            var locations = new[] { "Main Street", "Secondary Street", "Third Street", "Fourth Circle" };
            var currentTime = DateTime.Now;
            int id = 1; // Initialize id to 1

            var peakHours = new[] { 7, 8, 9, 16, 17, 18 };

            foreach (var location in locations)
            {
                for (int i = 0; i < 24; i++) // Generate hourly data for all streets
                {
                    trafficDataList.Add(new TrafficData
                    {
                        Id = id++, // Assign unique id to each TrafficData entity
                        Location = location,
                        TrafficFlow = random.Next(0, 101), // Traffic flow percentage between 0 and 100
                        Timestamp = currentTime.AddHours(i)
                    });
                }
            }

            return trafficDataList;
        }
    }
}
