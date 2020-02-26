// <copyright file="IQueueApi.cs" company="Michael Silver">
// Copyright (c) Michael Silver. All rights reserved.
// </copyright>

using System.ServiceModel;
using System.Threading.Tasks;
using NWorkQueue.Common.Models;

namespace NWorkQueue.Common
{
    [ServiceContract(Name = "Queue.Api")]
    public interface IQueueApi
    {
        ValueTask<CreateQueueResponse> CreateQueue(CreateQueueRequest request);
    }
}
