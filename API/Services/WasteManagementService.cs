using API.Data;
using API.DTOs;
using API.Entities;
using API.Interfaces;
using AutoMapper;
using Microsoft.EntityFrameworkCore;

namespace API.Services
{
    /// <summary>
    /// Service for managing waste bins and optimizing waste collection routes.
    /// </summary>
    public class WasteManagementService : IWasteManagementService
    {
        private readonly DataContext _context;
        private readonly IMapper _mapper;
        private readonly Dictionary<string, List<string>> _graph; // Graph of waste bin locations
        private readonly Dictionary<(string, string), int> _distances; // Distances between waste bin locations

        public WasteManagementService(DataContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
            _graph = CreateGraph();
            _distances = CreateDistances();
        }

        /// <summary>
        /// Gets all waste bins.
        /// </summary>
        /// <returns>List of waste bin DTOs.</returns>
        public async Task<List<WasteBinDTO>> GetWasteBinsAsync()
        {
            var wasteBins = await _context.WasteBins.ToListAsync();
            return _mapper.Map<List<WasteBinDTO>>(wasteBins);
        }

        /// <summary>
        /// Updates a waste bin's details.
        /// </summary>
        /// <param name="wasteBinDto">The waste bin DTO with updated details.</param>
        /// <returns>The updated waste bin DTO.</returns>
        public async Task<WasteBinDTO> UpdateWasteBinAsync(WasteBinDTO wasteBinDto)
        {
            var wasteBin = await _context.WasteBins.FindAsync(wasteBinDto.Id);
            if (wasteBin == null) return null;

            wasteBin.CurrentFillLevel = wasteBinDto.CurrentFillLevel;
            wasteBin.LastEmptied = wasteBinDto.LastEmptied;

            await _context.SaveChangesAsync();
            return _mapper.Map<WasteBinDTO>(wasteBin);
        }

        /// <summary>
        /// Gets the optimal path for waste collection using Particle Swarm Optimization (PSO).
        /// </summary>
        /// <returns>A tuple containing a boolean indicating if the path was found and the path itself.</returns>
        public async Task<(bool, List<string>)> GetOptimalPathAsync()
        {
            var wasteBins = await _context.WasteBins.ToListAsync();

            if (!wasteBins.Any(bin => bin.CurrentFillLevel > 0))
            {
                return (false, new List<string> { "No need for optimal path, all bins are empty" });
            }

            var currentTime = DateTime.Now;
            var trafficData = await _context.TrafficData
                .Where(td => td.Timestamp >= currentTime.Date && td.Timestamp < currentTime.Date.AddDays(1))
                .ToListAsync();

            var trafficFlow = GetTrafficFlow(trafficData);
            var startLocation = wasteBins.OrderByDescending(bin => bin.CurrentFillLevel).First().Location;
            var optimalPath = PSOAlgorithm(startLocation, wasteBins, trafficFlow);

            return (true, optimalPath);
        }

        /// <summary>
        /// Creates a graph of the waste bin locations.
        /// </summary>
        /// <returns>A dictionary representing the graph.</returns>
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

        /// <summary>
        /// Creates a dictionary of distances between waste bin locations.
        /// </summary>
        /// <returns>A dictionary with distances between locations.</returns>
        private Dictionary<(string, string), int> CreateDistances()
        {
            return new Dictionary<(string, string), int>
            {
                { ("3is Septemvriou", "Alexandras"), 200 },
                { ("3is Septemvriou", "Stadiou"), 300 },
                { ("Alexandras", "3is Septemvriou"), 200 },
                { ("Alexandras", "Stadiou"), 150 },
                { ("Alexandras", "Vouliagmenis"), 400 },
                { ("Stadiou", "3is Septemvriou"), 300 },
                { ("Stadiou", "Alexandras"), 150 },
                { ("Stadiou", "Vouliagmenis"), 200 },
                { ("Vouliagmenis", "Alexandras"), 400 },
                { ("Vouliagmenis", "Stadiou"), 200 },
                { ("3is Septemvriou", "Vouliagmenis"), 250 },
                { ("Vouliagmenis", "3is Septemvriou"), 250 }
            };
        }

        /// <summary>
        /// Gets traffic flow data for each street.
        /// </summary>
        /// <param name="trafficData">List of traffic data.</param>
        /// <returns>A dictionary with traffic flow for each street.</returns>
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

