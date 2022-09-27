using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NetCore.AutoRegisterDi;
using USCAssociation.RobotClient.DataRepositories;
using USCAssociation.RobotClient.Services.SeedDatas;
using USCAssociation.RobotClient.Services.WebhookClients;

//添加依赖注入
using IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration(builder =>
    {
        builder.AddCommandLine(args);//设置添加命令行
    })
    .ConfigureAppConfiguration((hostingContext, config) =>
    {
        config.AddJsonFile("BackUp/Secrets.json",
            optional: true,
            reloadOnChange: true);
    })
    .ConfigureServices((_, services) =>
    {
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped(sp => new HttpClient());

        services.RegisterAssemblyPublicNonGenericClasses()
              .Where(c => c.Name.EndsWith("Service") || c.Name.EndsWith("Provider"))
              .AsPublicImplementedInterfaces(ServiceLifetime.Scoped);
    })
    .Build();


//获取服务提供程序
using IServiceScope serviceScope = host.Services.CreateScope();
IServiceProvider provider = serviceScope.ServiceProvider;

var _webhookClient = provider.GetRequiredService<IWebhookClientService>();
var _seedDataService = provider.GetRequiredService<ISeedDataService>();

//初始化种子数据
_seedDataService.InitData();


await host.RunAsync();

