using Cddo.Data.Marketplace.Api.Dto.Requests.ManageUser;
using Cddo.Data.Marketplace.Api.Dto.Responses.ManageUser;

namespace Cddo.Data.Marketplace.Logic.Services.Interfaces;

public interface IManageOrganisationService
{
    Task<ManageOrganisationsResponse?> GetManageOrganisationsAsync(ManageOrganisationsRequest manageOrganisationRequest, CancellationToken cancellationToken = default);
}