        /// <summary>
        /// Gets traffic flow for a specific street.
        /// </summary>
        /// <param name="trafficData">List of traffic data.</param>
        /// <param name="street">The street to get traffic flow for.</param>
        /// <returns>Traffic flow for the street.</returns>
        private int GetTrafficFlowForStreet(List<TrafficData> trafficData, string street)
        {
            return trafficData
                .Where(td => td.Location == street)
                .OrderByDescending(td => td.Timestamp)
                .FirstOrDefault()?.TrafficFlow ?? 0;
        }

        /// <summary>
        /// Implements the Particle Swarm Optimization (PSO) algorithm to find the optimal path.
        /// </summary>
        /// <param name="start">The start location.</param>
        /// <param name="wasteBins">List of waste bins.</param>
        /// <param name="trafficFlow">Traffic flow data.</param>
        /// <returns>List of locations representing the optimal path.</returns>
        public List<string> PSOAlgorithm(string start, List<WasteBin> wasteBins, Dictionary<string, int> trafficFlow)
        {
            int numParticles = 30;
            int maxIterations = 100;
            var nodes = wasteBins.Select(bin => bin.Location).Distinct().ToList();
            var particles = InitializeParticles(start, nodes, numParticles, trafficFlow);
            var bestParticle = particles.OrderBy(p => p.BestCost).First();

            for (int i = 0; i < maxIterations; i++)
            {
                foreach (var particle in particles)
                {
                    particle.UpdatePosition(GenerateNewPosition(particle.Position, wasteBins));
                    if (particle.BestCost < bestParticle.BestCost)
                    {
                        bestParticle = particle;
                    }
                }
            }

            return bestParticle.BestPosition;
        }

        /// <summary>
        /// Initializes particles for the PSO algorithm.
        /// </summary>
        /// <param name="start">The start location.</param>
        /// <param name="nodes">List of nodes (locations).</param>
        /// <param name="numParticles">Number of particles.</param>
        /// <param name="trafficFlow">Traffic flow data.</param>
        /// <returns>List of initialized particles.</returns>
        private List<Particle> InitializeParticles(string start, List<string> nodes, int numParticles, Dictionary<string, int> trafficFlow)
        {
            var particles = new List<Particle>();
            for (int i = 0; i < numParticles; i++)
            {
                var initialPosition = new List<string> { start };
                var random = new Random();
                var remainingNodes = nodes
                    .Where(node => _context.WasteBins.FirstOrDefault(bin => bin.Location == node)?.CurrentFillLevel > 0)
                    .Except(new[] { start })
                    .ToList();

                while (remainingNodes.Count > 0)
                {
                    int index = random.Next(remainingNodes.Count);
                    string node = remainingNodes[index];

                    if (node != initialPosition.Last())
                    {
                        initialPosition.Add(node);
                        remainingNodes.RemoveAt(index);
                    }
                }

                initialPosition.Add(start);
                particles.Add(new Particle(initialPosition, _distances, trafficFlow));
            }
            return particles;
        }

        /// <summary>
        /// Generates a new position for a particle in the PSO algorithm.
        /// </summary>
        /// <param name="currentPosition">The current position of the particle.</param>
        /// <param name="wasteBins">List of waste bins.</param>
        /// <returns>A new position for the particle.</returns>
        private List<string> GenerateNewPosition(List<string> currentPosition, List<WasteBin> wasteBins)
        {
            var newPosition = new List<string>();
            var random = new Random();

            // Start with the same node as the current position
            newPosition.Add(currentPosition[0]);

            // Try to create a path with only non-empty nodes
            var nonEmptyNodes = wasteBins
                .Where(bin => bin.CurrentFillLevel > 0 && bin.Location != currentPosition[0])
                .Select(bin => bin.Location)
                .ToList();

            if (nonEmptyNodes.Count >= currentPosition.Count - 2)
            {
                // Visit all non-empty nodes
                while (nonEmptyNodes.Count > 0)
                {
                    int index = random.Next(nonEmptyNodes.Count);
                    string node = nonEmptyNodes[index];

                    newPosition.Add(node);
                    nonEmptyNodes.RemoveAt(index);
                }
            }
            else
            {
                // If there aren't enough non-empty nodes, visit all nodes
                var remainingNodes = wasteBins
                    .Where(bin => bin.Location != currentPosition[0])
                    .Select(bin => bin.Location)
                    .ToList();

                while (remainingNodes.Count > 0)
                {
                    int index = random.Next(remainingNodes.Count);
                    string node = remainingNodes[index];

                    newPosition.Add(node);
                    remainingNodes.RemoveAt(index);
                }
            }

            // End with the same node as the start
            newPosition.Add(currentPosition[0]);

            return newPosition;
        }
    }
}
