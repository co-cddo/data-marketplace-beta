using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Agm.Catalog.DotNet.Dto.Models.DataAssets.Profiles.DcatUk.V3_1;
using Cddo.Data.Marketplace.Api.Dto.Responses.Catalog;

namespace Cddo.Data.Marketplace.Api.Dto.Requests.Catalog.Questions
{
    public class QuestionLicenceRequest : CatalogDataRequestBase
    {
        public License? License { get; set; } = new License();
    }
}
