// <copyright file="DeleteQuqueByNameRequest.cs" company="Michael Silver">
// Copyright (c) Michael Silver. All rights reserved.
// </copyright>

using System.Runtime.Serialization;
using System.ServiceModel;

namespace NWorkQueue.Common.Models
{
    [ServiceContract]
    public class DeleteQueueByNameRequest
    {
        [DataMember(Order=1)]
        public string QueueName { get; set; }
    }
}