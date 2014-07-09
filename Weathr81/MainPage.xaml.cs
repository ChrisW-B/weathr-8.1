using BackgroundTask;
using DataTemplates;
using FlickrInfo;
using ForecastIOData;
using LocationHelper;
using SerializerClass;
using StoreLabels;
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
using Windows.Networking.Connectivity;
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

        async private Task<bool> trialNotOver()
        {
            //checks whether or not app can be run
            if (isFullVersion() || boughtTime())
            {
                return true;
            }
            else
            {
                DateTime firstRun;
                if (store.Values.ContainsKey(Values.FIRST_START))
                {
                    firstRun = (DateTime)Serializer.get(Values.FIRST_START, typeof(DateTime), store);
                }
                else
                {
                    Serializer.save(DateTime.Now, typeof(DateTime), Values.FIRST_START, store);
                    firstRun = DateTime.Now;
                }
                int daysRemaining = 7 - (int)(DateTime.UtcNow - firstRun).TotalDays;
                daysRemaining = -1;
                if (daysRemaining >= 0)
                {
                    MessageDialog dialog = new MessageDialog("Using the best weather sources avalible isn't free, so the free trial is only 7 days. You have " + daysRemaining + " left. Would you like to upgrade to unlimited for $.99?", "Welcome to Weathr!");
                    dialog.Commands.Add(new UICommand("Upgrade", delegate(IUICommand cmd)
                    {
                        //navigate to the store
                    }));
                    dialog.Commands.Add(new UICommand("Not now", delegate(IUICommand cmd)
                    {
                        return;
                    }));
                    await dialog.ShowAsync();
                    return true;
                }
                else
                {
                    MessageDialog dialog = new MessageDialog("Using the best weather sources avalible isn't free, so the free trial is only 7 days. You have 0 left. Would you like to upgrade to unlimited for $.99?", "Your trial has expired!");
                    dialog.Commands.Add(new UICommand("Upgrade", delegate(IUICommand cmd)
                    {
                        //navigate to the store
                    }));
                    dialog.Commands.Add(new UICommand("Close App", delegate(IUICommand cmd)
                    {
                        Application.Current.Exit();
                    }));
                    await dialog.ShowAsync();
                    return false;
                }
            }
        }
        private bool boughtTime()
        {
            //determines whether the time extension has been purchased
            return false;
        }
        private bool isFullVersion()
        {
            //determines whether the app is the full version
            return true;
        }

        async private void runApp()
        {
            //central point of app, runs other methods
            tryBackgroundTask();
            if (await trialNotOver())
            {
                await statusBar.ShowAsync();
                statusBar.BackgroundColor = Colors.Black;
                statusBar.BackgroundOpacity = 0;
                statusBar.ProgressIndicator.Text = "Getting your location...";
                await statusBar.ProgressIndicator.ShowAsync();
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
                        await statusBar.ProgressIndicator.HideAsync();
                        if (allowedToSetBG())
                        {
                            await statusBar.ProgressIndicator.ShowAsync();
                            GeoTemplate geo = await GetGeoposition.getLocation(new TimeSpan(0, 0, 10), new TimeSpan(1, 0, 0));
                            NowTemplate nowTemplate = (now.DataContext as NowTemplate);
                            await statusBar.ProgressIndicator.HideAsync();
                            if (nowTemplate != null && geo.position != null)
                            {
                                setBG(nowTemplate.conditions, geo.position.Position.Latitude, geo.position.Position.Longitude);
                            }
                        }
                    }
                }
                else
                {
                    Frame.Navigate(typeof(AddLocation));
                }
            }
            disablePinIfPinned();
        }

        private bool allowedToSetBG()
        {
            if (store.Values.ContainsKey(Values.ALLOW_MAIN_BG))
            {
                if ((bool)store.Values[Values.ALLOW_MAIN_BG])
                {
                    if (store.Values.ContainsKey(Values.MAIN_BG_WIFI_ONLY))
                    {
                        if (isOnWifi())
                        {
                            return true;
                        }
                        else
                        {
                            return !(bool)store.Values[Values.MAIN_BG_WIFI_ONLY];
                        }
                    }
                    return true;
                }
                return false;
            }
            else
            {
                store.Values[Values.ALLOW_MAIN_BG] = true;
                return allowedToSetBG();
            }
        }
        private bool isOnWifi()
        {
            //checks whether phone is on wifi
            ConnectionProfile prof = NetworkInformation.GetInternetConnectionProfile();
            return prof.IsWlanConnectionProfile;
        }

        async private void disablePinIfPinned()
        {
            if (await tileExists())
            {
                AppBarButton pinButton = appBar.PrimaryCommands[2] as AppBarButton;
                if (pinButton != null)
                {
                    pinButton.IsEnabled = false;
                }
            }
        }
        async private Task<bool> tileExists()
        {
            IReadOnlyCollection<SecondaryTile> tiles = await SecondaryTile.FindAllForPackageAsync();
            foreach (SecondaryTile tile in tiles)
            {
                if (tile.Arguments == currentLocation.LocUrl)
                {
                    return true;
                }
            }
            return false;
        }

        private void tryBackgroundTask()
        {
            if (localStore.Values.ContainsKey(Values.ALLOW_BG_TASK))
            {
                if ((bool)localStore.Values[Values.ALLOW_BG_TASK])
                {
                    if (!UpdateTiles.IsTaskRegistered(Values.TASK_NAME))
                    {
                        if (localStore.Values.ContainsKey(Values.UPDATE_FREQ))
                        {
                            UpdateTiles.Register(Values.TASK_NAME, (uint)localStore.Values[Values.UPDATE_FREQ]);
                            return;
                        }
                        UpdateTiles.Register(Values.TASK_NAME, 120);
                    }
                }
            }
        }

        private bool allowedToAutoFind()
        {
            if (localStore.Values.ContainsKey(Values.ALLOW_LOC))
            {
                return (bool)localStore.Values[Values.ALLOW_LOC];
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
            await statusBar.ProgressIndicator.ShowAsync();
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
                    Serializer.save(DateTime.Now, typeof(DateTime), Values.LAST_SAVE, localStore);
                    updateWeatherInfo(downloadedForecast, isSI);
                    updateForecastIO(geo.position.Position.Latitude, geo.position.Position.Longitude, isSI);
                    if (allowedToSetBG())
                    {
                        setBG(downloadedForecast.currentConditions, geo.position.Position.Latitude, geo.position.Position.Longitude);
                    }
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
            if (localStore.Values.ContainsKey(Values.NOW_SAVE))
            {
                NowTemplate nowTemplate = Serializer.get(Values.NOW_SAVE, typeof(NowTemplate), localStore) as NowTemplate;
                if (nowTemplate != null)
                {
                    now.DataContext = nowTemplate;
                    nowDone = true;
                }
            }
            if (localStore.Values.ContainsKey(Values.FORECAST_SAVE))
            {
                ForecastTemplate forecastTemplate = Serializer.get(Values.FORECAST_SAVE, typeof(ForecastTemplate), localStore) as ForecastTemplate;
                if (forecastTemplate != null)
                {
                    forecast.DataContext = forecastTemplate;
                    forecastDone = true;
                }
            }
            if (localStore.Values.ContainsKey(Values.ALERT_SAVE))
            {
                AlertsTemplate alertsTemplate = Serializer.get(Values.ALERT_SAVE, typeof(AlertsTemplate), localStore) as AlertsTemplate;
                if (alertsTemplate != null)
                {
                    alerts.DataContext = alertsTemplate;
                    alertDone = true;
                }
            }
            if (localStore.Values.ContainsKey(Values.HOURLY_SAVE))
            {
                ObservableCollection<ForecastIOItem> forecastList = Serializer.get(Values.HOURLY_SAVE, typeof(ObservableCollection<ForecastIOItem>), localStore) as ObservableCollection<ForecastIOItem>;
                if (forecastList != null)
                {
                    ForecastIOTemplate forecastTemplate = new ForecastIOTemplate() { forecastIO = new ForecastIOList() { hoursList = forecastList } };
                    hourly.DataContext = forecastTemplate;
                    hourlyDone = true;
                }
            }
            if (localStore.Values.ContainsKey(Values.LAST_LOC))
            {
                Location lastLoc = Serializer.get(Values.LAST_LOC, typeof(Location), localStore) as Location;
                if (lastLoc != null)
                {
                    sameLoc = ((lastLoc.IsCurrent && currentLocation.IsCurrent) || (lastLoc.LocUrl == currentLocation.LocUrl));
                    if (sameLoc)
                    {
                        hub.Header = lastLoc.LocName;
                    }
                }
            }
            if (localStore.Values.ContainsKey(Values.LAST_SAVE))
            {
                try
                {
                    DateTime lastRun = (DateTime)Serializer.get(Values.LAST_SAVE, typeof(DateTime), localStore);
                    TimeSpan elapsed = DateTime.Now - lastRun;
                    withinThirtyMins = elapsed.TotalMinutes < 30;
                }
                catch
                {
                    withinThirtyMins = false;
                }
            }
            if (localStore.Values.ContainsKey(Values.UNITS_CHANGED))
            {
                sameUnits = !(bool)localStore.Values[Values.UNITS_CHANGED];
                localStore.Values[Values.UNITS_CHANGED] = false;
            }
            return nowDone && forecastDone && alertDone && hourlyDone && withinThirtyMins && sameLoc && sameUnits;
        }
        private bool unitsAreSI()
        {
            if (store.Values.ContainsKey(Values.UNITS_ARE_SI))
            {
                return (bool)store.Values[Values.UNITS_ARE_SI];
            }
            else
            {
                store.Values[Values.UNITS_ARE_SI] = true;
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
                Serializer.save(alertsData, typeof(AlertsTemplate), Values.ALERT_SAVE, localStore);
            }
            if (forecastIOData != null)
            {
                ObservableCollection<ForecastIOItem> shortForecast = new ObservableCollection<ForecastIOItem>();
                for (int i = 0; i < 13 && i < forecastIOData.forecastIO.hoursList.Count; i++)
                {
                    shortForecast.Add(forecastIOData.forecastIO.hoursList[i]);
                }
                Serializer.save(shortForecast, typeof(ObservableCollection<ForecastIOItem>), Values.HOURLY_SAVE, localStore);
            }
            if (nowData != null)
            {
                Serializer.save(nowData, typeof(NowTemplate), Values.NOW_SAVE, localStore);
            }
            if (forecastData != null)
            {
                Serializer.save(forecastData, typeof(ForecastTemplate), Values.FORECAST_SAVE, localStore);
            }
            Serializer.save(currentLocation, typeof(Location), Values.LAST_LOC, localStore);
        }
        async private Task<bool> setFavoriteLocations()
        {
            LocationTemplate locTemplate = new LocationTemplate() { locations = new LocationList() };
            if (store.Values.ContainsKey(Values.LOC_STORE))
            {
                ObservableCollection<Location> list = (Serializer.get(Values.LOC_STORE, typeof(ObservableCollection<Location>), store) as ObservableCollection<Location>);
                if (list != null)
                {
                    locTemplate.locations.locationList = list;
                }
                else
                {
                    //something wrong with the list, reset roaming and try again
                    store.Values.Remove(Values.LOC_STORE);
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
                        Serializer.save(locTemplate.locations.locationList, typeof(ObservableCollection<Location>), Values.LOC_STORE, store);
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
                localStore.Values[Values.ALLOW_LOC] = true;
                Serializer.save(locList.locationList, typeof(ObservableCollection<Location>), Values.LOC_STORE, store);
            }));
            dialog.Commands.Add(new UICommand("No", delegate(IUICommand cmd)
            {
                localStore.Values[Values.ALLOW_LOC] = false;
            }));
            await dialog.ShowAsync();


            return locList;
        }

        //set up Wunderground
        async private Task<WeatherInfo> setWeather(double lat, double lon)
        {
            await statusBar.ProgressIndicator.ShowAsync();
            statusBar.ProgressIndicator.Text = "Getting your current weather...";
            GetWundergroundData weatherData = new GetWundergroundData(Values.WUND_API, lat, lon);
            WeatherInfo downloadedForecast = await weatherData.getConditions();
            await statusBar.ProgressIndicator.HideAsync();
            return downloadedForecast;
        }
        async private Task<WeatherInfo> setWeather(string wUrl)
        {
            await statusBar.ProgressIndicator.ShowAsync();
            statusBar.ProgressIndicator.Text = "Getting your current weather...";
            GetWundergroundData weatherData = new GetWundergroundData(Values.WUND_API, wUrl);
            WeatherInfo downloadedForecast = await weatherData.getConditions();
            await statusBar.ProgressIndicator.HideAsync();
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
            await statusBar.ProgressIndicator.ShowAsync();
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
            await statusBar.ProgressIndicator.HideAsync();
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
            await statusBar.ProgressIndicator.ShowAsync();
            statusBar.ProgressIndicator.Text = "Getting alerts...";
            GetAlerts a = new GetAlerts(lat, lon);
            AlertData d = await a.getAlerts();
            alerts.DataContext = createAlertsList(d.alerts);
            await statusBar.ProgressIndicator.HideAsync();
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
            await statusBar.ProgressIndicator.ShowAsync();
            statusBar.ProgressIndicator.Text = "Getting your background...";
            FlickrImage bg = await getBGInfo(conditions, true, true, lat, lon, 0);
            if (bg != null)
            {
                addArtistInfo(bg.artist, bg.artistUri);
                setHubBG(bg.uri);
            }
            await statusBar.ProgressIndicator.HideAsync();
        }

        private void addArtistInfo(string artistName, Uri artistUri)
        {
            LocationTemplate locTemp = locList.DataContext as LocationTemplate;
            if (locTemp != null)
            {
                locTemp.PhotoDetails = artistName;
                locTemp.ArtistUri = artistUri;
                locList.DataContext = null;
                locList.DataContext = locTemp;
            }
        }


        async private Task<FlickrImage> getBGInfo(string cond, bool useGroup, bool useLoc, double lat, double lon, int timesRun)
        {
            //gets a uri for a background image from flickr
            if (timesRun > 1)
            {
                return null;
            }
            GetFlickrInfo f = new GetFlickrInfo(Values.FLICKR_API);
            FlickrData imgList = await f.getImages(getTags(cond), useGroup, useLoc, lat, lon);
            if (!imgList.fail && imgList.images.Count > 0)
            {
                Random r = new Random();
                int num = r.Next(imgList.images.Count);
                FlickrImage bgFlickr = new FlickrImage();
                FlickrImageData img = imgList.images[num];
                bgFlickr.uri = f.getImageUri(img, GetFlickrInfo.ImageSize.large);
                FlickrUser user = await f.getUser(img.owner);
                bgFlickr.artist = user.userName;
                bgFlickr.artistUri = user.profUri;
                return bgFlickr;
            }
            else
            {
                return await getBGInfo(cond, useGroup, useLoc, lat, lon, timesRun++);
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
        async private void refresh_Click(object sender, RoutedEventArgs e)
        {
            if (await trialNotOver())
            {
                updateUI();
            }
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
            secondaryTile.VisualElements.Square71x71Logo = new Uri("ms-appx:///Assets/Square71x71Logo.png");
            secondaryTile.VisualElements.Square150x150Logo = new Uri("ms-appx:///Assets/Logo.png");
            secondaryTile.VisualElements.Square310x310Logo = new Uri("ms-appx:///Assets/SmallLogo.scale-240.png");
            secondaryTile.VisualElements.Wide310x150Logo = new Uri("ms-appx:///Assets/WideLogo.png");
            await secondaryTile.RequestCreateAsync();

        }
        async private void changePic_Click(object sender, RoutedEventArgs e)
        {
            await statusBar.ProgressIndicator.ShowAsync();
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
            await statusBar.ProgressIndicator.HideAsync();
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
        private AppBarButton addLocButton;
        async private void hub_SectionsInViewChanged(object sender, SectionsInViewChangedEventArgs e)
        {
            if (addLocButton == null)
            {
                addLocButton = new AppBarButton();
                addLocButton.Click += addLoc_Click;
                addLocButton.Icon = new SymbolIcon(Symbol.Add);
                addLocButton.Label = "Add place";
            }
            Hub hub = sender as Hub;
            if (hub != null)
            {
                HubSection section = hub.SectionsInView[0];
                switch (section.Name)
                {
                    case "now":
                        appBar.ClosedDisplayMode = AppBarClosedDisplayMode.Compact;
                        if (appBar.PrimaryCommands.Contains(addLocButton))
                        {
                            appBar.PrimaryCommands.Remove(addLocButton);
                        }
                        break;
                    case "hourly":
                        appBar.ClosedDisplayMode = AppBarClosedDisplayMode.Minimal;
                        if (appBar.PrimaryCommands.Contains(addLocButton))
                        {
                            appBar.PrimaryCommands.Remove(addLocButton);
                        }
                        break;
                    case "maps":
                        appBar.ClosedDisplayMode = AppBarClosedDisplayMode.Minimal;
                        if (appBar.PrimaryCommands.Contains(addLocButton))
                        {
                            appBar.PrimaryCommands.Remove(addLocButton);
                        }
                        if (!mapsSet)
                        {
                            await statusBar.ProgressIndicator.ShowAsync();
                            statusBar.ProgressIndicator.Text = "Setting up maps...";
                            GeoTemplate geoMaps = await GetGeoposition.getLocation(new TimeSpan(0, 0, 10), new TimeSpan(1, 0, 0));
                            setMaps(geoMaps.position);
                            setupMaps();
                            await statusBar.ProgressIndicator.HideAsync();
                            mapsSet = true;
                        }
                        break;
                    case "alerts":
                        appBar.ClosedDisplayMode = AppBarClosedDisplayMode.Minimal;
                        if (appBar.PrimaryCommands.Contains(addLocButton))
                        {
                            appBar.PrimaryCommands.Remove(addLocButton);
                        }
                        await statusBar.ProgressIndicator.ShowAsync();
                        statusBar.ProgressIndicator.Text = "Getting alerts...";
                        GeoTemplate geoAlerts = await GetGeoposition.getLocation(new TimeSpan(0, 0, 10), new TimeSpan(1, 0, 0));
                        setAlerts(geoAlerts.position.Position.Latitude, geoAlerts.position.Position.Longitude);
                        await statusBar.ProgressIndicator.HideAsync();
                        break;
                    case "locList":
                        appBar.ClosedDisplayMode = AppBarClosedDisplayMode.Compact;
                        appBar.PrimaryCommands.Add(addLocButton);
                        break;
                }
            }
        }
    }
}
