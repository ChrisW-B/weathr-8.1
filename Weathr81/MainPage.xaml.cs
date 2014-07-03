using BackgroundTask;
using DataTemplates;
using FlickrInfo;
using ForecastIOData;
using LocationHelper;
using SerializerClass;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using WeatherData;
using WeatherDotGovAlerts;
using Weathr81.Common;
using Weathr81.HelperClasses;
using Weathr81.OtherPages;
using Windows.Devices.Geolocation;
using Windows.Foundation;
using Windows.Storage;
using Windows.System;
using Windows.UI;
using Windows.UI.Popups;
using Windows.UI.StartScreen;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Maps;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Shapes;
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
        private const string WUND_API = "fb1dd3f4321d048d";
        private const string FLICKR_API = "2781c025a4064160fc77a52739b552ff";
        private const string LOC_STORE = "locList";
        private const string NOW_SAVE = "oldNow";
        private const string HOURLY_SAVE = "hourSave";
        private const string FORECAST_SAVE = "forecastSave";
        private const string ALERT_SAVE = "alertSave";
        private const string LAST_LOC = "lastLoc";
        private const string LAST_SAVE = "lastSaveTime";
        private const string UNITS_CHANGED = "unitsChanged";
        private const string UNITS_ARE_SI = "unitsAreSI";
        private const string BG_REG = "bgRegistered";
        private const string ALLOW_BG = "allowBackground";
        private const string UPDATE_FREQ = "updateFreq";
        private const string TASK_NAME = "Weathr Tile Updater";
        private const string ALLOW_LOC = "allowAutoLocation";

        private ApplicationDataContainer store = Windows.Storage.ApplicationData.Current.RoamingSettings;
        private ApplicationDataContainer localStore = Windows.Storage.ApplicationData.Current.LocalSettings;
        private GetGeoposition GetGeoposition;
        private Location currentLocation;
        private StatusBar statusBar;
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
            saveData();
        }
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            this.navigationHelper.OnNavigatedTo(e);
            if (e.Parameter != null)
            {
                this.currentLocation = (e.Parameter as Location);
            }
            statusBar = StatusBar.GetForCurrentView();
            statusBar.ForegroundColor = Colors.White;
            runApp();
        }

        async private void runApp()
        {
            //central point of app, runs other methods
            statusBar.BackgroundColor = Colors.Black;
            statusBar.BackgroundOpacity = .25;
            statusBar.ProgressIndicator.ShowAsync();
            statusBar.ProgressIndicator.Text = "Getting your location...";
            if (await setFavoriteLocations())
            {
                GetGeoposition = new GetGeoposition(currentLocation, allowedToAutoFind());
                if (!restoreData())
                {
                    if (!(await GetGeoposition.getLocation(new TimeSpan(0, 0, 10), new TimeSpan(1, 0, 0))).fail) //gets geoLocation too
                    {
                        updateUI();
                    }
                    else
                    {
                        displayError("I'm having a problem getting your location. Make sure location services are enabled, or try again in a little bit");
                    }
                }
                else
                {
                    GeoTemplate geo = await GetGeoposition.getLocation(new TimeSpan(0, 0, 10), new TimeSpan(1, 0, 0));
                    setMaps(geo.position);
                    NowTemplate nowTemplate = (now.DataContext as NowTemplate);
                    if (nowTemplate != null && geo.position!=null)
                    {
                        setBG(nowTemplate.conditions, geo.position.Position.Latitude, geo.position.Position.Longitude);
                    }
                }
            }
            else
            {
                Frame.Navigate(typeof(AddLocation));
            }
        }

        private bool allowedToAutoFind()
        {
            if (localStore.Values.ContainsKey(ALLOW_LOC))
            {
                return (bool)localStore.Values[ALLOW_LOC];
            }
            return false;
        }
        private void displayError(string errorMsg)
        {
            now.DataContext = new NowTemplate() { errorText = errorMsg };
            forecast.DataContext = null;
            maps.DataContext = null;
            alerts.DataContext = null;
        }
        async private void updateUI()
        {
            //updates the ui/weather conditions of app
            WeatherInfo downloadedForecast;
            GeoTemplate geo = await GetGeoposition.getLocation(new TimeSpan(0, 0, 10), new TimeSpan(1, 0, 0));
            if (!geo.fail)
            {
                if (geo.useCoord)
                {
                    downloadedForecast = await setWeather(geo.position.Position.Latitude, geo.position.Position.Longitude);
                }
                else
                {
                    downloadedForecast = await setWeather(geo.wUrl);
                }
                if (!downloadedForecast.fail)
                {
                    bool isSI = unitsAreSI();
                    Serializer.save(DateTime.Now, typeof(DateTime), LAST_SAVE, localStore);
                    updateWeatherInfo(downloadedForecast, isSI);
                    updateForecastIO(geo.position.Position.Latitude, geo.position.Position.Longitude, isSI);
                    setAlerts(geo.position.Position.Latitude, geo.position.Position.Longitude);
                    setMaps(geo.position);
                    setBG(downloadedForecast.currentConditions, geo.position.Position.Latitude, geo.position.Position.Longitude);
                }
                else
                {
                    setBG("sky", geo.position.Position.Latitude, geo.position.Position.Longitude);
                    displayError(downloadedForecast.error);
                }
            }
            else
            {
                displayError(geo.errorMsg);
            }
        }
        private bool restoreData()
        {
            //attempts to restore everything, returns true if it can
            //false if something goes wrong
            bool nowDone = false;
            bool forecastDone = false;
            bool hourlyDone = false;
            bool alertDone = false;
            bool withinThirtyMins = false;
            bool sameLoc = false;
            bool sameUnits = true;
            if (localStore.Values.ContainsKey(NOW_SAVE))
            {
                NowTemplate nowTemplate = Serializer.get(NOW_SAVE, typeof(NowTemplate), localStore) as NowTemplate;
                if (nowTemplate != null)
                {
                    now.DataContext = nowTemplate;
                    nowDone = true;
                }
            }
            if (localStore.Values.ContainsKey(FORECAST_SAVE))
            {
                ForecastTemplate forecastTemplate = Serializer.get(FORECAST_SAVE, typeof(ForecastTemplate), localStore) as ForecastTemplate;
                if (forecastTemplate != null)
                {
                    forecast.DataContext = forecastTemplate;
                    forecastDone = true;
                }
            }
            if (localStore.Values.ContainsKey(ALERT_SAVE))
            {
                AlertsTemplate alertsTemplate = Serializer.get(ALERT_SAVE, typeof(AlertsTemplate), localStore) as AlertsTemplate;
                if (alertsTemplate != null)
                {
                    alerts.DataContext = alertsTemplate;
                    alertDone = true;
                }
            }
            if (localStore.Values.ContainsKey(HOURLY_SAVE))
            {
                ObservableCollection<ForecastIOItem> forecastList = Serializer.get(HOURLY_SAVE, typeof(ObservableCollection<ForecastIOItem>), localStore) as ObservableCollection<ForecastIOItem>;
                if (forecastList != null)
                {
                    ForecastIOTemplate forecastTemplate = new ForecastIOTemplate() { forecastIO = new ForecastIOList() { hoursList = forecastList } };
                    hourly.DataContext = forecastTemplate;
                    hourlyDone = true;
                }
            }
            if (localStore.Values.ContainsKey(LAST_LOC))
            {
                Location lastLoc = Serializer.get(LAST_LOC, typeof(Location), localStore) as Location;
                if (lastLoc != null)
                {
                    sameLoc = ((lastLoc.IsCurrent && currentLocation.IsCurrent) || (lastLoc.LocUrl == currentLocation.LocUrl));
                    if (sameLoc)
                    {
                        hub.Header = lastLoc.LocName;
                    }
                }
            }
            if (localStore.Values.ContainsKey(LAST_SAVE))
            {
                try
                {
                    DateTime lastRun = (DateTime)Serializer.get(LAST_SAVE, typeof(DateTime), localStore);
                    TimeSpan elapsed = DateTime.Now - lastRun;
                    withinThirtyMins = elapsed.TotalMinutes < 30;
                }
                catch
                {
                    withinThirtyMins = false;
                }
            }
            if (localStore.Values.ContainsKey(UNITS_CHANGED))
            {
                sameUnits = !(bool)localStore.Values[UNITS_CHANGED];
                localStore.Values[UNITS_CHANGED] = false;
            }
            return nowDone && forecastDone && alertDone && hourlyDone && withinThirtyMins && sameLoc && sameUnits;
        }
        private bool unitsAreSI()
        {
            if (store.Values.ContainsKey(UNITS_ARE_SI))
            {
                return (bool)store.Values[UNITS_ARE_SI];
            }
            else
            {
                store.Values[UNITS_ARE_SI] = true;
                return true;
            }
        }
        private void saveData()
        {
            //saves all the data contexts into a super context that might be used later
            AlertsTemplate alertsData = alerts.DataContext as AlertsTemplate;
            ForecastIOTemplate forecastIOData = hourly.DataContext as ForecastIOTemplate;
            NowTemplate nowData = now.DataContext as NowTemplate;
            ForecastTemplate forecastData = forecast.DataContext as ForecastTemplate;
            DataSaveClass save = new DataSaveClass();
            if (alertsData != null)
            {
                Serializer.save(alertsData, typeof(AlertsTemplate), ALERT_SAVE, localStore);
            }
            if (forecastIOData != null)
            {
                ObservableCollection<ForecastIOItem> shortForecast = new ObservableCollection<ForecastIOItem>();
                for (int i = 0; i < 13 && i < forecastIOData.forecastIO.hoursList.Count; i++)
                {
                    shortForecast.Add(forecastIOData.forecastIO.hoursList[i]);
                }
                Serializer.save(shortForecast, typeof(ObservableCollection<ForecastIOItem>), HOURLY_SAVE, localStore);
            }
            if (nowData != null)
            {
                Serializer.save(nowData, typeof(NowTemplate), NOW_SAVE, localStore);
            }
            if (forecastData != null)
            {
                Serializer.save(forecastData, typeof(ForecastTemplate), FORECAST_SAVE, localStore);
            }
            Serializer.save(currentLocation, typeof(Location), LAST_LOC, localStore);
        }
        async private Task<bool> setFavoriteLocations()
        {
            LocationTemplate locTemplate = new LocationTemplate() { locations = new LocationList() };
            if (store.Values.ContainsKey(LOC_STORE))
            {
                ObservableCollection<Location> list = (Serializer.get(LOC_STORE, typeof(ObservableCollection<Location>), store) as ObservableCollection<Location>);
                if (list != null)
                {
                    locTemplate.locations.locationList = list;
                }
                else
                {
                    //something wrong with the list, reset roaming and try again
                    store.Values.Remove(LOC_STORE);
                    await setFavoriteLocations();
                }
            }
            else
            {
                locTemplate.locations = await setupLocation();
            }
            if (currentLocation == null)
            {
                foreach (Location loc in locTemplate.locations.locationList)
                {
                    if (loc.IsDefault)
                    {
                        currentLocation = loc;
                        break;
                    }
                }
                if (currentLocation == null)
                {
                    if (locTemplate.locations.locationList.Count > 0)
                    {
                        locTemplate.locations.locationList[0].IsDefault = true;
                        locTemplate.locations.locationList[0].Image = "/Assets/favs.png";
                        currentLocation = locTemplate.locations.locationList[0];
                        Serializer.save(locTemplate.locations.locationList, typeof(ObservableCollection<Location>), LOC_STORE, store);
                    }
                }
            }
            locList.DataContext = locTemplate;
            return locTemplate.locations.locationList.Count > 0;
        }
        async private Task<LocationList> setupLocation()
        {
            LocationList locList = new LocationList();
            locList.locationList = new ObservableCollection<Location>();
            MessageDialog dialog = new MessageDialog("Weathr can use your phone's location to find more accurate forecast. Allow Weathr to use your location?", "Allow location?");
            dialog.Commands.Add(new UICommand("Use location", delegate(IUICommand cmd)
            {
                locList.locationList.Add(new Location() { IsCurrent = true, LocName = "Current Location", LocUrl = "currLoc", IsDefault = true, Lat = 0, Lon = 0, Image = "/Assets/favs.png" });
                localStore.Values[ALLOW_LOC] = true;
                Serializer.save(locList.locationList, typeof(ObservableCollection<Location>), LOC_STORE, store);
            }));
            dialog.Commands.Add(new UICommand("No", delegate(IUICommand cmd)
            {
                localStore.Values[ALLOW_LOC] = false;
            }));
            await dialog.ShowAsync();


            return locList;
        }

        //set up Wunderground
        async private Task<WeatherInfo> setWeather(double lat, double lon)
        {
            statusBar.ShowAsync();
            statusBar.ProgressIndicator.Text = "Getting your current weather...";
            GetWundergroundData weatherData = new GetWundergroundData(WUND_API, lat, lon);
            WeatherInfo downloadedForecast = await weatherData.getConditions();
            statusBar.HideAsync();
            return downloadedForecast;
        }
        async private Task<WeatherInfo> setWeather(string wUrl)
        {
            statusBar.ProgressIndicator.Text = "Getting your current weather...";
            GetWundergroundData weatherData = new GetWundergroundData(WUND_API, wUrl);
            WeatherInfo downloadedForecast = await weatherData.getConditions();
            return downloadedForecast;
        }
        private void updateWeatherInfo(WeatherInfo downloadedForecast, bool isSI)
        {
            hub.Header = downloadedForecast.city + ", " + downloadedForecast.state;
            NowTemplate nowTemplate = new NowTemplate() { temp = downloadedForecast.tempC + "°", conditions = downloadedForecast.currentConditions.ToUpper(), feelsLike = "Feels like: " + downloadedForecast.feelsLikeC + "°", humidity = "Humidity: " + downloadedForecast.humidity, tempCompare = "TOMORROW WILL BE " + downloadedForecast.tempCompareC + " TODAY", wind = "Wind " + downloadedForecast.windSpeedK + " " + downloadedForecast.windDir };
            ForecastTemplate forecastTemplate = createForecastList(downloadedForecast.forecastC);

            if (!isSI)
            {
                nowTemplate.temp = downloadedForecast.tempF + "°";
                nowTemplate.feelsLike = "Feels like: " + downloadedForecast.feelsLikeF + "°";
                nowTemplate.tempCompare = "TOMORROW WILL BE " + downloadedForecast.tempCompareF + " TODAY";
                nowTemplate.wind = "Wind " + downloadedForecast.windSpeedM + " " + downloadedForecast.windDir;
                forecastTemplate = createForecastList(downloadedForecast.forecastF);
            }
            now.DataContext = nowTemplate;
            forecast.DataContext = forecastTemplate;
        }
        private ForecastTemplate createForecastList(ObservableCollection<WundForecastItem> forecast)
        {
            ObservableCollection<ForecastItem> forecastData = new ObservableCollection<ForecastItem>();
            foreach (WundForecastItem day in forecast)
            {
                forecastData.Add(new ForecastItem() { title = day.title, text = day.text, pop = day.pop });
            }
            return new ForecastTemplate() { forecast = new ForecastList() { forecastList = forecastData } };
        }

        //set up Forecast.IO
        async private void updateForecastIO(double lat, double lon, bool isSI)
        {
            statusBar.ShowAsync();
            statusBar.ProgressIndicator.Text = "Getting your forecast...";
            GetForecastIOData getForecastIOData = new GetForecastIOData(lat, lon, isSI);
            ForecastIOClass forecastIOClass = await getForecastIOData.getForecast();
            if (!forecastIOClass.fail)
            {
                if (forecastIOClass.flags.hoursExists)
                {
                    updateHourList(forecastIOClass.hours.hours);
                }
                if (forecastIOClass.flags.minsExists)
                {
                    tryDisplayNextHour(forecastIOClass.mins.summary);
                }
            }
            statusBar.HideAsync();
        }
        private void tryDisplayNextHour(string minSum)
        {
            if (now.DataContext != null)
            {
                NowTemplate nowContext = (now.DataContext as NowTemplate);
                if (nowContext != null)
                {
                    nowContext.nextHour = minSum;
                    now.DataContext = null;
                    now.DataContext = nowContext;
                }
            }
        }
        private void updateHourList(ObservableCollection<HourForecast> hours)
        {
            ForecastIOTemplate forecastIOTemplate = new ForecastIOTemplate();
            forecastIOTemplate.forecastIO = new ForecastIOList();
            forecastIOTemplate.forecastIO.hoursList = new ObservableCollection<ForecastIOItem>();
            foreach (HourForecast hour in hours)
            {
                DateTime time = (unixTimeStampToDateTime(hour.time));
                string timeString = time.ToString("h:mm tt") + " on " + time.DayOfWeek;
                forecastIOTemplate.forecastIO.hoursList.Add(new ForecastIOItem() { description = hour.summary, chanceOfPrecip = Convert.ToString(hour.precipProbability * 100) + "% Chance of Precip", temp = Convert.ToString((int)hour.temperature) + "°", time = timeString });
            }
            hourly.DataContext = forecastIOTemplate;
        }
        private DateTime unixTimeStampToDateTime(double unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddSeconds(unixTimeStamp).ToLocalTime();
            return dtDateTime;
        }

        //set up maps
        private void setMaps(Geopoint pos)
        {
            maps.DataContext = new MapsTemplate() { center = pos };
        }
        //save mapcontrol for later
        private MapControl radMap;
        private MapControl satMap;
        private void setupMaps()
        {
            setupRadar();
            setupSatellite();
        }

        async private void setupSatellite()
        {
            HttpMapTileDataSource dataSource = new HttpMapTileDataSource("http://mesonet.agron.iastate.edu/cache/tile.py/1.0.0/goes-ir-4km-900913/{zoomlevel}/{x}/{y}.png?" + (DateTime.Now));
            MapTileSource tileSource = new MapTileSource(dataSource);
            satMap.TileSources.Add(tileSource);
            GeoTemplate point = await tryGetLocation();
            if (!point.fail)
            {
                Polygon triangle = createMapMarker();
                MapControl.SetLocation(triangle, point.position);
                satMap.Children.Add(triangle);
            }
        }

        async private void setupRadar()
        {
            HttpMapTileDataSource dataSource = new HttpMapTileDataSource("http://mesonet.agron.iastate.edu/cache/tile.py/1.0.0/nexrad-n0q-900913/{zoomlevel}/{x}/{y}.png?" + (DateTime.Now));
            MapTileSource tileSource = new MapTileSource(dataSource);
            radMap.TileSources.Add(tileSource);
            GeoTemplate point = await tryGetLocation();
            if (!point.fail)
            {
                Polygon triangle = createMapMarker();
                MapControl.SetLocation(triangle, point.position);
                radMap.Children.Add(triangle);
            }
        }
        private void radarMap_Loaded(object sender, RoutedEventArgs e)
        {
            radMap = (sender as MapControl);
        }
        private void satMap_Loaded(object sender, RoutedEventArgs e)
        {
            satMap = (sender as MapControl);
        }
        private Polygon createMapMarker()
        {
            //create a marker
            Polygon triangle = new Polygon();
            triangle.Fill = new SolidColorBrush(Colors.Black);
            triangle.Points.Add((new Point(0, 0)));
            triangle.Points.Add((new Point(0, 40)));
            triangle.Points.Add((new Point(20, 40)));
            triangle.Points.Add((new Point(20, 20)));
            ScaleTransform flip = new ScaleTransform();
            flip.ScaleY = -1;
            triangle.RenderTransform = flip;
            return triangle;
        }
        async private Task<GeoTemplate> tryGetLocation()
        {
            //keeps from SystemAccessViolation, hopefully
            if (GetGeoposition != null)
            {
                GeoTemplate origTemplate = await GetGeoposition.getLocation(new TimeSpan(0, 0, 10), new TimeSpan(1, 0, 0));
                GeoTemplate newTemplate = new GeoTemplate() { fail = origTemplate.fail, errorMsg = origTemplate.errorMsg, useCoord = origTemplate.useCoord, wUrl = origTemplate.wUrl };
                Geopoint point;
                if (origTemplate.position != null)
                {
                    point = new Geopoint(new BasicGeoposition() { Latitude = origTemplate.position.Position.Latitude, Longitude = origTemplate.position.Position.Longitude });
                    newTemplate.position = point;
                }
                else
                {
                    newTemplate.fail = true;
                    newTemplate.errorMsg = "pos not defined";
                }
                return newTemplate;
            }
            return null;
        }

        //set up alerts
        async private void setAlerts(double lat, double lon)
        {
            statusBar.ShowAsync();
            statusBar.ProgressIndicator.Text = "Getting alerts...";
            GetAlerts a = new GetAlerts(lat, lon);
            AlertData d = await a.getAlerts();
            alerts.DataContext = createAlertsList(d.alerts);
            statusBar.HideAsync();
        }
        private object createAlertsList(ObservableCollection<Alert> alerts)
        {
            ObservableCollection<AlertItem> alertsData = new ObservableCollection<AlertItem>();
            foreach (Alert item in alerts)
            {
                AlertItem alert = new AlertItem();
                if (item.url != null)
                {
                    alert.allClear = false;
                }
                else
                {
                    alert.allClear = true;
                }
                alert.Headline = item.headline;
                alert.TextUrl = item.url;
                alertsData.Add(alert);
            }
            return new AlertsTemplate() { alerts = new AlertList() { alertList = alertsData } };
        }

        //setting a background
        async private void setBG(string conditions, double lat, double lon)
        {
            statusBar.ShowAsync();
            statusBar.ProgressIndicator.Text = "Getting your background...";
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
            GetFlickrInfo f = new GetFlickrInfo(FLICKR_API);
            FlickrData imgList = await f.getImages(getTags(cond), useGroup, useLoc, lat, lon);
            if (!imgList.fail && imgList.images.Count > 0)
            {
                Random r = new Random();
                int num = r.Next(imgList.images.Count);
                return f.getImageUri(imgList.images[num], GetFlickrInfo.ImageSize.large);
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
            statusBar.HideAsync();
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

        //buttons and stuff
        async private void satMap_Tap(object sender, TappedRoutedEventArgs e)
        {
            MapLaunchClass mapLaunchClass = new MapLaunchClass() { loc = await GetGeoposition.getLocation(new TimeSpan(0, 0, 10), new TimeSpan(1, 0, 0)), type = MapLaunchClass.mapType.satellite };
            Frame.Navigate(typeof(WeatherMap), mapLaunchClass);
        }
        async private void radarMap_Tap(object sender, TappedRoutedEventArgs e)
        {
            MapLaunchClass mapLaunchClass = new MapLaunchClass() { loc = await GetGeoposition.getLocation(new TimeSpan(0, 0, 10), new TimeSpan(1, 0, 0)), type = MapLaunchClass.mapType.radar };
            Frame.Navigate(typeof(WeatherMap), mapLaunchClass);
        }
        private void locationName_Tapped(object sender, TappedRoutedEventArgs e)
        {
            Location loc = (Location)(sender as StackPanel).DataContext;
            Frame.Navigate(typeof(MainPage), loc);
        }
        private void Alert_Tapped(object sender, TappedRoutedEventArgs e)
        {
            AlertItem alert = (AlertItem)(sender as StackPanel).DataContext;
            if (!alert.allClear)
            {
                Launcher.LaunchUriAsync(new Uri(alert.TextUrl));
            }
            return;
        }
        private void refresh_Click(object sender, RoutedEventArgs e)
        {
            updateUI();
        }
        private void settings_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(SettingsPivot));
        }
        async private void pinLoc_Click(object sender, RoutedEventArgs e)
        {
            SecondaryTile secondaryTile = new SecondaryTile() { Arguments = currentLocation.LocUrl, TileId = currentLocation.Lat + "_" + currentLocation.Lon, DisplayName = currentLocation.LocName, RoamingEnabled = true };
            secondaryTile.VisualElements.ShowNameOnSquare150x150Logo = true;
            secondaryTile.VisualElements.ShowNameOnWide310x150Logo = true;
            secondaryTile.VisualElements.Square150x150Logo = new Uri("ms-appx:///Assets/Square71x71Logo.scale-240.png");
            secondaryTile.VisualElements.Square310x310Logo = new Uri("ms-appx:///Assets/SmallLogo.scale-240.png");
            secondaryTile.VisualElements.Wide310x150Logo = new Uri("ms-appx:///Assets/WideLogo.scale-240.png");
            await secondaryTile.RequestCreateAsync();

        }
        async private void changePic_Click(object sender, RoutedEventArgs e)
        {
            GeoTemplate loc = await GetGeoposition.getLocation(new TimeSpan(0, 0, 10), new TimeSpan(1, 0, 0));
            NowTemplate nowTemplate = (now.DataContext as NowTemplate);
            if (nowTemplate != null)
            {
                setBG(nowTemplate.conditions, loc.position.Position.Latitude, loc.position.Position.Longitude);
            }
            else
            {
                setBG("sky", loc.position.Position.Latitude, loc.position.Position.Longitude);
            }

        }
        private void about_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(AboutPage));
        }
        private void addLoc_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(AddLocation));
        }

        //set of bools to make sure things aren't set more than once
        private bool mapsSet = false;
        private void hub_SectionsInViewChanged(object sender, SectionsInViewChangedEventArgs e)
        {
            Hub hub = sender as Hub;
            if (hub != null)
            {
                //IList<HubSection> inView = hub.SectionsInView;
                HubSection section = hub.SectionsInView[0];
                if (section.Name == "maps")
                {
                    setupMaps();
                }
            }
        }
    }
}
