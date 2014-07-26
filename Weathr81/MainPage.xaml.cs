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
using System.IO;
using System.Net;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using TileCreater;
using WeatherData;
using WeatherDotGovAlerts;
using Weathr81.Common;
using Weathr81.HelperClasses;
using Weathr81.OtherPages;
using Windows.Devices.Geolocation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Display;
using Windows.Graphics.Imaging;
using Windows.Networking.Connectivity;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.System;
using Windows.UI;
using Windows.UI.Core;
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
            localStore.Values.Remove(Values.LAST_CMD_BAR);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            this.navigationHelper.OnNavigatedFrom(e);
            if (e.Parameter != null)
            {
                if (e.Parameter.GetType() != typeof(Location))
                {
                    localStore.Values[Values.LAST_HUB_SECTION] = hub.SectionsInView[0].Tag;
                }
            }
            else
            {
                localStore.Values[Values.LAST_HUB_SECTION] = hub.SectionsInView[0].Tag;
            }
            localStore.Values.Remove(Values.LAST_CMD_BAR);
            saveData();
        }
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            this.navigationHelper.OnNavigatedTo(e);
            localStore.Values.Remove(Values.LAST_CMD_BAR);
            BottomAppBar = new CommandBar();
            if (e.Parameter != null)
            {
                this.currentLocation = (e.Parameter as Location);
            }
            statusBar = StatusBar.GetForCurrentView();
            statusBar.ForegroundColor = Colors.White;

            runApp();
        }

        private bool connectedToInternet()
        {
            ConnectionProfile prof = NetworkInformation.GetInternetConnectionProfile();
            return prof != null;
        }

        async private Task<bool> trialNotOver()
        {
            //checks whether or not app can be run
            if (isFullVersion() || boughtTime())
            {
                if (!store.Values.ContainsKey(Values.FIRST_START))
                {
                    Serializer.save(DateTime.Now, typeof(DateTime), Values.FIRST_START, store);
                }
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
            await statusBar.ProgressIndicator.ShowAsync();
            tryBackgroundTask();
            if (await trialNotOver())
            {
                await statusBar.ShowAsync();
                statusBar.BackgroundColor = Colors.Black;
                statusBar.BackgroundOpacity = 0;
                statusBar.ProgressIndicator.Text = "Getting your location...";
                if (connectedToInternet())
                {
                    if (await setFavoriteLocations())
                    {
                        GetGeoposition = new GetGeoposition(currentLocation, allowedToAutoFind());
                        if (!restoreData())
                        {
                            clearApp();
                            hub.Header = "loading...";
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
                        }
                    }
                    else
                    {
                        Frame.Navigate(typeof(AddLocation));
                    }
                }
                else
                {
                    displayError("You need to be connected to the internet!");
                }
            }
        }

        async private void displayStatusError(string errorMsg)
        {
            statusBar.ProgressIndicator.Text = errorMsg;
            statusBar.ProgressIndicator.ProgressValue = 0;
            await Task.Delay(2000);
            statusBar.ProgressIndicator.Text = "";
            await statusBar.ProgressIndicator.HideAsync();
            statusBar.ProgressIndicator.ProgressValue = null;
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
        async private Task<SecondaryTile> getCurrentTile()
        {
            IReadOnlyCollection<SecondaryTile> tiles = await SecondaryTile.FindAllForPackageAsync();
            foreach (SecondaryTile tile in tiles)
            {
                if (tile.Arguments == currentLocation.LocUrl)
                {
                    return tile;
                }
            }
            return null;

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
        async private void displayError(string errorMsg)
        {
            clearApp();
            await statusBar.ProgressIndicator.HideAsync();
            now.DataContext = new NowTemplate() { errorText = errorMsg };
        }
        private void clearApp()
        {
            now.DataContext = null;
            hourly.DataContext = null;
            forecast.DataContext = null;
            maps.DataContext = null;
            alerts.DataContext = null;
        }
        async private void updateUI()
        {
            //updates the ui/weather conditions of app
            if (store.Values.ContainsKey("lastError"))
            {
                MessageDialog d = new MessageDialog((string)store.Values["lastError"]);
                store.Values.Remove("lastError");
                d.ShowAsync();
            }
            await statusBar.ProgressIndicator.ShowAsync();
            WeatherInfo downloadedForecast;
            GeoTemplate geo = await GetGeoposition.getLocation(new TimeSpan(0, 0, 10), new TimeSpan(1, 0, 0));
            if (!geo.fail)
            {
                if (geo.useCoord)
                {
                    downloadedForecast = await setWeather(geo.position.Position.Latitude, geo.position.Position.Longitude);
                }
                else if (geo.wUrl != null)
                {
                    downloadedForecast = await setWeather(geo.wUrl);
                }
                else
                {
                    displayError("Oops, something went wrong with this location. Try removing it and adding it back");
                    return;
                }
                if (!downloadedForecast.fail)
                {
                    bool isSI = unitsAreSI();
                    Serializer.save(DateTime.Now, typeof(DateTime), currentLocation.LocUrl + Values.LAST_SAVE, localStore);
                    updateWeatherInfo(ref downloadedForecast, isSI);
                    updateForecastIO(geo.position.Position.Latitude, geo.position.Position.Longitude, isSI);

                    //set tile data
                    string tempCompare = downloadedForecast.tomorrowShort + " tomorrow, and " + downloadedForecast.tempCompareC.ToLowerInvariant() + " today";
                    string current = "Currently " + downloadedForecast.currentConditions + ", " + downloadedForecast.tempC + "°C";
                    string today = "Today: " + downloadedForecast.todayShort + " " + downloadedForecast.todayHighC + "/" + downloadedForecast.todayLowC;
                    string tomorrow = "Tomorrow: " + downloadedForecast.tomorrowShort + " " + downloadedForecast.tomorrowHighC + "/" + downloadedForecast.tomorrowLowC;
                    if (!(new CreateTile().unitsAreSI()))
                    {
                        tempCompare = downloadedForecast.tomorrowShort + " tomorrow, and " + downloadedForecast.tempCompareF.ToLowerInvariant() + " today";
                        current = "Currently " + downloadedForecast.currentConditions + ", " + downloadedForecast.tempF + "°F";
                        today = "Today: " + downloadedForecast.todayShort + " " + downloadedForecast.todayHighF + "/" + downloadedForecast.todayLowF;
                        tomorrow = "Tomorrow: " + downloadedForecast.tomorrowShort + " " + downloadedForecast.tomorrowHighF + "/" + downloadedForecast.tomorrowLowF;
                    }

                    if (allowedToSetBG())
                    {
                        FlickrImage bgImg = await getBG(downloadedForecast.currentConditions, geo.position.Position.Latitude, geo.position.Position.Longitude);
                        if (bgImg != null)
                        {
                            hub.Background = null;
                            addArtistInfo(bgImg.artist, bgImg.artistUri);
                            string imgLoc = await saveBackground(bgImg.uri, currentLocation.LocUrl + "recentBG");
                            if (imgLoc != null)
                            {
                                ImageBrush backBrush = new ImageBrush() { ImageSource = new BitmapImage(new Uri(imgLoc)) };
                                setHubBG(backBrush);
                                await updateCurrentTile(downloadedForecast, tempCompare, current, today, tomorrow, backBrush);
                            }
                            else
                            {
                                displayStatusError("Unable to save background");
                                ImageBrush backBrush = new ImageBrush() { ImageSource = new BitmapImage(bgImg.uri) };
                                setHubBG(backBrush);
                                await updateCurrentTile(downloadedForecast, tempCompare, current, today, tomorrow, backBrush);
                            }
                        }
                        else
                        {
                            await updateCurrentTile(downloadedForecast, tempCompare, current, today, tomorrow);
                            displayStatusError("Couldn't download background!");
                        }
                    }
                    else
                    { await updateCurrentTile(downloadedForecast, tempCompare, current, today, tomorrow); }
                }
                else
                {
                    FlickrImage bgImg = await getBG("sky", geo.position.Position.Latitude, geo.position.Position.Longitude);
                    if (bgImg != null)
                    {
                        string imageLoc = await saveBackground(bgImg.uri, currentLocation.LocUrl + "recentBG");
                        if (imageLoc != null)
                        {
                            addArtistInfo(bgImg.artist, bgImg.artistUri);
                            ImageBrush backBrush = new ImageBrush() { ImageSource = new BitmapImage(new Uri(imageLoc)) };
                            setHubBG(backBrush);
                        }

                        else
                        {
                            displayStatusError("Unable to save background");
                            ImageBrush backBrush = new ImageBrush() { ImageSource = new BitmapImage(bgImg.uri) };
                            setHubBG(backBrush);
                        }
                    }
                    else
                    {
                        displayStatusError("Couldn't download background!");
                    }
                    displayError(downloadedForecast.error);
                }
            }
            else
            {
                displayError(geo.errorMsg);
            }
            if ((string)hub.Header == "")
            {
                //make sure there is always a title
                hub.Header = currentLocation.LocName;
            }
        }

        //updating tile for current view
        async private Task updateCurrentTile(WeatherInfo downloadedForecast, string tempCompare, string current, string today, string tomorrow, ImageBrush background = null)
        {
            CreateTile createTile = new CreateTile();
            if (background != null)
            {
                try
                {
                    background.ImageOpened += async (sender, eventArgs) => await imageOpenedHandler(sender, eventArgs, downloadedForecast, tempCompare, current, today, tomorrow);
                }
                catch { }
            }
            else if (currentLocation.IsDefault)
            {
                await renderTileSet(createTile.createTileWithParams(downloadedForecast), tempCompare, current, today, tomorrow);
            }
            else if (await tileExists())
            {
                await renderTileSet(createTile.createTileWithParams(downloadedForecast), tempCompare, current, today, tomorrow);
            }
        }
        async private Task imageOpenedHandler(object sender, RoutedEventArgs eventArgs, WeatherInfo downloadedForecast, string tempCompare, string current, string today, string tomorrow)
        {
            CreateTile createTile = new CreateTile();
            ImageBrush background = sender as ImageBrush;
            if (background != null)
            {
                string artistName = "unknown";
                if (currentLocation.IsDefault)
                {
                    LocationTemplate locTemp = locList.DataContext as LocationTemplate;
                    if (locTemp != null)
                    {
                        artistName = locTemp.PhotoDetails;
                    }
                    await renderTileSet(createTile.createTileWithParams(downloadedForecast, background, artistName), tempCompare, current, today, tomorrow);
                }
                if (await tileExists())
                {
                    LocationTemplate locTemp = locList.DataContext as LocationTemplate;
                    if (locTemp != null)
                    {
                        artistName = locTemp.PhotoDetails;
                    }
                    await renderTileSet(createTile.createTileWithParams(downloadedForecast, background, artistName), tempCompare, current, today, tomorrow);
                }
            }
            await Task.Delay(new TimeSpan(0, 0, 0, 10));
            tileHider = null;
        }
        async private Task renderTileSet(TileGroup tiles, string tempCompare, string current, string today, string tomorrow)
        {
            if (currentLocation.LocName == null)
            {
                currentLocation.LocName = hub.Header as string;
            }
            await renderTile(tiles.smTile, (currentLocation.LocUrl).Replace(":", "").Replace(".", "").Replace("/", "") + "sm");
            await renderTile(tiles.sqTile, (currentLocation.LocUrl).Replace(":", "").Replace(".", "").Replace("/", "") + "sq");
            await renderTile(tiles.wideTile, (currentLocation.LocUrl).Replace(":", "").Replace(".", "").Replace("/", "") + "wd");
            CreateTile tileMaker = new CreateTile();
            if (currentLocation.IsDefault)
            {
                tileMaker.pushImageToTile(Values.SAVE_LOC + (currentLocation.LocUrl).Replace(":", "").Replace(".", "").Replace("/", "") + "sm", Values.SAVE_LOC + (currentLocation.LocUrl).Replace(":", "").Replace(".", "").Replace("/", "") + "sq", Values.SAVE_LOC + (currentLocation.LocUrl).Replace(":", "").Replace(".", "").Replace("/", "") + "wd", tempCompare, current, today, tomorrow);
            }
            if (await tileExists())
            {
                SecondaryTile currentTile = await getCurrentTile();
                if (currentTile != null)
                {
                    tileMaker.pushImageToTile(Values.SAVE_LOC + (currentLocation.LocUrl).Replace(":", "").Replace(".", "").Replace("/", "") + "sm", Values.SAVE_LOC + (currentLocation.LocUrl).Replace(":", "").Replace(".", "").Replace("/", "") + "sq", Values.SAVE_LOC + (currentLocation.LocUrl).Replace(":", "").Replace(".", "").Replace("/", "") + "wd", tempCompare, current, today, tomorrow, currentTile);
                }
            }
        }
        async private Task renderTile(UIElement tile, string tileName)
        {
            tileHider.Children.Add(tile);
            RenderTargetBitmap bm = new RenderTargetBitmap();
            await bm.RenderAsync(tile);
            Windows.Storage.Streams.IBuffer pixBuf = await bm.GetPixelsAsync();

            StorageFolder localFolder = ApplicationData.Current.LocalFolder;
            StorageFile tileImageFile = await localFolder.CreateFileAsync(tileName, CreationCollisionOption.ReplaceExisting);
            DisplayInformation dispInfo = DisplayInformation.GetForCurrentView();

            using (var stream = await tileImageFile.OpenAsync(FileAccessMode.ReadWrite))
            {
                var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream);
                encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Straight, (uint)bm.PixelWidth, (uint)bm.PixelHeight, dispInfo.LogicalDpi, dispInfo.LogicalDpi, pixBuf.ToArray());
                await encoder.FlushAsync();
            }
        }

        //helper methods
        private bool restoreData()
        {
            //attempts to restore everything, returns true if it can
            //false if something goes wrong
            bool nowDone = false;
            bool forecastDone = false;
            bool hourlyDone = false;
            bool withinThirtyMins = false;
            bool bgRestore = false;
            bool sameUnits = true;
            bool locName = false;

            if (localStore.Values.ContainsKey(Values.LAST_HUB_SECTION))
            {
                hub.ScrollToSection(getSectionFromTag(Convert.ToInt16(localStore.Values[Values.LAST_HUB_SECTION])));
            }
            if (localStore.Values.ContainsKey(currentLocation.LocUrl + Values.LAST_LOC_NAME))
            {
                hub.Header = localStore.Values[currentLocation.LocUrl + Values.LAST_LOC_NAME];
                locName = true;
            }
            if (localStore.Values.ContainsKey(currentLocation.LocUrl + Values.NOW_SAVE))
            {
                NowTemplate nowTemplate = Serializer.get(currentLocation.LocUrl + Values.NOW_SAVE, typeof(NowTemplate), localStore) as NowTemplate;
                if (nowTemplate != null)
                {
                    now.DataContext = nowTemplate;
                    nowDone = true;
                }
            }
            if (localStore.Values.ContainsKey(currentLocation.LocUrl + Values.FORECAST_SAVE))
            {
                ForecastTemplate forecastTemplate = Serializer.get(currentLocation.LocUrl + Values.FORECAST_SAVE, typeof(ForecastTemplate), localStore) as ForecastTemplate;
                if (forecastTemplate != null)
                {
                    forecast.DataContext = forecastTemplate;
                    forecastDone = true;
                }
            }
            if (localStore.Values.ContainsKey(currentLocation.LocUrl + Values.ALERT_SAVE))
            {
                AlertsTemplate alertsTemplate = Serializer.get(currentLocation.LocUrl + Values.ALERT_SAVE, typeof(AlertsTemplate), localStore) as AlertsTemplate;
                if (alertsTemplate != null)
                {
                    alerts.DataContext = alertsTemplate;
                }
            }
            if (localStore.Values.ContainsKey(currentLocation.LocUrl + Values.HOURLY_SAVE))
            {
                ObservableCollection<ForecastIOItem> forecastList = Serializer.get(currentLocation.LocUrl + Values.HOURLY_SAVE, typeof(ObservableCollection<ForecastIOItem>), localStore) as ObservableCollection<ForecastIOItem>;
                if (forecastList != null)
                {
                    ForecastIOTemplate forecastTemplate = new ForecastIOTemplate() { forecastIO = new ForecastIOList() { hoursList = forecastList } };
                    hourly.DataContext = forecastTemplate;
                    hourlyDone = true;
                }
            }
            try
            {
                hub.Background = new ImageBrush() { ImageSource = new BitmapImage(new Uri(Values.SAVE_LOC + (currentLocation.LocUrl).Replace(":", "").Replace(".", "").Replace("/", "") + "recentBG.png")), Opacity = .7, Stretch = Stretch.UniformToFill };
                bgRestore = true;
            }
            catch (Exception e)
            {

            }
            if (localStore.Values.ContainsKey(currentLocation.LocUrl + Values.LAST_SAVE))
            {
                try
                {
                    DateTime lastRun = (DateTime)Serializer.get(currentLocation.LocUrl + Values.LAST_SAVE, typeof(DateTime), localStore);
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
            return nowDone && forecastDone && hourlyDone && withinThirtyMins && sameUnits && locName && bgRestore;
        }

        private HubSection getSectionFromTag(int i)
        {
            switch (i)
            {
                case 0:
                    return now;
                case 1:
                    return hourly;
                case 2:
                    return maps;
                case 3:
                    return forecast;
                case 4:
                    return alerts;
                case 5:
                    return locList;
                default:
                    return now;
            }
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
                Serializer.save(alertsData, typeof(AlertsTemplate), currentLocation.LocUrl + Values.ALERT_SAVE, localStore);
            }
            if (forecastIOData != null)
            {
                ObservableCollection<ForecastIOItem> shortForecast = new ObservableCollection<ForecastIOItem>();
                for (int i = 0; i < 13 && i < forecastIOData.forecastIO.hoursList.Count; i++)
                {
                    shortForecast.Add(forecastIOData.forecastIO.hoursList[i]);
                }
                Serializer.save(shortForecast, typeof(ObservableCollection<ForecastIOItem>), currentLocation.LocUrl + Values.HOURLY_SAVE, localStore);
            }
            if (nowData != null)
            {
                Serializer.save(nowData, typeof(NowTemplate), currentLocation.LocUrl + Values.NOW_SAVE, localStore);
            }
            if (forecastData != null)
            {
                Serializer.save(forecastData, typeof(ForecastTemplate), currentLocation.LocUrl + Values.FORECAST_SAVE, localStore);
            }
            if (hub.Header != null && currentLocation != null)
            {
                localStore.Values[currentLocation.LocUrl + Values.LAST_LOC_NAME] = hub.Header;
            }
        }
        async private Task<bool> setFavoriteLocations()
        {
            LocationTemplate locTemplate = new LocationTemplate() { locations = new LocationList() };
            if (!localStore.Values.ContainsKey(Values.IS_NEW_DEVICE))
            {
                localStore.Values[Values.IS_NEW_DEVICE] = false;
                locTemplate.locations = await setupLocation();
            }
            if (store.Values.ContainsKey(Values.LOC_STORE))
            {
                ObservableCollection<Location> list = (Serializer.get(Values.LOC_STORE, typeof(ObservableCollection<Location>), store) as ObservableCollection<Location>);
                if (list == null || list.Count < 1)
                {
                    //something wrong with the list, reset roaming and try again
                    store.Values.Remove(Values.LOC_STORE);
                    await setFavoriteLocations();
                }
                else
                {
                    locTemplate.locations.locationList = list;
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
            statusBar.ProgressIndicator.Text = "Getting your current weather...";
            GetWundergroundData weatherData = new GetWundergroundData(Values.WUND_API_KEY, lat, lon);
            WeatherInfo downloadedForecast = await weatherData.getConditions();
            return downloadedForecast;
        }
        async private Task<WeatherInfo> setWeather(string wUrl)
        {
            statusBar.ProgressIndicator.Text = "Getting your current weather...";
            GetWundergroundData weatherData = new GetWundergroundData(Values.WUND_API_KEY, wUrl);
            WeatherInfo downloadedForecast = await weatherData.getConditions();
            return downloadedForecast;
        }
        private void updateWeatherInfo(ref WeatherInfo downloadedForecast, bool isSI)
        {
            hub.Header = downloadedForecast.city + ", " + downloadedForecast.state;
            NowTemplate nowTemplate = new NowTemplate() { temp = downloadedForecast.tempC + "°", conditions = downloadedForecast.currentConditions.ToUpper(), feelsLike = "Feels like: " + downloadedForecast.feelsLikeC + "°", humidity = "Humidity: " + downloadedForecast.humidity, tempCompare = "TOMORROW WILL BE " + downloadedForecast.tempCompareC + " TODAY", wind = "Wind: " + downloadedForecast.windSpeedK + " " + downloadedForecast.windDir, };
            double uvIndex = Convert.ToDouble(downloadedForecast.UV);
            nowTemplate.uvIndex = (uvIndex > -1) ? "UV Index: " + downloadedForecast.UV : "";
            ForecastTemplate forecastTemplate = createForecastList(downloadedForecast.forecastC);

            if (!isSI)
            {
                nowTemplate.temp = downloadedForecast.tempF + "°";
                nowTemplate.feelsLike = "Feels like: " + downloadedForecast.feelsLikeF + "°";
                nowTemplate.tempCompare = "TOMORROW WILL BE " + downloadedForecast.tempCompareF + " TODAY";
                nowTemplate.wind = "Wind: " + downloadedForecast.windSpeedM + " " + downloadedForecast.windDir;
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
            statusBar.ProgressIndicator.Text = "Getting your forecast...";
            GetForecastIOData getForecastIOData = new GetForecastIOData(lat, lon, isSI);
            ForecastIOClass forecastIOClass = await getForecastIOData.getForecast();
            if (!forecastIOClass.fail)
            {
                ObservableCollection<AlertItem> alertsList = null;
                String minSum = "";
                if (forecastIOClass.flags.hoursExists)
                {
                    hourly.DataContext = updateHourList(forecastIOClass.hours.hours);
                }
                if (forecastIOClass.flags.minsExists)
                {
                    minSum = forecastIOClass.mins.summary;
                }
                if (forecastIOClass.flags.numAlerts > 0)
                {
                    alertsList = getAlertItems(forecastIOClass.Alerts);
                    alerts.DataContext = new AlertsTemplate() { alerts = new AlertList() { alertList = alertsList } };
                }
                if (alerts != null)
                {
                    updateNowWithForecastIO(minSum, alertsList);
                }
            }
        }
        private ObservableCollection<AlertItem> getAlertItems(ObservableCollection<ForecastIOAlert> alerts)
        {
            ObservableCollection<AlertItem> alertsData = new ObservableCollection<AlertItem>();
            foreach (ForecastIOAlert item in alerts)
            {
                AlertItem alert = new AlertItem();
                if (item.uri != null)
                {
                    alert.allClear = false;
                }
                else
                {
                    alert.allClear = true;
                }
                alert.Headline = item.title;
                alert.details = item.description;
                alert.TextUrl = item.uri;
                alertsData.Add(alert);
            }
            return alertsData;
        }
        private void updateNowWithForecastIO(string minSum, ObservableCollection<AlertItem> alerts)
        {
            NowTemplate nowContext = (now.DataContext as NowTemplate);
            if (nowContext != null)
            {
                nowContext.nextHour = minSum;
                int numAlerts = 0;
                foreach (AlertItem alert in alerts)
                {
                    if (!alert.allClear)
                    {
                        numAlerts++;
                    }
                }
                if (numAlerts == 1)
                {
                    nowContext.alerts = "1 Alert";
                }
                else if (numAlerts > 1)
                {
                    nowContext.alerts = alerts.Count + " alerts";
                }
                now.DataContext = null;
                now.DataContext = nowContext;
            }
        }
        private ForecastIOTemplate updateHourList(ObservableCollection<HourForecast> hours)
        {
            ForecastIOTemplate forecastIOTemplate = new ForecastIOTemplate();
            forecastIOTemplate.forecastIO = new ForecastIOList();
            forecastIOTemplate.forecastIO.hoursList = new ObservableCollection<ForecastIOItem>();
            foreach (HourForecast hour in hours)
            {
                DateTime time = (unixTimeStampToDateTime(hour.time));
                string timeString = ((twentyFourHrTime()) ? time.ToString("HH:mm") : time.ToString("h:mm tt")) + " on " + time.DayOfWeek;
                forecastIOTemplate.forecastIO.hoursList.Add(new ForecastIOItem() { description = hour.summary, chanceOfPrecip = Convert.ToString(hour.precipProbability * 100) + "% Chance of Precip", temp = Convert.ToString((int)hour.temperature) + "°", time = timeString });
            }
            return forecastIOTemplate;
        }

        //forecast.io helpers
        private bool twentyFourHrTime()
        {
            if (store.Values.ContainsKey(Values.TWENTY_FOUR_HR_TIME))
            {
                return (bool)store.Values[Values.TWENTY_FOUR_HR_TIME];
            }
            store.Values[Values.TWENTY_FOUR_HR_TIME] = false;
            return false;
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
            try
            {
                setupRadar();
                setupSatellite();
            }
            catch
            {
                displayStatusError("Problem getting map data");
            }
        }
        async private void setupSatellite()
        {
            HttpMapTileDataSource dataSource = new HttpMapTileDataSource() { AllowCaching = true };
            dataSource.UriRequested += (sender, args) => dataSource_UriRequested(sender, args, MapLaunchClass.mapType.satellite);
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
            HttpMapTileDataSource dataSource = new HttpMapTileDataSource() { AllowCaching = true };
            dataSource.UriRequested += (sender, args) => dataSource_UriRequested(sender, args, MapLaunchClass.mapType.radar);
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

        private void dataSource_UriRequested(HttpMapTileDataSource sender, MapTileUriRequestedEventArgs args, MapLaunchClass.mapType type)
        {
            MapTileUriRequestDeferral d = args.Request.GetDeferral();
            Random rand = new Random();
            char serverPre = Values.ALPHA_NUM[rand.Next(Values.ALPHA_NUM.Length)];
            try
            {
                String uri;
                if (type == MapLaunchClass.mapType.radar)
                {
                    uri = Values.HTTP + serverPre + Values.OWM_MAIN + Values.OWM_RAD + "/" + args.ZoomLevel + "/" + args.X + "/" + args.Y + Values.OWM_POST;
                }
                else
                {
                    uri = Values.HTTP + serverPre + Values.OWM_MAIN + Values.OWM_SAT + "/" + args.ZoomLevel + "/" + args.X + "/" + args.Y + Values.OWM_POST;
                }
                args.Request.Uri = new Uri(uri);
            }
            catch (Exception ex)
            {

            }
            d.Complete();
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

        //setting a background
        async private Task<FlickrImage> getBG(string conditions, double lat, double lon)
        {
            statusBar.ProgressIndicator.Text = "Getting your background...";
            FlickrImage bg = await getBGInfo(conditions, true, true, lat, lon, 0);
            if (bg != null)
            {
                return bg;
            }
            return null;
        }
        private void addArtistInfo(string artistName, Uri artistUri)
        {
            LocationTemplate locTemp = locList.DataContext as LocationTemplate;
            if (locTemp != null)
            {
                locTemp.description = "Background by ";
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
            FlickrData imgList = await f.getImages(GetFlickrInfo.getTags(cond), useGroup, useLoc, lat, lon);
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
                return await getBGInfo(cond, useGroup, false, lat, lon, timesRun + 1);
            }
        }
        private void setHubBG(ImageBrush bg)
        {
            //sets the background of the hub to a given image imagebrush
            bg.ImageOpened += bg_ImageOpened;
            bg.Opacity = .7;
            bg.Stretch = Stretch.UniformToFill;
            hub.Background = bg;
        }

        async private Task<string> saveBackground(Uri image, string imageName)
        {
            hub.Background = null;
            imageName = imageName.Replace(":", "").Replace(".", "").Replace("/", "") + ".png";
            try
            {
                // await (await ApplicationData.Current.LocalFolder.GetFileAsync(imageName)).DeleteAsync();
                using (WebResponse response = await HttpWebRequest.CreateHttp(image).GetResponseAsync())
                {
                    using (Stream stream = response.GetResponseStream())
                    {
                        StorageFile file = await ApplicationData.Current.LocalFolder.CreateFileAsync(imageName, CreationCollisionOption.ReplaceExisting);
                        Stream fileStream = await file.OpenStreamForWriteAsync();
                        using (Stream outputStream = fileStream)
                        {
                            await stream.CopyToAsync(outputStream);

                            //disposing
                            await outputStream.FlushAsync();
                            response.Dispose();
                            fileStream.Dispose();
                            outputStream.Dispose();

                            
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                store.Values["lastError"] = ex.Message;
            }
            return Values.SAVE_LOC + imageName;
            //return null;
        }
        async void bg_ImageOpened(object sender, RoutedEventArgs e)
        {
            await statusBar.ProgressIndicator.HideAsync();
        }

        //buttons and stuff
        async private void satMap_Tap(object sender, TappedRoutedEventArgs e)
        {
            if (GetGeoposition != null)
            {
                MapLaunchClass mapLaunchClass = new MapLaunchClass() { loc = await GetGeoposition.getLocation(new TimeSpan(0, 0, 10), new TimeSpan(1, 0, 0)), type = MapLaunchClass.mapType.satellite };
                Frame.Navigate(typeof(WeatherMap), mapLaunchClass);
            }
        }
        async private void radarMap_Tap(object sender, TappedRoutedEventArgs e)
        {
            if (GetGeoposition != null)
            {
                MapLaunchClass mapLaunchClass = new MapLaunchClass() { loc = await GetGeoposition.getLocation(new TimeSpan(0, 0, 10), new TimeSpan(1, 0, 0)), type = MapLaunchClass.mapType.radar };
                Frame.Navigate(typeof(WeatherMap), mapLaunchClass);
            }
        }
        private void locationName_Tapped(object sender, TappedRoutedEventArgs e)
        {
            localStore.Values.Remove(Values.LAST_HUB_SECTION);
            Location loc = (Location)(sender as StackPanel).DataContext;
            Frame.Navigate(typeof(MainPage), loc);
        }
        async private void Alert_Tapped(object sender, TappedRoutedEventArgs e)
        {
            AlertItem alert = (AlertItem)(sender as StackPanel).DataContext;
            String alertTitle = (alert.Headline != null) ? alert.Headline : "Unknown Alert";
            String alertDetails = (alert.details != null) ? alert.details : "No more details";
            if (!alert.allClear)
            {
                MessageDialog d = new MessageDialog(alertDetails, alertTitle);
                await d.ShowAsync();
            }
            return;
        }
        async private void refresh_Click(object sender, RoutedEventArgs e)
        {

            if (await trialNotOver())
            {
                if (connectedToInternet())
                {
                    await statusBar.ProgressIndicator.ShowAsync();
                    if (await setFavoriteLocations())
                    {

                        updateUI();
                    }
                }
                else
                {
                    displayError("You need to be connected to the internet!");
                }
            }
        }
        private void settings_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(SettingsPivot));
        }
        async private void pinLoc_Click(object sender, RoutedEventArgs e)
        {
            //activate background task if it hasn't been turned off
            if (!localStore.Values.ContainsKey(Values.ALLOW_BG_TASK))
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
                FlickrImage bgImg = await getBG(nowTemplate.conditions, loc.position.Position.Latitude, loc.position.Position.Longitude);
                if (bgImg != null)
                {
                    hub.Background = null;
                    addArtistInfo(bgImg.artist, bgImg.artistUri);
                    string imgLoc = await saveBackground(bgImg.uri, currentLocation.LocUrl + "recentBG");
                    if (imgLoc != null)
                    {
                        ImageBrush backBrush = new ImageBrush() { ImageSource = new BitmapImage(new Uri(imgLoc)) };
                        setHubBG(backBrush);
                    }
                    else
                    {
                        displayStatusError("Unable to save background");
                        ImageBrush backBrush = new ImageBrush() { ImageSource = new BitmapImage(bgImg.uri) };
                        setHubBG(backBrush);
                    }
                }
            }
            else
            {
                FlickrImage bgImg = await getBG("sky", loc.position.Position.Latitude, loc.position.Position.Longitude);
                if (bgImg != null)
                {
                    hub.Background = null;
                    addArtistInfo(bgImg.artist, bgImg.artistUri);
                    string imgLoc = await saveBackground(bgImg.uri, currentLocation.LocUrl + "recentBG");
                    if (imgLoc != null)
                    {
                        ImageBrush backBrush = new ImageBrush() { ImageSource = new BitmapImage(new Uri(imgLoc)) };
                        setHubBG(backBrush);
                    }
                    else
                    {
                        displayStatusError("Unable to save background");
                        ImageBrush backBrush = new ImageBrush() { ImageSource = new BitmapImage(bgImg.uri) };
                        setHubBG(backBrush);
                    }
                }
                else
                {
                    displayStatusError("Couldn't download background!");
                }
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
        private void alertsNum_Tapped(object sender, TappedRoutedEventArgs e)
        {
            hub.ScrollToSection(alerts);
        }

        //set of bools to make sure things aren't set more than once
        private bool mapsSet = false;

        async private void hub_SectionsInViewChanged(object sender, SectionsInViewChangedEventArgs e)
        {
            await Task.Delay(50);
            int secNum = Convert.ToInt16(hub.SectionsInView[0].Tag);
            if (hub.SectionsInView.Count > 2)
            {
                secNum++;
            }
            secNum = (secNum == 6) ? 0 : secNum;
            if (allowedToChangeCmdBar(secNum))
            {
                switch (secNum)
                {
                    case 0:
                        BottomAppBar = await createCommandBar(false, true);
                        break;
                    case 2:
                        await setMapsOnHubSwitch();
                        BottomAppBar = await createCommandBar();
                        break;
                    case 5:
                        BottomAppBar = await createCommandBar(true, true);
                        break;
                    default:
                        BottomAppBar = await createCommandBar();
                        break;
                }
            }
        }

        private bool allowedToChangeCmdBar(int secNum)
        {
            if (hub.SectionsInView.Count > 3)
            {
                return false;
            }
            if (localStore.Values.ContainsKey(Values.LAST_CMD_BAR))
            {
                int lastLoc = (int)localStore.Values[Values.LAST_CMD_BAR];
                if (lastLoc == secNum)
                {
                    //make sure each bar is only loaded once
                    return false;
                }
            }
            localStore.Values[Values.LAST_CMD_BAR] = secNum;
            return true;
        }
        async private Task setMapsOnHubSwitch()
        {
            statusBar.ProgressIndicator.Text = "Setting up maps...";
            if (localStore.Values.ContainsKey(Values.LAST_HUB_SECTION))
            {
                if (Convert.ToInt16(localStore.Values[Values.LAST_HUB_SECTION]) == 2)
                {
                    //delay to prevent null errors
                    await statusBar.ProgressIndicator.ShowAsync();
                    await Task.Delay(1000);
                    localStore.Values.Remove(Values.LAST_HUB_SECTION);
                }
            }
            if (!mapsSet && connectedToInternet())
            {
                await statusBar.ProgressIndicator.ShowAsync();
                GeoTemplate geoMaps = await GetGeoposition.getLocation(new TimeSpan(0, 0, 10), new TimeSpan(1, 0, 0));
                setMaps(geoMaps.position);
                setupMaps();
                mapsSet = true;
            }
            await statusBar.ProgressIndicator.HideAsync();
        }
        async private Task<CommandBar> createCommandBar(bool showAddLoc = false, bool compact = false)
        {
            CommandBar appBar = new CommandBar() { Opacity = .85, };
            ObservableCollection<AppBarButton> primaryCmds = await createPrimaryCmds(showAddLoc);
            ObservableCollection<AppBarButton> secondaryCmds = createSecondaryCmds();
            foreach (AppBarButton cmd in primaryCmds)
            {
                appBar.PrimaryCommands.Add(cmd);
            }
            foreach (AppBarButton cmd in secondaryCmds)
            {
                appBar.SecondaryCommands.Add(cmd);
            }
            toggleAppMenu(ref appBar, compact);
            return appBar;
        }
        private ObservableCollection<AppBarButton> createSecondaryCmds()
        {
            AppBarButton about = new AppBarButton() { Label = "about" };
            about.Click += about_Click;
            AppBarButton changePic = new AppBarButton() { Label = "change background" };
            changePic.Click += changePic_Click;
            ObservableCollection<AppBarButton> cmds = new ObservableCollection<AppBarButton>();
            cmds.Add(changePic);
            cmds.Add(about);
            return cmds;
        }
        async private Task<ObservableCollection<AppBarButton>> createPrimaryCmds(bool showAddLoc)
        {
            ObservableCollection<AppBarButton> cmds = new ObservableCollection<AppBarButton>();
            AppBarButton refresh = new AppBarButton() { Label = "refresh", Icon = new BitmapIcon() { UriSource = new Uri("ms-appx:///Assets/refresh.png") } };
            refresh.Click += refresh_Click;
            cmds.Add(refresh);

            AppBarButton settings = new AppBarButton() { Label = "settings", Icon = new SymbolIcon(Symbol.Setting) };
            settings.Click += settings_Click;
            cmds.Add(settings);

            AppBarButton pinLoc = new AppBarButton() { Label = "pin", Icon = new SymbolIcon(Symbol.Pin) };
            pinLoc.Click += pinLoc_Click;
            if (await tileExists())
            {
                pinLoc.IsEnabled = false;
            }
            cmds.Add(pinLoc);
            if (showAddLoc)
            {
                AppBarButton addLoc = new AppBarButton() { Label = "add place", Icon = new SymbolIcon(Symbol.Add) };
                addLoc.Click += addLoc_Click;
                cmds.Add(addLoc);
            }
            return cmds;
        }
        private void toggleAppMenu(ref CommandBar bar, bool compact = false)
        {
            bar.ClosedDisplayMode = (compact ? AppBarClosedDisplayMode.Compact : AppBarClosedDisplayMode.Minimal);
        }
    }
}
