using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Windows.Devices.Geolocation;

namespace Weathr81
{
   public class GeoTemplate
    {
       public bool fail { get; set; }
       public string errorMsg { get; set; }
       public Geopoint position { get; set; }
    }
}
