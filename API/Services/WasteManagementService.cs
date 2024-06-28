using API.Data;
using API.DTOs;
using API.Entities;
using API.Interfaces;
using AutoMapper;
using Microsoft.EntityFrameworkCore;


namespace API.Services
{
    public class WasteManagementService : IWasteManagementService
    {
        private readonly DataContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<WasteManagementService> _logger;
        private readonly Dictionary<string, List<string>> _graph;
        private readonly Dictionary<(string, string), int> _distances;

        public WasteManagementService(DataContext context, IMapper mapper, ILogger<WasteManagementService> logger)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
            _graph = CreateGraph();
            _distances = CreateDistances();
        }

        public async Task<List<WasteBinDTO>> GetWasteBinsAsync()
        {
            var wasteBins = await _context.WasteBins.ToListAsync();
            return _mapper.Map<List<WasteBinDTO>>(wasteBins);
        }

        public async Task<WasteBinDTO> UpdateWasteBinAsync(WasteBinDTO wasteBinDto)
        {
            var wasteBin = await _context.WasteBins.FindAsync(wasteBinDto.Id);
            if (wasteBin == null) return null;

            wasteBin.CurrentFillLevel = wasteBinDto.CurrentFillLevel;
            wasteBin.LastEmptied = wasteBinDto.LastEmptied;

            await _context.SaveChangesAsync();
            return _mapper.Map<WasteBinDTO>(wasteBin);
        }

        public async Task<List<string>> GetOptimalPathAsync()
        {
            var wasteBins = await _context.WasteBins.ToListAsync();
            var heuristics = GetHeuristics(wasteBins);

            _logger.LogInformation("Waste Bins: {@WasteBins}", wasteBins);
            _logger.LogInformation("Heuristics: {@Heuristics}", heuristics);

            var currentTime = DateTime.Now;
            var trafficData = await _context.TrafficData
                .Where(td => td.Timestamp >= currentTime.Date && td.Timestamp < currentTime.Date.AddDays(1))
                .ToListAsync();

            var trafficFlow = GetTrafficFlow(trafficData);

            _logger.LogInformation("Traffic Flow: {@TrafficFlow}", trafficFlow);

            var startLocation = wasteBins.OrderByDescending(bin => bin.CurrentFillLevel).First().Location;

            _logger.LogInformation("Start Location: {StartLocation}", startLocation);

            var optimalPath = TSPAlgorithm(startLocation, _graph, trafficFlow);

            _logger.LogInformation("Optimal Path: {@OptimalPath}", optimalPath);

            return optimalPath;
        }

        private Dictionary<string, List<string>> CreateGraph()
        {
            return new Dictionary<string, List<string>>
            {
                { "3is Septemvriou", new List<string> { "Alexandras", "Stadiou" } },
                { "Alexandras", new List<string> { "3is Septemvriou", "Stadiou", "Vouliagmenis" } },
                { "Stadiou", new List<string> { "3is Septemvriou", "Alexandras", "Vouliagmenis" } },
                { "Vouliagmenis", new List<string> { "Alexandras", "Stadiou" } }
            };
        }

        private Dictionary<(string, string), int> CreateDistances()
        {
            return new Dictionary<(string, string), int>
            {
                { ("3is Septemvriou", "Alexandras"), 50 },
                { ("3is Septemvriou", "Stadiou"), 100 },
                { ("Alexandras", "3is Septemvriou"), 50 },
                { ("Alexandras", "Stadiou"), 50 },
                { ("Alexandras", "Vouliagmenis"), 100 },
                { ("Stadiou", "3is Septemvriou"), 100 },
                { ("Stadiou", "Alexandras"), 50 },
                { ("Stadiou", "Vouliagmenis"), 50 },
                { ("Vouliagmenis", "Alexandras"), 100 },
                { ("Vouliagmenis", "Stadiou"), 50 }
            };
        }

