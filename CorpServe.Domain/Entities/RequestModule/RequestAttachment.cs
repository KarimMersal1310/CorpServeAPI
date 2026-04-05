using CorpServe.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CorpServe.Domain.Entities.RequestModule
{
    public class RequestAttachment : BaseEntity<string>
    {
        public string FileUrl { get; set; } = default!;

        #region RelationShips

        #region Request - RequestAttachment (One-to-Many)

        public string RequestId { get; set; } = default!;
        public Request Request { get; set; } = default!;

        #endregion
        #endregion

    }
}
