using LocationHelper;
using StoreLabels;
using System;
using Weathr81.Common;
using Weathr81.HelperClasses;
using Windows.Foundation;
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

            //dataSource = new HttpMapTileDataSource(Values.RAD_URL + DateTime.UtcNow.Millisecond);
            dataSource.UriRequested += dataSource_UriRequested;

            MapTileSource tileSource = new MapTileSource(dataSource);
            Map.TileSources.Add(tileSource);
            addMarker(mapLaunchClass.loc);
        }

        void dataSource_UriRequested(HttpMapTileDataSource sender, MapTileUriRequestedEventArgs args)
        {
            MapTileUriRequestDeferral d = args.Request.GetDeferral();
            double x = args.X;
            double y = args.Y;
            double zoom = args.ZoomLevel;
            const double TILE_HEIGHT = 256, TILE_WIDTH = 256;
            try
            {
                double W = (float)(x * TILE_WIDTH) * 360 / (float)(TILE_WIDTH * Math.Pow(2, zoom)) - 180;
                double N = (float)Math.Asin((Math.Exp((0.5 - (y * TILE_HEIGHT) / (TILE_HEIGHT) / Math.Pow(2, zoom)) * 4 * Math.PI) - 1) / (Math.Exp((0.5 - (y * TILE_HEIGHT) / 256 / Math.Pow(2, zoom)) * 4 * Math.PI) + 1)) * 180 / (float)Math.PI;
                double E = (float)((x + 1) * TILE_WIDTH) * 360 / (float)(TILE_WIDTH * Math.Pow(2, zoom)) - 180;
                double S = (float)Math.Asin((Math.Exp((0.5 - ((y + 1) * TILE_HEIGHT) / (TILE_HEIGHT) / Math.Pow(2, zoom)) * 4 * Math.PI) - 1) / (Math.Exp((0.5 - ((y + 1) * TILE_HEIGHT) / 256 / Math.Pow(2, zoom)) * 4 * Math.PI) + 1)) * 180 / (float)Math.PI;


                String uri;
                if (type == MapLaunchClass.mapType.radar)
                {
                    uri = WUND_PRE + RAD_PRE + "rad.png?" + MIN_LON + W + MAX_LON + E + MIN_LAT + S + MAX_LAT + N + WIDTH + HEIGHT + RAD_RAINSNOW + TIME_LABEL + RAD_SHOW_BG + FRAMES + "15" + RAD_CLUTTER;
                }
                else
                {
                    uri = WUND_PRE + SAT_PRE + "sat.png?" + MIN_LON + W + MAX_LON + E + MIN_LAT + S + MAX_LAT + N + WIDTH + HEIGHT + SAT_KEY + SAT_BASEMAP + TIME_LABEL + SAT_GTT + SAT_BORDERS + FRAMES + "8";
                }
                args.Request.Uri = new Uri(uri);
            }
            catch (Exception ex)
            {

            }
            d.Complete();
        }

        private double tileToLon(int x, int z)
        {
            return x / Math.Pow(2.0, z) * 360.0 - 180;
        }

        private double tileToLat(int y, int z)
        {
            double n = Math.PI - (2.0 * Math.PI * y) / Math.Pow(2.0, z);
            return 180.0 / (Math.PI * (Math.Atan(Math.Sinh(n))));
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
        private const string SAT_PRE = "animatedsatellite/";
        private const string RAD_PRE = "animatedradar/";
        private const string MIN_LON = "&minlon=";
        private const string MIN_LAT = "&minlat=";
        private const string MAX_LON = "&maxlon=";
        private const string MAX_LAT = "&maxlat=";
        private const string WIDTH = "&width=256";
        private const string HEIGHT = "&height=256";
        private const string SAT_KEY = "&key=sat_ir4";
        private const string SAT_BASEMAP = "&basemap=0";
        private const string SAT_GTT = "&gtt=107";
        private const string SAT_BORDERS = "&borders=0";
        private const string FRAMES = "&num=";
        private const string RAD_RAINSNOW = "&rainsnow=1";
        private const string RAD_SHOW_BG = "&newmaps=0";
        private const string TIME_LABEL = "&timelabel=0";
        private const string RAD_CLUTTER = "&noclutter=1";


    }
}
