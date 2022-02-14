using System;
using Microsoft.Extensions.Hosting;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;

namespace XAccount.Services
{
    public class APIDataService : BackgroundService,IDisposable
    {
        private readonly IServiceScope _scope;

        public APIDataService(IServiceProvider services)
        {
            _scope = services.CreateScope(); // CreateScope is in Microsoft.Extensions.DependencyInjection
        }

        public override void Dispose()
        {
            _scope?.Dispose();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    //需要执行的任务
                    ITalksService context = _scope.ServiceProvider.GetRequiredService<ITalksService>();

                    await context.TalksTimeFix();       //修正聊天异常

                    await context.GameResultFix();      //修正麻将游戏异常 退换筹码

                }
                catch (Exception ex)
                {
                    //LogHelper.Error(ex.Message);
                }
                await Task.Delay(1000*60*2, stoppingToken);//等待2分钟秒
            }
        }
    }
}
