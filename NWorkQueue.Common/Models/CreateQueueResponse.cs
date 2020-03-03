// <copyright file="CreateQueueResponse.cs" company="Michael Silver">
// Copyright (c) Michael Silver. All rights reserved.
// </copyright>

using System.Runtime.Serialization;

namespace NWorkQueue.Common.Models
{
    [DataContract]
    public class CreateQueueResponse
    {
        public CreateQueueResponse()
        {

        }

        public CreateQueueResponse(long queueId)
        {
            this.QueueId = queueId;
        }

        [DataMember(Order = 1)]
        public long QueueId { get; set; }
    }
}