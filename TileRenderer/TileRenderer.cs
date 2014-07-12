using DataTemplates;
using System;
using Windows.UI;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace TileRendererClass
{
    public class CreateTile
    {
        //tile rendering
        public TileGroup createTileImages(BackgroundTemplate data, BitmapImage background = null)
        {
            //given lat, lon, and conditions, creates a tile image
            try
            {
                TileGroup group = new TileGroup();
                if (background != null)
                {

                    group.sqTile = createTile(data, TileSize.medium, background);
                    group.wideTile = createTile(data, TileSize.wide, background);
                    group.smTile = createTile(data, TileSize.small, background);
                }
                else
                {
                    group.sqTile = createTile(data, TileSize.medium);
                    group.wideTile = createTile(data, TileSize.wide);
                    group.smTile = createTile(data, TileSize.small);
                }
                return group;
            }
            catch
            {
                return null;
            }
        }
        private UIElement createTile(BackgroundTemplate data, TileSize tileSize, BitmapImage background = null)
        {
            //given data, size, and a name, save a tile
            UIElement g;
            if (background != null)
            {
                g = createImage(data, tileSize, ref background);
            }
            else
            {
                g = createImage(data, tileSize);
            }
            return g;

        }
        #region tile ui design
        //creating grid of tile design
        private Grid createImage(BackgroundTemplate data, TileSize tileSize, ref BitmapImage background)
        {
            if (tileSize == TileSize.small)
            {
                return createSmallTile(data.weather.currentTemp, data.weather.conditions);
            }
            else if (background != null)
            {
                if (tileSize == TileSize.medium)
                {
                    return createMediumTile(data, ref background);
                }
                else if (tileSize == TileSize.wide)
                {
                    return createWideTile(data, ref background);
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

        private Grid createImage(BackgroundTemplate data, TileSize tileSize)
        {
            if (tileSize == TileSize.small)
            {
                return createSmallTile(data.weather.currentTemp, data.weather.conditions);
            }
            else if (tileSize == TileSize.medium)
            {
                return createMediumTile(data);
            }
            else if (tileSize == TileSize.wide)
            {
                return createWideTile(data);
            }
            return null;
        }
        private Grid createSmallTile(string temp, string conditions)
        {
            Grid g;

            g = createBackgroundGrid(TileSize.small, true);
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
                g = (createBackgroundGrid(TileSize.wide, false, ref background));
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
        private Grid createWideTile(BackgroundTemplate data)
        {
            Grid g;

            g = (createBackgroundGrid(TileSize.wide, true));
            g.Children.Add(createWideStackPanel(data, true));
            return g;
        }
        private Grid createMediumTile(BackgroundTemplate data, ref BitmapImage background)
        {
            Grid g;
            g = createBackgroundGrid(TileSize.medium, false, ref background);
            g.Children.Add(createOverlay(data, false));
            return g;
        }
        private Grid createMediumTile(BackgroundTemplate data)
        {
            Grid g;
            g = createBackgroundGrid(TileSize.medium, true);
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
        private Grid createBackgroundGrid(TileSize tileSize, bool transparent, ref BitmapImage background)
        {
            Grid g = new Grid() { Background = new SolidColorBrush() { Color = Colors.Transparent } };
            g.Height = g.Width = 150;
            if (tileSize == TileSize.wide)
            {
                g.Width = 310;
            }
            else if (tileSize == TileSize.small)
            {
                g.Height = g.Width = 71;
            }
            if (!transparent)
            {
                g.Background = new ImageBrush() { ImageSource = background, Stretch = Stretch.UniformToFill };
            }
            return g;
        }

        private Grid createBackgroundGrid(TileSize tileSize, bool transparent)
        {
            Grid g = new Grid() { Background = new SolidColorBrush() { Color = Colors.Transparent } };
            g.Height = g.Width = 150;
            if (tileSize == TileSize.wide)
            {
                g.Width = 310;
            }
            else if (tileSize == TileSize.small)
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
    }
}
