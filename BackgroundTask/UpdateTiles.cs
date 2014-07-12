using DataTemplates;
using FlickrInfo;
using LocationHelper;
using SerializerClass;
using StoreLabels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using TileCreatorProject;
using WeatherData;
using Windows.ApplicationModel.Background;
using Windows.Graphics.Display;
using Windows.Graphics.Imaging;
using Windows.Networking.Connectivity;
using Windows.Storage;
using Windows.UI.StartScreen;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media.Imaging;
using WundergroundData;


namespace BackgroundTask
{
    public sealed class UpdateTiles : XamlRenderingBackgroundTask
    {
        #region variables

        private static ObservableCollection<Location> locationList;
        ApplicationDataContainer store = ApplicationData.Current.RoamingSettings;
        private ApplicationDataContainer localStore = Windows.Storage.ApplicationData.Current.LocalSettings;
        #endregion

        #region registration
        public async static void Register(string name, uint mins)
        {
            if (IsTaskRegistered(name))
            {
                Unregister(name);
            }
            BackgroundAccessStatus result = await BackgroundExecutionManager.RequestAccessAsync();
            BackgroundTaskBuilder builder = new BackgroundTaskBuilder();
            builder.Name = name;
            builder.TaskEntryPoint = typeof(UpdateTiles).FullName;
            builder.SetTrigger(new TimeTrigger(mins, false));
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
            if (store.Values.ContainsKey(Values.LOC_STORE))
            {
                ObservableCollection<Location> list = (Serializer.get(Values.LOC_STORE, typeof(ObservableCollection<Location>), store) as ObservableCollection<Location>);
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
            if (allowedToUpdate())
            {
                await updateMainTile();
                await updateSecondaryTiles();
            }
        }

        private bool allowedToUpdate()
        {
            //determines whether tiles should update or not
            if (localStore.Values.ContainsKey(Values.UPDATE_ON_CELL))
            {
                if ((bool)localStore.Values[Values.UPDATE_ON_CELL])
                {
                    return true;
                }
                return isOnWifi();
            }
            else
            {
                localStore.Values[Values.UPDATE_ON_CELL] = true;
                return true;
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

                    GetGeoposition pos = new GetGeoposition(loc, allowedToAutoFind());
                    GeoTemplate geoTemplate = await pos.getLocation(new TimeSpan(0, 0, 3), new TimeSpan(0, 1, 0));
                    if (!geoTemplate.fail)
                    {
                        GetWundergroundData getWundData = loc.IsCurrent ? new GetWundergroundData(Values.getWundApi(), geoTemplate.position.Position.Latitude, geoTemplate.position.Position.Longitude) : new GetWundergroundData(Values.getWundApi(), loc.LocUrl);
                        WeatherInfo weatherInfo = await getWundData.getConditions();
                        if (!weatherInfo.fail)
                        {
                            BackgroundTemplate data = new BackgroundTemplate()
                            {
                                medName = mediumTileName,
                                wideName = wideTileName,
                                smallName = smallTileName,
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
                            TileGroup tiles = new TileGroup();
                            CreateTile creater = new CreateTile();
                            if (!isTransparent())
                            {
                                data.flickrData = await getBGInfo(data, true, true, 0);
                                //save flickr image so it doesn't have to be requested twice
                                BitmapImage flickrBG = new BitmapImage(data.flickrData.imageUri);
                                tiles = creater.createTileImages(data, ref flickrBG);
                            }
                            else
                            {
                                tiles = creater.createTileImages(data);
                            }
                            if (tiles != null)
                            {
                                await renderTile(tiles.smTile, data.smallName);
                                await renderTile(tiles.sqTile, data.medName);
                                await renderTile(tiles.wideTile, data.wideName);
                            }
                            creater.pushImageToMainTile(Values.SAVE_LOC + smallTileName, Values.SAVE_LOC + mediumTileName, Values.SAVE_LOC + wideTileName, data.weather.tempCompare, current, today, tomorrow);
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

            GetGeoposition pos = new GetGeoposition(tileLoc, allowedToAutoFind());
            GeoTemplate geoTemplate = await pos.getLocation(new TimeSpan(0, 0, 500), new TimeSpan(2, 0, 0));
            if (!geoTemplate.fail)
            {
                GetWundergroundData getWundData = tileLoc.IsCurrent ? new GetWundergroundData(Values.getWundApi(), geoTemplate.position.Position.Latitude, geoTemplate.position.Position.Longitude) : new GetWundergroundData(Values.getWundApi(), tileLoc.LocUrl);
                WeatherInfo weatherInfo = await getWundData.getConditions();
                if (!weatherInfo.fail)
                {
                    BackgroundTemplate data = new BackgroundTemplate()
                    {
                        medName = mediumTileName,
                        wideName = wideTileName,
                        smallName = smallTileName,
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
                    TileGroup tiles = new TileGroup();
                    CreateTile creater = new CreateTile();
                    if (!isTransparent())
                    {
                        data.flickrData = await getBGInfo(data, true, true, 0);
                        //save flickr image so it doesn't have to be requested twice
                        BitmapImage flickrBG = new BitmapImage(data.flickrData.imageUri);
                        tiles = creater.createTileImages(data, ref flickrBG);
                    }
                    else
                    {
                        tiles = creater.createTileImages(data);
                    }
                    if (tiles != null)
                    {
                        await renderTile(tiles.smTile, data.smallName);
                        await renderTile(tiles.sqTile, data.medName);
                        await renderTile(tiles.wideTile, data.wideName);
                    }
                    creater.pushImageToSecondaryTile(tile, Values.SAVE_LOC + smallTileName, Values.SAVE_LOC + mediumTileName, Values.SAVE_LOC + wideTileName, data.weather.tempCompare, current, today, tomorrow);
                }
            }
        }

        //rending tiles to images
        async private Task renderTile(UIElement tile, string tileName)
        {
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

        //helpers
        async private Task<BackgroundFlickr> getBGInfo(BackgroundTemplate data, bool useGroup, bool useLoc, int timesRun)
        {
            //gets a uri for a background image from flickr
            if (timesRun > 1)
            {
                return null;
            }
            GetFlickrInfo f = new GetFlickrInfo(Values.FLICKR_API);
            FlickrData imgList = await f.getImages(GetFlickrInfo.getTags(data.weather.conditions), useGroup, useLoc, data.location.lat, data.location.lon);
            if (!imgList.fail && imgList.images.Count > 0)
            {
                Random r = new Random();
                int num = r.Next(imgList.images.Count);
                BackgroundFlickr bgFlickr = new BackgroundFlickr();
                FlickrImageData img = imgList.images[num];
                bgFlickr.imageUri = f.getImageUri(img, GetFlickrInfo.ImageSize.medium640);
                bgFlickr.userId = img.owner;
                bgFlickr.userName = (await f.getUser(bgFlickr.userId)).userName;
                return bgFlickr;
            }
            else
            {
                return await getBGInfo(data, useGroup, false, timesRun++);
            }
        }
        private bool unitsAreSI()
        {
            //determines whether tile units should be SI
            if (store.Values.ContainsKey(Values.TILE_UNITS_ARE_SI))
            {
                return (bool)store.Values[Values.TILE_UNITS_ARE_SI];
            }
            store.Values[Values.TILE_UNITS_ARE_SI] = true;
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
            if (localStore.Values.ContainsKey(Values.TRANSPARENT_TILE))
            {
                return (bool)(localStore.Values[Values.TRANSPARENT_TILE]);
            }
            localStore.Values[Values.TRANSPARENT_TILE] = true;
            return true;
        }
        private bool isOnWifi()
        {
            //checks whether phone is on wifi
            ConnectionProfile prof = NetworkInformation.GetInternetConnectionProfile();
            return prof.IsWlanConnectionProfile;
        }
    }
}