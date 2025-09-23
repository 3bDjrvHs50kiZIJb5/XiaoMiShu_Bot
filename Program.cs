using back.Components;
using back.TelegramBot;
using back.TelegramBot.Forms;
using FreeScheduler;
using FreeSql;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using Serilog.Events;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.FileProviders;
using back.TelegramBot.Services;
using BootstrapBlazor.Components;

// 配置 Serilog - 直接在代码中配置，支持环境特定配置
var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";

Log.Logger = new LoggerConfiguration()
    // 设置最小日志级别
    .MinimumLevel.Information()
    // 设置特定命名空间的日志级别
    .MinimumLevel.Override("Microsoft", LogEventLevel.Error)
    .MinimumLevel.Override("System", LogEventLevel.Error)
    // 配置输出目标 - 控制台
    .WriteTo.Console()
    // 配置输出目标 - 文件（按天滚动）
    .WriteTo.File(
        path: "Logs/log-.txt",
        rollingInterval: RollingInterval.Day,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
    )
    // 添加日志上下文信息
    .Enrich.FromLogContext()
    .CreateLogger();

try
{
    Log.Information("启动应用程序...");
    Log.Information($"当前环境: {environment}");

    var builder = WebApplication.CreateBuilder(args);

    // 验证配置是否正确加载
    var domain = builder.Configuration["Domain"];
    Log.Information($"域名配置: {domain}");

    // 添加数据保护配置
    builder.Services.AddDataProtection()
        .PersistKeysToFileSystem(new DirectoryInfo("keys")) //替换为你的实际路径
        .SetApplicationName("xiaomishu_bot");

    // 添加 Serilog 到服务容器
    builder.Host.UseSerilog();

    // 添加CORS服务
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("CorsPolicy", corsBuilder =>
        {
            var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>();
            corsBuilder
                .WithOrigins(allowedOrigins)
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials();
            // .SetIsOriginAllowed(_ => true); // 临时添加，用于调试
        });
    });


    builder.AddAdminBlazor(new AdminBlazorOptions
    {
        DebugTenantId = "main", //localhost环境下，测试租户Id
        Assemblies = [typeof(Program).Assembly],
        FreeSqlBuilder = a => a
            .UseConnectionString(DataType.Sqlite, @"Data Source=Sqlite.db")
            .UseMonitorCommand(cmd =>
            {
                var lowerCmd = cmd.CommandText.ToLower();
                System.Console.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {cmd.CommandText}\r\n");
            }) //监听SQL语句
            .UseAutoSyncStructure(true), //自动同步实体结构到数据库，只有CRUD时才会生成表。
        SchedulerExecuting = OnSchedulerExecuting //定时任务-自定义触发
    });


    builder.Services.AddRazorComponents()
        .AddInteractiveServerComponents();

    // 添加目录浏览服务
    builder.Services.AddDirectoryBrowser();

    // 禁用自动模型验证，必须加上，否则前端提交表单会报异常。无法post数据
    builder.Services.Configure<ApiBehaviorOptions>(options => { options.SuppressModelStateInvalidFilter = true; });

    // 注册 Telegram Bot 服务
    builder.Services.AddHostedService<TelegramBotService>();
    builder.Services.AddSingleton<MessageHandler>();
    builder.Services.AddSingleton<MemberManagementService>();
    builder.Services.AddSingleton<ChatManagementService>();
    builder.Services.AddSingleton<OrderManagementService>();
    builder.Services.AddSingleton<MessageManagementService>();
    builder.Services.AddSingleton<DataInitializationService>();
    builder.Services.AddSingleton<CommandConfigService>();

    var app = builder.Build();

    UpdateSysTenantHost(app);

    void UpdateSysTenantHost(WebApplication app)
    {
        try
        {
            // 直接使用 app.Configuration 读取配置，避免解析 DI 造成的 Blazor 相关服务构造
            var domainConfig = app.Configuration["Domain"];
            string parsedDomain = "";
            if (!string.IsNullOrEmpty(domainConfig))
            {
                try
                {
                    // 仅提取域名部分，例如 https://jz.bot123.cc -> jz.bot123.cc
                    parsedDomain = new Uri(domainConfig).Host;
                }
                catch (Exception ex)
                {
                    // 解析失败时记录警告日志
                    Log.Warning(ex, "解析配置 Domain 失败，原始值: {DomainConfig}", domainConfig);
                }
            }

            // 使用独立 FreeSql 实例执行一次性更新，避免在此时机解析 IFreeSql 触发 AdminContext 构造
            using var freeSql = new FreeSqlBuilder()
                .UseConnectionString(DataType.Sqlite, @"Data Source=Sqlite.db")
                .UseAutoSyncStructure(false)
                .Build();

            SysTenant sysTenant = freeSql.Select<SysTenant>().ToOne();
            sysTenant.Host = parsedDomain;

            var affrows = freeSql.Update<SysTenant>().SetSource(sysTenant).ExecuteAffrows();
            Log.Information("更新 SysTenant.Host 成功，影响行数: {Affrows}，Host: {Host}", affrows, parsedDomain);
        }
        catch (Exception ex)
        {
            // 不中断应用启动，但记录错误以便后续排查
            Log.Error(ex, "启动时更新 SysTenant.Host 失败");
        }
    }

    // 使用正确的CORS策略名称
    app.UseCors("CorsPolicy");

    // 配置静态文件服务
    app.UseStaticFiles();
    app.UseAntiforgery();

    app.UseBootstrapBlazor();
    app.MapRazorComponents<App>()
        .AddAdditionalAssemblies(typeof(AdminBlazorOptions).Assembly)
        .AddInteractiveServerRenderMode();

    app.UseAdminOmniApi();

    app.Run();

    //自定义触发
    static void OnSchedulerExecuting(IServiceProvider service, TaskInfo task)
    {
        switch (task.Topic)
        {
            case "武林大会":
                //todo..
                break;
            case "攻城活动":
                //todo..
                break;
        }
    }


}
catch (Exception ex)
{
    Log.Fatal(ex, "应用程序启动失败");
}
finally
{
    Log.CloseAndFlush();
}