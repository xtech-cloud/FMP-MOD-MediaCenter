
//*************************************************************************************
//   !!! Generated by the fmp-cli.  DO NOT EDIT!
//*************************************************************************************

using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Grpc.Net.Client;
using Grpc.Net.Client.Web;
using AntDesign.ProLayout;
using XTC.FMP.LIB.MVCS;
using XTC.FMP.MOD.MediaCenter.LIB.MVCS;
using XTC.FMP.MOD.MediaCenter.App.Web;

public partial class Program
{
    public static async Task Main(string[] args)
    {
        var permissioS = new Dictionary<string,string>();

        permissioS[Permissions.HealthyCreate] = "";
        permissioS[Permissions.HealthyUpdate] = "";
        permissioS[Permissions.HealthyRetrieve] = "";
        permissioS[Permissions.HealthyDelete] = "";


        var channel = GrpcChannel.ForAddress("https://localhost:19000/", new GrpcChannelOptions
        {
            HttpHandler = new GrpcWebHandler(new HttpClientHandler())
        });

        Logger logger = new ConsoleLogger();
        Framework framework = new Framework();
        framework.setConfig(new Config());
        framework.setLogger(logger);
        framework.Initialize();

        framework.Setup();
        var builder = WebAssemblyHostBuilder.CreateDefault(args);
        builder.RootComponents.Add<App>("#app");

        builder.Services.AddScoped(sp => framework);
        builder.Services.AddScoped(sp => logger);
        builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
        builder.Services.AddAntDesign();
        builder.Services.Configure<ProSettings>(builder.Configuration.GetSection("ProSettings"));

        var entry = new Entry();
        var options = new Options();
        options.setChannel(channel);
        options.setPermissionS(permissioS);
        framework.setUserData("XTC.FMP.MOD.MediaCenter.LIB.MVCS.Entry", entry);
        entry.Inject(framework, options);
        entry.DynamicRegister("default", logger);
        await builder.Build().RunAsync();

        framework.Dismantle();
        entry.StaticCancel("default", logger);
        framework.Release();
    }
}
