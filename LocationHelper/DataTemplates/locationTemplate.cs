using System;
using System.Collections.ObjectModel;

namespace LocationHelper
{
    public class LocationTemplate
    {
        public LocationList locations { get; set; }
        public string PhotoDetails { get; set; }
        public Uri ArtistUri { get; set; }
    }
    public class LocationList
    {
        public ObservableCollection<Location> locationList { get; set; }
    }
    public class Location
    {
        public string LocName { get; set; }
        public string LocUrl { get; set; }
        public bool IsCurrent { get; set; }
        public bool IsDefault { get; set; }
        public double Lat { get; set; }
        public double Lon { get; set; }
        public string Image { get; set; }
    }
}
