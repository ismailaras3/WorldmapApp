using System.ComponentModel;
using System.Runtime.CompilerServices;
using WorldMap.Core.Models;
using WorldMap.Core.Projections;

namespace WorldMap.App.ViewModels;

public sealed class MainWindowViewModel :
    INotifyPropertyChanged
{
    // Bewaart waarden die in de interface getoond of gekozen worden.
    private IMapProjection? _selectedProjection;
    private MapDataset? _selectedDataset;

    private string _coordinateText =
        "Lon: --   Lat: --";

    private string _zoomText =
        "Zoom: 1.00x";

    private string _countryCountText =
        "Landen: 0";

    private string _renderTimeText =
        "Render: -- ms";

    private string _loadTimeText =
        "Load: -- ms";

    private string _datasetInfoText =
        "Dataset: --";

    private string _solarValueText =
    "Waarde: --";

    private string _colorScaleText =
    "Schaal: --";
    private string _colorScaleTitle =
    "Daglengte";

    private string _selectedContinent =
    "Europe";
    private double _daylightTimeMinutes =
    720.0;

    private string _daylightTimeText =
        "12:00 UTC";

    
    // Gekozen datum voor de zonneberekeningen.
    private DateTime _selectedDate =
        DateTime.Today;

    // Voorlopig beginnen we met daglengte.
    private string _selectedSolarParameter =
        "Daglengte";

    public IReadOnlyList<IMapProjection> Projections { get; }

    public IReadOnlyList<MapDataset> Datasets { get; }
    public string SolarValueText
    {
        get => _solarValueText;

        set => SetField(
            ref _solarValueText,
            value);
    }
    
    public string ColorScaleText
    {
        get => _colorScaleText;

        set => SetField(
            ref _colorScaleText,
            value);
    }
    public double DaylightTimeMinutes
    {
        get => _daylightTimeMinutes;

        set => SetField(
            ref _daylightTimeMinutes,
            value);
    }

    public string DaylightTimeText
    {
        get => _daylightTimeText;

        set => SetField(
            ref _daylightTimeText,
            value);
    }
    // Deze lijst komt later in een ComboBox.
    public IReadOnlyList<string> SolarParameters { get; } =
    [
        "Daglengte",
        "Solar noon",
        "Zonsopgang",
        "Zonsondergang"
    ];
    public IReadOnlyList<string> Continents { get; } =
[
    "Africa",
    "Asia",
    "Europe",
    "North America",
    "South America",
    "Oceania",
    "Antarctica"
];
    public string SelectedContinent
    {
        get => _selectedContinent;

        set => SetField(
            ref _selectedContinent,
            value);
    }
    public string ColorScaleTitle
    {
        get => _colorScaleTitle;

        set => SetField(
            ref _colorScaleTitle,
            value);
    }
    public IMapProjection? SelectedProjection
    {
        get => _selectedProjection;

        set
        {
            if (ReferenceEquals(
                    _selectedProjection,
                    value))
            {
                return;
            }

            _selectedProjection =
                value;

            OnPropertyChanged();
        }
    }

    public MapDataset? SelectedDataset
    {
        get => _selectedDataset;

        set
        {
            if (ReferenceEquals(
                    _selectedDataset,
                    value))
            {
                return;
            }

            _selectedDataset =
                value;

            OnPropertyChanged();
        }
    }

    public DateTime SelectedDate
    {
        get => _selectedDate;

        set => SetField(
            ref _selectedDate,
            value);
    }

    public string SelectedSolarParameter
    {
        get => _selectedSolarParameter;

        set => SetField(
            ref _selectedSolarParameter,
            value);
    }

    public string CoordinateText
    {
        get => _coordinateText;

        set => SetField(
            ref _coordinateText,
            value);
    }

    public string ZoomText
    {
        get => _zoomText;

        set => SetField(
            ref _zoomText,
            value);
    }

    public string CountryCountText
    {
        get => _countryCountText;

        set => SetField(
            ref _countryCountText,
            value);
    }

    public string RenderTimeText
    {
        get => _renderTimeText;

        set => SetField(
            ref _renderTimeText,
            value);
    }

    public string LoadTimeText
    {
        get => _loadTimeText;

        set => SetField(
            ref _loadTimeText,
            value);
    }

    public string DatasetInfoText
    {
        get => _datasetInfoText;

        set => SetField(
            ref _datasetInfoText,
            value);
    }

    public MainWindowViewModel(
        IEnumerable<IMapProjection> projections,
        IEnumerable<MapDataset> datasets)
    {
        Projections =
            projections.ToList();

        Datasets =
            datasets.ToList();

        SelectedProjection =
            Projections.FirstOrDefault();

        SelectedDataset =
            Datasets.FirstOrDefault();
    }

    public event PropertyChangedEventHandler?
        PropertyChanged;

    // Laat WPF weten dat een property veranderd is.
    private void OnPropertyChanged(
        [CallerMemberName]
        string? propertyName = null)
    {
        PropertyChanged?.Invoke(
            this,
            new PropertyChangedEventArgs(
                propertyName));
    }

    // Verandert een waarde alleen als ze echt anders is
    // en verwittigt daarna de interface.
    private bool SetField<T>(
        ref T field,
        T value,
        [CallerMemberName]
        string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(
                field,
                value))
        {
            return false;
        }

        field =
            value;

        OnPropertyChanged(
            propertyName);

        return true;
    }
}