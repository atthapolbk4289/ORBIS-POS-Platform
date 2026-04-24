using PosSystem.Models.Dtos;
using System.Threading.Tasks;

namespace PosSystem.Services.Interfaces
{
    public interface IAuthService
    {
        Task<LoginResult> LoginAsync(string username, string password, string ipAddress, string userAgent);
    }
}
