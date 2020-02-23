using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Threading.Tasks;

namespace NWorkQueue.Common
{
    [ServiceContract(Name = "")]
    interface IQueueApi
    {
        ValueTask<>
    }
}
