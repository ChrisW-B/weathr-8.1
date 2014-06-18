using System.Collections.ObjectModel;

namespace ForecastIOData
{
    public class ForecastIOClass
    {
        public Currently current { get; set; }
        public Minutely mins { get; set; }
        public Hourly hours { get; set; }
        public Daily days { get; set; }
        public ObservableCollection<Alert> Alerts { get; set; }
        public Flags flags { get; set; }
        public bool fail { get; set; }
        public string errorMsg { get; set; }
    }

    public class Currently
    {
        public double time { get; set; }
        public string summary { get; set; }
        public string icon { get; set; }
        public double nearestStormDistance { get; set; }
        public double nearestStormBearing { get; set; }
        public double precipIntensity { get; set; }
        public double precipProbability { get; set; }
        public double temperature { get; set; }
        public double apparentTemperature { get; set; }
        public double dewPoint { get; set; }
        public double humidity { get; set; }
        public double windSpeed { get; set; }
        public double windBearing { get; set; }
        public double visibility { get; set; }
        public double cloudCover { get; set; }
        public double pressure { get; set; }
        public double ozone { get; set; }
    }
    public class Minutely
    {
        public string summary { get; set; }
        public string icon { get; set; }
        public ObservableCollection<MinuteForecast> minutes { get; set; }
    }
    public class Hourly
    {
        public string summary { get; set; }
        public string icon { get; set; }
        public ObservableCollection<HourForecast> hours { get; set; }
    }
    public class Daily
    {
        public string summary { get; set; }
        public string icon { get; set; }
        public ObservableCollection<DayForecast> days { get; set; }
    }

    public class MinuteForecast
    {
        public double time { get; set; }
        public double precipIntensity { get; set; }
        public double precipProbability { get; set; }
    }
    public class HourForecast
    {
        public double time { get; set; }
        public string summary { get; set; }
        public string icon { get; set; }
        public double precipIntensity { get; set; }
        public double precipProbability { get; set; }
        public double temperature { get; set; }
        public double apparentTemperature { get; set; }
        public double dewPoint { get; set; }
        public double humidity { get; set; }
        public double windSpeed { get; set; }
        public double windBearing { get; set; }
        public double visibility { get; set; }
        public double cloudCover { get; set; }
        public double pressure { get; set; }
        public double ozone { get; set; }
    }
    public class DayForecast
    {
        public double time { get; set; }
        public string summary { get; set; }
        public string icon { get; set; }
        public double sunriseTime { get; set; }
        public double sunsetTime { get; set; }
        public double precipIntensity { get; set; }
        public double precipIntensityMax { get; set; }
        public double precipProbability { get; set; }
        public double temperatureMin { get; set; }
        public double temperatureMinTime { get; set; }
        public double temperatureMax { get; set; }
        public double temperatureMaxTime { get; set; }
        public double apparentTemperatureMin { get; set; }
        public double apparentTemperatureMinTime { get; set; }
        public double apparentTemperatureMax { get; set; }
        public double apparentTemperatureMaxTime { get; set; }
        public double dewPoint { get; set; }
        public double humidity { get; set; }
        public double windSpeed { get; set; }
        public double windBearing { get; set; }
        public double visibility { get; set; }
        public double cloudCover { get; set; }
        public double pressure { get; set; }
        public double ozone { get; set; }
        public double precipIntensityMaxTime { get; set; }
        public string precipType { get; set; }
    }

    public class Alert
    {
        public string title { get; set; }
        public double time { get; set; }
        public double expires { get; set; }
        public string description { get; set; }
        public string uri { get; set; }
    }
    public class Flags
    {
        public ObservableCollection<string> sources { get; set; }
        public ObservableCollection<string> __invalid_name__isd_stations { get; set; }
        public ObservableCollection<string> __invalid_name__madis_stations { get; set; }
        public ObservableCollection<string> __invalid_name__lamp_stations { get; set; }
        public ObservableCollection<string> __invalid_name__darksky_stations { get; set; }
        public string units { get; set; }

        public bool minsExists { get; set; }
        public bool hoursExists { get; set; }
        public bool dayssExists { get; set; }

        public int numAlerts { get; set; }
    }
}
