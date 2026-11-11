using API.DTOs;
using API.Entities;

namespace API.Interfaces
{
    public interface IWasteService
    {
        Task<List<WasteBinDTO>> GetActiveBinsAsync(WasteType? type);
        /// <summary>Throws <see cref="API.Errors.DomainValidationException"/> when the bin is outside the boundary.</summary>
        Task<WasteBinDTO> CreateBinAsync(WasteBinInputDTO input);
        /// <summary>Returns null when the bin does not exist.</summary>
        Task<WasteBinDTO> UpdateBinAsync(int id, WasteBinInputDTO input);
        /// <summary>Soft delete. Returns false when the bin does not exist.</summary>
        Task<bool> DeactivateBinAsync(int id);
        /// <summary>Sets the fill to zero and LastEmptied to now. Returns null when the bin does not exist.</summary>
        Task<WasteBinDTO> MarkEmptiedAsync(int id);
        Task<WasteRouteDTO> BuildRouteAsync(WasteType? type, int minFillPercent);
    }
}
