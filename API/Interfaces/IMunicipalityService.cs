using API.DTOs;
using API.Entities;
using API.Geo;
using API.Services;

namespace API.Interfaces
{
    public interface IMunicipalityService
    {
        /// <summary>The tracked single municipality row. Callers must not expose it directly.</summary>
        Task<Municipality> GetEntityAsync();
        Task<MunicipalityDTO> GetAsync();
        /// <summary>Updates settings. Throws <see cref="GeoJsonException"/> for an invalid boundary.</summary>
        Task<MunicipalityDTO> UpdateAsync(MunicipalityUpdateDTO dto);
        Task<GeoGeometry> GetBoundaryAsync();
        Task<bool> IsInsideBoundaryAsync(double lat, double lng);
        /// <summary>Stores a boundary chosen from OpenStreetMap, along with the centre and source metadata.</summary>
        Task ApplyBoundaryAsync(BoundaryCandidate candidate);
    }
}
