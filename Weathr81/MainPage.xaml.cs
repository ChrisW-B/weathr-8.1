using FlickrInfo;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using WeatherData;
using WeatherDotGovAlerts;
using Weathr81.Common;
using Weathr81.DataTemplates;
using Windows.Devices.Geolocation;
using Windows.Storage;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using WundergroundData;

// The Basic Page item template is documented at http://go.microsoft.com/fwlink/?LinkID=390556

namespace Weathr81
{
    public sealed partial class MainPage : Page
    {
        #region navHelperStuff
        private NavigationHelper navigationHelper;
        private ObservableDictionary defaultViewModel = new ObservableDictionary();
        public NavigationHelper NavigationHelper
        {
            get { return this.navigationHelper; }
        }
        public ObservableDictionary DefaultViewModel
        {
            get { return this.defaultViewModel; }
        }
        private void NavigationHelper_LoadState(object sender, LoadStateEventArgs e)
        {
        }
        private void NavigationHelper_SaveState(object sender, SaveStateEventArgs e)
        {
        }
        #endregion

        #region variables
        //String wundApiKey = "102b8ec7fbd47a05";
        //testkey:
        const string wundApiKey = "fb1dd3f4321d048d";
        const string flickrApiKey = "2781c025a4064160fc77a52739b552ff";
        ApplicationDataContainer store = Windows.Storage.ApplicationData.Current.RoamingSettings;
        ApplicationDataContainer localStore = Windows.Storage.ApplicationData.Current.LocalSettings;
        #endregion

