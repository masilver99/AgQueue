// <copyright file="StringExtensions.cs" company="Michael Silver">
// Copyright (c) Michael Silver. All rights reserved.
// </copyright>

namespace AgQueue.Common.Extensions
{
    /// <summary>
    /// Extensions methods to be used on stings.
    /// </summary>
    public static class StringExtensions
    {
        /// <summary>
        /// Standardizes the queue name so it's always consistent.
        /// </summary>
        /// <param name="rawQueueName">Raw queue name.</param>
        /// <returns>Standardized queue name.</returns>
        public static string StandardizeQueueName(this string rawQueueName)
        {
            return rawQueueName.Replace(" ", string.Empty);
        }
    }
}
