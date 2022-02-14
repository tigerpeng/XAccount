using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace XAccount
{
    public class Program
    {
        //public static void Main(string[] args)
        //{
        //    CreateWebHostBuilder(args).Build().Run();
        //}

        //public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
        //    WebHost.CreateDefaultBuilder(args)
        //        .UseStartup<Startup>();


        public static void Main(string[] args)
        {
            BuildWebHost(args).Run();
        }

        public static IWebHost BuildWebHost(string[] args)
        {
            var configuration = new ConfigurationBuilder().AddCommandLine(args).Build();

            return WebHost.CreateDefaultBuilder(args)
                .UseConfiguration(configuration)
                //.UseUrls("http://*:5000")  //调试使用 上线去掉 如果不配置这条信息，会导致局域网无法通过ip访问 
                .UseStartup<Startup>()
                .Build();

            //经过上面改动后 通过参数可改变启动端口
            //dotnet XAccount.dll  urls=http://*:2001 
            //修改端口
            //https://stackoverflow.com/questions/45803247/asp-net-core-2-0-standalone-passing-listening-url-via-command-line
        }
    }
}
