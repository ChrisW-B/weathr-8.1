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
using Windows.Graphics.Display;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Notifications;
using Windows.UI.StartScreen;
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
        private enum tileSize
        {
            medium,
            large
        }
        ApplicationDataContainer store = ApplicationData.Current.RoamingSettings;

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
                                lat = geoTemplate.position.Position.Latitude,
                                lon = geoTemplate.position.Position.Longitude,
                                conditions = weatherInfo.currentConditions,
                                tempCompare = "Tomorrow will be  " + weatherInfo.tempCompareC.ToLowerInvariant() + " today",
                                high = weatherInfo.todayHighC,
                                low = weatherInfo.todayLowC,
                                currentTemp = weatherInfo.tempC,
                                medName = mediumTileName,
                                wideName = wideTileName
                            };
                            if (!unitsAreSI())
                            {
                                data.high = weatherInfo.todayHighF;
                                data.low = weatherInfo.todayLowF;
                                data.currentTemp = weatherInfo.tempF;
                                data.tempCompare = "Tomorrow will be " + weatherInfo.tempCompareF.ToLowerInvariant() + " today";
                            }
                            data.imageUri = await getBGUri(data, true, true, 0);
                            await createTileImage(data);
                            pushImageToMainTile(SAVE_LOC + mediumTileName, SAVE_LOC + wideTileName, data.tempCompare);
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
                await renderTile(data, data.medName, tileSize.medium);
                await renderTile(data, data.wideName, tileSize.large);
                return true;
            }
            catch
            {
                return false;
            }
        }
        async private Task renderTile(BackgroundTemplate data, string tileName, tileSize tileSize)
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
        private UIElement createImage(BackgroundTemplate data, tileSize tileSize)
        {
            Grid g = new Grid();
            StackPanel s = new StackPanel() { Orientation = Orientation.Horizontal, Height = 150, Width = 150, HorizontalAlignment = HorizontalAlignment.Center };
            s.Background = new SolidColorBrush() { Color = Colors.Black, Opacity = .3 };
            BitmapImage icon = new BitmapImage(new Uri("ms-appx:///Assets/Icons/Medium/" + getWeatherLogo(data.conditions) + ".png"));
            Image weatherIcon = new Image() { Source = icon, Width = 50, Height = 50, Margin = new Thickness(10, 0, 10, 0) };
            TextBlock temp = new TextBlock() { Text = data.currentTemp + "°", FontSize = 30 };
            temp.VerticalAlignment = VerticalAlignment.Center;
            weatherIcon.VerticalAlignment = VerticalAlignment.Center;
            BitmapImage background = new BitmapImage(data.imageUri);
            g.Height = g.Width = 150;
            if (tileSize == UpdateTiles.tileSize.large)
            {
                g.Width = 310;
                s.Width = 310;
            }
            g.Background = new ImageBrush() { ImageSource = background, Stretch = Stretch.UniformToFill };

            s.Children.Add(weatherIcon);
            s.Children.Add(temp);
            g.Children.Add(s);
            return g;
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
                //uh... what happened to it
            }
            else if (conditions.Contains("wind"))
            {
                return "Windy170";
            }
            return "SunCloudTrans170";
        }


        private void pushImageToMainTile(string mediumTileLoc, string wideTileLoc, string tileBack)
        {
            //pushes the image to the tiles
            ITileSquare150x150PeekImageAndText04 mediumTile = TileContentFactory.CreateTileSquare150x150PeekImageAndText04();
            mediumTile.TextBodyWrap.Text = tileBack;
            mediumTile.Branding = TileBranding.None;
            mediumTile.Image.Alt = "altMed";
            mediumTile.Image.Src = mediumTileLoc;
            mediumTile.StrictValidation = true;

            ITileWide310x150PeekImage03 wideTile = TileContentFactory.CreateTileWide310x150PeekImage03();
            wideTile.TextHeadingWrap.Text = tileBack;
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
        async private Task<Uri> getBGUri(BackgroundTemplate data, bool useGroup, bool useLoc, int timesRun)
        {
            //gets a uri for a background image from flickr
            if (timesRun > 1)
            {
                return null;
            }
            GetFlickrInfo f = new GetFlickrInfo(FLICKR_API);
            FlickrData imgList = await f.getImages(getTags(data.conditions), useGroup, useLoc, data.lat, data.lon);
            if (!imgList.fail && imgList.images.Count > 0)
            {
                Random r = new Random();
                int num = r.Next(imgList.images.Count);
                return f.getImageUri(imgList.images[num], GetFlickrInfo.ImageSize.medium800);
            }
            else
            {
                return await getBGUri(data, useGroup, false, timesRun++);
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
    }
}
