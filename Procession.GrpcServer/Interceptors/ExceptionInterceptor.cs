using Grpc.Core;
using Grpc.Core.Interceptors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Procession.GrpcServer.Interceptors
{
    public class ExceptionInterceptor : Interceptor
    {
        public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(TRequest request, ServerCallContext context, UnaryServerMethod<TRequest, TResponse> continuation)
        {
            try
            {
                return await base.UnaryServerHandler(request, context, continuation);
            }
            catch (Exception e)
            {
                // No sense it wrapping an RPC exception in an RPC exception!
                if (e.GetType() == typeof(RpcException))
                {
                    throw;
                }
                
                throw new RpcException(new Status(StatusCode.Internal, e.Message));
            }
        }
    }
}
