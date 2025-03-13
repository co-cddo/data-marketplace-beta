using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cddo.Data.Marketplace.Api.Dto.Requests.Catalog.Questions
{
    public class QuestionAccessRightsRequest : CatalogDataRequestBase
    {
        public string? AccessRights { get; set; }
    }
}
