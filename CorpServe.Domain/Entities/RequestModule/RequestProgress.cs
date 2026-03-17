using EventHub.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CorpServe.Domain.Entities.RequestModule
{
    public class RequestProgress : BaseEntity<string>
    {
        public int Percentage { get; set; }
        public string Description { get; set; } = default!;
        public DateTime UpdatedAt { get; set; }
        #region RelationShips

        #endregion
    }
}
