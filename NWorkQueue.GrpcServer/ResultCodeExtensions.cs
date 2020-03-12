using Grpc.Core;
using NWorkQueue.Server.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NWorkQueue.GrpcServer
{
    public static class ResultCodeExtensions
    {
        public static StatusCode ToGrpcStatus(this ResultCode resultCode)
        {
            return (StatusCode)(int)resultCode;
        }

        public static RpcException CreateRpcException(this ApiResult apiResult)
        {
            return new RpcException(new Status(apiResult.ResultCode.ToGrpcStatus(), apiResult.Message));
        }
    }
}
