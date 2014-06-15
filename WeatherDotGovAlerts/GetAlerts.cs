using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace WeatherDotGovAlerts
{
    public class GetAlerts
    {
        const string URL_PRE = "http://forecast.weather.gov/MapClick.php?";
        const string LAT = "lat=";
        const string LON = "&lon=";
        const string URL_POST = "&FcstType=dwml";
        private Uri uri;

        public GetAlerts(string lat, string lon)
        {
            uri = new Uri(URL_PRE + LAT + lat + LON + lon + URL_POST);
        }
        public GetAlerts(double lat, double lon)
        {
            uri = new Uri(URL_PRE + LAT + lat + LON + lon + URL_POST);
        }

        async public Task<AlertData> getAlerts()
        {
            try
            {
                HttpClient client = new HttpClient();
                Stream str = await client.GetStreamAsync(uri);
                return alertsToClass(XDocument.Load(str));
            }
            catch
            {
                return new AlertData() { fail = true, error = "problem downloading info" };
            }
        }

        private AlertData alertsToClass(XDocument doc)
        {
            ObservableCollection<Alert> alerts = new ObservableCollection<Alert>();

            IEnumerable<XElement> paramDesc = doc.Element("dwml").Element("data").Element("parameters").Elements("hazards");
            if (paramDesc == null || paramDesc.Count() < 1)
            {
                alerts.Add(new Alert() { headline = "All clear right now!", url = null });
            }
            else
            {
                foreach (XElement elm in paramDesc)
                {
                    XElement hazard = elm.Element("hazard-conditions").Element("hazard");
                    string hazardHeadline = (string)hazard.Attribute("headline").Value;
                    if (hazardHeadline == "")
                    {
                        hazardHeadline = "Unknown Alert";
                    }
                    string hazardUrl = (string)hazard.Element("hazardTextURL").Value;
                    alerts.Add(new Alert() { headline = hazardHeadline, url = hazardUrl });
                }
            }
            return new AlertData() { alerts = alerts };
        }
    }
}
