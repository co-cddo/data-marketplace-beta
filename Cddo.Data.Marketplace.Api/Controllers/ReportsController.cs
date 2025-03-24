using Cddo.Data.Marketplace.Api.Dto.Requests.Reports;
using Cddo.Data.Marketplace.Api.Dto.Responses.Reports;
using Cddo.Data.Marketplace.Logic.Services.Reports;
using Cddo.Data.Marketplace.Logic.Services.Users;
using Cddo.Data.Marketplace.Logic.Services.Users.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cddo.Data.Marketplace.Api.Controllers
{
    [ApiController]
    public class ReportsController(
        ILogger<ReportsController> logger,
        IReportsService reportsService,
        IReportsResponseFactory reportsResponseFactory,
        IUserProfilePresenter userProfilePresenter) : ControllerBase
    {
        [Authorize]
        [HttpPost("query-catalog-reports-data")]
        [ProducesResponseType(typeof(QueryCatalogReportsDataResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> QueryCatalogReportsData(
            QueryCatalogReportsDataRequest queryCatalogReportsDataRequest)
        {
            ArgumentNullException.ThrowIfNull(queryCatalogReportsDataRequest);

            try
            {
                var initiatingUserDetails = await DoGetInitiatingUserDetailsAsync();

                var getCatalogReportsDataResult = await reportsService.GetCatalogReportsDataAsync(
                    initiatingUserDetails,
                    queryCatalogReportsDataRequest.RequiredFields,
                    queryCatalogReportsDataRequest.Filter,
                    queryCatalogReportsDataRequest.StartRecordIndex,
                    queryCatalogReportsDataRequest.NumberOfRecords,
                    null);

                if (!getCatalogReportsDataResult.Success)
                {
                    logger.LogError("Failed to get catalog reports data from the ReportsService: {Error}", getCatalogReportsDataResult.Error);
                    return BadRequest(getCatalogReportsDataResult.Error);
                }

                var response = reportsResponseFactory.CreateGetCatalogReportsDataResponse(getCatalogReportsDataResult.Data!);

                return Ok(response);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, message: ex.Message);
                return BadRequest(ex.Message);
            }
        }
        [Authorize]
        [HttpPost("download-catalog-reports-data")]
        [ProducesResponseType(typeof(QueryCatalogReportsDataResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DownloadCatalogReportsData(
            QueryCatalogReportsDataRequest queryCatalogReportsDataRequest)
        {
            ArgumentNullException.ThrowIfNull(queryCatalogReportsDataRequest);

            try
            {
                var initiatingUserDetails = await DoGetInitiatingUserDetailsAsync();

                var getCatalogReportsDataResult = await reportsService.GetCatalogReportsDataAsync(
                    initiatingUserDetails,
                    queryCatalogReportsDataRequest.RequiredFields,
                    queryCatalogReportsDataRequest.Filter,
                    queryCatalogReportsDataRequest.StartRecordIndex,
                    queryCatalogReportsDataRequest.NumberOfRecords,
                    null);

                if (!getCatalogReportsDataResult.Success)
                {
                    logger.LogError("Failed to get catalog reports data from the ReportsService: {Error}", getCatalogReportsDataResult.Error);
                    return BadRequest(getCatalogReportsDataResult.Error);
                }

                var response = reportsResponseFactory.CreateGetCatalogReportsDataResponse(getCatalogReportsDataResult.Data!);

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
}
