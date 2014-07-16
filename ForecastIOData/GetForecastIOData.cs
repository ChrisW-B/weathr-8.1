using Newtonsoft.Json;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace ForecastIOData
{
   public class GetForecastIOData
    {
        //uri is formatted as such: "https://api.forecast.io/forecast/b9ee7cf875f46fc8bc2b0743efc8c800/37.8267,-122.423?units=si" 
        private Uri uri;
        private const string URL_BASE = "https://api.forecast.io/forecast/";
        private const string API_KEY = "b9ee7cf875f46fc8bc2b0743efc8c800";
        private const string SI = "?units=si";
        private const string US = "?units=us";

        public GetForecastIOData(string loc)
        {
            uri = new Uri(URL_BASE + API_KEY + loc + SI);
        }
        public GetForecastIOData(string loc, bool isSI)
        {
            string url = URL_BASE + API_KEY + loc;
            url += isSI ? SI : US;
            uri = new Uri(url);
        }
        public GetForecastIOData(string lat, string lon)
        {
            uri = new Uri(URL_BASE + API_KEY + "/" + lat + "," + lon + SI);

        }
        public GetForecastIOData(double lat, double lon)
        {
            uri = new Uri(URL_BASE + API_KEY + "/" + Convert.ToDouble(lat, new CultureInfo("en-US")) + "," + Convert.ToDouble(lon, new CultureInfo("en-US")) + SI);
        }
        public GetForecastIOData(string lat, string lon, bool isSI)
        {
            string url = URL_BASE + API_KEY + "/" + lat + "," + lon;
            url += isSI ? SI : US;
            uri = new Uri(url);
        }
        public GetForecastIOData(double lat, double lon, bool isSI)
        {
            string url = URL_BASE + API_KEY + "/" + Convert.ToDouble(lat, new CultureInfo("en-US")) + "," + Convert.ToDouble(lon, new CultureInfo("en-US"));
            url += isSI ? SI : US;
            uri = new Uri(url);
        }

        async public Task<ForecastIOClass> getForecast()
        {
            try
            {
                HttpClient client = new HttpClient();
                return forecastToClass(JsonConvert.DeserializeXNode(new StreamReader(await client.GetStreamAsync(uri)).ReadToEnd(), "root"));
            }
            catch
            {
                return new ForecastIOClass() { fail = true, errorMsg = "problem downloading info" };
            }
        }

        private ForecastIOClass forecastToClass(XDocument doc)
        {
            ForecastIOClass forecastIOClass = new ForecastIOClass();
            forecastIOClass.flags = new Flags() { dayssExists = false, hoursExists = false, minsExists = false };

            //Get Current Conditions
            forecastIOClass.current = doCurrent(doc);

            //get minute by minute forecasts
            Minutely mins = doMinutely(doc);
            if (mins != null)
            {
                forecastIOClass.flags.minsExists = true;
                forecastIOClass.mins = mins;
            }

            //get hourly forecasts
            Hourly hours = doHourly(doc);
            if (hours != null)
            {
                forecastIOClass.flags.hoursExists = true;
                forecastIOClass.hours = hours;
            }

            //get daily forecasts
            Daily days = doDaily(doc);
            if (days != null)
            {
                forecastIOClass.flags.dayssExists = true;
                forecastIOClass.days = days;
            }

            //Create List of Alerts
            forecastIOClass.Alerts = doAlerts(doc);
            forecastIOClass.flags.numAlerts = forecastIOClass.Alerts.Count;

            return forecastIOClass;
        }

        private Currently doCurrent(XDocument doc)
        {
            Currently currently = new Currently();
            XElement currentConditions = doc.Element("root").Element("currently");

            XElement summary = currentConditions.Element("summary");
            currently.summary = doString(summary);

            XElement icon = currentConditions.Element("icon");
            currently.icon = doString(icon);

            XElement nearestStormDistance = currentConditions.Element("nearestStormDistance");
            currently.nearestStormDistance = doDouble(nearestStormDistance);

            XElement nearestStormBearing = currentConditions.Element("nearestStormBearing");
            currently.nearestStormBearing = doDouble(nearestStormBearing);

            XElement precipIntensity = currentConditions.Element("precipIntensity");
            currently.precipIntensity = doDouble(precipIntensity);

            XElement precipProb = currentConditions.Element("precipProbability");
            currently.precipProbability = doDouble(precipProb);

            XElement temp = currentConditions.Element("temperature");
            currently.temperature = doDouble(temp);

            XElement appTemp = currentConditions.Element("apparentTemperature");
            currently.apparentTemperature = doDouble(appTemp);

            XElement dewPoint = currentConditions.Element("dewPoint");
            currently.dewPoint = doDouble(dewPoint);

            XElement humidity = currentConditions.Element("humidity");
            currently.humidity = doDouble(humidity);

            XElement windSpeed = currentConditions.Element("windSpeed");
            currently.windSpeed = doDouble(windSpeed);

            XElement windBearing = currentConditions.Element("windBearing");
            currently.windBearing = doDouble(windBearing);

            XElement cloudCover = currentConditions.Element("cloudCover");
            currently.cloudCover = doDouble(cloudCover);

            XElement pressure = currentConditions.Element("pressure");
            currently.pressure = doDouble(pressure);

            XElement ozone = currentConditions.Element("ozone");
            currently.ozone = doDouble(ozone);

            return currently;
        }
        private Minutely doMinutely(XDocument doc)
        {
            XElement minutes = doc.Element("root").Element("minutely");
            if (minutes != null)
            {
                Minutely mins = new Minutely();
                XElement summary = minutes.Element("summary");
                mins.summary = doString(summary);

                mins.minutes = new ObservableCollection<MinuteForecast>();

                foreach (XElement elm in minutes.Elements("data"))
                {
                    mins.minutes.Add(doEachMin(elm));
                }
                return mins;
            }
            return null;
        }
        private Hourly doHourly(XDocument doc)
        {
            XElement hours = doc.Element("root").Element("hourly");
            if (hours != null)
            {
                Hourly hourly = new Hourly();
                XElement summary = hours.Element("summary");
                hourly.summary = doString(summary);

                XElement icon = hours.Element("icon");
                hourly.icon = doString(icon);

                hourly.hours = new ObservableCollection<HourForecast>();
                foreach (XElement elm in hours.Elements("data"))
                {
                    hourly.hours.Add(doEachHour(elm));
                }
                return hourly;
            }
            return null;
        }
        private Daily doDaily(XDocument doc)
        {
            XElement days = doc.Element("root").Element("daily");
            if (days != null)
            {
                Daily daily = new Daily();
                XElement summary = days.Element("summary");
                daily.summary = doString(summary);

                XElement icon = days.Element("icon");
                daily.icon = doString(icon);

                daily.days = new ObservableCollection<DayForecast>();

                foreach (XElement elm in days.Elements("data"))
                {

                    daily.days.Add(doEachDay(elm));
                }
                return daily;
            }
            return null;
        }
        private ObservableCollection<ForecastIOAlert> doAlerts(XDocument doc)
        {
            ObservableCollection<ForecastIOAlert> alerts = new ObservableCollection<ForecastIOAlert>();
            foreach (XElement elm in doc.Element("root").Elements("alerts"))
            {
                alerts.Add(doEachAlert(elm));
            }
            if (alerts.Count == 0)
            {
                alerts.Add(new ForecastIOAlert() { title = "Nothing right now!", uri = null });
            }
            return alerts;
        }
        private ForecastIOAlert doEachAlert(XElement elm)
        {
            ForecastIOAlert alert = new ForecastIOAlert();

            XElement title = elm.Element("title");
            alert.title = doString(title);

            XElement description = elm.Element("description");
            alert.description = doString(description);

            XElement time = elm.Element("time");
            alert.time = doDouble(time);

            XElement expires = elm.Element("expires");
            alert.expires = doDouble(expires);

            XElement uri = elm.Element("uri");
            alert.uri = doString(uri);

            return alert;
        }
        private MinuteForecast doEachMin(XElement elm)
        {
            MinuteForecast min = new MinuteForecast();

            XElement precipIntensity = elm.Element("precipIntensity");
            min.precipIntensity = doDouble(precipIntensity);

            XElement precipProbability = elm.Element("precipProbability");
            min.precipProbability = doDouble(precipProbability);

            XElement time = elm.Element("time");
            min.time = doDouble(time);

            return min;
        }
        private HourForecast doEachHour(XElement elm)
        {
            HourForecast hour = new HourForecast();

            XElement time = elm.Element("time");
            hour.time = Convert.ToDouble(time.Value);

            XElement summary = elm.Element("summary");
            hour.summary = doString(summary);

            XElement precipIntensity = elm.Element("precipIntensity");
            hour.precipIntensity = doDouble(precipIntensity);

            XElement precipProbability = elm.Element("precipProbability");
            hour.precipProbability = doDouble(precipProbability);

            XElement temperature = elm.Element("temperature");
            hour.temperature = doDouble(temperature);

            XElement apparentTemperature = elm.Element("apparentTemperature");
            hour.apparentTemperature = doDouble(apparentTemperature);

            XElement dewPoint = elm.Element("dewPoint");
            hour.dewPoint = doDouble(dewPoint);

            XElement humidity = elm.Element("humidity");
            hour.humidity = doDouble(humidity);

            XElement windSpeed = elm.Element("windSpeed");
            hour.windSpeed = doDouble(windSpeed);

            XElement windBearing = elm.Element("windBearing");
            hour.windBearing = doDouble(windBearing);

            XElement visibility = elm.Element("visibility");
            hour.visibility = doDouble(visibility);

            XElement cloudCover = elm.Element("cloudCover");
            hour.cloudCover = doDouble(cloudCover);

            XElement pressure = elm.Element("pressure");
            hour.pressure = doDouble(pressure);

            XElement ozone = elm.Element("ozone");
            hour.ozone = doDouble(ozone);

            return hour;
        }
        private DayForecast doEachDay(XElement elm)
        {
            DayForecast day = new DayForecast();

            XElement icon = elm.Element("icon");
            day.icon = doString(icon);

            XElement time = elm.Element("time");
            day.time = doDouble(time);

            XElement summary = elm.Element("summary");
            day.summary = doString(summary);

            XElement sunriseTime = elm.Element("sunriseTime");
            day.sunriseTime = doDouble(sunriseTime);

            XElement sunsetTime = elm.Element("sunsetTime");
            day.sunsetTime = doDouble(sunsetTime);

            XElement precipIntens = elm.Element("precipIntensity");
            day.precipIntensity = doDouble(precipIntens);

            XElement precipIntensMax = elm.Element("precipIntensityMax");
            day.precipIntensityMax = doDouble(precipIntensMax);

            XElement PrecipIntensMaxTime = elm.Element("precipIntensityMaxTime");
            day.precipIntensityMaxTime = doDouble(PrecipIntensMaxTime);

            XElement precipType = elm.Element("precipType");
            day.precipType = doString(precipType);

            XElement precipProb = elm.Element("precipProbability");
            day.precipProbability = doDouble(precipProb);

            XElement tempMax = elm.Element("temperatureMax");
            day.temperatureMax = doDouble(tempMax);

            XElement tempMaxTime = elm.Element("temperatureMaxTime");
            day.temperatureMaxTime = doDouble(tempMaxTime);

            XElement tempMin = elm.Element("temperatureMin");
            day.temperatureMin = doDouble(tempMin);

            XElement tempMinTime = elm.Element("temperatureMinTime");
            day.temperatureMinTime = doDouble(tempMinTime);

            XElement appTempMin = elm.Element("apparentTemperatureMin");
            day.apparentTemperatureMin = doDouble(appTempMin);

            XElement appTempMinTime = elm.Element("apparentTemperatureMinTime");
            day.apparentTemperatureMinTime = doDouble(appTempMinTime);

            XElement appTempMax = elm.Element("apparentTemperatureMax");
            day.apparentTemperatureMax = doDouble(appTempMax);

            XElement appTempMaxTime = elm.Element("apparentTemperatureMaxTime");
            day.apparentTemperatureMaxTime = doDouble(appTempMaxTime);

            XElement dewPoint = elm.Element("dewPoint");
            day.dewPoint = doDouble(dewPoint);

            XElement humidity = elm.Element("humidity");
            day.humidity = doDouble(humidity);

            XElement windSpeed = elm.Element("windSpeed");
            day.windSpeed = doDouble(windSpeed);

            XElement windBearing = elm.Element("windBearing");
            day.windBearing = doDouble(windBearing);

            XElement cloudCover = elm.Element("cloudCover");
            day.cloudCover = doDouble(cloudCover);

            XElement pressure = elm.Element("pressure");
            day.pressure = doDouble(pressure);

            XElement ozone = elm.Element("ozone");
            day.ozone = doDouble(ozone);

            return day;
        }

        private double doDouble(XElement elm)
        {
            if (elm != null)
            {
                return Convert.ToDouble(elm.Value);
            }
            return 0;
        }
        private string doString(XElement elm)
        {
            if (elm != null)
            {
                return elm.Value;
            }
            return "null";
        }
    }
}
