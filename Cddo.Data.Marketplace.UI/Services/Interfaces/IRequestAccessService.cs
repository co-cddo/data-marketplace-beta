using Cddo.Data.Marketplace.Api.Dto.Requests.RequestAccess;
using Cddo.Data.Marketplace.Api.Dto.Responses.RequestAccess;

namespace Cddo.Data.Marketplace.UI.Services.Interfaces;

public interface IRequestAccessService
{
    Task<int?> SubmitOrganisationRequestAsync(CreateOrganisationRequest organisationAccessRequest, CancellationToken cancellationToken = default);
    Task<int?> UpdateOrganisationRequestAsync(OrganisationAccessResponse organisationAccessRequest, CancellationToken cancellationToken = default);
    Task<List<OrganisationAccessResponse>?> GetOrganisationAllRequestAsync(CancellationToken cancellationToken = default);
    Task<OrganisationAccessResponse?> GetOrganisationRequestByIdAsync(int? organisationRequestID, CancellationToken cancellationToken = default);
}
