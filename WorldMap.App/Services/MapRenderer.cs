using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using WorldMap.Core.Models;
using WorldMap.Core.Projections;
using WorldMap.Core.Geometry;

namespace WorldMap.App.Services;

public sealed class MapRenderer
{//naam zegt het zelf eigenlijk deze stuk code krijgt enkel de geografische coordinaten en zet het dan om 
    //projecteert long/lat naar x/y
    //bepaald de totale bounding box van de kaart 
    //berekend de schaal factor
    //
    
    private readonly Canvas _canvas;

    private double _minX;
    private double _maxY;
    private double _fitScale;
    private double _offsetX;
    private double _offsetY;

    private bool _hasValidTransform;

    public MapRenderer(Canvas canvas)
    {
        _canvas = canvas;
    }
    public bool TryCanvasToProjected(//Deze methode zet een positie op het Canvas terug om naar de geprojecteerde kaartcoördinaten.
                                     //Daarna kan ik met de inverse kaartprojectie longitude en latitude bepalen
    Point canvasPoint,
    out ProjectedPoint projectedPoint)
    {
        projectedPoint = default;

        if (!_hasValidTransform)
            return false;

        if (_fitScale <= 0 ||
            !double.IsFinite(_fitScale))
        {
            return false;
        }

        double projectedX =
            _minX +
            (canvasPoint.X - _offsetX)
            / _fitScale;

        double projectedY =
            _maxY -
            (canvasPoint.Y - _offsetY)
            / _fitScale;

        if (!double.IsFinite(projectedX) ||
            !double.IsFinite(projectedY))
        {
            return false;
        }

        projectedPoint =
            new ProjectedPoint(
                projectedX,
                projectedY);

        return true;
    }

    public void RenderCountries(//RenderCountries projecteert eerst alle geografische punten.
                                //Daarna bepaalt hij de totale grootte van de geprojecteerde kaart en berekent
                                //hij een schaalfactor zodat de kaart netjes binnen het WPF Canvas past.
        IReadOnlyList<Country> countries,
        IMapProjection projection)
    {
        _canvas.Children.Clear();

        double canvasWidth = _canvas.ActualWidth;
        double canvasHeight = _canvas.ActualHeight;

        if (canvasWidth <= 0 || canvasHeight <= 0)
            return;

        var projectedPolygons =
    new List<(
        Country Country,
        List<List<ProjectedPoint>> Rings)>();

        double minX = double.MaxValue;
        double maxX = double.MinValue;
        double minY = double.MaxValue;
        double maxY = double.MinValue;

        foreach (Country country in countries)
        {
            foreach (GeoPolygon polygon in country.Polygons)
            {
                var rings = new List<List<ProjectedPoint>>();

                AddRing(//AddRing zet een geografische polygon-ring om naar een lijst met geprojecteerde X/Y-punten.
                    polygon.OuterRing,
                    rings,
                    projection,
                    ref minX,
                    ref maxX,
                    ref minY,
                    ref maxY);

                foreach (var hole in polygon.Holes)
                {
                    AddRing(
                        hole,
                        rings,
                        projection,
                        ref minX,
                        ref maxX,
                        ref minY,
                        ref maxY);
                }

                projectedPolygons.Add(
    (country, rings));
            }
        }

        double mapWidth = maxX - minX;
        double mapHeight = maxY - minY;

        if (mapWidth <= 0 || mapHeight <= 0)
            return;

        const double margin = 20;

        double availableWidth =
            Math.Max(1, canvasWidth - 2 * margin);

        double availableHeight =
            Math.Max(1, canvasHeight - 2 * margin);

        double scale =
            Math.Min(
                availableWidth / mapWidth,
                availableHeight / mapHeight);

        double renderedWidth = mapWidth * scale;
        double renderedHeight = mapHeight * scale;

        double offsetX =
            (canvasWidth - renderedWidth) / 2.0;

        double offsetY =
            (canvasHeight - renderedHeight) / 2.0;
        _minX = minX;
        _maxY = maxY;
        _fitScale = scale;
        _offsetX = offsetX;
        _offsetY = offsetY;

        _hasValidTransform = true;

        foreach (var item in projectedPolygons)
        {
            Country country =
                item.Country;

            List<List<ProjectedPoint>> polygonRings =
                item.Rings;
            var geometry = new PathGeometry
            {
                FillRule = FillRule.EvenOdd //hier zorg ik er voor dat de holes in polygonen correct transparant blijven 
            };

            foreach (var ring in polygonRings)
            {
                if (projection is AzimuthalEquidistantProjection)
                {
                    AddAzimuthalRingFigures(
                        geometry,
                        ring,
                        minX,
                        maxX,
                        minY,
                        maxY,
                        scale,
                        offsetX,
                        offsetY);
                }
                else
                {
                    AddNormalRingFigure(
                        geometry,
                        ring,
                        minX,
                        maxY,
                        scale,
                        offsetX,
                        offsetY);
                }
            }

            var path =
    new Path
    {
        Data = geometry,
        Fill = Brushes.LightGoldenrodYellow,
        Stroke = Brushes.DarkSlateGray,
        StrokeThickness = 0.6,

        // De Path onthoudt nu bij welk land hij hoort.
        Tag = country
    };

            _canvas.Children.Add(path);
        }
    }

