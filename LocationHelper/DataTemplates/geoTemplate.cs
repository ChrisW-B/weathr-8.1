using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Windows.Devices.Geolocation;

namespace LocationHelper
{
    public class GeoTemplate
    {
        public bool fail { get; set; }
        public string errorMsg { get; set; }
        public Geopoint position { get; set; }
        public string wUrl { get; set; }
        public bool useCoord { get; set; }
    }
}

