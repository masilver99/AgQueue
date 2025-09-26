// <copyright file="TransactionResult.cs" company="Michael Silver">
// Copyright (c) Michael Silver. All rights reserved.
// </copyright>

namespace Procession.Common
{
    /// <summary>
    /// Result of transaction commital.
    /// </summary>
    /// <remarks>Only commits use the results since commits may have valid responses.  If a rollback fails, an exception should be thrown, at least for now.</remarks>
    public enum TransactionResult
    {
        /// <summary>
        /// Transaction was successfully committed.
        /// </summary>
        Success = 0,

        /// <summary>
        /// Transaction expired before the commit could be completed.
        /// </summary>
        Expired = 1,

        /// <summary>
        /// The transaction was already closed.
        /// </summary>
        Closed = 2,

        /// <summary>
        /// Transaction could not be found. Perhaps this should be an exception.
        /// </summary>
        NotFound = 3
    }
}
