using Cddo.Data.Marketplace.Api.Dto.ManageUser;

namespace Cddo.Data.Marketplace.Logic.Services.Interfaces;

public interface IManageOrganisationsService
{
    Task<OrganisationDetail?> GetOrganisationAsync(int organisationId, CancellationToken cancellationToken = default);

    Task UpdateDataShareRequestMailboxAddress(int domainId, string? dataShareRequestMailboxAddress, CancellationToken cancellationToken = default);
}