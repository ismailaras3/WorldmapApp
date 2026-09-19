using System.Diagnostics;
using System.IO;
using System.Windows;
using WorldMap.App.Services;
using WorldMap.Core.Interfaces;
using WorldMap.Core.Models;
using WorldMap.Core.Projections;
using WorldMap.Infrastructure.GeoJson;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Controls;
using WorldMap.App.ViewModels;
using System.Windows.Media.Imaging;
using WorldMap.Core.Solar;
using WorldMap.Infrastructure.TimeZones;







namespace WorldMap.App;

public partial class MainWindow : Window
{


    // Per bitmap-pixel bewaren we de geografische positie.
    // null betekent dat de pixel niet op land ligt.
    private GeoCoordinate?[]? _solarPixelCache;

    private int _solarCacheWidth;
    private int _solarCacheHeight;

    // Cache voor deel 2:
    // alle geldige kaartpixels, dus land én zee.
    private GeoCoordinate?[]? _daylightPixelCache;

    private int _daylightCacheWidth;

    private int _daylightCacheHeight;

    private readonly ICountryRepository _countryRepository;
    // Eén instantie hergebruiken voor alle tijdzoneberekeningen.
    private readonly TimeZoneService _timeZoneService =
        new TimeZoneService();

    // Cache zodat we niet telkens dezelfde tijdzone opnieuw opzoeken.
    private readonly Dictionary<(int Lat, int Lon), TimeZoneInfo>
        _timeZoneCache = [];
    private IMapProjection _projection;
    //krijgt de DP

    private readonly ScaleTransform _scaleTransform =
    new ScaleTransform(1.0, 1.0);

    private readonly TranslateTransform _translateTransform =
        new TranslateTransform(0.0, 0.0);

    private readonly TransformGroup _mapTransform =
        new TransformGroup();

    private bool _isPanning;

    private Point _panStartMousePosition;

    private double _panStartX;

    private double _panStartY;

    private const double MinZoom = 1.0;

    private const double MaxZoom = 20.0;

    

    private IReadOnlyList<Country> _countries =
        Array.Empty<Country>();

    private MapRenderer? _mapRenderer;

    // Bitmaplaag waarop we de zonnekaarten zullen tekenen.
    private WriteableBitmap? _solarBitmap;



    private readonly MainWindowViewModel _viewModel;

    public MainWindow(
        ICountryRepository countryRepository,
        
        MainWindowViewModel viewModel)
    {
        InitializeComponent();

        _countryRepository =
            countryRepository;

        

        _viewModel =
            viewModel;

        DataContext =
            _viewModel;

        _projection =
            _viewModel.SelectedProjection
            ?? throw new InvalidOperationException(
                "Geen standaardprojectie beschikbaar.");

        ConfigureMapTransform();
        ConfigureMouseInteraction();

        Loaded += MainWindow_Loaded;
        SizeChanged += MainWindow_SizeChanged;
    }

    private async void MainWindow_Loaded(
    object sender,
    RoutedEventArgs e)
    {
        try
        {
            _mapRenderer =
                new MapRenderer(MapCanvas);

            if (_viewModel.SelectedDataset
                is MapDataset dataset)
            {
                await LoadDatasetAsync(dataset);
            }

        }
        catch (Exception ex)
        {
            MessageBox.Show(
                ex.ToString(),
                "Fout bij laden gegevens");
        }
    }
    private void ConfigureMapTransform()
    {
        _mapTransform.Children.Add(_scaleTransform);
        _mapTransform.Children.Add(_translateTransform);

        // De kaart en de zonne-overlay gebruiken exact
        // dezelfde pan- en zoomtransformatie.
        MapCanvas.RenderTransform =
            _mapTransform;

        SolarOverlay.RenderTransform =
            _mapTransform;

        MapCanvas.RenderTransformOrigin =
            new Point(0, 0);

        SolarOverlay.RenderTransformOrigin =
            new Point(0, 0);
    }



    private void RenderMap()
    {
        if (_mapRenderer is null)
            return;

        Stopwatch stopwatch =
            Stopwatch.StartNew();

        _mapRenderer.RenderCountries(
            _countries,
            _projection);
        UpdateCountryStrokeThickness();
        
       
        stopwatch.Stop();

        _viewModel.RenderTimeText =
    $"Render: {stopwatch.Elapsed.TotalMilliseconds:F2} ms";
    }

    

    

    private void ConfigureMouseInteraction()
    {
        MapViewport.MouseWheel +=
            MapViewport_MouseWheel;

        MapViewport.MouseLeftButtonDown +=
            MapViewport_MouseLeftButtonDown;

        MapViewport.MouseMove +=
            MapViewport_MouseMove;

        MapViewport.MouseLeftButtonUp +=
            MapViewport_MouseLeftButtonUp;

        MapViewport.MouseLeave +=
            MapViewport_MouseLeave;
    }

    private void MapViewport_MouseLeave(
    object sender,
    MouseEventArgs e)
    {
        if (_isPanning &&
            e.LeftButton == MouseButtonState.Released)
        {
            StopPanning();
        }
    }

    private void MapViewport_MouseLeftButtonUp(
    object sender,
    MouseButtonEventArgs e)
    {
        StopPanning();
        RenderMap();

        e.Handled = true;
    }