        public MainPage()
        {
            this.InitializeComponent();

            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += this.NavigationHelper_LoadState;
            this.navigationHelper.SaveState += this.NavigationHelper_SaveState;
        }
        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            this.navigationHelper.OnNavigatedFrom(e);
        }
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            this.navigationHelper.OnNavigatedTo(e);
            hideStatusBar();
            runApp();
        }

        //starting the app up
        private void hideStatusBar()
        {
            //hides the status bar for app
            StatusBar s = Windows.UI.ViewManagement.StatusBar.GetForCurrentView();
            s.HideAsync();
        }
        async private void runApp()
        {
            //central point of app, runs other methods
            Geopoint point = await getGeo();
            updateUI(point);
        }
        async private void updateUI(Geopoint point)
        {
            //updates the ui/weather conditions of app
            setFavoriteLocations();
            WeatherInfo downloadedForecast = await setWeather(point.Position.Latitude, point.Position.Longitude);
            updateWeatherInfo(downloadedForecast);
            setBG(downloadedForecast.currentConditions, point.Position.Latitude, point.Position.Longitude);
            setAlerts(point.Position.Latitude, point.Position.Longitude);
        }

        private void setFavoriteLocations()
        {
            LocationTemplate locTemplate = new LocationTemplate() { locations = new LocationList() };
            //store.Values.Remove("locList");
            if (store.Values.ContainsKey("locList"))
            {
                locTemplate.locations = getLocFromRoaming("locList");
            }
            else
            {
                locTemplate.locations = new LocationList();
                locTemplate.locations.locationList = new ObservableCollection<Location>();
                locTemplate.locations.locationList.Add(new Location() { IsCurrent = true });
                saveLocToRoaming(locTemplate.locations, "locList");

            }
            locList.DataContext = locTemplate;
        }

        //serialize and deserialize so locations can be synced
        private void saveLocToRoaming(LocationList locations, string value)
        {
            String serialized = serialize(locations);
            if (serialized.Length > 0)
            {
                store.Values[value] = serialized;
            }
        }
        private LocationList getLocFromRoaming(string value)
        {
            string locAsXml = (string)store.Values[value];
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(LocationList));
                LocationList locs = new LocationList();
                using (var reader = new StringReader(locAsXml))
                {
                    locs = (LocationList)serializer.Deserialize(reader);
                }
                return locs;
            }

            catch (Exception exc)
            {
                System.Diagnostics.Debug.WriteLine(exc);
                LocationList emptyList = new LocationList();
                return emptyList;
            }

        }
        private string serialize(LocationList locations)
        {
            try
            {
                XmlSerializer xmlIzer = new XmlSerializer(typeof(LocationList));
                var writer = new StringWriter();
                xmlIzer.Serialize(writer, locations);
                System.Diagnostics.Debug.WriteLine(writer.ToString());
                return writer.ToString();
            }

            catch (Exception exc)
            {
                System.Diagnostics.Debug.WriteLine(exc);
                return String.Empty;
            }
        }

        //set up weather
        async private Task<WeatherInfo> setWeather(double lat, double lon)
        {
            GetWundergroundData weatherData = new GetWundergroundData(wundApiKey, lat, lon);
            WeatherInfo downloadedForecast = await weatherData.getConditions();
            return downloadedForecast;
        }
        private void updateWeatherInfo(WeatherInfo downloadedForecast)
        {
            hub.Header = downloadedForecast.city + ", " + downloadedForecast.state;
            now.DataContext = new nowTemplate() { temp = downloadedForecast.tempC + "°", conditions = downloadedForecast.currentConditions.ToUpper(), feelsLike = "Feels like: " + downloadedForecast.feelsLikeC + "°", humidity = "Humidity: " + downloadedForecast.humidity, tempCompare = "TOMORROW WILL BE " + downloadedForecast.tempCompareC + " TODAY", wind = "Wind " + downloadedForecast.windSpeedM + " " + downloadedForecast.windDir };
            forecast.DataContext = createForecastList(downloadedForecast.forecastC);
        }
        private object createForecastList(ObservableCollection<ForecastC> forecast)
        {
            ObservableCollection<ForecastItem> forecastData = new ObservableCollection<ForecastItem>();
            foreach (ForecastC day in forecast)
            {
                forecastData.Add(new ForecastItem() { title = day.title, text = day.text, pop = day.pop });
            }
            return new ForecastTemplate() { forecast = new ForecastList() { forecastList = forecastData } };
        }

        //set up alerts
        async private void setAlerts(double lat, double lon)
        {
            GetAlerts a = new GetAlerts(lat, lon);
            AlertData d = await a.getAlerts();
            if (!d.fail)
            {
                alerts.DataContext = createAlertsList(d.alerts);
            }
        }
        private object createAlertsList(ObservableCollection<Alert> alerts)
        {
            ObservableCollection<AlertItem> alertsData = new ObservableCollection<AlertItem>();
            foreach (Alert item in alerts)
            {
                alertsData.Add(new AlertItem() { Headline = item.headline, TextUrl = item.url });
            }
            return new AlertsTemplate() { alerts = new AlertList() { alertList = alertsData } };
        }

        //setting a background
        async private void setBG(string conditions, double lat, double lon)
        {
            Uri bg = await GetHubBGUri(conditions, true, true, lat, lon, 0);
            if (bg != null)
            {
                setHubBG(bg);
            }
        }
        async private Task<Uri> GetHubBGUri(string cond, bool useGroup, bool useLoc, double lat, double lon, int timesRun)
        {
            //gets a uri for a background image from flickr
            if (timesRun > 1)
            {
                return null;
            }
            GetFlickrInfo f = new GetFlickrInfo(flickrApiKey);
            FlickrData imgList = await f.getImages(getTags(cond), useGroup, useLoc, lat, lon);
            if (!imgList.fail && imgList.images.Count > 0)
            {
                Random r = new Random();
                int num = r.Next(imgList.images.Count);
                return f.getImageUri(imgList.images[num]);

            }
            else
            {
                return await GetHubBGUri(cond, useGroup, false, lat, lon, timesRun++);
            }
        }
        private void setHubBG(Uri bg)
        {
            //sets the background of the hub to a given image uri
            BitmapImage img = new BitmapImage(bg);
            Brush imgBrush = new ImageBrush() { ImageSource = img, Opacity = .7 };
            hub.Background = imgBrush;
        }
        private string getTags(string cond)
        {
            //converts weather conditions into tags for flickr
            if (cond == null)
            {
                return "sky";
            }
            else
            {
                string weatherUpper = cond.ToUpper();

                if (weatherUpper.Contains("THUNDER"))
                {
                    return "thunder, thunderstorm, lightning, storm";
                }
                else if (weatherUpper.Contains("RAIN"))
                {
                    return "rain, drizzle, rainy";
                }
                else if (weatherUpper.Contains("SNOW") || weatherUpper.Contains("FLURRY"))
                {
                    return "snow, flurry, snowing";
                }
                else if (weatherUpper.Contains("FOG") || weatherUpper.Contains("MIST"))
                {
                    return "fog, foggy, mist";
                }
                else if (weatherUpper.Contains("CLEAR"))
                {
                    return "clear, sun, sunny, blue sky";
                }
                else if (weatherUpper.Contains("OVERCAST"))
                {
                    return "overcast, cloudy";
                }
                else if (weatherUpper.Contains("CLOUDS") || weatherUpper.Contains("CLOUDY"))
                {
                    return "cloudy, clouds, fluffy cloud";
                }
                else
                {
                    return weatherUpper;
                }
            }
        }

        //helpers
        async private Task<Geopoint> getGeo()
        {
            //returns the phone's current position
            Geolocator geo = new Geolocator();
            return (await geo.GetGeopositionAsync(new TimeSpan(2, 0, 0), new TimeSpan(0, 0, 0, 2))).Coordinate.Point;
        }

        //maps
        async private void satMap_Loaded(object sender, RoutedEventArgs e)
        {
            maps.DataContext = new mapsTemplates() { center = await getGeo() };

            //satTilesString = "http://mesonet.agron.iastate.edu/cache/tile.py/1.0.0/goes-ir-4km-900913/{0}/{1}/{2}.png"
            //radTilesString = "http://mesonet.agron.iastate.edu/cache/tile.py/1.0.0/nexrad-n0q-900913/{0}/{1}/{2}.png"
        }
        async private void radarMap_Loaded(object sender, RoutedEventArgs e)
        {
            maps.DataContext = new mapsTemplates() { center = await getGeo() };
        }

        //buttons and stuff
        private void satMap_Tap(object sender, TappedRoutedEventArgs e)
        {
            throw new NotImplementedException();
        }
        private void radarMap_Tap(object sender, TappedRoutedEventArgs e)
        {
            throw new NotImplementedException();
        }
        private void LocationListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
        private void locationName_Tapped(object sender, TappedRoutedEventArgs e)
        {

        }
        private void Alert_Tapped(object sender, TappedRoutedEventArgs e)
        {

        }
    }
}
