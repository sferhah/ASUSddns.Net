using ASUSddns.Core;
using ASUSddns.ServiceApp;

using IHost host = Host.CreateDefaultBuilder(args)
    .UseWindowsService(options => options.ServiceName = "ASUSddns.Net")
    .UseSystemd()
    .ConfigureServices((hostContext, services) =>
    {
        services.AddSingleton(new MainService());
        services.AddSingleton(hostContext.Configuration.GetSection("Options").Get<MainServiceOptions>());
        services.AddHostedService<MainBackgroundService>();
    })
    .Build();

await host.RunAsync();
