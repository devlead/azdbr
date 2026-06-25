public partial class Program
{
    static partial void AddServices(IServiceCollection services)
    {
        services
            .AddCakeCore()
            .AddAzdbrServices();
    }

    static partial void ConfigureApp(AppServiceConfig appServiceConfig)
    {
        appServiceConfig.SetApplicationName("azdbr");

        appServiceConfig
            .AddCommand<RefreshCommand>("refresh")
            .WithDescription("Refresh target Azure SQL database from source (same tenant).")
            .WithExample(["refresh", "prod-server", "prod-db", "staging", "staging-db"]);
    }
}
