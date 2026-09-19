using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using WorldMap.App.Services;
using WorldMap.App.ViewModels;
using WorldMap.Core.Interfaces;
using WorldMap.Core.Projections;
using WorldMap.Infrastructure.GeoJson;
using WorldMap.Infrastructure.Osm;
using WorldMap.Infrastructure.Spatial;

namespace WorldMap.App;

public partial class App : Application
{
    private readonly ServiceProvider _serviceProvider;

    public App()
    {
        ServiceCollection services =
            new ServiceCollection();

        ConfigureServices(
            services);

        _serviceProvider =
            services.BuildServiceProvider();
    }

    private static void ConfigureServices(
        IServiceCollection services)
    {
        // --------------------------
        // Repositories
        // --------------------------

        services.AddSingleton<
            ICountryRepository,
            GeoJsonCountryRepository>();

        services.AddSingleton<
            ICityRepository,
            OsmCityRepository>();

        // --------------------------
        // Spatial
        // --------------------------

        services.AddSingleton<
            ICitySpatialIndex,
            CitySpatialIndex>();

        // --------------------------
        // Projecties
        // --------------------------

        services.AddSingleton<IMapProjection>(
            new WebMercatorProjection());

        services.AddSingleton<IMapProjection>(
            new Lambert2008Projection());

        services.AddSingleton<IMapProjection>(
            new AzimuthalEquidistantProjection());

        // --------------------------
        // App services
        // --------------------------

        services.AddSingleton<
            MapDatasetProvider>();

        // --------------------------
        // ViewModel
        // --------------------------

        services.AddSingleton(
            provider =>
            {
                IEnumerable<IMapProjection>
                    projections =
                        provider
                            .GetServices<
                                IMapProjection>();

                MapDatasetProvider
                    datasetProvider =
                        provider
                            .GetRequiredService<
                                MapDatasetProvider>();

                return new MainWindowViewModel(
                    projections,
                    datasetProvider.GetDatasets());
            });

        // --------------------------
        // View
        // --------------------------

        services.AddSingleton<
            MainWindow>();
    }

    protected override void OnStartup(
        StartupEventArgs e)
    {
        base.OnStartup(e);

        MainWindow mainWindow =
            _serviceProvider
                .GetRequiredService<
                    MainWindow>();

        mainWindow.Show();
    }

    protected override void OnExit(
        ExitEventArgs e)
    {
        _serviceProvider.Dispose();

        base.OnExit(e);
    }
}