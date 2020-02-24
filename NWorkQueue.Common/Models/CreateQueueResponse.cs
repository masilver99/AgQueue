// <copyright file="CreateQueueResponse.cs" company="Michael Silver">
// Copyright (c) Michael Silver. All rights reserved.
// </copyright>

using System.Runtime.Serialization;

namespace NWorkQueue.Common.Models
{
    [DataContract]
    public class CreateQueueResponse
    {
        [DataMember(Order = 1)]
        public CreateQueueStatus Status;

        [DataMember(Order = 2)]
        public long QueueId;

        [DataMember(Order = 3)]
        public string Message;
    }
}