        private Dictionary<string, int> GetTrafficFlow(List<TrafficData> trafficData)
        {
            return new Dictionary<string, int>
            {
                { "3is Septemvriou", GetTrafficFlowForStreet(trafficData, "3is Septemvriou") },
                { "Alexandras", GetTrafficFlowForStreet(trafficData, "Alexandras") },
                { "Stadiou", GetTrafficFlowForStreet(trafficData, "Stadiou") },
                { "Vouliagmenis", GetTrafficFlowForStreet(trafficData, "Vouliagmenis") }
            };
        }

        private int GetTrafficFlowForStreet(List<TrafficData> trafficData, string street)
        {
            var trafficFlow = trafficData
                .Where(td => td.Location == street)
                .OrderByDescending(td => td.Timestamp)
                .FirstOrDefault()?.TrafficFlow ?? 0;

            return trafficFlow;
        }

        private Dictionary<string, int> GetHeuristics(List<WasteBin> wasteBins)
        {
            var uniqueWasteBins = wasteBins
                .GroupBy(bin => bin.Location)
                .Select(group => group.First())
                .ToList();

            var heuristics = new Dictionary<string, int>();
            var maxFillLevel = wasteBins.Max(bin => bin.CurrentFillLevel);

            foreach (var bin in uniqueWasteBins)
            {
                heuristics[bin.Location] = maxFillLevel - bin.CurrentFillLevel;
            }

            return heuristics;
        }

        private List<string> TSPAlgorithm(string start, Dictionary<string, List<string>> graph, Dictionary<string, int> trafficFlow)
        {
            var unvisitedNodes = new HashSet<string>(graph.Keys);
            unvisitedNodes.Remove(start);

            var currentPath = new List<string> { start };

            while (unvisitedNodes.Count > 0)
            {
                var nextNode = FindClosestUnvisitedNode(currentPath.Last(), unvisitedNodes, trafficFlow);
                currentPath.Add(nextNode);
                unvisitedNodes.Remove(nextNode);
            }

            currentPath.Add(start); // Add the start node to the end of the path to complete the circuit

            return OptimizePathWith2OptSwap(currentPath, trafficFlow);
        }

        private string FindClosestUnvisitedNode(string current, HashSet<string> unvisitedNodes, Dictionary<string, int> trafficFlow)
        {
            string closestNode = null;
            int minDistance = int.MaxValue;

            foreach (var neighbor in _graph[current])
            {
                if (!unvisitedNodes.Contains(neighbor)) continue;

                var distance = trafficFlow.GetValueOrDefault(neighbor, int.MaxValue);

                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestNode = neighbor;
                }
            }

            return closestNode;
        }

        private List<string> OptimizePathWith2OptSwap(List<string> path, Dictionary<string, int> trafficFlow)
        {
            bool improved = true;

            while (improved)
            {
                improved = false;

                for (int i = 1; i < path.Count - 2; i++)
                {
                    for (int j = i + 1; j < path.Count - 1; j++)
                    {
                        var newPath = SwapPathSegments(path, i, j);
                        var newPathCost = CalculatePathCost(newPath, trafficFlow);
                        var currentPathCost = CalculatePathCost(path, trafficFlow);

                        if (newPathCost < currentPathCost)
                        {
                            path = newPath;
                            improved = true;
                            break;
                        }
                    }
                }
            }

            return path;
        }

        private List<string> SwapPathSegments(List<string> path, int i, int j)
        {
            var reversedSegment = path.Skip(i).Take(j - i + 1).Reverse().ToList();
            var newPath = path.Take(i).Concat(reversedSegment).Concat(path.Skip(j + 1)).ToList();
            return newPath;
        }

        private int CalculatePathCost(List<string> path, Dictionary<string, int> trafficFlow)
        {
            int cost = 0;

            for (int i = 0; i < path.Count - 1; i++)
            {
                cost += trafficFlow.GetValueOrDefault(path[i], int.MaxValue) + _distances.GetValueOrDefault((path[i], path[i + 1]), int.MaxValue);
            }

            return cost;
        }
    }
}
