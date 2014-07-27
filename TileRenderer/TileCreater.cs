using DataTemplates;
using NotificationsExtensions.TileContent;
using StoreLabels;
using System;
using System.Threading.Tasks;
using WeatherData;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Notifications;
using Windows.UI.StartScreen;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace TileCreater
{
    public class CreateTile
    {
        private ApplicationDataContainer store = ApplicationData.Current.RoamingSettings;
        private ApplicationDataContainer localStore = Windows.Storage.ApplicationData.Current.LocalSettings;

        //push images to tiles
        public void pushImageToTile(string smallTileLoc, string mediumTileLoc, string wideTileLoc, string compare, string current, string today, string tomorrow, SecondaryTile tile = null)
        {
            //pushes the image to the tiles
            if (tile != null)
            {
                if (!SecondaryTile.Exists(tile.TileId))
                {
                    return;
                }
            }
            ITileSquare71x71IconWithBadge smallTile = TileContentFactory.CreateTileSquare71x71IconWithBadge();
            smallTile.ImageIcon.Src = smallTileLoc;
            smallTile.ImageIcon.Alt = "altSmall";
            smallTile.Branding = TileBranding.Name;


            ITileSquare150x150PeekImageAndText04 mediumTile = TileContentFactory.CreateTileSquare150x150PeekImageAndText04();
            mediumTile.TextBodyWrap.Text = compare;
            mediumTile.Branding = TileBranding.None;
            mediumTile.Image.Alt = "altMed";
            mediumTile.Image.Src = mediumTileLoc;
            mediumTile.Square71x71Content = smallTile;

            ITileWide310x150PeekImageAndText02 wideTile = TileContentFactory.CreateTileWide310x150PeekImageAndText02();
            wideTile.TextBody1.Text = current;
            wideTile.TextBody2.Text = today;
            wideTile.TextBody3.Text = tomorrow;
            wideTile.Branding = TileBranding.None;
            wideTile.Image.Alt = "altWide";
            wideTile.Image.Src = wideTileLoc;
            wideTile.Square150x150Content = mediumTile;

            try
            {
                if (tile != null)
                {
                    TileNotification wideNotif = wideTile.CreateNotification();
                    TileUpdateManager.CreateTileUpdaterForSecondaryTile(tile.TileId).Clear();
                    TileUpdateManager.CreateTileUpdaterForSecondaryTile(tile.TileId).Update(wideNotif);
                }
                else
                {
                    TileNotification wideNotif = wideTile.CreateNotification();
                    TileUpdateManager.CreateTileUpdaterForApplication().Clear();
                    TileUpdateManager.CreateTileUpdaterForApplication().Update(wideNotif);
                }
            }
            catch (Exception e)
            {

            }
        }
        //gets set of tile images based on parameters
        public TileGroup createTileWithParams(WeatherInfo weatherInfo, int numAlerts = 0, ImageBrush background = null, string artistName = null)
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
                    data.flickrData.userName = artistName;
                    return createTileImages(data, numAlerts, ref newBg);
                }
                return createTileImages(data, numAlerts);
            }
            return null;
        }

        //tile rendering
        private TileGroup createTileImages(BackgroundTemplate data, int numAlerts, ref ImageBrush background)
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
        private TileGroup createTileImages(BackgroundTemplate data, int numAlerts)
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
        private Grid createImage(BackgroundTemplate data, LiveTileSize tileSize, int numAlerts, ref ImageBrush background)
        {
            if (tileSize == LiveTileSize.small)
            {
                return createSmallTile(data.weather.currentTemp, data.weather.conditions);
            }
            else if (background != null)
            {
                if (tileSize == LiveTileSize.medium)
                {
                    return createMediumTile(data, numAlerts, ref background);
                }
                else if (tileSize == LiveTileSize.wide)
                {
                    return createWideTile(data, numAlerts, ref background);
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
            }
            return null;
        }
        private Grid createImage(BackgroundTemplate data, int numAlerts, LiveTileSize tileSize)
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
        private Grid createSmallTile(string temp, string conditions)
        {
            Grid g;

            g = createBackgroundGrid(LiveTileSize.small, true);
            g.Children.Add(createSmallOverlay(temp, conditions));

            return g;
        }
        private StackPanel createSmallOverlay(string temp, string conditions)
        {
            StackPanel s = new StackPanel() { };
            s.Children.Add(createSmallTempText(temp));
            s.Children.Add(createSmallConditions(conditions));
            return s;
        }
        private TextBlock createSmallConditions(string conditions)
        {
            return new TextBlock() { IsTextScaleFactorEnabled = false, Text = conditions.ToUpper(), FontSize = 12, FontWeight = FontWeights.ExtraBold, VerticalAlignment = VerticalAlignment.Bottom, HorizontalAlignment = Windows.UI.Xaml.HorizontalAlignment.Right, TextTrimming = TextTrimming.Clip, TextWrapping = TextWrapping.WrapWholeWords };
        }
        private TextBlock createSmallTempText(string temp)
        {
            return new TextBlock() { IsTextScaleFactorEnabled = false, Text = temp, FontSize = 30, FontWeight = FontWeights.Thin, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Center };
        }
        private Grid createWideTile(BackgroundTemplate data, int numAlerts, ref ImageBrush background)
        {
            Grid g;
            if (background != null)
            {
                g = (createBackgroundGrid(LiveTileSize.wide, false, ref background));
                g.Children.Add(createWideDarkOverlay(data.flickrData.userName));
                g.Children.Add(createWideStackPanel(data, numAlerts, true));
            }
            else
            {
                g = (createBackgroundGrid(LiveTileSize.wide, true));
                g.Children.Add(createWideStackPanel(data, numAlerts, true));
            }
            return g;
        }

        private UIElement createAlertsGrid(int numAlerts)
        {
            Grid g = new Grid() { Height = 150, Width = 150, Background = new SolidColorBrush(Colors.Transparent) };
            TextBlock t = new TextBlock() { Height = 150, TextAlignment = TextAlignment.Right, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(0, 15, 15, 0), FontSize = 20, IsColorFontEnabled=true, Foreground = new SolidColorBrush(Colors.Red) };
            if (numAlerts > 0)
            {
                t.Text = numAlerts + "!";
            }
            g.Children.Add(t);
            return g;
        }
        private Grid createWideTile(BackgroundTemplate data, int numAlerts)
        {
            Grid g;

            g = (createBackgroundGrid(LiveTileSize.wide, true));
            g.Children.Add(createWideStackPanel(data, numAlerts, true));
            return g;
        }
        private Grid createMediumTile(BackgroundTemplate data, int numAlerts, ref ImageBrush background)
        {
            Grid g;
            g = createBackgroundGrid(LiveTileSize.medium, false, ref background);
            g.Children.Add(createOverlay(data, numAlerts, false));
            return g;
        }
        private Grid createMediumTile(BackgroundTemplate data, int numAlerts)
        {
            Grid g;
            g = createBackgroundGrid(LiveTileSize.medium, true);
            g.Children.Add(createOverlay(data, numAlerts, true));
            return g;
        }
        private Grid createWideDarkOverlay(string artistName)
        {
            Grid g = new Grid() { Width = 310, Height = 150, Background = new SolidColorBrush(Colors.Black) { Opacity = .3 } };
            g.Children.Add(new TextBlock() { IsTextScaleFactorEnabled = false, FontSize = 9, FontWeight = FontWeights.Bold, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Top, Text = "by " + artistName, Margin = new Thickness(0, 0, 3, 0) });
            return g;
        }
        private StackPanel createWideStackPanel(BackgroundTemplate data, int numAlerts, bool transparent)
        {
            StackPanel s = new StackPanel() { Orientation = Orientation.Horizontal };
            s.Children.Add(createFirstHalf(data, numAlerts, transparent));
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
            return new TextBlock() { IsTextScaleFactorEnabled = false, Text = compare.ToUpper(), FontWeight = FontWeights.ExtraBold, FontSize = 15, TextWrapping = TextWrapping.WrapWholeWords, VerticalAlignment = VerticalAlignment.Center, TextAlignment = TextAlignment.Right, Margin = new Thickness(0, 0, 5, 0) };
        }
        private Grid createFirstHalf(BackgroundTemplate data, int numAlerts, bool transparent)
        {
            Grid g = new Grid() { Height = 150, Width = 150 };
            g.Children.Add(createOverlay(data, numAlerts, transparent));
            return g;
        }
        private Grid createBackgroundGrid(LiveTileSize tileSize, bool transparent, ref ImageBrush background)
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
        private Grid createBackgroundGrid(LiveTileSize tileSize, bool transparent)
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
        private Grid createOverlay(BackgroundTemplate data, int numAlerts, bool transparent)
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
            g.Children.Add(createAlertsGrid(numAlerts));
            return g;
        }
        private TextBlock createTimeTextBlock()
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


        private TextBlock createFlickrSource(string artistName)
        {
            TextBlock t = new TextBlock() { IsTextScaleFactorEnabled = false, FontSize = 9, HorizontalAlignment = HorizontalAlignment.Right, Text = "by " + artistName, Margin = new Thickness(0, 0, 3, 0), MaxWidth = 90, FontWeight = FontWeights.Bold };
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
            Grid g = new Grid() { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(2, 0, 2, 0) };
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
            TextBlock t = new TextBlock() { IsTextScaleFactorEnabled = false, Text = temperature, FontWeight = FontWeights.Thin, FontSize = 45, CharacterSpacing = -60, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 0, 3, 0) };
            return t;
        }
        private StackPanel createForecastStackPanel(BackgroundTemplate data)
        {
            StackPanel s = new StackPanel() { Orientation = Orientation.Vertical, VerticalAlignment = VerticalAlignment.Center, Width = 80 };
            s.Children.Add(createConditionsTextBlock(data.weather.conditions));
            s.Children.Add(createHiLoTextBlock(data.weather.high, data.weather.low));
            return s;
        }
        private TextBlock createConditionsTextBlock(string conditions)
        {
            TextBlock t = new TextBlock() { IsTextScaleFactorEnabled = false, Text = conditions.ToUpperInvariant(), FontWeight = FontWeights.ExtraBold, TextWrapping = TextWrapping.WrapWholeWords, FontSize = 13, VerticalAlignment = VerticalAlignment.Center };
            return t;
        }
        private TextBlock createHiLoTextBlock(string high, string low)
        {
            TextBlock t = new TextBlock() { IsTextScaleFactorEnabled = false, Text = high + "/" + low, FontSize = 16, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Right, Width = 80 };
            return t;
        }
        private TextBlock createLocationTextBlock(string location)
        {
            TextBlock t = new TextBlock() { IsTextScaleFactorEnabled = false, Text = location.ToUpper(), FontSize = 14, FontWeight = FontWeights.SemiBold, Margin = new Thickness(10, 0, 0, 5), VerticalAlignment = VerticalAlignment.Bottom };
            return t;
        }
        #endregion

        //helper methods
        public bool unitsAreSI()
        {
            //determines whether tile units should be SI
            if (store.Values.ContainsKey(Values.TILE_UNITS_ARE_SI))
            {
                return (bool)store.Values[Values.TILE_UNITS_ARE_SI];
            }
            store.Values[Values.TILE_UNITS_ARE_SI] = true;
            return true;
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
        private bool twentyFourHrTime()
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