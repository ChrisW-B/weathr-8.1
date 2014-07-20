
using System;
namespace StoreLabels
{
    public class Values
    {
        public const string ALERT_SAVE = "alertSave";
        public const string ALLOW_BG_TASK = "allowBackground";
        public const string ALLOW_LOC = "allowAutoLocation";
        public const string ALLOW_MAIN_BG = "allowMainBackground";
        public const string FIRST_START = "firstStartDateTime";
        public const string FORECAST_SAVE = "forecastSave";
        public const string FLICKR_API = "2781c025a4064160fc77a52739b552ff";
        public const string GOOGLE_POST = "&sensor=true";
        public const string GOOGLE_URL = "http://maps.googleapis.com/maps/api/geocode/xml?address=";
        public const string HOURLY_SAVE = "hourSave";
        public const string IAP_FORE_IO = "forecastIOAccess";
        public const string IAP_NO_LIMIT = "unlimitedTime";
        public const string IS_NEW_DEVICE = "newDevice";
        public const string LAST_CMD_BAR = "lastCommandBarLoc";
        public const string LAST_LOC = "lastLoc";
        public const string LAST_LOC_NAME = "lastLocFullName";
        public const string LAST_SAVE = "lastSaveTime";
        public const string LOC_STORE = "locList";
        public const string MAIN_BG_WIFI_ONLY = "onlyAllowBGonWiFi";
        public const string NOW_SAVE = "oldNow";
        public const string RAD_URL = "http://mesonet.agron.iastate.edu/cache/tile.py/1.0.0/nexrad-n0q-900913/{zoomlevel}/{x}/{y}.png?";
        public const string SAT_URL = "http://mesonet.agron.iastate.edu/cache/tile.py/1.0.0/goes-ir-4km-900913/{zoomlevel}/{x}/{y}.png?";
        public const string SAVE_LOC = "ms-appdata:///local/";
        public const string TASK_NAME = "Weathr Tile Updater";
        public const string TILE_UNITS_ARE_SI = "tileUnitsAreSI";
        public const string TRANSPARENT_TILE = "tileIsTransparent";
        public const string TWENTY_FOUR_HR_TIME = "use24hrTime";
        public const string UPDATE_FREQ = "updateFreq";
        public const string UPDATE_ON_CELL = "allowUpdateOnNetwork";
        public const string UNITS_ARE_SI = "unitsAreSI";
        public const string UNITS_CHANGED = "unitsChanged";

        public const string LAST_HUB_SECTION = "lastHubLoc";
       
       // public const string getWundApi() = "fb1dd3f4321d048d";

        public static string getWundApi()
        {
            Random rand = new Random();
            double val = rand.Next(100);
            switch ((int)val % 6)
            {
                case 0:
                    return "2d73e75dbfe7f75c";
                default:
                    return "fb1dd3f4321d048d";
            }
        } 
    }
}
