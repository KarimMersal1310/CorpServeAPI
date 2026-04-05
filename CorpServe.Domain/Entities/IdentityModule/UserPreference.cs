using CorpServe.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CorpServe.Domain.Entities.IdentityModule
{
    public class UserPreference :BaseEntity<int>
    {
        public bool EmailNotification { get; set; } = true;
        public bool SystemNotification { get; set; } = true;
    }
}
