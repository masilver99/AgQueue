// <copyright file="WorkQueueException.cs" company="Michael Silver">
// Copyright (c) Michael Silver. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Text;

namespace Procession.Common
{
    /// <summary>
    /// Exception thrown for unique WorkQueue exceptions.
    /// </summary>
    public class WorkQueueException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WorkQueueException"/> class.
        /// </summary>
        /// <param name="message">Error message.</param>
        public WorkQueueException(string? message)
            : base(message)
        {
        }
    }
}
