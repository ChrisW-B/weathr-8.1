using Weathr81.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Display;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using WundergroundData;
using WeatherData;
using FlickrInfo;
using Windows.UI.Xaml.Media.Imaging;
using System.Threading.Tasks;
using Windows.Devices.Geolocation;
using Windows.UI.Xaml.Controls.Maps;
using Weathr81.DataTemplates;

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

        //String wundApiKey = "102b8ec7fbd47a05";
        //testkey:
        string wundApiKey = "fb1dd3f4321d048d";
        String flickrApiKey = "2781c025a4064160fc77a52739b552ff";
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
            Geopoint pt = await getGeo();
            GetWundergroundData d = new GetWundergroundData(wundApiKey, pt.Position.Latitude, pt.Position.Longitude);
            WeatherInfo f = await d.getConditions();
            updateWeatherInfo(f);
            Uri bg = await GetHubBGUri(f.currentConditions, true, true, pt.Position.Latitude, pt.Position.Longitude, 0);
            if (bg != null)
            {
                setHubBG(bg);
            }
        }

        private void updateWeatherInfo(WeatherInfo f)
        {
            hub.Header = f.city + ", " + f.state;
            now.DataContext = new nowTemplate() { temp = f.tempC + "°", conditions = f.currentConditions.ToUpper(), feelsLike = "Feels like: " + f.feelsLikeC + "°", humidity = "Humidity: " + f.humidity, tempCompare ="TOMORROW WILL BE " + f.tempCompareC + " TODAY", wind = "Wind " + f.windSpeedM + " " + f.windDir };

        }


        //setting a background
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
            maps.DataContext = new mapsTemplates() { enabled = false, center = await getGeo()};

            //satTilesString = "http://mesonet.agron.iastate.edu/cache/tile.py/1.0.0/goes-ir-4km-900913/{0}/{1}/{2}.png"
            //radTilesString = "http://mesonet.agron.iastate.edu/cache/tile.py/1.0.0/nexrad-n0q-900913/{0}/{1}/{2}.png"
        }

        private void radarMap_Loaded(object sender, RoutedEventArgs e)
        {

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
        private void alertListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
        private void LocationListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
        private void locationName_Tap(object sender, TappedRoutedEventArgs e)
        {

        }

    }
}
