using API.Geo;
using API.Services;

namespace API.Tests;

public class RouteOptimizerTests
{
    private static readonly LatLng Depot = new(37.8650, 23.7530);

    private static List<LatLng> RandomStops(int seed, int count)
    {
        var random = new Random(seed);
        return Enumerable.Range(0, count)
            .Select(_ => new LatLng(37.855 + random.NextDouble() * 0.02, 23.745 + random.NextDouble() * 0.02))
            .ToList();
    }

    [Fact]
    public void Nearest_neighbour_visits_every_stop_exactly_once()
    {
        var stops = RandomStops(1, 12);

        var order = RouteOptimizer.NearestNeighbour(Depot, stops);

        Assert.Equal(stops.Count, order.Count);
        Assert.Equal(Enumerable.Range(0, stops.Count), order.OrderBy(i => i));
    }

    [Fact]
    public void Nearest_neighbour_picks_the_closest_stop_first()
    {
        var stops = new List<LatLng> { new(37.90, 23.80), new(37.8651, 23.7531), new(37.87, 23.76) };

        var order = RouteOptimizer.NearestNeighbour(Depot, stops);

        Assert.Equal(1, order[0]);
    }

    [Fact]
    public void Two_opt_never_makes_the_tour_longer_than_nearest_neighbour()
    {
        for (var seed = 1; seed <= 10; seed++)
        {
            var stops = RandomStops(seed, 15);
            var nearest = RouteOptimizer.NearestNeighbour(Depot, stops);
            var improved = RouteOptimizer.TwoOpt(Depot, stops, nearest);

            var nearestLength = RouteOptimizer.TourLengthMeters(Depot, stops, nearest);
            var improvedLength = RouteOptimizer.TourLengthMeters(Depot, stops, improved);

            Assert.True(improvedLength <= nearestLength + 1e-6, $"seed {seed}: 2-opt {improvedLength} > NN {nearestLength}");
        }
    }

    [Fact]
    public void Two_opt_removes_a_crossing_that_nearest_neighbour_leaves_behind()
    {
        // Depot at the origin; the stops sit on a line, and a bad starting order crosses itself.
        var stops = new List<LatLng>
        {
            new(0, 1), new(0, 3), new(0, 2), new(0, 4),
        };
        var crossed = new[] { 1, 0, 2, 3 }; // visits positions 3, 1, 2, 4 along the line: it doubles back

        var improved = RouteOptimizer.TwoOpt(new LatLng(0, 0), stops, crossed);

        Assert.True(
            RouteOptimizer.TourLengthMeters(new LatLng(0, 0), stops, improved)
            < RouteOptimizer.TourLengthMeters(new LatLng(0, 0), stops, crossed));
    }

    [Fact]
    public void Optimize_visits_every_stop_once_and_the_depot_is_not_in_the_order()
    {
        var stops = RandomStops(7, 20);

        var order = RouteOptimizer.Optimize(Depot, stops);

        Assert.Equal(stops.Count, order.Count);
        Assert.Equal(Enumerable.Range(0, stops.Count), order.OrderBy(i => i));
    }

    [Fact]
    public void Empty_route_has_zero_length()
    {
        var order = RouteOptimizer.Optimize(Depot, new List<LatLng>());

        Assert.Empty(order);
        Assert.Equal(0, RouteOptimizer.TourLengthMeters(Depot, new List<LatLng>(), order));
    }
}
