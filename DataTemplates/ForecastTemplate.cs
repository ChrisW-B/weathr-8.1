using System.Collections.ObjectModel;

namespace DataTemplates
{
    public class ForecastTemplate
    {
        public ForecastList forecast { get; set; }
    }
    public class ForecastList
    {
        public ObservableCollection<ForecastItem> forecastList { get; set; }
    }
   public class ForecastItem
    {
       public string title { get; set; }
       public string text { get; set; }
       public string pop { get; set; }
    }
}
