// <copyright file="QueueInfo.cs" company="Michael Silver">
// Copyright (c) Michael Silver. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Text;

namespace NWorkQueue.Server.Common.Models
{
    /// <summary>
    /// Contains information a specific queue.
    /// </summary>
    public class QueueInfo
    {
        /// <summary>
        /// Gets or sets queue name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets queue ID.
        /// </summary>
        public long Id { get; set; } = 0;
    }
}
