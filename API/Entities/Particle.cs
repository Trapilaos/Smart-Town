public class Particle
{
    public List<string> Position { get; set; }
    public List<string> BestPosition { get; set; }
    public double BestCost { get; set; }
    private readonly Dictionary<(string, string), int> _distances;
    private readonly Dictionary<string, int> _trafficFlow;

    public Particle(List<string> initialPosition, Dictionary<(string, string), int> distances, Dictionary<string, int> trafficFlow)
    {
        Position = initialPosition;
        BestPosition = initialPosition;
        _distances = distances;
        _trafficFlow = trafficFlow;
        BestCost = CalculateCost(Position);
    }

    public double CalculateCost(List<string> path)
    {
        double cost = 0;
        for (int i = 0; i < path.Count - 1; i++)
        {
            string currentLocation = path[i];
            string nextLocation = path[i + 1];

            if (_distances.TryGetValue((currentLocation, nextLocation), out int distance))
            {
                cost += distance + _trafficFlow[nextLocation];
            }
            else
            {
                throw new Exception($"The given key '({currentLocation}, {nextLocation})' was not present in the dictionary.");
            }
        }
        return cost;
    }

    public void UpdatePosition(List<string> newPosition)
    {
        Position = newPosition;
        double cost = CalculateCost(Position);
        if (cost < BestCost)
        {
            BestCost = cost;
            BestPosition = newPosition;
        }
    }
}
