using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cddo.Data.Marketplace.Api.Dto.Responses.Catalog;

namespace Cddo.Data.Marketplace.Api.Dto.Requests.Catalog.Questions
{
    public class QuestionLicenceRequest : CatalogDataRequestBase
    {
        public Licence? Licence { get; set; }
    }
}
