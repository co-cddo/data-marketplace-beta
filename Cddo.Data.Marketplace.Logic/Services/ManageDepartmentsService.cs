using Cddo.Data.Marketplace.Api.Dto.Requests.ManageUser;
using Cddo.Data.Marketplace.Api.Dto.Responses.ManageUser;
using Cddo.Data.Marketplace.Audit;
using Cddo.Data.Marketplace.Logic.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cddo.Data.Marketplace.Api.Dto.Requests;
using Flurl;
using Flurl.Http;
using System.Text.Json.Serialization;
using Newtonsoft.Json;
using Cddo.Data.Marketplace.Api.Dto.ManageUser;

namespace Cddo.Data.Marketplace.Logic.Services
{
    public class ManageDepartmentsService : IManageDepartmentsService
    {
        private readonly string _apiUrl;
        private const string BaseRoute = "Department/";
        private readonly IAppInsightsLogger _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;
        public readonly IUserRoleService _userRoleService;

        public ManageDepartmentsService(IAppInsightsLogger logger,
            IConfiguration configuration,
            IHttpContextAccessor httpContextAccessor,
            IUserRoleService userRoleService)
        {
            ArgumentNullException.ThrowIfNull(logger);
            ArgumentNullException.ThrowIfNull(configuration);
            ArgumentNullException.ThrowIfNull(httpContextAccessor);
            ArgumentNullException.ThrowIfNull(userRoleService);

            _apiUrl = configuration.GetSection("ApiSettings:UsersAPI").Value!;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
            _userRoleService = userRoleService;
        }

        public async Task<ManageDepartmentsResponse?> GetManageDepartmentsAsync(ManageDepartmentRequest manageDepartmentRequest, CancellationToken cancellationToken = default)
        {
            if (_httpContextAccessor.HttpContext!.User.Identity!.IsAuthenticated)
            {
                bool isAGMAdministrator = await _userRoleService.IsUserRoleSystemAdmin();
                if (isAGMAdministrator)
                {
                    var token = _httpContextAccessor.HttpContext.Request.Cookies["CO-Datamarketplace"];

                    try
                    {
                        var response = await _apiUrl
                            .AppendPathSegments(BaseRoute, "departments-paged")
                            .WithOAuthBearerToken(token)
                            .SetQueryParams(new
                            {
                                page = manageDepartmentRequest?.PageNumber,
                                pageSize = manageDepartmentRequest?.PageSize,
                                searchTerm = manageDepartmentRequest?.SearchTerm,
                            })
                            .GetStringAsync(cancellationToken: cancellationToken);
                        var responseObject = JsonConvert.DeserializeObject<ManageDepartmentsResponse>(response);
                        if (responseObject != null)
                        {
                            return responseObject;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError("Get Departments Error ", ex);
                        return null;
                    }
                }
            }
            return null;
        }

        public async Task<Department?> GetDepartmentByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            if (_httpContextAccessor.HttpContext!.User.Identity!.IsAuthenticated)
            {
                bool isAGMAdministrator = await _userRoleService.IsUserRoleSystemAdmin();
                if (isAGMAdministrator)
                {
                    var token = _httpContextAccessor.HttpContext.Request.Cookies["CO-Datamarketplace"];

                    try
                    {
                        var response = await _apiUrl
                            .AppendPathSegments(BaseRoute, $"department/{id}")
                            .WithOAuthBearerToken(token)
                            .GetStringAsync(cancellationToken: cancellationToken);
                        var responseObject = JsonConvert.DeserializeObject<Department>(response);
                        if (responseObject != null)
                        {
                            return responseObject;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError("Get Departments Error ", ex);
                        return null;
                    }
                }
            }
            return null;
        }
        public async Task<bool> PostAddDepartmentAsync(string departmentName, CancellationToken cancellationToken = default)
        {
            if (_httpContextAccessor.HttpContext!.User.Identity!.IsAuthenticated)
            {
                bool isAGMAdministrator = await _userRoleService.IsUserRoleSystemAdmin();
                if (isAGMAdministrator)
                {
                    var token = _httpContextAccessor.HttpContext.Request.Cookies["CO-Datamarketplace"];

                    try
                    {
                        var jsonString = JsonConvert.SerializeObject(departmentName);
                        var content = new StringContent(jsonString, Encoding.UTF8, "application/json");
                        var response = await _apiUrl
                            .AppendPathSegments(BaseRoute, "create")
                            .WithOAuthBearerToken(token)
                            .PostAsync(content, default);

                        if (response.ResponseMessage.IsSuccessStatusCode)
                        {                           
                            return true;
                        }
                        else
                        {
                            return false;
                        }                       

                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning($"An error occurred while adding new department: {ex}");
                        return false;
                    }
                }
            }
            return false;
        }
    }
}
