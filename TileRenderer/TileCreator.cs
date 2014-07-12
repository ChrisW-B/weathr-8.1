using DataTemplates;
using NotificationsExtensions.TileContent;
using System;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Notifications;
using Windows.UI.StartScreen;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace TileCreatorProject
{
    public class CreateTile
    {
        //tile rendering
        public TileGroup createTileImages(BackgroundTemplate data, ref BitmapImage background)
        {
            //given lat, lon, and conditions, creates a tile image
            try
            {
                TileGroup group = new TileGroup();
                group.sqTile = createTile(data, LiveTileSize.medium, ref background);
                group.wideTile = createTile(data, LiveTileSize.wide, ref background);
                group.smTile = createTile(data, LiveTileSize.small, ref background);
                return group;
            }
            catch
            {
                return null;
            }
        }
        public TileGroup createTileImages(BackgroundTemplate data, ref ImageBrush background)
        {
            //given lat, lon, and conditions, creates a tile image
            try
            {
                TileGroup group = new TileGroup();
                group.sqTile = createTile(data, LiveTileSize.medium, ref background);
                group.wideTile = createTile(data, LiveTileSize.wide, ref background);
                group.smTile = createTile(data, LiveTileSize.small, ref background);

                return group;
            }
            catch
            {
                return null;
            }
        }
        public TileGroup createTileImages(BackgroundTemplate data)
        {
            //given lat, lon, and conditions, creates a tile image
            try
            {
                TileGroup group = new TileGroup();
                group.sqTile = createTile(data, LiveTileSize.medium);
                group.wideTile = createTile(data, LiveTileSize.wide);
                group.smTile = createTile(data, LiveTileSize.small);
                return group;
            }
            catch
            {
                return null;
            }
        }
        private UIElement createTile(BackgroundTemplate data, LiveTileSize tileSize)
        {
            //given data, size, and a name, save a tile
            return createImage(data, tileSize);
        }
        private UIElement createTile(BackgroundTemplate data, LiveTileSize tileSize, ref BitmapImage background)
        {
            //given data, size, and a name, save a tile
            return createImage(data, tileSize, ref background);
        }
        private UIElement createTile(BackgroundTemplate data, LiveTileSize tileSize, ref ImageBrush background)
        {
            //given data, size, and a name, save a tile
            return createImage(data, tileSize, ref background);
        }

        #region tile ui design
        //creating grid of tile design
        private Grid createImage(BackgroundTemplate data, LiveTileSize tileSize, ref BitmapImage background)
        {
            if (tileSize == LiveTileSize.small)
            {
                return createSmallTile(data.weather.currentTemp, data.weather.conditions);
            }
            else if (background != null)
            {
                if (tileSize == LiveTileSize.medium)
                {
                    return createMediumTile(data, ref background);
                }
                else if (tileSize == LiveTileSize.wide)
                {
                    return createWideTile(data, ref background);
                }
            }
            else
            {
                if (tileSize == LiveTileSize.medium)
                {
                    return createMediumTile(data);
                }
                else if (tileSize == LiveTileSize.wide)
                {
                    return createWideTile(data);
                }
            }
            return null;
        }
        private Grid createImage(BackgroundTemplate data, LiveTileSize tileSize, ref ImageBrush background)
        {
            if (tileSize == LiveTileSize.small)
            {
                return createSmallTile(data.weather.currentTemp, data.weather.conditions);
            }
            else if (background != null)
            {
                if (tileSize == LiveTileSize.medium)
                {
                    return createMediumTile(data, ref background);
                }
                else if (tileSize == LiveTileSize.wide)
                {
                    return createWideTile(data, ref background);
                }
            }
            else
            {
                if (tileSize == LiveTileSize.medium)
                {
                    return createMediumTile(data);
                }
                else if (tileSize == LiveTileSize.wide)
                {
                    return createWideTile(data);
                }
            }
            return null;
        }

        private Grid createImage(BackgroundTemplate data, LiveTileSize tileSize)
        {
            if (tileSize == LiveTileSize.small)
            {
                return createSmallTile(data.weather.currentTemp, data.weather.conditions);
            }
            else if (tileSize == LiveTileSize.medium)
            {
                return createMediumTile(data);
            }
            else if (tileSize == LiveTileSize.wide)
            {
                return createWideTile(data);
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
            return new TextBlock() { Text = conditions.ToUpper(), FontSize = 12, FontWeight = FontWeights.ExtraBold, VerticalAlignment = VerticalAlignment.Bottom, HorizontalAlignment = Windows.UI.Xaml.HorizontalAlignment.Right, TextTrimming = TextTrimming.Clip, TextWrapping = TextWrapping.WrapWholeWords };
        }
        private TextBlock createSmallTempText(string temp)
        {
            return new TextBlock() { Text = temp, FontSize = 30, FontWeight = FontWeights.Thin, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Center };
        }
        private Grid createWideTile(BackgroundTemplate data, ref BitmapImage background)
        {
            Grid g;
            if (background != null)
            {
                g = (createBackgroundGrid(LiveTileSize.wide, false, ref background));
                g.Children.Add(createWideDarkOverlay(data.flickrData.userName));
                g.Children.Add(createWideStackPanel(data, true));

            }
            else
            {
                g = (createBackgroundGrid(LiveTileSize.wide, true));
                g.Children.Add(createWideStackPanel(data, true));

            }
            return g;
        }
        private Grid createWideTile(BackgroundTemplate data, ref ImageBrush background)
        {
            Grid g;
            if (background != null)
            {
                g = (createBackgroundGrid(LiveTileSize.wide, false, ref background));
                g.Children.Add(createWideDarkOverlay(data.flickrData.userName));
                g.Children.Add(createWideStackPanel(data, true));

            }
            else
            {
                g = (createBackgroundGrid(LiveTileSize.wide, true));
                g.Children.Add(createWideStackPanel(data, true));

            }
            return g;
        }
        private Grid createWideTile(BackgroundTemplate data)
        {
            Grid g;

            g = (createBackgroundGrid(LiveTileSize.wide, true));
            g.Children.Add(createWideStackPanel(data, true));
            return g;
        }
        private Grid createMediumTile(BackgroundTemplate data, ref ImageBrush background)
        {
            Grid g;
            g = createBackgroundGrid(LiveTileSize.medium, false, ref background);
            g.Children.Add(createOverlay(data, false));
            return g;
        }
        private Grid createMediumTile(BackgroundTemplate data, ref BitmapImage background)
        {
            Grid g;
            g = createBackgroundGrid(LiveTileSize.medium, false, ref background);
            g.Children.Add(createOverlay(data, false));
            return g;
        }
        private Grid createMediumTile(BackgroundTemplate data)
        {
            Grid g;
            g = createBackgroundGrid(LiveTileSize.medium, true);
            g.Children.Add(createOverlay(data, true));
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
        private Grid createBackgroundGrid(LiveTileSize tileSize, bool transparent, ref BitmapImage background)
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
                g.Background = new ImageBrush() { ImageSource = background, Stretch = Stretch.UniformToFill };
            }
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
        public void pushImageToMainTile(string smallTileLoc, string mediumTileLoc, string wideTileLoc, string compare, string current, string today, string tomorrow)
        {
            //pushes the image to the tiles

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
        public void pushImageToSecondaryTile(SecondaryTile tile, string smallTileLoc, string mediumTileLoc, string wideTileLoc, string tempCompare, string current, string today, string tomorrow)
        {
            if (SecondaryTile.Exists(tile.TileId))
            {
                ITileSquare71x71IconWithBadge smallTile = TileContentFactory.CreateTileSquare71x71IconWithBadge();
                smallTile.ImageIcon.Src = smallTileLoc;
                smallTile.ImageIcon.Alt = "altSmall";
                smallTile.Branding = TileBranding.Name;

                ITileSquare150x150PeekImageAndText04 mediumTile = TileContentFactory.CreateTileSquare150x150PeekImageAndText04();
                mediumTile.TextBodyWrap.Text = tempCompare;
                mediumTile.Branding = TileBranding.None;
                mediumTile.Image.Alt = "altMed";
                mediumTile.Image.Src = mediumTileLoc;
                mediumTile.Square71x71Content = smallTile;
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

        //gets set of tile images based on parameters
        public TileGroup updateMainWithParams(string currentConditions, string tomorrowShort, string tempCompare, string todayHigh, string todayLow, string temp, string todayShort, string city, string tomorrowHigh, string tomorrowLow, ImageBrush background, string artistName)
        {
            ImageBrush newBg = new ImageBrush();
            newBg.ImageSource = background.ImageSource;
            newBg.Opacity = 1;
            newBg.Stretch = Stretch.UniformToFill;
            BackgroundTemplate data = new BackgroundTemplate()
            {
                weather = new BackgroundWeather()
                {
                    conditions = currentConditions,
                    tempCompare = tomorrowShort + " tomorrow, and " + tempCompare.ToLowerInvariant() + " today",
                    high = todayHigh,
                    low = todayLow,
                    currentTemp = temp.Split('.')[0] + "°",
                    todayForecast = todayShort,
                },
                location = new BackgroundLoc()
                {
                    location = city,
                },
                flickrData = new BackgroundFlickr()
                {
                    userName = artistName
                }
            };
            return createTileImages(data, ref newBg);
        }
        public TileGroup updateMainWithParams(string currentConditions, string tomorrowShort, string tempCompare, string todayHigh, string todayLow, string temp, string todayShort, string city, string tomorrowHigh, string tomorrowLow)
        {
            BackgroundTemplate data = new BackgroundTemplate()
            {
                weather = new BackgroundWeather()
                {
                    conditions = currentConditions,
                    tempCompare = tomorrowShort + " tomorrow, and " + tempCompare.ToLowerInvariant() + " today",
                    high = todayHigh,
                    low = todayLow,
                    currentTemp = temp.Split('.')[0] + "°",
                    todayForecast = todayShort,
                },
                location = new BackgroundLoc()
                {
                    location = city,
                }
            };
            return createTileImages(data);
        }
        public TileGroup updateSecondaryWithParams(SecondaryTile tile, string currentConditions, string tomorrowShort, string tempCompare, string todayHigh, string todayLow, string temp, string todayShort, string city, string tomorrowHigh, string tomorrowLow, ImageBrush background, string artistName)
        {
            ImageBrush newBg = new ImageBrush();
            newBg.ImageSource = background.ImageSource;
            newBg.Opacity = 1;
            newBg.Stretch = Stretch.UniformToFill;
            BackgroundTemplate data = new BackgroundTemplate()
            {
                weather = new BackgroundWeather()
                {
                    conditions = currentConditions,
                    tempCompare = tomorrowShort + " tomorrow, and " + tempCompare.ToLowerInvariant() + " today",
                    high = todayHigh,
                    low = todayLow,
                    currentTemp = temp.Split('.')[0] + "°",
                    todayForecast = todayShort,
                },
                location = new BackgroundLoc()
                {
                    location = city,
                },
                flickrData = new BackgroundFlickr()
                {
                    userName = artistName
                }
            };
            return createTileImages(data, ref newBg);
        }
        public TileGroup updateSecondaryWithParams(SecondaryTile tile, string currentConditions, string tomorrowShort, string tempCompare, string todayHigh, string todayLow, string temp, string todayShort, string city, string tomorrowHigh, string tomorrowLow)
        {
            BackgroundTemplate data = new BackgroundTemplate()
            {
                weather = new BackgroundWeather()
                {
                    conditions = currentConditions,
                    tempCompare = tomorrowShort + " tomorrow, and " + tempCompare.ToLowerInvariant() + " today",
                    high = todayHigh,
                    low = todayLow,
                    currentTemp = temp.Split('.')[0] + "°",
                    todayForecast = todayShort,
                },
                location = new BackgroundLoc()
                {
                    location = city,
                }
            };
            return createTileImages(data);
        }
    }
}