    private static void AddRing(
    IReadOnlyList<GeoCoordinate> coordinates,
    List<List<ProjectedPoint>> rings,
    IMapProjection projection,
    ref double minX,
    ref double maxX,
    ref double minY,
    ref double maxY)
    {
        IReadOnlyList<GeoCoordinate> usableCoordinates =
            coordinates;

        if (projection is WebMercatorProjection)
        {
            usableCoordinates =
                GeoClipper.ClipLatitude(//Mercator kan de polen niet projecteren omdat de formule daar naar oneindig gaat.
                                        //Daarom clip ik de latitude vóór het projecteren.
                    coordinates,
                    -WebMercatorProjection.MaxLatitude,
                    WebMercatorProjection.MaxLatitude);
        }
        else if (projection is Lambert2008Projection)
        {
            usableCoordinates =
                GeoClipper.ClipLatitude(
                    coordinates,
                    -60.0,
                    85.0);
        }

        if (usableCoordinates.Count < 3)
            return;

        var projectedRing =
            new List<ProjectedPoint>();

        foreach (GeoCoordinate coordinate in usableCoordinates)
        {
            ProjectedPoint point =
                projection.Project(coordinate);

            if (!double.IsFinite(point.X) ||
    !double.IsFinite(point.Y))
            {
                continue;
            }

            projectedRing.Add(point);

            minX = Math.Min(minX, point.X);
            maxX = Math.Max(maxX, point.X);
            minY = Math.Min(minY, point.Y);
            maxY = Math.Max(maxY, point.Y);
        }

        if (projectedRing.Count >= 3)
        {
            rings.Add(projectedRing);
        }
    }

    private static Point ToCanvasPoint(
        ProjectedPoint point,
        double minX,
        double maxY,
        double scale,
        double offsetX,
        double offsetY)
    {
        double x =
            offsetX + (point.X - minX) * scale;

        double y =
            offsetY + (maxY - point.Y) * scale;//y is nomgekeerd in wpf en anders in projectie daarom de -

        return new Point(x, y);
    }
    private static void AddAzimuthalRingFigures(
    PathGeometry geometry,
    IReadOnlyList<ProjectedPoint> ring,
    double minX,
    double maxX,
    double minY,
    double maxY,
    double scale,
    double offsetX,
    double offsetY)
    {
        if (ring.Count < 3)
            return;

        double mapWidth =
            maxX - minX;

        double mapHeight =
            maxY - minY;

        double mapDiagonal =
            Math.Sqrt(
                mapWidth * mapWidth +
                mapHeight * mapHeight);

        double jumpThreshold =
            mapDiagonal * 0.10;

        List<ProjectedPoint> currentPart = [];

        currentPart.Add(ring[0]);

        bool wasSplit = false;

        for (int i = 1; i < ring.Count; i++)
        {
            ProjectedPoint previous =
                ring[i - 1];

            ProjectedPoint current =
                ring[i];

            double dx =
                current.X - previous.X;

            double dy =
                current.Y - previous.Y;

            double distance =
                Math.Sqrt(
                    dx * dx +
                    dy * dy);

            if (distance > jumpThreshold)
            {
                wasSplit = true;

                AddAzimuthalPart(
                    geometry,
                    currentPart,
                    minX,
                    maxY,
                    scale,
                    offsetX,
                    offsetY,
                    closeAndFill: false);

                currentPart = [];
            }

            currentPart.Add(current);
        }

        AddAzimuthalPart(
            geometry,
            currentPart,
            minX,
            maxY,
            scale,
            offsetX,
            offsetY,
            closeAndFill: !wasSplit);
    }

    private static void AddAzimuthalPart(
        PathGeometry geometry,
        IReadOnlyList<ProjectedPoint> points,
        double minX,
        double maxY,
        double scale,
        double offsetX,
        double offsetY,
        bool closeAndFill)
    {
        if (points.Count < 2)
            return;

        Point firstPoint =
            ToCanvasPoint(
                points[0],
                minX,
                maxY,
                scale,
                offsetX,
                offsetY);

        var figure =
            new PathFigure
            {
                StartPoint = firstPoint,
                IsClosed = closeAndFill,
                IsFilled = closeAndFill
            };

        var segment =
            new PolyLineSegment();

        for (int i = 1; i < points.Count; i++)
        {
            segment.Points.Add(
                ToCanvasPoint(
                    points[i],
                    minX,
                    maxY,
                    scale,
                    offsetX,
                    offsetY));
        }

        figure.Segments.Add(segment);

        geometry.Figures.Add(figure);
    }

    private static void AddNormalRingFigure(
        PathGeometry geometry,
        IReadOnlyList<ProjectedPoint> ring,
        double minX,
        double maxY,
        double scale,
        double offsetX,
        double offsetY)
    {
        if (ring.Count < 3)
            return;

        Point firstPoint =
            ToCanvasPoint(
                ring[0],
                minX,
                maxY,
                scale,
                offsetX,
                offsetY);

        var figure =
            new PathFigure
            {
                StartPoint = firstPoint,
                IsClosed = true,
                IsFilled = true
            };

        var segment =
            new PolyLineSegment();

        for (int i = 1; i < ring.Count; i++)
        {
            segment.Points.Add(
                ToCanvasPoint(
                    ring[i],
                    minX,
                    maxY,
                    scale,
                    offsetX,
                    offsetY));
        }

        figure.Segments.Add(segment);

        geometry.Figures.Add(figure);
    }

    public bool TryProjectedToCanvas(
    ProjectedPoint projectedPoint,
    out Point canvasPoint)
    {
        canvasPoint = default;

        if (!_hasValidTransform)
            return false;

        if (_fitScale <= 0 ||
            !double.IsFinite(_fitScale))
        {
            return false;
        }

        if (!double.IsFinite(projectedPoint.X) ||
            !double.IsFinite(projectedPoint.Y))
        {
            return false;
        }

        double x =
            _offsetX +
            (projectedPoint.X - _minX) *
            _fitScale;

        double y =
            _offsetY +
            (_maxY - projectedPoint.Y) *
            _fitScale;

        if (!double.IsFinite(x) ||
            !double.IsFinite(y))
        {
            return false;
        }

        canvasPoint = new Point(x, y);

        return true;
    }

}