    // Bewaart een polygon samen met zijn geografische grenzen.
    // Zo hoeven we niet voor elke pixel meteen de volledige polygon te testen.
    private sealed class PolygonLookup
    {
        public GeoPolygon Polygon { get; init; } = null!;

        public double MinLongitude { get; init; }
        public double MaxLongitude { get; init; }

        public double MinLatitude { get; init; }
        public double MaxLatitude { get; init; }
    }

    private bool TryGetBaseGeoCoordinate(
    Point canvasPoint,
    out GeoCoordinate coordinate)
    {
        coordinate = default;

        if (_mapRenderer is null)
            return false;

        if (!_mapRenderer.TryCanvasToProjected(
                canvasPoint,
                out ProjectedPoint projectedPoint))
        {
            return false;
        }

        try
        {
            coordinate =
                _projection.Unproject(
                    projectedPoint);

            coordinate =
                new GeoCoordinate(
                    NormalizeLongitude(
                        coordinate.Longitude),
                    coordinate.Latitude);

            if (!double.IsFinite(coordinate.Longitude) ||
                !double.IsFinite(coordinate.Latitude))
            {
                return false;
            }

            return coordinate.Latitude
                is >= -90.0 and <= 90.0;
        }
        catch
        {
            return false;
        }
    }
    private List<PolygonLookup> BuildPolygonLookups()
    {
        List<PolygonLookup> result = [];

        foreach (Country country in _countries)
        {
            foreach (GeoPolygon polygon in country.Polygons)
            {
                if (polygon.OuterRing.Count < 3)
                    continue;

                double minLongitude =
                    polygon.OuterRing.Min(
                        point => point.Longitude);

                double maxLongitude =
                    polygon.OuterRing.Max(
                        point => point.Longitude);

                double minLatitude =
                    polygon.OuterRing.Min(
                        point => point.Latitude);

                double maxLatitude =
                    polygon.OuterRing.Max(
                        point => point.Latitude);

                result.Add(
                    new PolygonLookup
                    {
                        Polygon = polygon,

                        MinLongitude = minLongitude,
                        MaxLongitude = maxLongitude,

                        MinLatitude = minLatitude,
                        MaxLatitude = maxLatitude
                    });
            }
        }

        return result;
    }
    private void MapViewport_MouseMove(
    object sender,
    MouseEventArgs e)
    {
        Point currentPosition =
            e.GetPosition(MapViewport);

        if (_isPanning)
        {
            

            Vector difference =
                currentPosition -
                _panStartMousePosition;

            _translateTransform.X =
                _panStartX +
                difference.X;

            _translateTransform.Y =
                _panStartY +
                difference.Y;
        }

        UpdateMouseCoordinates(currentPosition);
    }

    private void MapViewport_MouseLeftButtonDown(
    object sender,
    MouseButtonEventArgs e)
    {
        _isPanning = true;

        _panStartMousePosition =
    e.GetPosition(MapViewport);


        _panStartX =
            _translateTransform.X;

        _panStartY =
            _translateTransform.Y;

        MapViewport.CaptureMouse();

        MapViewport.Cursor =
            Cursors.Hand;

        e.Handled = true;
    }

    private void MapViewport_MouseWheel(
    object sender,
    MouseWheelEventArgs e)
    {
        Stopwatch stopwatch =
            Stopwatch.StartNew();

        Point mousePosition =
            e.GetPosition(MapViewport);

        double oldScale =
            _scaleTransform.ScaleX;

        double zoomFactor =
            e.Delta > 0
                ? 1.2
                : 1.0 / 1.2;

        double newScale =
            oldScale * zoomFactor;

        newScale =
            Math.Clamp(
                newScale,
                MinZoom,
                MaxZoom);

        if (Math.Abs(newScale - oldScale) < 0.000001)
            return;

        double worldX =
            (mousePosition.X - _translateTransform.X)
            / oldScale;

        double worldY =
            (mousePosition.Y - _translateTransform.Y)
            / oldScale;

        _scaleTransform.ScaleX =
            newScale;

        _scaleTransform.ScaleY =
            newScale;
        UpdateCountryStrokeThickness();
        _translateTransform.X =
            mousePosition.X -
            worldX * newScale;

        _translateTransform.Y =
            mousePosition.Y -
            worldY * newScale;

        if (newScale <= MinZoom)
        {
            ResetPan();
        }

        _viewModel.ZoomText =
    $"Zoom: {newScale:F2}x";

        stopwatch.Stop();

        _viewModel.RenderTimeText =
    $"Zoom: {stopwatch.Elapsed.TotalMilliseconds:F2} ms";
        UpdateMouseCoordinates(
    mousePosition); RenderMap();
        e.Handled = true;
    }

    private void ResetPan()
    {
        _translateTransform.X = 0;
        _translateTransform.Y = 0;
    }


    private void StopPanning()
    {
        if (!_isPanning)
            return;

        _isPanning = false;

        MapViewport.ReleaseMouseCapture();

        MapViewport.Cursor =
            Cursors.Arrow;
    }

