// <copyright file="InitializeStorageRequest.cs" company="Michael Silver">
// Copyright (c) Michael Silver. All rights reserved.
// </copyright>

using System.Runtime.Serialization;

namespace NWorkQueue.Server
{
    [DataContract]
    public class InitializeStorageRequest
    {
        [DataMember(Order = 1)]
        public bool DeleteExistingData { get; set; } = false;
    }
}