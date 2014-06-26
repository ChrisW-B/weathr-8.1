using DataTemplates;
using FlickrInfo;
using LocationHelper;
using NotificationsExtensions.TileContent;
using SerializerClass;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        private const string TASK_NAME = "Weathr Tile Updater";
        private const string WUND_API = "fb1dd3f4321d048d";
        private const string FLICKR_API = "2781c025a4064160fc77a52739b552ff";
        private const string LOC_STORE = "locList";
        private const string SAVE_LOC = "ms-appdata:///local/";
        private static ObservableCollection<Location> locationList;
        ApplicationDataContainer store = ApplicationData.Current.RoamingSettings;
        private ApplicationDataContainer localStore = Windows.Storage.ApplicationData.Current.LocalSettings;
        private enum TileSize
        {
            small,
            medium,
            wide
        }


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
            // await updateSecondaryTiles();
        }

        //updating the main tile
        async private Task updateMainTile()
        {
            foreach (Location loc in locationList)
            {
                if (loc.IsDefault)
                {
                    string name = "default";
                    string smallTileName = name + "small.png";
                    string mediumTileName = name + "med.png";
                    string wideTileName = name + "wide.png";

                    GetGeoposition pos = new GetGeoposition(loc);
                    GeoTemplate geoTemplate = await pos.getLocation();
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
                                    tempCompare = "Tomorrow will be " + weatherInfo.tempCompareC.ToLowerInvariant() + " today",
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
                            data.flickrData= await getBGInfo(data, true, true, 0);
                            await createTileImage(data);

                            pushImageToMainTile(SAVE_LOC + smallTileName, SAVE_LOC + mediumTileName, SAVE_LOC + wideTileName, data.weather.tempCompare, current, today, tomorrow);
                        }
                    }
                }
            }
        }

        private bool unitsAreSI()
        {
            return true;
        }

        private Location findTile(string args)
        {
            foreach (Location loc in locationList)
            {
                if (args == loc.LocUrl)
                {
                    return loc;
                }
            }
            return null;
        }

        //tile rendering
        async private Task<bool> createTileImage(BackgroundTemplate data)
        {
            //given lat, lon, and conditions, creates a tile image
            try
            {
                await renderTile(data, data.medName, TileSize.medium);
                await renderTile(data, data.wideName, TileSize.wide);
                //await renderTile(data, data.smallName, TileSize.small);
                return true;
            }
            catch
            {
                return false;
            }
        }
        async private Task renderTile(BackgroundTemplate data, string tileName, TileSize tileSize)
        {
            //given a image uri
            RenderTargetBitmap bm = new RenderTargetBitmap();
            UIElement g = createImage(data, tileSize);
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
        private UIElement createImage(BackgroundTemplate data, TileSize tileSize)
        {
            bool transparent = isTransparent();
            Grid g = createBackgroundGrid(data.flickrData.imageUri, tileSize, transparent);
            g.Children.Add(createOverlay(tileSize, transparent, data));
            return g;
        }

        private bool isTransparent()
        {
            if (localStore.Values.ContainsKey("flickrTile"))
            {
                return !(bool)(localStore.Values["flickrTile"]);
            }
            localStore.Values["flickrTile"] = true;
            return false;
        }

        private Grid createBackgroundGrid(Uri imageUri, TileSize tileSize, bool transparent)
        {
            Grid g = new Grid() { Background = new SolidColorBrush() { Color = Colors.Transparent } };
            g.Height = g.Width = 150;
            if (!transparent)
            {
                BitmapImage background = new BitmapImage(imageUri);
                g.Background = new ImageBrush() { ImageSource = background, Stretch = Stretch.UniformToFill };
            }
            return g;
        }
        private Grid createOverlay(TileSize tileSize, bool transparent, BackgroundTemplate data)
        {
            Grid g = new Grid() { Height = 150, VerticalAlignment = VerticalAlignment.Center };
            if (!transparent)
            {
                g.Background = new SolidColorBrush(Colors.Black) { Opacity = .3 };
            }
            g.Children.Add(createTimeTextBlock());
            g.Children.Add(createFlickrSource(data.flickrData.userName));
            g.Children.Add(createDataStackPanel(data));
            g.Children.Add(createLocationTextBlock(data.location.location));
            return g;
        }
        private TextBlock createTimeTextBlock()
        {
            TextBlock t = new TextBlock() { FontSize = 9, HorizontalAlignment = HorizontalAlignment.Left, Text = DateTime.Now.ToString("h:mm tt"), Margin = new Thickness(3,0,0,0) };
            return t;
        }
        private TextBlock createFlickrSource(string artistName)
        {
            TextBlock t = new TextBlock() { FontSize = 9, HorizontalAlignment = HorizontalAlignment.Right, Text = "by " + artistName, Margin = new Thickness(0,0,3,0), MaxWidth = 100 };
            return t;
        }
        private StackPanel createDataStackPanel(BackgroundTemplate data)
        {
            StackPanel s = new StackPanel() { VerticalAlignment = VerticalAlignment.Center };
            s.Children.Add(createCenterGrid(data));
            s.Children.Add(createTodayTextBlock(data.weather.todayForecast));
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
            StackPanel s = new StackPanel() { Orientation = Orientation.Horizontal, MaxWidth=150 };
            s.Children.Add(createTempTextBlock(data.weather.currentTemp));
            s.Children.Add(createForecastStackPanel(data));
            return s;
        }
        private TextBlock createTempTextBlock(string temperature)
        {
            TextBlock t = new TextBlock() { Text = temperature, FontWeight = FontWeights.Thin, FontSize = 40, HorizontalAlignment = HorizontalAlignment.Right};
            return t;
        }
        private StackPanel createForecastStackPanel(BackgroundTemplate data)
        {
            StackPanel s = new StackPanel() { Orientation = Orientation.Vertical, Width = 80 };
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
            TextBlock t = new TextBlock() { Text = high + "/" + low, FontSize = 16, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Right, Width=80 };
            return t;
        }
        private UIElement createTodayTextBlock(string forecast)
        {
            TextBlock t = new TextBlock() { Text = forecast + " today", FontWeight =  FontWeights.Bold, HorizontalAlignment = HorizontalAlignment.Center, FontSize= 14, TextWrapping = Windows.UI.Xaml.TextWrapping.WrapWholeWords, Margin = new Thickness(0, 15, 0, 0) };
            return t;

        }
        private TextBlock createLocationTextBlock(string location)
        {
            TextBlock t = new TextBlock() { Text = location, FontSize = 17, FontWeight = FontWeights.Medium, Margin = new Thickness(10, 0, 0, 5), VerticalAlignment = VerticalAlignment.Bottom };
            return t;
        }


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

        //getting flickr images for background
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

        //helpers
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
        private string getWeatherLogo(string conditions)
        {
            conditions = conditions.ToLowerInvariant();
            if (conditions.Contains("thunder") || conditions.Contains("storm"))
            {
                return "Thunder170";
            }
            else if (conditions.Contains("overcast"))
            {
                return "Cloudy170";
            }
            else if (conditions.Contains("shower") || conditions.Contains("drizzle") || conditions.Contains("light rain"))
            {
                return "Drizzle170";
            }
            else if (conditions.Contains("flurry") || conditions.Contains("snow shower") || conditions.Contains("light snow"))
            {
                return "Flurry170";
            }
            else if (conditions.Contains("fog") || conditions.Contains("mist") || conditions.Contains("haz"))
            {
                return "Fog170";
            }
            else if (conditions.Contains("freezing"))
            {
                return "FreezingRain170";
            }
            else if (conditions.Contains("cloudy") || conditions.Contains("partly") || conditions.Contains("mostly") || conditions.Contains("clouds"))
            {
                return "PartlyCloudy170";
            }
            else if (conditions.Contains("rain"))
            {
                return "Rain170";
            }
            else if (conditions.Contains("sleet") || conditions.Contains("pellet"))
            {
                return "Sleet170";
            }
            else if (conditions.Contains("snow") || conditions.Contains("blizzard"))
            {
                return "Snow170";
            }
            else if (conditions.Contains("sun") || conditions.Contains("sunny") || conditions.Contains("clear"))
            {
                return "Sunny170";
            }
            else if (conditions.Contains("wind"))
            {
                return "Windy170";
            }
            return "SunCloudTrans170";
        }
    }
}
