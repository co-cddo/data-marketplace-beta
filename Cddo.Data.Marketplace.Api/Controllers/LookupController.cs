using Agm.Catalog.DotNet.Dto.Models.CatalogData;
using Agm.Catalog.DotNet.Dto.Requests.Lookup;
using Agm.Catalog.DotNet.Dto.Responses.Lookup;
using Cddo.Data.Marketplace.Logic.Services.DataAssets;
using Cddo.Data.Marketplace.Logic.Services.Reports;
using Cddo.Data.Marketplace.Logic.Services.Users.Model;
using Cddo.Data.Marketplace.Logic.Services.Users;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Agm.Catalog.DotNet.Dto.Requests.DataAssets;
using Cddo.Data.Marketplace.Api.Dto.Models.Reports.CatalogData.Filters;
using Cddo.Data.Marketplace.Api.Dto.Requests.Reports;
using Microsoft.IdentityModel.Tokens;
using Cddo.Data.Marketplace.Logic.Services.Reports.Results;
using static NPOI.HSSF.UserModel.HeaderFooter;
using Agm.Catalog.DotNet.Dto.Models.Reports.CatalogData;

namespace Cddo.Data.Marketplace.Api.Controllers;

[Authorize(AuthenticationSchemes = "InteractiveScheme")]
[ApiController]
public class LookupController(
    ILogger<LookupController> logger,
    IDataAssetService dataAssetService,
    IUserProfilePresenter userProfilePresenter,
    IReportsService reportsService) : ControllerBase
{
    [Authorize]
    [HttpGet("Topics")]
    [ProducesResponseType(typeof(GetCddoTopicsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> TopicsLookUpAsync(
        [FromQuery] GetCddoTopicsRequest getCddoTopicsRequest)
    {
        try
        {
            var initiatingUserDetails = await DoGetInitiatingUserDetailsAsync();

            var getCddoTopicsResult = await dataAssetService.GetCddoTopicsAsync(
                initiatingUserDetails,
                getCddoTopicsRequest.DataAssetStatuses);

            if (!getCddoTopicsResult.Success)
            {
                logger.LogError("Failed to Get CDDO Topics from DataAssetsService: {Error}", getCddoTopicsResult.Error);

                return BadRequest(getCddoTopicsResult.Error);
            }

            var response = new GetCddoTopicsResponse
            {
                Topics = getCddoTopicsResult.Data!.Topics.ToList()
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, message: ex.Message);
            return BadRequest(ex.Message);
        }
    }

    [Authorize]
    [HttpGet("Organisations")]
    [ProducesResponseType(typeof(GetCddoOrganisationsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> OrganisationLookUpAsync(
        [FromQuery] GetCddoOrganisationsRequest getCddoOrganisationsRequest)
    {
        try
        {
            var initiatingUserDetails = await DoGetInitiatingUserDetailsAsync();

            var getCddoOrganisationsResult = await dataAssetService.GetCddoOrganisationsAsync(
                initiatingUserDetails,
                getCddoOrganisationsRequest.DataAssetStatuses);

            if (!getCddoOrganisationsResult.Success)
            {
                logger.LogError("Failed to get CDDO organisations from DataAssetsService: {Error}", getCddoOrganisationsResult.Error);

                return BadRequest(getCddoOrganisationsResult.Error);
            }

            var response = new GetCddoOrganisationsResponse
            {
                Organisations = getCddoOrganisationsResult.Data!.Organisations.ToList()
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, message: ex.Message);
            return BadRequest(ex.Message);
        }
    }

    private async Task<IUserDetails> DoGetInitiatingUserDetailsAsync()
    {
        var initiatingUserDetails = await userProfilePresenter.GetInitiatingUserDetailsAsync();

        if (initiatingUserDetails == null)
        {
            logger.LogError("Unable to get user details for initiating user");
        }

        return initiatingUserDetails!;
    }

    [Authorize]
    [HttpGet("FilteredMenuOptions")]
    [ProducesResponseType(typeof(CatalogueFilterOptions), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> FilteredMenuResults([FromQuery] GetCddoDataAssetsRequest getCddoDataAssetsRequest)
    {

        var request = new QueryCatalogReportsDataRequest()
        {
            StartRecordIndex = 0,
            NumberOfRecords = 1000,
            RequiredFields = new List<CatalogAssetField>() { CatalogAssetField.DataAssetType, CatalogAssetField.Publisher, CatalogAssetField.Themes},
        };

        if (getCddoDataAssetsRequest != null)
        {
            request.Filter = SetFilterFromDataAssetRequest(getCddoDataAssetsRequest);
            if (!string.IsNullOrEmpty(getCddoDataAssetsRequest.SearchText))
            {
                request.SearchText = getCddoDataAssetsRequest.SearchText;
            }
            request.Filter.FilterByInitiatingUserPermissions = false;
            var initiatingUserDetails = await userProfilePresenter.GetInitiatingUserDetailsAsync();

            var organisations = new List<string>();
            var result = await reportsService.GetCatalogReportsDataAsync(initiatingUserDetails, request.RequiredFields, request.Filter, request.StartRecordIndex, request.NumberOfRecords, request.SearchText);
            //var response = result.Data.CatalogReportsDataItems.ToList();


            var fieldValues = result.Data.CatalogReportsDataItems
            .SelectMany(item => item.CatalogReportsDataItemFields)
            .GroupBy(field => field.Field)
            .ToDictionary(
                group => group.Key,
                group => group
                    .SelectMany(item => item.Values ?? new List<string>())
                    .Distinct()
                    .ToList()
            );

            List<string> publishers = fieldValues.ContainsKey(CatalogAssetField.Publisher) ? fieldValues[CatalogAssetField.Publisher] : new List<string>();
            List<string> dataAssetTypeValues = fieldValues.ContainsKey(CatalogAssetField.DataAssetType) ? fieldValues[CatalogAssetField.DataAssetType] : new List<string>();
            List<string> toipics = fieldValues.ContainsKey(CatalogAssetField.Themes) ? fieldValues[CatalogAssetField.Themes] : new List<string>();
            var response = new CatalogueFilterOptions()
            {
                DataAssetTypes = dataAssetTypeValues,
                Organisations = publishers,
                Topics = toipics
            };
            return Ok(response);
        }


        return Ok(null);
    }

    private CatalogReportsFilter? SetFilterFromDataAssetRequest(GetCddoDataAssetsRequest getCddoDataAssetsRequest)
    {
        var filter = new CatalogReportsFilter();
        if (getCddoDataAssetsRequest.DataAssetTypes != null)
        {
            filter.FieldFilters.Add(new CatalogReportFieldFilter(){Field = CatalogAssetField.DataAssetType, Values = getCddoDataAssetsRequest.DataAssetTypes.Select(type => type.ToString()).ToList() });
        }

        if (getCddoDataAssetsRequest.DataAssetStatuses != null)
        {
            filter.FieldFilters.Add(new CatalogReportFieldFilter() { Field = CatalogAssetField.DataAssetStatus, Values = getCddoDataAssetsRequest.DataAssetStatuses.Select(type => type.ToString()).ToList() });
        }

        if (getCddoDataAssetsRequest.Creator != null)
        {
            filter.FieldFilters.Add(new CatalogReportFieldFilter() { Field = CatalogAssetField.Publisher, Values = getCddoDataAssetsRequest.Creator.Select(type => type.ToString()).ToList() });
        }
        if (getCddoDataAssetsRequest.Themes != null)
        {
            filter.FieldFilters.Add(new CatalogReportFieldFilter() { Field = CatalogAssetField.Themes, Values = getCddoDataAssetsRequest.Themes.Select(type => type.ToString()).ToList() });
        }

        return filter;
    }

    private class CatalogueFilterOptions
    {
        public List<string>? Organisations { get; set; }
        public List<string>? Topics { get; set; }
        public List<string>? DataAssetTypes { get; set; }
    }
}
