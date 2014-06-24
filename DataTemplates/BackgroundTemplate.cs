using System;

namespace DataTemplates
{
    public class BackgroundTemplate
    {
        public string conditions { get; set; }
        public string currentTemp { get; set; }
        public string high { get; set; }
        public string low { get; set; }
        public string tempCompare { get; set; }
        public double lat { get; set; }
        public double lon { get; set; }
        public Uri imageUri { get; set; }
        public string wideName { get; set; }
        public string medName { get; set; }
        public string smallName { get; set; }
        public string location { get; set; }
    }
}
