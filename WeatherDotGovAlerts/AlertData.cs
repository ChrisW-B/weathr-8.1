using System.Collections.ObjectModel;

namespace WeatherDotGovAlerts
{
    public class AlertData
    {
        public bool fail { get; set; }
        public string error { get; set; }
        public ObservableCollection<Alert> alerts { get; set; }
    }

    public class Alert
    {
        public string url { get; set; }
        public string headline { get; set; }
    }
}
