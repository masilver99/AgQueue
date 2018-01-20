using System;
using System.Collections.Generic;
using System.Text;

namespace NWorkQueue.Library
{
    interface IStorage : IDisposable
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="deleteExistingData"></param>
        /// <param name="settings">Could be connection string, could be empty, could be json settings.  Depends on the underlying storage class</param>
        void InitializeStorage(bool deleteExistingData, string settings);

    }
}
