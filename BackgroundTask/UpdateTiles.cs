using DataTemplates;
using FlickrInfo;
using LocationHelper;
using NotificationsExtensions.TileContent;
using SerializerClass;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using WeatherData;
using Windows.ApplicationModel.Background;
using Windows.Data.Xml.Dom;
using Windows.Graphics.Display;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Notifications;
using Windows.UI.StartScreen;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using WundergroundData;


namespace BackgroundTask
{
    public sealed class UpdateTiles : XamlRenderingBackgroundTask
    {
        #region variables
        private const string TASK_NAME = "Weathr Tile Updater";
        private const string WUND_API = "fb1dd3f4321d048d";
        private const string FLICKR_API = "2781c025a4064160fc77a52739b552ff";
        private const string LOC_STORE = "locList";
        private const string SAVE_LOC = "ms-appdata:///local/";
        private const string TILE_UNITS_ARE_SI = "tileUnitsAreSI";
        private const string TRANSPARENT_TILE = "tileIsTransparent";
        private static ObservableCollection<Location> locationList;
        ApplicationDataContainer store = ApplicationData.Current.RoamingSettings;
        private ApplicationDataContainer localStore = Windows.Storage.ApplicationData.Current.LocalSettings;
        private enum TileSize
        {
            small,
            medium,
            wide
        }
        #endregion

        #region registration
        public async static void Register(uint mins)
        {
            if (IsTaskRegistered(TASK_NAME))
            {
                Unregister(TASK_NAME);
            }
            BackgroundAccessStatus result = await BackgroundExecutionManager.RequestAccessAsync();
            BackgroundTaskBuilder builder = new BackgroundTaskBuilder();
            builder.Name = TASK_NAME;
            builder.TaskEntryPoint = typeof(UpdateTiles).FullName;
            builder.SetTrigger(new TimeTrigger(mins, false));
            SystemCondition condition = new SystemCondition(SystemConditionType.InternetAvailable);
            builder.AddCondition(condition);
            builder.Register();
        }
        public async static void RunFromApp()
        {
            //allows the background task to auto run once
            if (IsTaskRegistered(TASK_NAME + "Mini"))
            {
                Unregister(TASK_NAME + "Mini");
            }
            BackgroundAccessStatus result = await BackgroundExecutionManager.RequestAccessAsync();
            BackgroundTaskBuilder builder = new BackgroundTaskBuilder();
            builder.Name = TASK_NAME + "Mini";
            builder.TaskEntryPoint = typeof(UpdateTiles).FullName;
            builder.SetTrigger(new TimeTrigger(15, true));
            SystemCondition condition = new SystemCondition(SystemConditionType.InternetAvailable);
            builder.AddCondition(condition);
            builder.Register();
        }
        public static void Unregister(string name)
        {
            var entry = BackgroundTaskRegistration.AllTasks.FirstOrDefault(kvp => kvp.Value.Name == name);
            if (entry.Value != null)
            {
                entry.Value.Unregister(true);
            }
        }
        public static bool IsTaskRegistered(string name)
        {
            var entry = BackgroundTaskRegistration.AllTasks.FirstOrDefault(kvp => kvp.Value.Name == name);
            if (entry.Value != null)
            {
                return true;
            }
            return false;
        }
        #endregion

        //run the task
        async protected override void OnRun(IBackgroundTaskInstance tI)
        {
            base.OnRun(tI);
            BackgroundTaskDeferral def = tI.GetDeferral();
            if (setLocationList())
            {
                await updateTiles();
            }
            def.Complete();

        }
        private bool setLocationList()
        {
            //sets up the location list, returning true if sucessful
            if (store.Values.ContainsKey(LOC_STORE))
            {
                ObservableCollection<Location> list = (Serializer.get(LOC_STORE, typeof(ObservableCollection<Location>), store) as ObservableCollection<Location>);
                if (list != null)
                {
                    locationList = list;
                    return true;
                }
            }
            return false;
        }
        async private Task updateTiles()
        {
            await updateMainTile();
            await updateSecondaryTiles();
        }

