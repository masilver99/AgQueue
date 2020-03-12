using System;
using System.Collections.Generic;
using System.Text;

namespace NWorkQueue.Server.Common.Extensions
{
    public static class StringExtensions
    {
        internal static string StandardizeQueueName(this string rawQueueName)
        {
            return rawQueueName.Replace(" ", string.Empty);
        }
    }
}
