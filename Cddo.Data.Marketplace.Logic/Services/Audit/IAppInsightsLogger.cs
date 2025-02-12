
using Agm.Catalog.DotNet.Dto.Models.CatalogData;
using Agm.Catalog.DotNet.Dto.Models.DataAssets.Enums;
using Agm.Catalog.DotNet.Dto.Models.DataAssets;
using Cddo.Data.Marketplace.Logic.Services.Users.Model;
using static Cddo.Data.Marketplace.Audit.EventTypes;

namespace Cddo.Data.Marketplace.Audit
{
    public interface IAppInsightsLogger
    {
        void LogCritical(string message);
        void LogError(string format, Exception ex, params object[] args);
        void LogEvent<TEnum>(TEnum eventType, Dictionary<string, string> properties = null, Dictionary<string, double> metrics = null) where TEnum : Enum;
        void LogInformation(string message);
        void LogWarning(string message);
        void LogEventMainBase<TEnum>(TEnum eventType, string pageName, string client, string action, string subject, string setting, Dictionary<string, string> additionalProperties = null) where TEnum : Enum;
        void LogAdminEventBase(AdminAuditEvent adminEvent, string pageName, string client, string action, string subject, string setting, Dictionary<string, string> additionalProperties = null);
        void LogDataAssetValidationErrorBase(
            CatalogAssetField catalogAssetField,
            DataAssetActionSourceEnum actionSource,
            string dataAssetPropertyName,
            string errorMessage,
            DataAssetValidationPropertyErrorSeverity errorSeverity,
            DataAssetPropertyValidationErrorType errorType,
            IUserDetails initiatingUserDetails);
    }
}