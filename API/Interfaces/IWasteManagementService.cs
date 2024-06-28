using API.DTOs;

namespace API.Interfaces
{
    public interface IWasteManagementService
    {
        Task<List<WasteBinDTO>> GetWasteBinsAsync();
        Task<WasteBinDTO> UpdateWasteBinAsync(WasteBinDTO wasteBinDto);
        Task<List<string>> GetOptimalPathAsync();
    }
}
