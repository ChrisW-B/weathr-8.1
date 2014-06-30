using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Geolocation;

namespace LocationHelper
{
  public class GetGeoposition
    {
        private Location currentLocation;
        private GeoTemplate geoTemplate;
        private bool allowAutofind;

        public GetGeoposition(Location loc, bool allowAutofind)
        {
            this.currentLocation = loc;
            this.allowAutofind = allowAutofind;
        }
        async public Task<GeoTemplate> getLocation(TimeSpan waitTime, TimeSpan history)
        {
            if (geoTemplate != null)
            {
                return geoTemplate;
            }
            else
            {
                await setPosition(waitTime, history);
                return geoTemplate;
            }
        }
        public void updateLocation(Location loc, bool allowAutofind)
        {
            this.currentLocation = loc;
            this.allowAutofind = allowAutofind;
            geoTemplate = null;
        }

        async private Task setPosition(TimeSpan waitTime, TimeSpan history)
        {
            if (currentLocation.IsCurrent && allowAutofind)
            {
                if (geoTemplate == null || geoTemplate.fail)
                {
                    geoTemplate = new GeoTemplate();
                    try
                    {
                        Geolocator geo = new Geolocator();
                        Geoposition pos = await geo.GetGeopositionAsync(history, waitTime);
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
            else if (currentLocation.IsCurrent && !allowAutofind)
            {
                geoTemplate = new GeoTemplate();
                geoTemplate.errorMsg = "Can't find your current location, autofind seems to be off!";
                geoTemplate.fail = true;
                geoTemplate.useCoord = false;
            }
            else
            {
                if (geoTemplate == null || geoTemplate.fail)
                {
                    geoTemplate = new GeoTemplate();
                    geoTemplate.position = new Geopoint(new BasicGeoposition() { Latitude = currentLocation.Lat, Longitude = currentLocation.Lon });
                    geoTemplate.fail = false;
                    geoTemplate.wUrl = currentLocation.LocUrl;
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
