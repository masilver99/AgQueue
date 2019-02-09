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
