using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Procession.Common;
using Procession.Server.Common;
using Procession.Sqlite;
using System;
using System.Collections.Generic;
using System.Text;
using Procession.GrpcServer;
using Procession.GrpcServer.Interceptors;
using Microsoft.Extensions.Configuration;
using System.IO;
using Microsoft.Extensions.Logging;

namespace Procession.Integration.Tests
{
    class StartupGrpc
    {
        /// <summary>
        /// This method gets called by the runtime. Use this method to add services to the container.
        /// For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940 .
        /// </summary>
        /// <param name="services">Service Collection passed in via runtime.</param>
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddGrpc(config =>
            {
                config.Interceptors.Add<ExceptionInterceptor>();

                //config.ResponseCompressionLevel = System.IO.Compression.CompressionLevel.Optimal;
            });

            //services.AddSingleton<IStorage>(new StorageSqlite(@"Data Source=Sharable;Mode=Memory;Cache=Shared;"));
            services.AddSingleton<IStorage>(new StorageSqlite(@"Data Source=SqliteTesting.db;Cache=Shared;"));
            services.AddSingleton(typeof(InternalApi));
            services.AddLogging(logging =>
            {
                logging.AddConsole();
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGrpcService<ProcessionService>();
            });
        }
    }
}
