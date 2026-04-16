using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CorpServe.Domain.Entities.IdentityModule
{
    public class ProfileDocument : BaseEntity<string>
    {
        public string Name { get; set; } = default!;
        public string DocumentType { get; set; } = default!;
        public string DocumentUrl { get; set; } = default!;

        public string ProfileId { get; set; } = default!;
        public UserProfile Profile { get; set; } = default!;
    }
}