        //updating the tiles
        async private Task updateMainTile()
        {
            //finds the main tile location and uses it to update the main tile
            foreach (Location loc in locationList)
            {
                if (loc.IsDefault)
                {
                    string name = "default";
                    string smallTileName = name + "small.png";
                    string mediumTileName = name + "med.png";
                    string wideTileName = name + "wide.png";

                    GetGeoposition pos = new GetGeoposition(loc);
                    GeoTemplate geoTemplate = await pos.getLocation(new TimeSpan(0, 0, 2), new TimeSpan(0, 1, 0));
                    if (!geoTemplate.fail)
                    {
                        GetWundergroundData getWundData = loc.IsCurrent ? new GetWundergroundData(WUND_API, geoTemplate.position.Position.Latitude, geoTemplate.position.Position.Longitude) : new GetWundergroundData(WUND_API, loc.LocUrl);
                        WeatherInfo weatherInfo = await getWundData.getConditions();
                        if (!weatherInfo.fail)
                        {
                            BackgroundTemplate data = new BackgroundTemplate()
                            {
                                medName = mediumTileName,
                                wideName = wideTileName,
                                weather = new BackgroundWeather()
                                {
                                    conditions = weatherInfo.currentConditions,
                                    tempCompare = weatherInfo.tomorrowShort + " tomorrow, and " + weatherInfo.tempCompareC.ToLowerInvariant() + " today",
                                    high = weatherInfo.todayHighC,
                                    low = weatherInfo.todayLowC,
                                    currentTemp = weatherInfo.tempC.Split('.')[0] + "°",
                                    todayForecast = weatherInfo.todayShort,
                                },
                                location = new BackgroundLoc()
                                {
                                    location = weatherInfo.city,
                                    lat = geoTemplate.position.Position.Latitude,
                                    lon = geoTemplate.position.Position.Longitude,
                                }

                            };
                            string current = "Currently " + weatherInfo.currentConditions + ", " + weatherInfo.tempC + "°C";
                            string today = "Today: " + weatherInfo.todayShort + " " + weatherInfo.todayHighC + "/" + weatherInfo.todayLowC;
                            string tomorrow = "Tomorrow: " + weatherInfo.tomorrowShort + " " + weatherInfo.tomorrowHighC + "/" + weatherInfo.tomorrowLowC;
                            if (!unitsAreSI())
                            {
                                data.weather.high = weatherInfo.todayHighF;
                                data.weather.low = weatherInfo.todayLowF;
                                data.weather.currentTemp = weatherInfo.tempF.Split('.')[0] + "°";
                                data.weather.tempCompare = "Tomorrow will be " + weatherInfo.tempCompareF.ToLowerInvariant() + " today";
                                current = "Currently: " + weatherInfo.currentConditions + ", " + weatherInfo.tempF + "°F";
                                today = "Today: " + weatherInfo.todayShort + " " + weatherInfo.todayHighF + "/" + weatherInfo.todayLowF;
                                tomorrow = "Tomorrow: " + weatherInfo.tomorrowShort + " " + weatherInfo.tomorrowHighF + "/" + weatherInfo.tomorrowLowF;
                            }
                            if (!isTransparent())
                            {
                                data.flickrData = await getBGInfo(data, true, true, 0);
                                //save flickr image so it doesn't have to be requested twice
                                BitmapImage flickrBG = new BitmapImage(data.flickrData.imageUri);
                                await createTileImage(data, flickrBG);
                            }
                            else
                            {
                                await createTileImage(data);
                            }

                            pushImageToMainTile(SAVE_LOC + smallTileName, SAVE_LOC + mediumTileName, SAVE_LOC + wideTileName, data.weather.tempCompare, current, today, tomorrow);
                        }
                    }
                }
            }
        }
        async private Task updateSecondaryTiles()
        {
            //finds the given secondary tile in the list of locations, then uses that to update the tile
            IReadOnlyCollection<SecondaryTile> tiles = await SecondaryTile.FindAllForPackageAsync();
            foreach (SecondaryTile tile in tiles)
            {
                Location tileLoc = findTile(tile.Arguments);
                await updateSecondaryTile(tile, tileLoc);
            }
        }
        async private Task updateSecondaryTile(SecondaryTile tile, Location tileLoc)
        {
            string name = tile.TileId;
            string smallTileName = name + "small.png";
            string mediumTileName = name + "med.png";
            string wideTileName = name + "wide.png";

            GetGeoposition pos = new GetGeoposition(tileLoc);
            GeoTemplate geoTemplate = await pos.getLocation(new TimeSpan(0, 0, 500), new TimeSpan(2, 0, 0));
            if (!geoTemplate.fail)
            {
                GetWundergroundData getWundData = tileLoc.IsCurrent ? new GetWundergroundData(WUND_API, geoTemplate.position.Position.Latitude, geoTemplate.position.Position.Longitude) : new GetWundergroundData(WUND_API, tileLoc.LocUrl);
                WeatherInfo weatherInfo = await getWundData.getConditions();
                if (!weatherInfo.fail)
                {
                    BackgroundTemplate data = new BackgroundTemplate()
                    {
                        medName = mediumTileName,
                        wideName = wideTileName,
                        weather = new BackgroundWeather()
                        {
                            conditions = weatherInfo.currentConditions,
                            tempCompare = weatherInfo.tomorrowShort + " tomorrow, and " + weatherInfo.tempCompareC.ToLowerInvariant() + " today",
                            high = weatherInfo.todayHighC,
                            low = weatherInfo.todayLowC,
                            currentTemp = weatherInfo.tempC.Split('.')[0] + "°",
                            todayForecast = weatherInfo.todayShort,
                        },
                        location = new BackgroundLoc()
                        {
                            location = weatherInfo.city,
                            lat = geoTemplate.position.Position.Latitude,
                            lon = geoTemplate.position.Position.Longitude,
                        }

                    };
                    string current = "Currently " + weatherInfo.currentConditions + ", " + weatherInfo.tempC + "°C";
                    string today = "Today: " + weatherInfo.todayShort + " " + weatherInfo.todayHighC + "/" + weatherInfo.todayLowC;
                    string tomorrow = "Tomorrow: " + weatherInfo.tomorrowShort + " " + weatherInfo.tomorrowHighC + "/" + weatherInfo.tomorrowLowC;
                    if (!unitsAreSI())
                    {
                        data.weather.high = weatherInfo.todayHighF;
                        data.weather.low = weatherInfo.todayLowF;
                        data.weather.currentTemp = weatherInfo.tempF.Split('.')[0] + "°";
                        data.weather.tempCompare = "Tomorrow will be " + weatherInfo.tempCompareF.ToLowerInvariant() + " today";
                        current = "Currently: " + weatherInfo.currentConditions + ", " + weatherInfo.tempF + "°F";
                        today = "Today: " + weatherInfo.todayShort + " " + weatherInfo.todayHighF + "/" + weatherInfo.todayLowF;
                        tomorrow = "Tomorrow: " + weatherInfo.tomorrowShort + " " + weatherInfo.tomorrowHighF + "/" + weatherInfo.tomorrowLowF;
                    }
                    if (!isTransparent())
                    {
                        data.flickrData = await getBGInfo(data, true, true, 0);
                        //save flickr image so it doesn't have to be requested twice
                        BitmapImage flickrBG = new BitmapImage(data.flickrData.imageUri);
                        await createTileImage(data, flickrBG);
                    }
                    else
                    {
                        await createTileImage(data);
                    }
                    pushImageToSecondaryTile(tile, SAVE_LOC + smallTileName, SAVE_LOC + mediumTileName, SAVE_LOC + wideTileName, data.weather.tempCompare, current, today, tomorrow);
                }
            }
        }

