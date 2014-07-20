using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml.Linq;
using WeatherData;

namespace WundergroundData
{
    public class GetWundergroundData
    {
        private Uri uri;
        private const string WUND_PRE = "http://api.wunderground.com/api/";
        private const string WUND_MID = "/conditions/forecast";
        private const string SEARCH = "/q/";
        private const string WUND_POST = ".xml";


        public GetWundergroundData(string apiKey, string urlKey)
        {
            uri = new Uri(WUND_PRE + apiKey + WUND_MID + urlKey + WUND_POST, UriKind.Absolute);
        }

        public GetWundergroundData(string apiKey, double lat, double lon)
        {
            uri = new Uri(WUND_PRE + apiKey + WUND_MID + SEARCH + lat + "," + lon + WUND_POST, UriKind.Absolute);
        }
        public GetWundergroundData(string apiKey, string lat, string lon)
        {
            uri = new Uri(WUND_PRE + apiKey + WUND_MID + SEARCH + lat + "," + lon + WUND_POST, UriKind.Absolute);
        }

        async public Task<WeatherInfo> getConditions()
        {
            try
            {
                HttpClient client = new HttpClient();
                Stream str = await client.GetStreamAsync(uri);
                return weatherToClass(XDocument.Load(str));
            }
            catch
            {
                return new WeatherInfo() { fail = true, error = "problem downloading info" };
            }
        }

