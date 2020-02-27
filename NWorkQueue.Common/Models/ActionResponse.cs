// <copyright file="ActionResponse.cs" company="Michael Silver">
// Copyright (c) Michael Silver. All rights reserved.
// </copyright>

using System.Runtime.Serialization;

namespace NWorkQueue.Common.Models
{
    [DataContract]
    public class ActionResponse
    {
        [DataMember(Order = 1)]
        public bool Success { get; set; }

        [DataMember(Order = 2)]
        public string Message { get; set; }
    }
}