using Agm.Catalog.DotNet.Dto.Models.DataAssets;
using Agm.Catalog.DotNet.Dto.Models.DataAssets.Profiles.DcatUk.V3_1;
using Cddo.Data.Marketplace.Logic.Services.Users.Model;
using Microsoft.AspNetCore.Mvc;

namespace Cddo.Data.Marketplace.Api.Validation
{
    public interface IModelValidationService
    {
        ErrorMessage? RecordModelStateErrorsAndBuildErrorResponse(ActionContext context, IUserDetails initiatingUserDetails);
        ErrorMessage? RecordDataAssetValidationErrorsAndBuildErrorResponseIfInvalid(
          IEnumerable<IDataAssetValidationPropertyResult> validationPropertyResults,
          IUserDetails initiatingUserDetails);
        List<CataloguedResource> GetMockedCataloguedResources();
        DataSet GetMockedDataset(string datasetId);
        DataSet GetMockedUpdatedDataset(string datasetId, DataSet patchModel);
        DataService GetMockedDataServive(string dataServiceId);
        DataService GetMockedUpdatedDataService(string dataServiceId);
        (int, ErrorMessage)? HandleSimulatedErrors(CataloguedResource? dataset, string? datasetId, bool isDataset);
    }
}