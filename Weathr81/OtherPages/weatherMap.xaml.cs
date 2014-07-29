using LocationHelper;
using StoreLabels;
using System;
using System.Globalization;
using Weathr81.Common;
using Weathr81.HelperClasses;
using Windows.Foundation;
using Windows.Phone.UI.Input;
using Windows.UI;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Maps;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Shapes;

// The Basic Page item template is documented at http://go.microsoft.com/fwlink/?LinkID=390556

namespace Weathr81.OtherPages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class WeatherMap : Page
    {
        private NavigationHelper navigationHelper;
        private ObservableDictionary defaultViewModel = new ObservableDictionary();

        public WeatherMap()
        {
            this.InitializeComponent();

            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += this.NavigationHelper_LoadState;
            this.navigationHelper.SaveState += this.NavigationHelper_SaveState;
        }

        /// <summary>
        /// Gets the <see cref="NavigationHelper"/> associated with this <see cref="Page"/>.
        /// </summary>
        public NavigationHelper NavigationHelper
        {
            get { return this.navigationHelper; }
        }

        /// <summary>
        /// Gets the view model for this <see cref="Page"/>.
        /// This can be changed to a strongly typed view model.
        /// </summary>
        public ObservableDictionary DefaultViewModel
        {
            get { return this.defaultViewModel; }
        }

        /// <summary>
        /// Populates the page with content passed during navigation.  Any saved state is also
        /// provided when recreating a page from a prior session.
        /// </summary>
        /// <param name="sender">
        /// The source of the event; typically <see cref="NavigationHelper"/>
        /// </param>
        /// <param name="e">Event data that provides both the navigation parameter passed to
        /// <see cref="Frame.Navigate(Type, Object)"/> when this page was initially requested and
        /// a dictionary of state preserved by this page during an earlier
        /// session.  The state will be null the first time a page is visited.</param>
        private void NavigationHelper_LoadState(object sender, LoadStateEventArgs e)
        {
        }

        /// <summary>
        /// Preserves state associated with this page in case the application is suspended or the
        /// page is discarded from the navigation cache.  Values must conform to the serialization
        /// requirements of <see cref="SuspensionManager.SessionState"/>.
        /// </summary>
        /// <param name="sender">The source of the event; typically <see cref="NavigationHelper"/></param>
        /// <param name="e">Event data that provides an empty dictionary to be populated with
        /// serializable state.</param>
        private void NavigationHelper_SaveState(object sender, SaveStateEventArgs e)
        {
        }

        #region NavigationHelper registration

        /// <summary>
        /// The methods provided in this section are simply used to allow
        /// NavigationHelper to respond to the page's navigation methods.
        /// <para>
        /// Page specific logic should be placed in event handlers for the  
        /// <see cref="NavigationHelper.LoadState"/>
        /// and <see cref="NavigationHelper.SaveState"/>.
        /// The navigation parameter is available in the LoadState method 
        /// in addition to page state preserved during an earlier session.
        /// </para>
        /// </summary>
        /// <param name="e">Provides data for navigation methods and event
        /// handlers that cannot cancel the navigation request.</param>
        /// 

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            this.navigationHelper.OnNavigatedTo(e);
            if (e.Parameter != null)
            {
                MapLaunchClass mapLaunchClass = (e.Parameter as MapLaunchClass);
                type = mapLaunchClass.type;
                constructMap(mapLaunchClass);
            }
        }
        private MapLaunchClass.mapType type;

        private void constructMap(MapLaunchClass mapLaunchClass)
        {
            Map.Center = mapLaunchClass.loc.position;
            HttpMapTileDataSource dataSource = new HttpMapTileDataSource() { AllowCaching = true };

            dataSource.UriRequested += dataSource_UriRequested;

            MapTileSource tileSource = new MapTileSource(dataSource);
            Map.TileSources.Add(tileSource);
            addMarker(mapLaunchClass.loc);
        }


        void dataSource_UriRequested(HttpMapTileDataSource sender, MapTileUriRequestedEventArgs args)
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
                d.Complete();
            }
            catch
            {
                d.Complete();
            }

        }

        private void addMarker(GeoTemplate loc)
        {
            Polygon triangle = createMapMarker();
            MapControl.SetLocation(triangle, loc.position);
            Map.Children.Add(triangle);
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

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            this.navigationHelper.OnNavigatedFrom(e);
        }



        #endregion
        private const string WUND_PRE = "http://api.wunderground.com/api/" + Values.WUND_API_KEY + "/";
        private const string SAT_PRE = "satellite/";
        private const string ANI_SAT_PRE = "animated" + SAT_PRE;
        private const string RAD_PRE = "radar/";
        private const string ANI_RAD_PRE = "animated" + RAD_PRE;
        private const string LARGE_TILE_SIZE = "&width=1000" + "&height=1600";
        private const string SAT_POST = "&key=sat_ir4" + "&gtt=107" + "&smooth=1" + "&timelabel=0";
        private const string RAD_POST = "&rainsnow=1" + "&timelabel=0" + "&noclutter=1";
        private const string SHOW_SAT_BASEMAP = "&basemap=";
        private const string SHOW_SAT_BORDERS = "&borders=";
        private const string SHOW_RAD_BASEMAP = "&newmaps=";
        private const string MIN_LON = "&minlon=";
        private const string MIN_LAT = "&minlat=";
        private const string MAX_LON = "&maxlon=";
        private const string MAX_LAT = "&maxlat=";
        private const string SAT_CENTER_LON = "&lon=";
        private const string SAT_CENTER_LAT = "&lat=";
        private const string RAD_CENTER_LON = "&centerlon=";
        private const string RAD_CENTER_LAT = "&centerlat=";
        private const string FRAMES = "&num=";
        private const string RADIUS = "&radius=150";

        private void animateButton_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            Uri uri;
            if (type == MapLaunchClass.mapType.radar)
            {
                uri = new Uri(WUND_PRE + ANI_RAD_PRE + "aniRad.gif?" + RAD_CENTER_LON + Convert.ToString(Map.Center.Position.Longitude, new CultureInfo("en-US")) + RAD_CENTER_LAT + Convert.ToString(Map.Center.Position.Latitude, new CultureInfo("en-US")) + LARGE_TILE_SIZE + RAD_POST + FRAMES + "15" + RADIUS + SHOW_RAD_BASEMAP + "1");
            }
            else
            {
                uri = new Uri(WUND_PRE + ANI_SAT_PRE + "aniSat.gif?" + SAT_CENTER_LON + Convert.ToString(Map.Center.Position.Longitude, new CultureInfo("en-US")) + SAT_CENTER_LAT + Convert.ToString(Map.Center.Position.Latitude, new CultureInfo("en-US")) + LARGE_TILE_SIZE + SAT_POST + FRAMES + "8" + RADIUS + SHOW_SAT_BORDERS + "1" + SHOW_SAT_BASEMAP + "1");
            }

            Frame.Navigate(typeof(AnimatedMap), uri);
        }
    }
}

