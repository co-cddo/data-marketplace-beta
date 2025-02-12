using Agm.Catalog.DotNet.Dto.Requests.Lookup;
using Agm.Catalog.DotNet.Dto.Responses.Lookup;
using Cddo.Data.Marketplace.Logic.Services.DataAssets;
using Cddo.Data.Marketplace.Logic.Services.Users.Model;
using Cddo.Data.Marketplace.Logic.Services.Users;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace Cddo.Data.Marketplace.Api.Controllers;

[Authorize(AuthenticationSchemes = "InteractiveScheme")]
[ApiController]
public class LookupController(
    ILogger<LookupController> logger,
    IDataAssetService dataAssetService,
    IUserProfilePresenter userProfilePresenter) : ControllerBase
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
}
