﻿using System;
using Windows.Devices.Geolocation;

namespace Weathr81.DataTemplates
{
    public class MapsTemplate
    {
        public Geopoint center { get; set; }
        public String satTilesString { get; set; }
        public String radTilesString { get; set; }

    }
}