    private void MainWindow_SizeChanged(
    object sender,
    SizeChangedEventArgs e)
    {
        if (_countries.Count == 0)
            return;

        ResetView();

        RenderMap();

        // Andere grootte = oude landpixel-cache niet meer bruikbaar.
        _solarPixelCache = null;
        _daylightPixelCache = null;
        SolarOverlay.Source = null;
    }
    private void ResetView()
    {
        _scaleTransform.ScaleX = 1.0;
        _scaleTransform.ScaleY = 1.0;

        _translateTransform.X = 0.0;
        _translateTransform.Y = 0.0;

        _viewModel.ZoomText =
    "Zoom: 1.00x";
    }

    private async Task LoadDatasetAsync(
    MapDataset dataset)
    {
        string filePath =
             System.IO.Path.Combine(
                AppContext.BaseDirectory,
                "Data",
                dataset.FileName);

        Stopwatch stopwatch =
            Stopwatch.StartNew();

        _countries =
            await _countryRepository
                .LoadCountriesAsync(filePath);
        foreach (var group in _countries
             .GroupBy(country => country.Continent))
        {
            Debug.WriteLine(
                $"CONTINENT: '{group.Key}' -> {group.Count()} landen");
        }
        // Andere landdata = landpixel-cache opnieuw maken.
        _solarPixelCache = null;
        _daylightPixelCache = null;
        SolarOverlay.Source = null;

        stopwatch.Stop();

        _viewModel.CountryCountText =
    $"Landen: {_countries.Count}";

        _viewModel.LoadTimeText =
    $"Load: {stopwatch.Elapsed.TotalMilliseconds:F2} ms";
        UpdateDatasetInfo(dataset);

        ResetView();

        RenderMap();

        Title =
            $"Interactive World Map - {dataset.Name}";
    }

