using Cddo.Data.Marketplace.Api.Dto.ManageUser;
using Cddo.Data.Marketplace.Api.Dto.Requests;
using Cddo.Data.Marketplace.Api.Dto.Responses.ManageUser;

namespace Cddo.Data.Marketplace.Logic.Services
{
    public interface IManageDepartmentsService
    {
        Task<ManageDepartmentsResponse?> GetManageDepartmentsAsync(ManageDepartmentRequest manageDepartmentRequest, CancellationToken cancellationToken = default);
        Task<bool> PostAddDepartmentAsync(string departmentName, CancellationToken cancellationToken = default);
        Task<Department?> GetDepartmentByIdAsync(int id, CancellationToken cancellationToken = default);

    }
}