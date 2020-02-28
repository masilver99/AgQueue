using Grpc.Core;
using NWorkQueue.Library;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NWorkQueue.Server
{
    public static class ResultCodeExtensions
    {
        public static StatusCode ToGrpcStatus(this ResultCode resultCode)
        {
            return (StatusCode)(int)resultCode;
        }
    }
}
