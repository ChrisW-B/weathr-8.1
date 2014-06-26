using System;

namespace DataTemplates
{
    public class BackgroundTemplate
    {
        public BackgroundWeather weather { get; set; }
        public BackgroundLoc location { get; set; }
        public BackgroundFlickr flickrData { get; set; }
       
        public string wideName { get; set; }
        public string medName { get; set; }
        public string smallName { get; set; }  
    }

    public class BackgroundWeather
    {
        public string conditions { get; set; }
        public string currentTemp { get; set; }
        public string high { get; set; }
        public string low { get; set; }
        public string tempCompare { get; set; }
        public string todayForecast { get; set; }
    }

    public class BackgroundLoc
    {
        public string location { get; set; }
        public double lat { get; set; }
        public double lon { get; set; }
    }

    public class BackgroundFlickr
    {
        public Uri imageUri { get; set; }
        public string userId { get; set; }
        public string userName { get; set; }
    }
}
