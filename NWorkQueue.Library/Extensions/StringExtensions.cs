using System;
using System.Collections.Generic;
using System.Text;

namespace NWorkQueue.Library.Extensions
{
    public static class StringExtensions
    {
        internal static string StandardizeQueueName(this string rawQueueName)
        {
            return rawQueueName.Replace(" ", string.Empty);
        }
    }
}
