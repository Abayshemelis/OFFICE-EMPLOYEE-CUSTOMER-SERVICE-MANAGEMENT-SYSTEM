using System.Collections.Generic;
using System.Threading.Tasks;
using OECSMS.Domain.Entities;

namespace OECSMS.Application.Interfaces
{
    public interface IContactManagerRequestRepository
    {
        Task<ContactManagerRequest> AddAsync(ContactManagerRequest request);
        Task<ContactManagerRequest> GetByIdAsync(int id);
        Task<IEnumerable<ContactManagerRequest>> GetAllAsync();
        Task UpdateAsync(ContactManagerRequest request);
        Task DeleteAsync(int id);
    }
}
