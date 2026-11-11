using API.Geo;

namespace API.Services
{
    /// <summary>
    /// Closed-tour heuristic for waste collection: nearest-neighbour construction from the depot,
    /// then 2-opt until no swap shortens the tour. Pure and deterministic.
    /// </summary>
    public static class RouteOptimizer
    {
        private const double MinImprovementMeters = 1e-9;

        /// <summary>Visiting order (indices into <paramref name="stops"/>) by repeatedly going to the nearest unvisited stop.</summary>
        public static IReadOnlyList<int> NearestNeighbour(LatLng depot, IReadOnlyList<LatLng> stops)
        {
            var visited = new bool[stops.Count];
            var order = new List<int>(stops.Count);
            var current = depot;

            for (var step = 0; step < stops.Count; step++)
            {
                var best = -1;
                var bestDistance = double.MaxValue;
                for (var i = 0; i < stops.Count; i++)
                {
                    if (visited[i]) continue;
                    var distance = GeoService.HaversineMeters(current, stops[i]);
                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        best = i;
                    }
                }

                visited[best] = true;
                order.Add(best);
                current = stops[best];
            }

            return order;
        }

        /// <summary>Improves a visiting order with 2-opt on the closed tour depot -> stops -> depot. The depot never moves.</summary>
        public static IReadOnlyList<int> TwoOpt(LatLng depot, IReadOnlyList<LatLng> stops, IReadOnlyList<int> order)
        {
            // points[0] is the depot; points[k] is stops[order[k - 1]].
            var points = new LatLng[order.Count + 1];
            points[0] = depot;
            for (var k = 0; k < order.Count; k++) points[k + 1] = stops[order[k]];

            var tour = Enumerable.Range(0, points.Length).ToArray();
            var m = tour.Length;

            var improved = true;
            while (improved)
            {
                improved = false;
                for (var i = 1; i < m - 1; i++)
                {
                    for (var j = i + 1; j < m; j++)
                    {
                        var a = points[tour[i - 1]];
                        var b = points[tour[i]];
                        var c = points[tour[j]];
                        var d = points[tour[(j + 1) % m]];

                        var delta = GeoService.HaversineMeters(a, c) + GeoService.HaversineMeters(b, d)
                                  - GeoService.HaversineMeters(a, b) - GeoService.HaversineMeters(c, d);
                        if (delta < -MinImprovementMeters)
                        {
                            Array.Reverse(tour, i, j - i + 1);
                            improved = true;
                        }
                    }
                }
            }

            return tour.Skip(1).Select(k => order[k - 1]).ToList();
        }

        /// <summary>Nearest-neighbour followed by 2-opt.</summary>
        public static IReadOnlyList<int> Optimize(LatLng depot, IReadOnlyList<LatLng> stops)
        {
            if (stops.Count == 0) return Array.Empty<int>();
            return TwoOpt(depot, stops, NearestNeighbour(depot, stops));
        }

        /// <summary>Length in metres of the closed tour depot -> stops in <paramref name="order"/> -> depot.</summary>
        public static double TourLengthMeters(LatLng depot, IReadOnlyList<LatLng> stops, IReadOnlyList<int> order)
        {
            var total = 0.0;
            var previous = depot;
            foreach (var index in order)
            {
                total += GeoService.HaversineMeters(previous, stops[index]);
                previous = stops[index];
            }
            if (order.Count > 0) total += GeoService.HaversineMeters(previous, depot);
            return total;
        }
    }
}
