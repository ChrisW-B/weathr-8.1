using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Weathr81.DataTemplates;
using Windows.Devices.Geolocation;

namespace Weathr81.HelperClasses
{
    class GetGeoposition
    {
        private Location currentLocation;
        private GeoTemplate geoTemplate;

        public GetGeoposition(Location loc)
        {
            this.currentLocation = loc;
        }
        async public Task<GeoTemplate> getLocation()
        {
            if (geoTemplate != null)
            {
                return geoTemplate;
            }
            else
            {
                await setPosition();
                return geoTemplate;
            }
        }
        public void updateLocation(Location loc)
        {
            this.currentLocation = loc;
            geoTemplate = null;
        }

        async private Task setPosition()
        {
            if (currentLocation.IsCurrent)
            {
                if (geoTemplate == null || geoTemplate.fail)
                {
                    geoTemplate = new GeoTemplate();
                    try
                    {
                        Geolocator geo = new Geolocator();
                        Geoposition pos = await geo.GetGeopositionAsync(new TimeSpan(0, 30, 0), new TimeSpan(0, 0, 15));
                        geoTemplate.position = pos.Coordinate.Point;
                        geoTemplate.useCoord = true;
                        geoTemplate.fail = false;
                    }
                    catch (Exception e)
                    {
                        geoTemplate.errorMsg = e.Message;
                        geoTemplate.fail = true;
                        geoTemplate.useCoord = false;
                    }
                }
            }
            else
            {
                if (geoTemplate == null || geoTemplate.fail)
                {
                    geoTemplate = new GeoTemplate();
                    geoTemplate.position = new Geopoint(new BasicGeoposition() { Latitude = currentLocation.Lat, Longitude = currentLocation.Lon });
                    geoTemplate.fail = false;
                    geoTemplate.useCoord = (currentLocation.LocUrl == null);
                    if (currentLocation.IsCurrent)
                    {
                        geoTemplate.useCoord = true;
                    }
                }
            }
        }
    }
}
