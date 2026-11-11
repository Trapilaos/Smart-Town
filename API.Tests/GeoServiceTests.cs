using API.Geo;

namespace API.Tests;

public class GeoServiceTests
{
    private static GeoGeometry Square(double minLng, double minLat, double maxLng, double maxLat) => new(new[]
    {
        new GeoPolygon(new IReadOnlyList<GeoPoint>[]
        {
            new[]
            {
                new GeoPoint(minLng, minLat), new GeoPoint(maxLng, minLat), new GeoPoint(maxLng, maxLat),
                new GeoPoint(minLng, maxLat), new GeoPoint(minLng, minLat),
            },
        }),
    });

    [Fact]
    public void Point_inside_a_polygon_is_inside()
    {
        var geometry = Square(23.7, 37.8, 23.8, 37.9);
        Assert.True(GeoService.IsInside(37.86, 23.75, geometry));
    }

    [Fact]
    public void Point_outside_a_polygon_is_outside()
    {
        var geometry = Square(23.7, 37.8, 23.8, 37.9);
        Assert.False(GeoService.IsInside(38.0, 23.75, geometry));
        Assert.False(GeoService.IsInside(37.86, 23.9, geometry));
    }

    [Fact]
    public void Point_in_a_hole_is_outside_but_point_in_the_ring_is_inside()
    {
        var geometry = new GeoGeometry(new[]
        {
            new GeoPolygon(new IReadOnlyList<GeoPoint>[]
            {
                new[] { new GeoPoint(0, 0), new GeoPoint(10, 0), new GeoPoint(10, 10), new GeoPoint(0, 10), new GeoPoint(0, 0) },
                new[] { new GeoPoint(4, 4), new GeoPoint(6, 4), new GeoPoint(6, 6), new GeoPoint(4, 6), new GeoPoint(4, 4) },
            }),
        });

        Assert.False(GeoService.IsInside(5, 5, geometry)); // inside the hole
        Assert.True(GeoService.IsInside(2, 2, geometry));  // inside the outer ring, outside the hole
    }

    [Fact]
    public void MultiPolygon_matches_any_member()
    {
        var geometry = new GeoGeometry(new[]
        {
            Square(0, 0, 1, 1).Polygons[0],
            Square(5, 5, 6, 6).Polygons[0],
        });

        Assert.True(GeoService.IsInside(0.5, 0.5, geometry));
        Assert.True(GeoService.IsInside(5.5, 5.5, geometry));
        Assert.False(GeoService.IsInside(3, 3, geometry));
    }

    [Fact]
    public void Haversine_gives_about_111_km_per_degree_of_latitude()
    {
        var metres = GeoService.HaversineMeters(37, 23, 38, 23);
        Assert.InRange(metres, 111_000, 111_400);
    }

    [Fact]
    public void Haversine_is_zero_for_the_same_point()
    {
        Assert.Equal(0, GeoService.HaversineMeters(37.865, 23.753, 37.865, 23.753), precision: 6);
    }

    [Fact]
    public void Parses_a_feature_and_normalises_it_to_a_polygon()
    {
        const string json = """
            {"type":"Feature","properties":{"name":"x"},"geometry":{"type":"Polygon","coordinates":[[[23.7,37.8],[23.8,37.8],[23.8,37.9],[23.7,37.9],[23.7,37.8]]]}}
            """;

        var geometry = GeoService.ParseGeoJson(json);

        Assert.Single(geometry.Polygons);
        Assert.Contains("\"type\":\"Polygon\"", GeoService.ToGeoJson(geometry));
    }

    [Fact]
    public void FeatureCollection_with_two_polygons_becomes_a_multipolygon()
    {
        const string json = """
            {"type":"FeatureCollection","features":[
              {"type":"Feature","geometry":{"type":"Polygon","coordinates":[[[0,0],[1,0],[1,1],[0,0]]]}},
              {"type":"Feature","geometry":{"type":"MultiPolygon","coordinates":[[[[5,5],[6,5],[6,6],[5,5]]]]}}
            ]}
            """;

        var geometry = GeoService.ParseGeoJson(json);

        Assert.Equal(2, geometry.Polygons.Count);
        Assert.Contains("\"type\":\"MultiPolygon\"", GeoService.ToGeoJson(geometry));
    }

    [Theory]
    [InlineData("""{"type":"Polygon","coordinates":[[[0,0],[1,0],[1,1],[0,1]]]}""", "closed")]
    [InlineData("""{"type":"Polygon","coordinates":[[[0,0],[1,0],[0,0]]]}""", "4 positions")]
    [InlineData("""{"type":"Polygon","coordinates":[[[0,0],[1,0],[1,95],[0,0]]]}""", "lat")]
    [InlineData("""{"type":"Polygon","coordinates":[[[0,0],[200,0],[1,1],[0,0]]]}""", "lng")]
    [InlineData("""{"type":"Point","coordinates":[1,2]}""", "Polygon")]
    [InlineData("""{"type":"FeatureCollection","features":[{"type":"Feature","geometry":{"type":"Point","coordinates":[1,2]}}]}""", "Polygon")]
    [InlineData("""{"type":"Polygon","coordinates":[[["a","b"],[1,0],[1,1],["a","b"]]]}""", "numbers")]
    [InlineData("not json", "valid JSON")]
    public void Invalid_geojson_is_rejected_with_a_clear_message(string json, string expectedFragment)
    {
        var error = Assert.Throws<GeoJsonException>(() => GeoService.ParseGeoJson(json));
        Assert.Contains(expectedFragment, error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Oversized_geojson_is_rejected()
    {
        var padding = new string(' ', GeoService.MaxGeoJsonBytes);
        var json = "{\"type\":\"Polygon\",\"coordinates\":[[" + padding + "]]}";

        var error = Assert.Throws<GeoJsonException>(() => GeoService.ParseGeoJson(json));
        Assert.Contains("2 MB", error.Message);
    }
}
