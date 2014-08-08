using DataTemplates;
using StoreLabels;
using System;
using System.Xml.Linq;
using WeatherData;
using Windows.Data.Xml.Dom;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Notifications;
using Windows.UI.StartScreen;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace TileCreater
{
    

    public static class CreateTile
    {
        private static ApplicationDataContainer store = ApplicationData.Current.RoamingSettings;
        private static ApplicationDataContainer localStore = Windows.Storage.ApplicationData.Current.LocalSettings;

        //push images to tiles
        public static void pushImageToTile(string smallTileLoc, string mediumTileLoc, string wideTileLoc, string compare, string current, string today, string tomorrow, SecondaryTile tile = null)
        {
            //pushes the image to the tiles
            if (tile != null)
            {
                if (!SecondaryTile.Exists(tile.TileId))
                {
                    return;
                }
            }

            string tileXml =
                "<tile>"
               + "<visual version='3' branding='none'>"
               + "<binding template='TileSquare71x71Image'>"
               + "<image id='1' src='" + smallTileLoc + "' alt='altSmall'/>"
               + "</binding>"
               + "<binding template='TileSquare150x150PeekImageAndText04' fallback='TileSquarePeekImageAndText04'>"
               + "<image id='1' src='" + mediumTileLoc + "' alt='altMed'/>"
               + "<text id='1'>" + compare + "</text>"
               + "</binding>"
               + "<binding template='TileWide310x150PeekImageAndText02' fallback='TileWidePeekImageAndText02'>"
               + "<image id='1' src='" + wideTileLoc + "' alt='altWide'/>"
               + "<text id='1'>" + current + "</text>"
               + "<text id='2'>" + today + "</text>"
               + "<text id='3'>" + tomorrow + "</text>"
               + "</binding>"
               + "</visual>"
               + "</tile>";

         XmlDocument xml = new XmlDocument();
            xml.LoadXml(tileXml);


                TileNotification tileNotif = new TileNotification(xml);
                try
                {
                    if (tile != null)
                    {
                        TileUpdater updater = TileUpdateManager.CreateTileUpdaterForSecondaryTile(tile.TileId);
                        updater.Clear();
                        updater.Update(tileNotif);
                    }
                    else
                    {
                        TileUpdateManager.CreateTileUpdaterForApplication().Clear();
                        TileUpdateManager.CreateTileUpdaterForApplication().Update(tileNotif);
                    }
                }
                catch (Exception e)
                {

                }
        }
        //gets set of tile images based on parameters
        public static TileGroup createTileWithParams(WeatherInfo weatherInfo, int numAlerts = 0, ImageBrush background = null, string artistName = null)
        {
            if (!weatherInfo.fail)
            {
                BackgroundTemplate data = new BackgroundTemplate()
                {
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
                    },
                    flickrData = new BackgroundFlickr()
                };
                if (!unitsAreSI())
                {
                    data.weather.high = weatherInfo.todayHighF;
                    data.weather.low = weatherInfo.todayLowF;
                    data.weather.currentTemp = weatherInfo.tempF.Split('.')[0] + "°";
                    data.weather.tempCompare = "Tomorrow will be " + weatherInfo.tempCompareF.ToLowerInvariant() + " today";
                }
                if (background != null && !isTransparent())
                {
                    ImageBrush newBg = new ImageBrush();
                    newBg.ImageSource = background.ImageSource;
                    newBg.Opacity = 1;
                    newBg.Stretch = Stretch.UniformToFill;
                    if (artistName != null)
                    {
                        data.flickrData.userName = artistName;
                    }
                    return createTileImages(data, numAlerts, ref newBg);
                }
                return createTileImages(data, numAlerts);
            }
            return null;
        }

        //tile rendering
        private static TileGroup createTileImages(BackgroundTemplate data, int numAlerts, ref ImageBrush background)
        {
            //given lat, lon, and conditions, creates a tile image
            try
            {
                TileGroup group = new TileGroup();
                group.sqTile = createImage(data, LiveTileSize.medium, numAlerts, ref background);
                group.wideTile = createImage(data, LiveTileSize.wide, numAlerts, ref background);
                group.smTile = createImage(data, LiveTileSize.small, numAlerts, ref background);

                return group;
            }
            catch
            {
                return null;
            }
        }
        private static TileGroup createTileImages(BackgroundTemplate data, int numAlerts)
        {
            //given lat, lon, and conditions, creates a tile image
            try
            {
                TileGroup group = new TileGroup();
                group.sqTile = createImage(data, numAlerts, LiveTileSize.medium);
                group.wideTile = createImage(data, numAlerts, LiveTileSize.wide);
                group.smTile = createImage(data, numAlerts, LiveTileSize.small);
                return group;
            }
            catch
            {
                return null;
            }
        }

        #region tile ui design
        //creating grid of tile design
        private static Grid createImage(BackgroundTemplate data, LiveTileSize tileSize, int numAlerts, ref ImageBrush background)
        {
            if (background != null)
            {
                if (tileSize == LiveTileSize.medium)
                {
                    return createMediumTile(data, numAlerts, ref background);
                }
                else if (tileSize == LiveTileSize.wide)
                {
                    return createWideTile(data, numAlerts, ref background);
                }
                else if (tileSize == LiveTileSize.small)
                {
                    return createSmallTile(data.weather.currentTemp, data.weather.conditions, ref background);
                }
            }
            else
            {
                if (tileSize == LiveTileSize.medium)
                {
                    return createMediumTile(data, numAlerts);
                }
                else if (tileSize == LiveTileSize.wide)
                {
                    return createWideTile(data, numAlerts);
                }
                else if (tileSize == LiveTileSize.small)
                {
                    return createSmallTile(data.weather.currentTemp, data.weather.conditions);
                }
            }
            return null;
        }
        private static Grid createImage(BackgroundTemplate data, int numAlerts, LiveTileSize tileSize)
        {
            if (tileSize == LiveTileSize.small)
            {
                return createSmallTile(data.weather.currentTemp, data.weather.conditions);
            }
            else if (tileSize == LiveTileSize.medium)
            {
                return createMediumTile(data, numAlerts);
            }
            else if (tileSize == LiveTileSize.wide)
            {
                return createWideTile(data, numAlerts);
            }
            return null;
        }
        private static Grid createSmallTile(string temp, string conditions, ref ImageBrush background)
        {
            Grid g = createBackgroundGrid(LiveTileSize.small, false, ref background);
            g.Children.Add(createSmallOverlay(temp, conditions, true));
            return g;
        }
        private static Grid createSmallTile(string temp, string conditions)
        {
            Grid g = createBackgroundGrid(LiveTileSize.small, true);
            g.Children.Add(createSmallOverlay(temp, conditions, false));
            return g;
        }
        private static Grid createSmallOverlay(string temp, string conditions, bool hasBackground)
        {
            Grid g = new Grid();
            if (hasBackground)
            {
                g.Background = new SolidColorBrush(Colors.Black) { Opacity = .3 };
            }
            StackPanel s = new StackPanel() { Margin = new Thickness(5, 5, 5, 5) };
            s.Children.Add(createSmallTempText(temp));
            s.Children.Add(createSmallConditions(conditions));
            g.Children.Add(s);
            return g;
        }
        private static TextBlock createSmallConditions(string conditions)
        {
            return new TextBlock() { IsTextScaleFactorEnabled = false, Text = conditions.ToUpper(), FontSize = 10, FontWeight = FontWeights.ExtraBold, VerticalAlignment = VerticalAlignment.Bottom, HorizontalAlignment = Windows.UI.Xaml.HorizontalAlignment.Right, TextTrimming = TextTrimming.Clip, TextWrapping = TextWrapping.WrapWholeWords };
        }
        private static TextBlock createSmallTempText(string temp)
        {
            return new TextBlock() { IsTextScaleFactorEnabled = false, Text = temp, FontSize = 25, FontWeight = FontWeights.Thin, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Center };
        }
        private static Grid createWideTile(BackgroundTemplate data, int numAlerts, ref ImageBrush background)
        {
            Grid g;
            if (background != null)
            {
                g = (createBackgroundGrid(LiveTileSize.wide, false, ref background));
                if (data.flickrData.userName != null)
                {
                    g.Children.Add(createWideDarkOverlay(data.flickrData.userName));
                }
                else
                {
                    g.Children.Add(createWideDarkOverlay());
                }
                g.Children.Add(createWideStackPanel(data, numAlerts, true));
            }
            else
            {
                g = (createBackgroundGrid(LiveTileSize.wide, true));
                g.Children.Add(createWideStackPanel(data, numAlerts, true));
            }
            return g;
        }
        private static UIElement createAlertsGrid(int numAlerts)
        {
            Grid g = new Grid() { Height = 150, Width = 150, Background = new SolidColorBrush(Colors.Transparent) };
            TextBlock t = new TextBlock() { Height = 150, TextAlignment = TextAlignment.Right, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(0, 15, 15, 0), FontSize = 20, IsColorFontEnabled = true, Foreground = new SolidColorBrush(Colors.Red) };
            if (numAlerts > 0)
            {
                t.Text = numAlerts + "!";
            }
            g.Children.Add(t);
            return g;
        }
        private static Grid createWideTile(BackgroundTemplate data, int numAlerts)
        {
            Grid g;

            g = (createBackgroundGrid(LiveTileSize.wide, true));
            g.Children.Add(createWideStackPanel(data, numAlerts, true));
            return g;
        }
        private static Grid createMediumTile(BackgroundTemplate data, int numAlerts, ref ImageBrush background)
        {
            Grid g;
            g = createBackgroundGrid(LiveTileSize.medium, false, ref background);
            g.Children.Add(createOverlay(data, numAlerts, false));
            return g;
        }
        private static Grid createMediumTile(BackgroundTemplate data, int numAlerts)
        {
            Grid g;
            g = createBackgroundGrid(LiveTileSize.medium, true);
            g.Children.Add(createOverlay(data, numAlerts, true));
            return g;
        }
        private static Grid createWideDarkOverlay(string artistName = null)
        {
            Grid g = new Grid() { Width = 310, Height = 150, Background = new SolidColorBrush(Colors.Black) { Opacity = .3 } };
            if (artistName != null)
            {
                g.Children.Add(new TextBlock() { IsTextScaleFactorEnabled = false, FontSize = 9, FontWeight = FontWeights.Bold, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Top, Text = "by " + artistName, Margin = new Thickness(0, 0, 3, 0) });
            }
            return g;
        }
        private static StackPanel createWideStackPanel(BackgroundTemplate data, int numAlerts, bool transparent)
        {
            StackPanel s = new StackPanel() { Orientation = Orientation.Horizontal };
            s.Children.Add(createFirstHalf(data, numAlerts, transparent));
            s.Children.Add(createTomorrowBox(data.weather.tempCompare));
            return s;

        }
        private static Grid createTomorrowBox(string compare)
        {
            Grid g = new Grid() { Width = 150, Height = 150, Margin = new Thickness(10, 0, 0, 0) };
            g.Children.Add(createTomorrowShortText(compare));
            return g;
        }
        private static TextBlock createTomorrowShortText(string compare)
        {
            return new TextBlock() { IsTextScaleFactorEnabled = false, Text = compare.ToUpper(), FontWeight = FontWeights.ExtraBold, FontSize = 15, TextWrapping = TextWrapping.WrapWholeWords, VerticalAlignment = VerticalAlignment.Center, TextAlignment = TextAlignment.Right, Margin = new Thickness(0, 0, 5, 0) };
        }
        private static Grid createFirstHalf(BackgroundTemplate data, int numAlerts, bool transparent)
        {
            Grid g = new Grid() { Height = 150, Width = 150 };
            g.Children.Add(createOverlay(data, numAlerts, transparent));
            return g;
        }
        private static Grid createBackgroundGrid(LiveTileSize tileSize, bool transparent, ref ImageBrush background)
        {
            Grid g = new Grid() { Background = new SolidColorBrush() { Color = Colors.Transparent } };
            g.Height = g.Width = 150;
            if (tileSize == LiveTileSize.wide)
            {
                g.Width = 310;
            }
            else if (tileSize == LiveTileSize.small)
            {
                g.Height = g.Width = 71;
            }
            if (!transparent)
            {
                g.Background = background;
            }
            return g;
        }
        private static Grid createBackgroundGrid(LiveTileSize tileSize, bool transparent)
        {
            Grid g = new Grid() { Background = new SolidColorBrush() { Color = Colors.Transparent } };
            g.Height = g.Width = 150;
            if (tileSize == LiveTileSize.wide)
            {
                g.Width = 310;
            }
            else if (tileSize == LiveTileSize.small)
            {
                g.Height = g.Width = 71;
            }
            return g;
        }
        private static Grid createOverlay(BackgroundTemplate data, int numAlerts, bool transparent)
        {
            Grid g = new Grid() { Height = 150, VerticalAlignment = VerticalAlignment.Center };
            g.Children.Add(createTimeTextBlock());
            if (!transparent)
            {
                g.Background = new SolidColorBrush(Colors.Black) { Opacity = .3 };
                if (data.flickrData.userName != null)
                {
                    g.Children.Add(createFlickrSource(data.flickrData.userName));
                }
            }
            g.Children.Add(createDataStackPanel(data));
            g.Children.Add(createLocationTextBlock(data.location.location));
            g.Children.Add(createAlertsGrid(numAlerts));
            return g;
        }
        private static TextBlock createTimeTextBlock()
        {
            string time = "";
            if (twentyFourHrTime())
            {
                time = DateTime.Now.ToString("HH:mm");
            }
            else
            {
                time = DateTime.Now.ToString("h:mm tt");
            }
            TextBlock t = new TextBlock() { IsTextScaleFactorEnabled = false, FontSize = 9, HorizontalAlignment = HorizontalAlignment.Left, Text = time, Margin = new Thickness(3, 0, 0, 0), FontWeight = FontWeights.Bold };
            return t;
        }


        private static TextBlock createFlickrSource(string artistName)
        {
            TextBlock t = new TextBlock() { IsTextScaleFactorEnabled = false, FontSize = 9, HorizontalAlignment = HorizontalAlignment.Right, Text = "by " + artistName, Margin = new Thickness(0, 0, 3, 0), MaxWidth = 90, FontWeight = FontWeights.Bold };
            return t;
        }
        private static StackPanel createDataStackPanel(BackgroundTemplate data)
        {
            StackPanel s = new StackPanel() { VerticalAlignment = VerticalAlignment.Center };
            s.Children.Add(createCenterGrid(data));
            return s;
        }
        private static Grid createCenterGrid(BackgroundTemplate data)
        {
            Grid g = new Grid() { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(2, 0, 2, 0) };
            g.Children.Add(createCentralStackPanel(data));
            return g;
        }
        private static StackPanel createCentralStackPanel(BackgroundTemplate data)
        {
            StackPanel s = new StackPanel() { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center, MaxWidth = 150 };
            s.Children.Add(createTempTextBlock(data.weather.currentTemp));
            s.Children.Add(createForecastStackPanel(data));
            return s;
        }
        private static TextBlock createTempTextBlock(string temperature)
        {
            TextBlock t = new TextBlock() { IsTextScaleFactorEnabled = false, Text = temperature, FontWeight = FontWeights.Thin, FontSize = 45, CharacterSpacing = -60, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 0, 3, 0) };
            return t;
        }
        private static StackPanel createForecastStackPanel(BackgroundTemplate data)
        {
            StackPanel s = new StackPanel() { Orientation = Orientation.Vertical, VerticalAlignment = VerticalAlignment.Center, Width = 80 };
            s.Children.Add(createConditionsTextBlock(data.weather.conditions));
            s.Children.Add(createHiLoTextBlock(data.weather.high, data.weather.low));
            return s;
        }
        private static TextBlock createConditionsTextBlock(string conditions)
        {
            TextBlock t = new TextBlock() { IsTextScaleFactorEnabled = false, Text = conditions.ToUpperInvariant(), FontWeight = FontWeights.ExtraBold, TextWrapping = TextWrapping.WrapWholeWords, FontSize = 13, VerticalAlignment = VerticalAlignment.Center };
            return t;
        }
        private static TextBlock createHiLoTextBlock(string high, string low)
        {
            TextBlock t = new TextBlock() { IsTextScaleFactorEnabled = false, Text = high + "/" + low, FontSize = 16, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Right, Width = 80 };
            return t;
        }
        private static TextBlock createLocationTextBlock(string location)
        {
            TextBlock t = new TextBlock() { IsTextScaleFactorEnabled = false, Text = location.ToUpper(), FontSize = 14, FontWeight = FontWeights.SemiBold, Margin = new Thickness(10, 0, 0, 5), VerticalAlignment = VerticalAlignment.Bottom };
            return t;
        }
        #endregion

        //helper methods
        public static bool unitsAreSI()
        {
            //determines whether tile units should be SI
            if (store.Values.ContainsKey(Values.TILE_UNITS_ARE_SI))
            {
                return (bool)store.Values[Values.TILE_UNITS_ARE_SI];
            }
            store.Values[Values.TILE_UNITS_ARE_SI] = true;
            return true;
        }
        private static bool isTransparent()
        {
            //determines whether the tile should be transparent
            if (localStore.Values.ContainsKey(Values.TRANSPARENT_TILE))
            {
                return (bool)(localStore.Values[Values.TRANSPARENT_TILE]);
            }
            localStore.Values[Values.TRANSPARENT_TILE] = true;
            return true;
        }
        private static bool twentyFourHrTime()
        {
            if (store.Values.ContainsKey(Values.TWENTY_FOUR_HR_TIME))
            {
                return (bool)store.Values[Values.TWENTY_FOUR_HR_TIME];
            }
            store.Values[Values.TWENTY_FOUR_HR_TIME] = false;
            return false;
        }
    }
}