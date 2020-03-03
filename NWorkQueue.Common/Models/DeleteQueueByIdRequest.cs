// <copyright file="QueueApi.cs" company="Michael Silver">
// Copyright (c) Michael Silver. All rights reserved.
// </copyright>

using System.Runtime.Serialization;
using System.ServiceModel;

namespace NWorkQueue.Server
{
    [ServiceContract]
    public class DeleteQueueByIdRequest
    {
        [DataMember(Order = 1)]
        public long QueueId { get; set; }
    }
}