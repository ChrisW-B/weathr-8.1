using System.Collections.ObjectModel;

namespace DataTemplates
{

    public class ForecastIOTemplate
    {
        public ForecastIOList forecastIO { get; set; }
    }
    public class ForecastIOList
    {
        public ObservableCollection<ForecastIOItem> hoursList { get; set; }
    }
    public class ForecastIOItem
    {
        public string time { get; set; }
        public string temp { get; set; }
        public string chanceOfPrecip { get; set; }
        public string description { get; set; }
    }
}