        private WeatherInfo weatherToClass(XDocument doc)
        {
            WeatherInfo currentWeather = new WeatherInfo();
            XElement response = doc.Element("response");
            if (response != null)
            {
                #region current conditions

                //Current Conditions
                var currentObservation = response.Element("current_observation");

                //location name
                currentWeather.city = (string)currentObservation.Element("display_location").Element("city");
                currentWeather.state = (string)currentObservation.Element("display_location").Element("state_name");
                currentWeather.shortCityName = (string)currentObservation.Element("display_location").Element("city");

                //Current currentWeather
                currentWeather.currentConditions = (string)currentObservation.Element("weather");

                //Current wind
                currentWeather.windSpeedM = (string)currentObservation.Element("wind_mph") + " mph";
                currentWeather.windSpeedK = (string)currentObservation.Element("wind_kph") + " kph";
                currentWeather.windDir = (string)currentObservation.Element("wind_dir");
                //Current Temp and feels like
                currentWeather.tempC = (string)currentObservation.Element("temp_c");
                currentWeather.feelsLikeC = (string)currentObservation.Element("feelslike_c");
                currentWeather.tempF = (string)currentObservation.Element("temp_f");
                currentWeather.feelsLikeF = (string)currentObservation.Element("feelslike_f");

                //current humidity
                currentWeather.humidity = (string)currentObservation.Element("relative_humidity");

                //current UV
                currentWeather.UV = (string)currentObservation.Element("UV");

                #endregion current conditions

                #region forecast conditions

                //Forecast Conditions
                XElement forecastDays = response.Element("forecast").Element("simpleforecast").Element("forecastdays");

                //Today's conditions
                XElement today = forecastDays.Element("forecastday");

                //Today's High/Low
                currentWeather.todayLowC = (string)today.Element("low").Element("celsius");
                currentWeather.todayHighC = (string)today.Element("high").Element("celsius");
                currentWeather.todayLowF = (string)today.Element("low").Element("fahrenheit");
                currentWeather.todayHighF = (string)today.Element("high").Element("fahrenheit");

                //Tomorrow's conditions
                XElement tomorrow = forecastDays.Element("forecastday").ElementsAfterSelf("forecastday").First();

                //Tomorrow's High/Low
                currentWeather.tomorrowHighC = (string)tomorrow.Element("high").Element("celsius");
                currentWeather.tomorrowLowC = (string)tomorrow.Element("low").Element("celsius");
                currentWeather.tomorrowHighF = (string)tomorrow.Element("high").Element("fahrenheit");
                currentWeather.tomorrowLowF = (string)tomorrow.Element("low").Element("fahrenheit");

                //convert to ints
                currentWeather.todayHighIntC = Convert.ToInt32(currentWeather.todayHighC);
                currentWeather.tomorrowHighIntC = Convert.ToInt32(currentWeather.tomorrowHighC);
                currentWeather.todayHighIntF = Convert.ToInt32(currentWeather.todayHighF);
                currentWeather.tomorrowHighIntF = Convert.ToInt32(currentWeather.tomorrowHighF);

                if (currentWeather.todayHighIntC + 10 < currentWeather.tomorrowHighIntC)
                {
                    currentWeather.tempCompareC = "MUCH WARMER THAN";
                }
                else if (currentWeather.todayHighIntC + 3 < currentWeather.tomorrowHighIntC)
                {
                    currentWeather.tempCompareC = "WARMER THAN";
                }
                else if (currentWeather.todayHighIntC - 10 > currentWeather.tomorrowHighIntC)
                {
                    currentWeather.tempCompareC = "MUCH COOLER THAN";
                }
                else if (currentWeather.todayHighIntC - 3 > currentWeather.tomorrowHighIntC)
                {
                    currentWeather.tempCompareC = "COOLER THAN";
                }
                else
                {
                    currentWeather.tempCompareC = "ABOUT THE SAME AS";
                }

                if (currentWeather.todayHighIntF + 20 < currentWeather.tomorrowHighIntF)
                {
                    currentWeather.tempCompareF = "MUCH WARMER THAN";
                }
                else if (currentWeather.todayHighIntF + 5 < currentWeather.tomorrowHighIntF)
                {
                    currentWeather.tempCompareF = "WARMER THAN";
                }
                else if (currentWeather.todayHighIntF - 20 > currentWeather.tomorrowHighIntF)
                {
                    currentWeather.tempCompareF = "MUCH COOLER THAN";
                }
                else if (currentWeather.todayHighIntF - 5 > currentWeather.tomorrowHighIntF)
                {
                    currentWeather.tempCompareF = "COOLER THAN";
                }
                else
                {
                    currentWeather.tempCompareF = "ABOUT THE SAME AS";
                }

                var forecastDaysTxt = response.Element("forecast").Element("txt_forecast").Element("forecastdays");

                //clear out forecast list first

                currentWeather.forecastC = new ObservableCollection<WundForecastItem>();
                currentWeather.forecastF = new ObservableCollection<WundForecastItem>();

                currentWeather.forecastC.Clear();
                currentWeather.forecastF.Clear();

                foreach (XElement elm in forecastDaysTxt.Elements("forecastday"))
                {
                    WundForecastItem forecastC = new WeatherData.WundForecastItem();
                    WundForecastItem forecastF = new WeatherData.WundForecastItem();

                    forecastC.title = forecastF.title = (string)elm.Element("title");
                    forecastC.text = (string)elm.Element("fcttext_metric");
                    forecastF.text = (string)elm.Element("fcttext");
                    forecastC.pop = forecastF.pop = (string)elm.Element("pop");

                    currentWeather.forecastF.Add(forecastF);
                    currentWeather.forecastC.Add(forecastC);
                }

                #endregion forecast conditions

                #region tile stuff

                currentWeather.todayShort = (string)today.Element("conditions");
                currentWeather.tomorrowShort = (string)tomorrow.Element("conditions");

                currentWeather.todayLowC = (string)today.Element("low").Element("celsius");
                currentWeather.todayHighC = (string)today.Element("high").Element("celsius");
                currentWeather.tomorrowLowC = (string)tomorrow.Element("low").Element("celsius");
                currentWeather.tomorrowHighC = (string)tomorrow.Element("high").Element("celsius");

                currentWeather.todayLowF = (string)today.Element("low").Element("fahrenheit");
                currentWeather.todayHighF = (string)today.Element("high").Element("fahrenheit");
                currentWeather.tomorrowHighF = (string)tomorrow.Element("high").Element("fahrenheit");
                currentWeather.tomorrowLowF = (string)tomorrow.Element("low").Element("fahrenheit");

                #endregion tile stuff

                return currentWeather;
            }
            return new WeatherInfo() { fail = true, error = "invalid response from server" };
        }
    }
}
