// <copyright file="DateTimeExtensions.cs" company="Michael Silver">
// Copyright (c) Michael Silver. All rights reserved.
// </copyright>

using System;

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
