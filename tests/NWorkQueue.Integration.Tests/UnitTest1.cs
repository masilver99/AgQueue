using Microsoft.VisualStudio.TestTools.UnitTesting;
using ProtoBuf.Grpc.Client;
using Grpc.Core;
using ProtoBuf.Grpc;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using NWorkQueue.Server;
using NWorkQueue.Common;

namespace NWorkQueue.Integration.Tests
{
    [TestClass]
    public class UnitTest1
    {
        private IWebHost StartServer()
        {
            var webHostBuilder = CreateHostBuilder();
            var webHost = webHostBuilder.Build();
            webHost.Start();
            return webHost;
        }

        public static IWebHostBuilder CreateHostBuilder() =>
            WebHost.CreateDefaultBuilder(new string[0])
                .ConfigureKestrel(option =>
                {
                    option.ListenLocalhost(10042, listenOptions =>
                    {
                        listenOptions.Protocols = HttpProtocols.Http2;
                    });
                }).UseStartup<StartupTest>();

    
        [TestMethod]
        public void TestMethod1()
        {
            var webHost = StartServer();
            GrpcClientFactory.AllowUnencryptedHttp2 = true;
            using var http = GrpcChannel.ForAddress("http://localhost:10042");
            var service = http.CreateGrpcService<IQueueApi>();
            var response = service.CreateQueue(new Common.Models.CreateQueueRequest() { QueueName = "Test" });
            var result = response.Result;
            webHost.StopAsync();
        }
    }
}