    private async void DatasetComboBox_SelectionChanged(
    object sender,
    SelectionChangedEventArgs e)
    {
        if (!IsLoaded)
            return;

        if (_viewModel.SelectedDataset
            is not MapDataset dataset)
        {
            return;
        }

        try
        {
            await LoadDatasetAsync(dataset);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                ex.Message,
                "Dataset kon niet worden geladen",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void ProjectionComboBox_SelectionChanged(
    object sender,
    SelectionChangedEventArgs e)
    {
        if (!IsLoaded)
            return;

        if (_viewModel.SelectedProjection
            is not IMapProjection selectedProjection)
        {
            return;
        }

        _projection =
    selectedProjection;

        ResetView();

        RenderMap();

        // Andere projectie = andere pixelposities,
        // dus de oude cache is niet meer geldig.
        _solarPixelCache = null;
        _daylightPixelCache = null;

        SolarOverlay.Source = null;


    }

    private Point ScreenToMapCanvas(
    Point screenPoint)
    {
        double scale =
            _scaleTransform.ScaleX;

        if (scale <= 0)
            return screenPoint;

        double canvasX =
            (screenPoint.X -
             _translateTransform.X)
            / scale;

        double canvasY =
            (screenPoint.Y -
             _translateTransform.Y)
            / scale;

        return new Point(
            canvasX,
            canvasY);
    }
    private void UpdateMouseCoordinates(
    Point mousePosition)
    {
        if (_mapRenderer is null)
        {
            _viewModel.CoordinateText =
                "Lon: --   Lat: --";

            _viewModel.SolarValueText =
                "Daglengte: --";

            return;
        }

        Point canvasPoint =
            ScreenToMapCanvas(
                mousePosition);

        if (!_mapRenderer.TryCanvasToProjected(
                canvasPoint,
                out ProjectedPoint projectedPoint))
        {
            _viewModel.CoordinateText =
                "Lon: --   Lat: --";

            _viewModel.SolarValueText =
                "Daglengte: --";

            return;
        }

        try
        {
            GeoCoordinate coordinate =
                _projection.Unproject(
                    projectedPoint);

            coordinate =
                new GeoCoordinate(
                    NormalizeLongitude(
                        coordinate.Longitude),
                    coordinate.Latitude);

            if (!double.IsFinite(
                    coordinate.Longitude) ||
                !double.IsFinite(
                    coordinate.Latitude))
            {
                _viewModel.CoordinateText =
                    "Lon: --   Lat: --";

                _viewModel.SolarValueText =
                    "Daglengte: --";

                return;
            }

            if (coordinate.Latitude < -90.0 ||
                coordinate.Latitude > 90.0)
            {
                _viewModel.CoordinateText =
                    "Lon: --   Lat: --";

                _viewModel.SolarValueText =
                    "Daglengte: --";

                return;
            }

            // Gewone longitude/latitude onder de muis.
            _viewModel.CoordinateText =
                $"Lon: {coordinate.Longitude:F6}°   " +
                $"Lat: {coordinate.Latitude:F6}°";

            string parameter =
     _viewModel.SelectedSolarParameter;

            if (parameter == "Daglengte")
            {
                double dayLength =
                    SolarCalculator.CalculateDayLengthHours(
                        coordinate,
                        _viewModel.SelectedDate);

                _viewModel.SolarValueText =
                    $"Daglengte: " +
                    $"{SolarCalculator.FormatHours(dayLength)}";
            }
            else
            {
                double? clockMinutes =
                    CalculateLocalClockMinutes(
                        coordinate,
                        _viewModel.SelectedDate,
                        parameter);

                _viewModel.SolarValueText =
                    clockMinutes.HasValue
                        ? $"{parameter}: " +
                          $"{SolarCalculator.FormatMinutesAsClock(clockMinutes.Value)}"
                        : $"{parameter}: --";
            }
        }
        catch
        {
            _viewModel.CoordinateText =
                "Lon: --   Lat: --";

            _viewModel.SolarValueText =
                "Daglengte: --";
        }
    }

    private bool TryGetGeoCoordinate(
    Point mousePosition,
    out GeoCoordinate coordinate)
    {
        coordinate = default;

        if (_mapRenderer is null)
            return false;

        Point canvasPoint =
            ScreenToMapCanvas(mousePosition);

        if (!_mapRenderer.TryCanvasToProjected(
                canvasPoint,
                out ProjectedPoint projectedPoint))
        {
            return false;
        }

        try
        {
            coordinate =
                _projection.Unproject(projectedPoint);
            coordinate =
    new GeoCoordinate(
        NormalizeLongitude(
            coordinate.Longitude),
        coordinate.Latitude);

            if (!double.IsFinite(coordinate.Longitude) ||
                !double.IsFinite(coordinate.Latitude))
            {
                return false;
            }

            return coordinate.Latitude is >= -90 and <= 90;
        }
        catch
        {
            return false;
        }
    }

    private static double NormalizeLongitude(
    double longitude)
    {
        while (longitude > 180.0)
        {
            longitude -= 360.0;
        }

        while (longitude < -180.0)
        {
            longitude += 360.0;
        }

        return longitude;
    }
    
    private bool TryGetViewportGeoBounds(
    out double minLongitude,
    out double minLatitude,
    out double maxLongitude,
    out double maxLatitude)
    {
        minLongitude = double.MaxValue;
        minLatitude = double.MaxValue;

        maxLongitude = double.MinValue;
        maxLatitude = double.MinValue;

        if (_mapRenderer is null)
            return false;

        double width =
            MapViewport.ActualWidth;

        double height =
            MapViewport.ActualHeight;

        if (width <= 0 ||
            height <= 0)
        {
            return false;
        }

        //
        // Niet alleen de vier hoeken:
        // we samplen meerdere plaatsen langs de viewport,
        // omdat projecties gekromd kunnen zijn.
        //
        const int samples = 8;

        List<Point> screenPoints = [];

        for (int i = 0; i <= samples; i++)
        {
            double fraction =
                i / (double)samples;

            double x =
                width * fraction;

            double y =
                height * fraction;

            // boven
            screenPoints.Add(
                new Point(x, 0));

            // onder
            screenPoints.Add(
                new Point(x, height));

            // links
            screenPoints.Add(
                new Point(0, y));

            // rechts
            screenPoints.Add(
                new Point(width, y));
        }

        int validCount = 0;

        foreach (Point screenPoint in screenPoints)
        {
            if (!TryGetGeoCoordinate(
                    screenPoint,
                    out GeoCoordinate coordinate))
            {
                continue;
            }

            double longitude =
                NormalizeLongitude(
                    coordinate.Longitude);

            minLongitude =
                Math.Min(
                    minLongitude,
                    longitude);

            maxLongitude =
                Math.Max(
                    maxLongitude,
                    longitude);

            minLatitude =
                Math.Min(
                    minLatitude,
                    coordinate.Latitude);

            maxLatitude =
                Math.Max(
                    maxLatitude,
                    coordinate.Latitude);

            validCount++;
        }

        return validCount >= 2;
    }
    private void SolarDatePicker_SelectedDateChanged(
     object? sender,
     SelectionChangedEventArgs e)
    {
        if (!IsLoaded)
            return;

        // Datum wordt via binding in de ViewModel bijgewerkt.
        // De kaart wordt pas berekend wanneer de gebruiker dit vraagt.
    }





    private void UpdateCountryStrokeThickness()
    {
        double zoom =
            _scaleTransform.ScaleX;

        if (zoom <= 0)
            return;

        double thickness =
            0.6 / zoom;

        foreach (UIElement child
                 in MapCanvas.Children)
        {
            if (child is System.Windows.Shapes.Path path &&
                path.Tag is Country)
            {
                path.StrokeThickness =
                    thickness;
            }
        }
    }
    private async Task RenderSelectedSolarOverlayAsync()
    {
        if (_mapRenderer is null)
            return;

        Stopwatch stopwatch =
            Stopwatch.StartNew();

        // De landcache bouwen we één keer.
        if (_solarPixelCache is null)
        {
            BuildSolarPixelCache();
        }

        if (_solarPixelCache is null ||
            _solarCacheWidth <= 0 ||
            _solarCacheHeight <= 0)
        {
            return;
        }

        int width =
            _solarCacheWidth;

        int height =
            _solarCacheHeight;

        DateTime selectedDate =
            _viewModel.SelectedDate;

        string parameter =
            _viewModel.SelectedSolarParameter;
        _viewModel.ColorScaleTitle =
    parameter;

        // Lokale kopie zodat de achtergrondthread
        // niet aan WPF-controls hoeft te komen.
        GeoCoordinate?[] coordinates =
            _solarPixelCache;

        double?[] values =
            await Task.Run(
                () =>
                {
                    double?[] result =
                        new double?[coordinates.Length];

                    for (int i = 0;
                         i < coordinates.Length;
                         i++)
                    {
                        GeoCoordinate? coordinate =
                            coordinates[i];

                        if (!coordinate.HasValue)
                            continue;

                        if (parameter == "Daglengte")
                        {
                            result[i] =
                                SolarCalculator
                                    .CalculateDayLengthHours(
                                        coordinate.Value,
                                        selectedDate);
                        }
                        else
                        {
                            result[i] =
                                CalculateLocalClockMinutes(
                                    coordinate.Value,
                                    selectedDate,
                                    parameter);
                        }
                    }

                    return result;
                });

        double minValue =
    double.MaxValue;

        double maxValue =
            double.MinValue;

        bool isClockParameter =
            parameter != "Daglengte";

        double clockScaleStart = 0.0;

        if (isClockParameter)
        {
            // Zoek de beste plaats om de 24-uurscirkel
            // open te knippen.
            clockScaleStart =
                FindClockScaleStart(values);
        }

        foreach (double? value in values)
        {
            if (!value.HasValue)
                continue;

            double comparableValue =
                isClockParameter
                    ? UnwrapClockMinutes(
                        value.Value,
                        clockScaleStart)
                    : value.Value;

            minValue =
                Math.Min(
                    minValue,
                    comparableValue);

            maxValue =
                Math.Max(
                    maxValue,
                    comparableValue);
        }

        if (minValue == double.MaxValue ||
            maxValue == double.MinValue)
        {
            _viewModel.ColorScaleText =
                "Geen waarden beschikbaar";

            SolarOverlay.Source =
                null;

            return;
        }

        // Legende correct formatteren.
        if (parameter == "Daglengte")
        {
            _viewModel.ColorScaleText =
                $"{SolarCalculator.FormatHours(minValue)} → " +
                $"{SolarCalculator.FormatHours(maxValue)}";
        }
        else
        {
            _viewModel.ColorScaleText =
                $"{SolarCalculator.FormatMinutesAsClock(minValue)} → " +
                $"{SolarCalculator.FormatMinutesAsClock(maxValue)}";
        }

        double range =
            maxValue - minValue;

        const int bytesPerPixel = 4;

        int stride =
            width * bytesPerPixel;

        byte[] pixels =
            new byte[
                height * stride];

        // Alleen het maken van de kleuren.
        await Task.Run(
            () =>
            {
                for (int pixelNumber = 0;
                     pixelNumber < values.Length;
                     pixelNumber++)
                {
                    double? value =
                        values[pixelNumber];

                    if (!value.HasValue)
                        continue;

                    double comparableValue =
    isClockParameter
        ? UnwrapClockMinutes(
            value.Value,
            clockScaleStart)
        : value.Value;

                    double normalized =
                        range < 1e-12
                            ? 0.5
                            : (comparableValue - minValue) /
                              range;

                    normalized =
                        Math.Clamp(
                            normalized,
                            0.0,
                            1.0);

                    byte red =
                        (byte)(
                            255 *
                            normalized);

                    byte green =
                        (byte)(
                            220 *
                            normalized);

                    byte blue =
                        (byte)(
                            255 *
                            (1.0 - normalized));

                    int index =
                        pixelNumber *
                        bytesPerPixel;

                    pixels[index] =
                        blue;

                    pixels[index + 1] =
                        green;

                    pixels[index + 2] =
                        red;

                    pixels[index + 3] =
                        190;
                }
            });

        _solarBitmap =
            new WriteableBitmap(
                width,
                height,
                96,
                96,
                PixelFormats.Bgra32,
                null);

        _solarBitmap.WritePixels(
            new Int32Rect(
                0,
                0,
                width,
                height),
            pixels,
            stride,
            0);

        SolarOverlay.Source =
            _solarBitmap;

        stopwatch.Stop();

        _viewModel.RenderTimeText =
            $"Solar: {stopwatch.Elapsed.TotalMilliseconds:F0} ms";
    }
    private async void CalculateSolarMapButton_Click(
    object sender,
    RoutedEventArgs e)
    {
        CalculateSolarMapButton.IsEnabled =
            false;

        try
        {
            await RenderSelectedSolarOverlayAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                ex.Message,
                "Fout bij zonneberekening",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            CalculateSolarMapButton.IsEnabled =
                true;
        }
    }

    private void UpdateDatasetInfo(
    MapDataset dataset)
    {
        string points =
            dataset.PointCount?.ToString("N0")
            ?? "--";

        string reduction =
            dataset.ReductionPercent.HasValue
                ? $"{dataset.ReductionPercent:F2}%"
                : "--";

        string fileSize =
            dataset.FileSizeMb.HasValue
                ? $"{dataset.FileSizeMb:F2} MB"
                : "--";

        _viewModel.DatasetInfoText =
            $"Punten: {points} | " +
            $"Reductie: {reduction} | " +
            $"Bestand: {fileSize}";
    }

    private sealed class CountryGeometryLookup
    {
        public Geometry Geometry { get; init; } = null!;

        public Rect Bounds { get; init; }
    }

    private void RenderDayLengthOverlay()
    {
        if (_mapRenderer is null)
            return;

        Stopwatch stopwatch =
            Stopwatch.StartNew();

        // Landpixels hoeven alleen opnieuw berekend te worden
        // wanneer projectie, dataset of grootte verandert.
        if (_solarPixelCache is null)
        {
            BuildSolarPixelCache();
        }

        if (_solarPixelCache is null ||
            _solarCacheWidth <= 0 ||
            _solarCacheHeight <= 0)
        {
            return;
        }

        int width =
            _solarCacheWidth;

        int height =
            _solarCacheHeight;

        DateTime selectedDate =
            _viewModel.SelectedDate;

        /*
         * Eerst alle daglengtes berekenen.
         * Zo kunnen we daarna het minimum en maximum
         * van deze kaart bepalen.
         */
        double?[] values =
            new double?[_solarPixelCache.Length];

        double minValue =
            double.MaxValue;

        double maxValue =
            double.MinValue;

        for (int i = 0;
             i < _solarPixelCache.Length;
             i++)
        {
            GeoCoordinate? cachedCoordinate =
                _solarPixelCache[i];

            // null betekent zee.
            if (!cachedCoordinate.HasValue)
                continue;

            double dayLength =
                SolarCalculator.CalculateDayLengthHours(
                    cachedCoordinate.Value,
                    selectedDate);

            values[i] =
                dayLength;

            minValue =
                Math.Min(
                    minValue,
                    dayLength);

            maxValue =
                Math.Max(
                    maxValue,
                    dayLength);
        }

        if (minValue == double.MaxValue ||
            maxValue == double.MinValue)
        {
            return;
        }

        // Toon het echte bereik van de huidige kaart.
        _viewModel.ColorScaleText =
            $"{SolarCalculator.FormatHours(minValue)}  →  " +
            $"{SolarCalculator.FormatHours(maxValue)}";

        _solarBitmap =
            new WriteableBitmap(
                width,
                height,
                96,
                96,
                PixelFormats.Bgra32,
                null);

        const int bytesPerPixel = 4;

        int stride =
            width * bytesPerPixel;

        byte[] pixels =
            new byte[
                height * stride];

        double range =
            maxValue - minValue;

        for (int pixelNumber = 0;
             pixelNumber < values.Length;
             pixelNumber++)
        {
            double? value =
                values[pixelNumber];

            // Geen waarde = zee.
            if (!value.HasValue)
                continue;

            /*
             * Waarde omzetten naar 0..1,
             * maar nu relatief aan het minimum en maximum
             * van deze specifieke kaart.
             */
            double normalized =
                range < 1e-12
                    ? 0.5
                    : (value.Value - minValue) / range;

            normalized =
                Math.Clamp(
                    normalized,
                    0.0,
                    1.0);

            // Korte dag = blauw.
            // Lange dag = geel.
            byte red =
                (byte)(
                    255 *
                    normalized);

            byte green =
                (byte)(
                    220 *
                    normalized);

            byte blue =
                (byte)(
                    255 *
                    (1.0 - normalized));

            int index =
                pixelNumber *
                bytesPerPixel;

            pixels[index] =
                blue;

            pixels[index + 1] =
                green;

            pixels[index + 2] =
                red;

            pixels[index + 3] =
                190;
        }

        _solarBitmap.WritePixels(
            new Int32Rect(
                0,
                0,
                width,
                height),
            pixels,
            stride,
            0);

        SolarOverlay.Source =
            _solarBitmap;

        stopwatch.Stop();

        _viewModel.RenderTimeText =
            $"Solar: {stopwatch.Elapsed.TotalMilliseconds:F0} ms";
    }

    private List<CountryGeometryLookup>
    GetCountryGeometryLookups()
    {
        List<CountryGeometryLookup> result = [];

        string selectedContinent =
            _viewModel.SelectedContinent;

        int totalPaths = 0;
        int countryPaths = 0;
        int matchingCountries = 0;

        foreach (UIElement child
                 in MapCanvas.Children)
        {
            if (child is not
                System.Windows.Shapes.Path path)
            {
                continue;
            }

            totalPaths++;

            if (path.Tag is not Country country)
                continue;

            countryPaths++;

            if (!string.Equals(
                    country.Continent?.Trim(),
                    selectedContinent?.Trim(),
                    StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            matchingCountries++;

            if (path.Data is not Geometry geometry)
                continue;

            result.Add(
                new CountryGeometryLookup
                {
                    Geometry = geometry,
                    Bounds = geometry.Bounds
                });
        }

        Debug.WriteLine(
            $"SOLAR FILTER | " +
            $"gekozen='{selectedContinent}' | " +
            $"paths={totalPaths} | " +
            $"countryPaths={countryPaths} | " +
            $"matches={matchingCountries} | " +
            $"geometries={result.Count}");

        return result;
    }
    private void ContinentComboBox_SelectionChanged(
    object sender,
    SelectionChangedEventArgs e)
    {
        if (!IsLoaded)
            return;

        // Ander continent betekent andere landpixels.
        _solarPixelCache = null;

        // Oude zonnekaart wegdoen.
        SolarOverlay.Source = null;

        _viewModel.ColorScaleText =
            "Schaal: --";
    }
    private void BuildSolarPixelCache()
    {
        double viewportWidth =
            MapViewport.ActualWidth;

        double viewportHeight =
            MapViewport.ActualHeight;

        if (viewportWidth <= 0 ||
            viewportHeight <= 0)
        {
            return;
        }

        // Lagere resolutie voor betere snelheid.
        const double resolutionScale = 0.30;

        int width =
            Math.Max(
                1,
                (int)(viewportWidth * resolutionScale));

        int height =
            Math.Max(
                1,
                (int)(viewportHeight * resolutionScale));

        _solarCacheWidth =
            width;

        _solarCacheHeight =
            height;

        _solarPixelCache =
            new GeoCoordinate?[width * height];

        // Gebruik de polygonen zoals ze werkelijk
        // door MapRenderer op het Canvas getekend zijn.
        List<CountryGeometryLookup> countries =
            GetCountryGeometryLookups();

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // Kleine bitmap-pixel omzetten
                // naar positie op het echte Canvas.
                double canvasX =
                    (x + 0.5) *
                    viewportWidth /
                    width;

                double canvasY =
                    (y + 0.5) *
                    viewportHeight /
                    height;

                Point canvasPoint =
                    new Point(
                        canvasX,
                        canvasY);

                bool isOnLand =
                    false;

                foreach (CountryGeometryLookup country
                         in countries)
                {
                    // Eerst goedkope bounding-box test.
                    if (!country.Bounds.Contains(
                            canvasPoint))
                    {
                        continue;
                    }

                    // Daarna pas exact controleren
                    // of de pixel werkelijk in het land ligt.
                    if (country.Geometry.FillContains(
                            canvasPoint))
                    {
                        isOnLand =
                            true;

                        break;
                    }
                }

                if (!isOnLand)
                    continue;

                // Voor de zonneberekening hebben we
                // uiteindelijk longitude/latitude nodig.
                if (!TryGetBaseGeoCoordinate(
                        canvasPoint,
                        out GeoCoordinate coordinate))
                {
                    continue;
                }

                _solarPixelCache[
                    y * width + x] =
                    coordinate;
            }
        }
    }

    private TimeZoneInfo GetCachedTimeZone(
    GeoCoordinate coordinate)
    {
        // Coördinaten afronden op ongeveer 0,5°.
        // Nabije pixels delen daardoor dezelfde timezone lookup.
        int latKey =
    (int)Math.Round(
        coordinate.Latitude * 10.0);

        int lonKey =
            (int)Math.Round(
                coordinate.Longitude * 10.0);

        var key =
            (latKey, lonKey);

        if (_timeZoneCache.TryGetValue(
                key,
                out TimeZoneInfo? cached))
        {
            return cached;
        }

        TimeZoneInfo timeZone =
            _timeZoneService.GetTimeZone(
                coordinate);

        _timeZoneCache[key] =
            timeZone;

        return timeZone;
    }
    private double? CalculateLocalClockMinutes(
    GeoCoordinate coordinate,
    DateTime date,
    string parameter)
    {
        DateTime? utcTime =
            parameter switch
            {
                "Solar noon" =>
                    SolarCalculator.CalculateSolarNoonUtc(
                        coordinate,
                        date),

                "Zonsopgang" =>
                    SolarCalculator.CalculateSunriseUtc(
                        coordinate,
                        date),

                "Zonsondergang" =>
                    SolarCalculator.CalculateSunsetUtc(
                        coordinate,
                        date),

                _ => null
            };

        // Bijvoorbeeld poolnacht/pooldag:
        // er bestaat dan geen gewone sunrise/sunset.
        if (!utcTime.HasValue)
            return null;

        TimeZoneInfo timeZone =
            GetCachedTimeZone(
                coordinate);

        DateTime localTime =
            TimeZoneInfo.ConvertTimeFromUtc(
                DateTime.SpecifyKind(
                    utcTime.Value,
                    DateTimeKind.Utc),
                timeZone);

        return
            localTime.Hour * 60.0 +
            localTime.Minute +
            localTime.Second / 60.0;
    }
    private static double FindClockScaleStart(
    IEnumerable<double?> values)
    {
        List<double> times =
            values
                .Where(value => value.HasValue)
                .Select(value =>
                    NormalizeClockMinutes(value!.Value))
                .OrderBy(value => value)
                .ToList();

        if (times.Count == 0)
            return 0.0;

        if (times.Count == 1)
            return times[0];

        double largestGap = -1.0;
        double scaleStart = times[0];

        for (int i = 0; i < times.Count - 1; i++)
        {
            double gap =
                times[i + 1] - times[i];

            if (gap > largestGap)
            {
                largestGap = gap;

                // De schaal begint na de grootste lege zone.
                scaleStart = times[i + 1];
            }
        }

        // Ook de overgang 23:xx -> 00:xx controleren.
        double midnightGap =
            (times[0] + 1440.0) -
            times[^1];

        if (midnightGap > largestGap)
        {
            scaleStart =
                times[0];
        }

        return scaleStart;
    }
    private static double UnwrapClockMinutes(
    double value,
    double scaleStart)
    {
        double normalized =
            NormalizeClockMinutes(value);

        if (normalized < scaleStart)
        {
            normalized += 1440.0;
        }

        return normalized;
    }
    private static double NormalizeClockMinutes(
    double minutes)
    {
        minutes %= 1440.0;

        if (minutes < 0.0)
            minutes += 1440.0;

        return minutes;
    }

    private void BuildDaylightPixelCache()
    {
        double viewportWidth =
            MapViewport.ActualWidth;

        double viewportHeight =
            MapViewport.ActualHeight;

        if (viewportWidth <= 0 ||
            viewportHeight <= 0)
        {
            return;
        }

        const double resolutionScale =
            0.30;

        int width =
            Math.Max(
                1,
                (int)(viewportWidth * resolutionScale));

        int height =
            Math.Max(
                1,
                (int)(viewportHeight * resolutionScale));

        _daylightCacheWidth =
            width;

        _daylightCacheHeight =
            height;

        _daylightPixelCache =
            new GeoCoordinate?[
                width * height];

        for (int y = 0;
             y < height;
             y++)
        {
            for (int x = 0;
                 x < width;
                 x++)
            {
                double canvasX =
                    (x + 0.5) *
                    viewportWidth /
                    width;

                double canvasY =
                    (y + 0.5) *
                    viewportHeight /
                    height;

                Point canvasPoint =
                    new Point(
                        canvasX,
                        canvasY);

                if (!TryGetBaseGeoCoordinate(
                        canvasPoint,
                        out GeoCoordinate coordinate))
                {
                    continue;
                }

                _daylightPixelCache[
                    y * width + x] =
                    coordinate;
            }
        }
    }


    private async Task RenderDaylightOverlayAsync()
    {
        Stopwatch stopwatch =
            Stopwatch.StartNew();

        if (_daylightPixelCache is null)
        {
            BuildDaylightPixelCache();
        }

        if (_daylightPixelCache is null)
            return;

        int width =
            _daylightCacheWidth;

        int height =
            _daylightCacheHeight;

        GeoCoordinate?[] coordinates =
            _daylightPixelCache;

        int selectedMinutes =
            (int)Math.Round(
                _viewModel.DaylightTimeMinutes);

        int hour =
            selectedMinutes / 60;

        int minute =
            selectedMinutes % 60;

        DateTime date =
            _viewModel.SelectedDate;

        DateTime utcTime =
            new DateTime(
                date.Year,
                date.Month,
                date.Day,
                hour,
                minute,
                0,
                DateTimeKind.Utc);

        const int bytesPerPixel = 4;

        int stride =
            width * bytesPerPixel;

        byte[] pixels =
            new byte[
                height * stride];

        await Task.Run(
            () =>
            {
                for (int i = 0;
                     i < coordinates.Length;
                     i++)
                {
                    GeoCoordinate? coordinate =
                        coordinates[i];

                    if (!coordinate.HasValue)
                        continue;

                    double altitude =
                        SolarCalculator
                            .CalculateSolarAltitudeDegrees(
                                coordinate.Value,
                                utcTime);

                    byte red;
                    byte green;
                    byte blue;
                    byte alpha;

                    if (altitude < -18.0)
                    {
                        // Astronomische nacht
                        red = 10;
                        green = 15;
                        blue = 45;
                        alpha = 210;
                    }
                    else if (altitude < -6.0)
                    {
                        // Nautische schemer
                        red = 45;
                        green = 55;
                        blue = 120;
                        alpha = 190;
                    }
                    else if (altitude < 0.0)
                    {
                        // Civiele schemer
                        red = 180;
                        green = 110;
                        blue = 80;
                        alpha = 170;
                    }
                    else
                    {
                        // Dag
                        red = 255;
                        green = 230;
                        blue = 120;
                        alpha = 120;
                    }

                    int index =
                        i * bytesPerPixel;

                    // BGRA
                    pixels[index] =
                        blue;

                    pixels[index + 1] =
                        green;

                    pixels[index + 2] =
                        red;

                    pixels[index + 3] =
                        alpha;
                }
            });

        _solarBitmap =
            new WriteableBitmap(
                width,
                height,
                96,
                96,
                PixelFormats.Bgra32,
                null);

        _solarBitmap.WritePixels(
            new Int32Rect(
                0,
                0,
                width,
                height),
            pixels,
            stride,
            0);

        SolarOverlay.Source =
            _solarBitmap;

        stopwatch.Stop();

        _viewModel.RenderTimeText =
            $"Daglicht: " +
            $"{stopwatch.Elapsed.TotalMilliseconds:F0} ms";
    }
    private async void CalculateDaylightMapButton_Click(
    object sender,
    RoutedEventArgs e)
    {
        CalculateDaylightMapButton.IsEnabled =
            false;

        try
        {
            await RenderDaylightOverlayAsync();
        }
        finally
        {
            CalculateDaylightMapButton.IsEnabled =
                true;
        }
    }
    private void DaylightTimeSlider_ValueChanged(
    object sender,
    RoutedPropertyChangedEventArgs<double> e)
    {
        if (_viewModel is null)
            return;

        int totalMinutes =
            (int)Math.Round(e.NewValue);

        int hours =
            totalMinutes / 60;

        int minutes =
            totalMinutes % 60;

        _viewModel.DaylightTimeText =
            $"{hours:D2}:{minutes:D2} UTC";
    }

}