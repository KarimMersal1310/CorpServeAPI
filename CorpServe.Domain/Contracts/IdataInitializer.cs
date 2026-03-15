using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventHub.Domain.Contracts
{
    public interface IDataInitializer
    {
        Task InitializeAsync();
    }
}