        //tile rendering
        async private Task<bool> createTileImage(BackgroundTemplate data, BitmapImage background = null)
        {
            //given lat, lon, and conditions, creates a tile image
            try
            {
                if (background != null)
                {
                    await renderTile(data, data.medName, TileSize.medium, background);
                    await renderTile(data, data.wideName, TileSize.wide, background);
                }
                else
                {
                    await renderTile(data, data.medName, TileSize.medium);
                    await renderTile(data, data.wideName, TileSize.wide);
                }
                //await renderTile(data, data.smallName, TileSize.small);
                return true;
            }
            catch
            {
                return false;
            }
        }
        async private Task renderTile(BackgroundTemplate data, string tileName, TileSize tileSize, BitmapImage background = null)
        {
            //given data, size, and a name, save a tile
            RenderTargetBitmap bm = new RenderTargetBitmap();
            UIElement g;
            if (background != null)
            {
                g = createImage(data, tileSize, background);
            }
            else
            {
                g = createImage(data, tileSize);
            }
            await bm.RenderAsync(g);
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

        #region tile ui design
        //creating grid of tile design
        private Grid createImage(BackgroundTemplate data, TileSize tileSize, BitmapImage background = null)
        {
            if (background != null)
            {
                if (tileSize == TileSize.medium)
                {
                    return createMediumTile(data, background);
                }
                else if (tileSize == TileSize.wide)
                {
                    return createWideTile(data, background);
                }
            }
            else
            {
                if (tileSize == TileSize.medium)
                {
                    return createMediumTile(data);
                }
                else if (tileSize == TileSize.wide)
                {
                    return createWideTile(data);
                }
            }
            return null;
        }
        private Grid createWideTile(BackgroundTemplate data, BitmapImage background = null)
        {
            Grid g;
            if (background != null)
            {
                g = (createBackgroundGrid(TileSize.wide, false, background));
                g.Children.Add(createWideDarkOverlay(data.flickrData.userName));
                g.Children.Add(createWideStackPanel(data, true));

            }
            else
            {
                g = (createBackgroundGrid(TileSize.wide, true));
                g.Children.Add(createWideStackPanel(data, true));

            }
            return g;
        }
        private Grid createMediumTile(BackgroundTemplate data, BitmapImage background = null)
        {
            Grid g;
            if (background != null)
            {
                g = createBackgroundGrid(TileSize.medium, false, background);
                g.Children.Add(createOverlay(data, false));
            }
            else
            {
                g = createBackgroundGrid(TileSize.medium, true);
                g.Children.Add(createOverlay(data, true));
            }
            return g;
        }
        private Grid createWideDarkOverlay(string artistName)
        {
            Grid g = new Grid() { Width = 310, Height = 150, Background = new SolidColorBrush(Colors.Black) { Opacity = .3 } };
            g.Children.Add(new TextBlock() { FontSize = 9, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Top, Text = "by " + artistName, Margin = new Thickness(0, 0, 3, 0) });
            return g;
        }
        private StackPanel createWideStackPanel(BackgroundTemplate data, bool transparent)
        {
            StackPanel s = new StackPanel() { Orientation = Orientation.Horizontal };
            s.Children.Add(createFirstHalf(data, transparent));
            s.Children.Add(createTomorrowBox(data.weather.tempCompare));
            return s;

        }
        private Grid createTomorrowBox(string compare)
        {
            Grid g = new Grid() { Width = 150, Height = 150, Margin = new Thickness(10, 0, 0, 0) };
            g.Children.Add(createTomorrowShortText(compare));
            return g;
        }
        private TextBlock createTomorrowShortText(string compare)
        {
            return new TextBlock() { Text = compare.ToUpper(), FontWeight = FontWeights.ExtraBold, FontSize = 15, TextWrapping = TextWrapping.WrapWholeWords, VerticalAlignment = VerticalAlignment.Center, TextAlignment = TextAlignment.Right, Margin = new Thickness(0, 0, 5, 0) };
        }
        private Grid createFirstHalf(BackgroundTemplate data, bool transparent)
        {
            Grid g = new Grid() { Height = 150, Width = 150 };
            g.Children.Add(createOverlay(data, transparent));
            return g;
        }
        private Grid createBackgroundGrid(TileSize tileSize, bool transparent, BitmapImage background = null)
        {
            Grid g = new Grid() { Background = new SolidColorBrush() { Color = Colors.Transparent } };
            g.Height = g.Width = 150;
            if (tileSize == TileSize.wide)
            {
                g.Width = 310;
            }
            if (!transparent)
            {
                g.Background = new ImageBrush() { ImageSource = background, Stretch = Stretch.UniformToFill };
            }
            return g;
        }
        private Grid createOverlay(BackgroundTemplate data, bool transparent)
        {
            Grid g = new Grid() { Height = 150, VerticalAlignment = VerticalAlignment.Center };
            g.Children.Add(createTimeTextBlock());
            if (!transparent)
            {
                g.Background = new SolidColorBrush(Colors.Black) { Opacity = .3 };
                g.Children.Add(createFlickrSource(data.flickrData.userName));
            }
            g.Children.Add(createDataStackPanel(data));
            g.Children.Add(createLocationTextBlock(data.location.location));
            return g;
        }
        private TextBlock createTimeTextBlock()
        {
            TextBlock t = new TextBlock() { FontSize = 9, HorizontalAlignment = HorizontalAlignment.Left, Text = DateTime.Now.ToString("h:mm tt"), Margin = new Thickness(3, 0, 0, 0) };
            return t;
        }
        private TextBlock createFlickrSource(string artistName)
        {
            TextBlock t = new TextBlock() { FontSize = 9, HorizontalAlignment = HorizontalAlignment.Right, Text = "by " + artistName, Margin = new Thickness(0, 0, 3, 0), MaxWidth = 100 };
            return t;
        }
        private StackPanel createDataStackPanel(BackgroundTemplate data)
        {
            StackPanel s = new StackPanel() { VerticalAlignment = VerticalAlignment.Center };
            s.Children.Add(createCenterGrid(data));
            return s;
        }
        private Grid createCenterGrid(BackgroundTemplate data)
        {
            Grid g = new Grid() { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(5, 0, 5, 0) };
            g.Children.Add(createCentralStackPanel(data));
            return g;
        }
        private StackPanel createCentralStackPanel(BackgroundTemplate data)
        {
            StackPanel s = new StackPanel() { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center, MaxWidth = 150 };
            s.Children.Add(createTempTextBlock(data.weather.currentTemp));
            s.Children.Add(createForecastStackPanel(data));
            return s;
        }
        private TextBlock createTempTextBlock(string temperature)
        {
            TextBlock t = new TextBlock() { Text = temperature, FontWeight = FontWeights.Thin, FontSize = 45, HorizontalAlignment = HorizontalAlignment.Right };
            return t;
        }
        private StackPanel createForecastStackPanel(BackgroundTemplate data)
        {
            StackPanel s = new StackPanel() { Orientation = Orientation.Vertical, VerticalAlignment = VerticalAlignment.Center, Width = 80, Margin = new Thickness(5, 0, 0, 0) };
            s.Children.Add(createConditionsTextBlock(data.weather.conditions));
            s.Children.Add(createHiLoTextBlock(data.weather.high, data.weather.low));
            return s;
        }
        private TextBlock createConditionsTextBlock(string conditions)
        {
            TextBlock t = new TextBlock() { Text = conditions.ToUpperInvariant(), FontWeight = FontWeights.ExtraBold, TextWrapping = TextWrapping.WrapWholeWords, FontSize = 13, VerticalAlignment = VerticalAlignment.Center };
            return t;
        }
        private TextBlock createHiLoTextBlock(string high, string low)
        {
            TextBlock t = new TextBlock() { Text = high + "/" + low, FontSize = 16, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Right, Width = 80 };
            return t;
        }
        private TextBlock createLocationTextBlock(string location)
        {
            TextBlock t = new TextBlock() { Text = location.ToUpper(), FontSize = 14, FontWeight = FontWeights.SemiBold, Margin = new Thickness(10, 0, 0, 5), VerticalAlignment = VerticalAlignment.Bottom };
            return t;
        }
        #endregion

        //push images to tiles
        private void pushImageToMainTile(string smallTileLoc, string mediumTileLoc, string wideTileLoc, string compare, string current, string today, string tomorrow)
        {
            //pushes the image to the tiles

            ITileSquare150x150PeekImageAndText04 mediumTile = TileContentFactory.CreateTileSquare150x150PeekImageAndText04();
            mediumTile.TextBodyWrap.Text = compare;
            mediumTile.Branding = TileBranding.None;
            mediumTile.Image.Alt = "altMed";
            mediumTile.Image.Src = mediumTileLoc;
            mediumTile.StrictValidation = true;

            ITileWide310x150PeekImageAndText02 wideTile = TileContentFactory.CreateTileWide310x150PeekImageAndText02();
            wideTile.TextBody1.Text = current;
            wideTile.TextBody2.Text = today;
            wideTile.TextBody3.Text = tomorrow;
            wideTile.Branding = TileBranding.None;
            wideTile.Image.Alt = "altWide";
            wideTile.Image.Src = wideTileLoc;
            wideTile.Square150x150Content = mediumTile;
            wideTile.StrictValidation = true;

            TileNotification wideNotif = wideTile.CreateNotification();
            TileUpdateManager.CreateTileUpdaterForApplication().Clear();
            TileUpdateManager.CreateTileUpdaterForApplication().Update(wideNotif);
        }
        private void pushImageToSecondaryTile(SecondaryTile tile, string smallTileLoc, string mediumTileLoc, string wideTileLoc, string tempCompare, string current, string today, string tomorrow)
        {
            if (SecondaryTile.Exists(tile.TileId))
            {
                ITileSquare150x150PeekImageAndText04 mediumTile = TileContentFactory.CreateTileSquare150x150PeekImageAndText04();
                mediumTile.TextBodyWrap.Text = tempCompare;
                mediumTile.Branding = TileBranding.None;
                mediumTile.Image.Alt = "altMed";
                mediumTile.Image.Src = mediumTileLoc;
                mediumTile.StrictValidation = true;

                ITileWide310x150PeekImageAndText02 wideTile = TileContentFactory.CreateTileWide310x150PeekImageAndText02();
                wideTile.TextBody1.Text = current;
                wideTile.TextBody2.Text = today;
                wideTile.TextBody3.Text = tomorrow;
                wideTile.Branding = TileBranding.None;
                wideTile.Image.Alt = "altWide";
                wideTile.Image.Src = wideTileLoc;
                wideTile.Square150x150Content = mediumTile;
                wideTile.StrictValidation = true;

                TileNotification wideNotif = wideTile.CreateNotification();
                TileUpdater tileUpdater = TileUpdateManager.CreateTileUpdaterForSecondaryTile(tile.TileId);
                tileUpdater.Clear();
                tileUpdater.Update(wideNotif);
            }
        }

        //helpers
        async private Task<BackgroundFlickr> getBGInfo(BackgroundTemplate data, bool useGroup, bool useLoc, int timesRun)
        {
            //gets a uri for a background image from flickr
            if (timesRun > 1)
            {
                return null;
            }
            GetFlickrInfo f = new GetFlickrInfo(FLICKR_API);
            FlickrData imgList = await f.getImages(getTags(data.weather.conditions), useGroup, useLoc, data.location.lat, data.location.lon);
            if (!imgList.fail && imgList.images.Count > 0)
            {
                Random r = new Random();
                int num = r.Next(imgList.images.Count);
                BackgroundFlickr bgFlickr = new BackgroundFlickr();
                FlickrImage img = imgList.images[num];
                bgFlickr.imageUri = f.getImageUri(img, GetFlickrInfo.ImageSize.medium800);
                bgFlickr.userId = img.owner;
                bgFlickr.userName = (await f.getUser(bgFlickr.userId)).userName;
                return bgFlickr;
            }
            else
            {
                return await getBGInfo(data, useGroup, false, timesRun++);
            }
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
        private bool unitsAreSI()
        {
            //determines whether tile units should be SI
            if (store.Values.ContainsKey(TILE_UNITS_ARE_SI))
            {
                return (bool)store.Values[TILE_UNITS_ARE_SI];
            }
            store.Values[TILE_UNITS_ARE_SI] = true;
            return true;
        }
        private Location findTile(string args)
        {
            //finds and returns the tile's location settings
            foreach (Location loc in locationList)
            {
                if (args == loc.LocUrl)
                {
                    return loc;
                }
            }
            return null;
        }
        private bool isTransparent()
        {
            //determines whether the tile should be transparent
            if (localStore.Values.ContainsKey(TRANSPARENT_TILE))
            {
                return (bool)(localStore.Values[TRANSPARENT_TILE]);
            }
            localStore.Values[TRANSPARENT_TILE] = true;
            return true;
        }
    }
}
