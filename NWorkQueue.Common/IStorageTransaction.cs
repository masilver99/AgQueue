// <copyright file="IStorageTransaction.cs" company="Michael Silver">
// Copyright (c) Michael Silver. All rights reserved.
// </copyright>

namespace NWorkQueue.Common
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    public interface IStorageTransaction
    {
        void Commit();

        void Rollback();
    }
}
