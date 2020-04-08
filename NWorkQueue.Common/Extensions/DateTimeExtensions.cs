using Google.Protobuf.WellKnownTypes;
using System;
using System.Collections.Generic;
using System.Text;

namespace NWorkQueue.Common.Extensions
{
    public static class DateTimeExtensions
    {
        public static long ToUnixEpoch(this DateTime dateTime)
        {
            return new DateTimeOffset(dateTime).ToUnixTimeMilliseconds();
        }
    }
}
