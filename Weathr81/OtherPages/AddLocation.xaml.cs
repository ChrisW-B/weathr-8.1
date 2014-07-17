using LocationHelper;
using OtherPages;
using SerializerClass;
using StoreLabels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml.Linq;
using Weathr81.Common;
using Windows.Devices.Geolocation;
using Windows.Storage;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

// The Basic Page item template is documented at http://go.microsoft.com/fwlink/?LinkID=390556

namespace Weathr81.OtherPages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class AddLocation : Page
    {
        #region navigation stuff
        private NavigationHelper navigationHelper;
        private ObservableDictionary defaultViewModel = new ObservableDictionary();

        public AddLocation()
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


        #endregion
        #endregion

        private ApplicationDataContainer store = Windows.Storage.ApplicationData.Current.RoamingSettings;
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            this.navigationHelper.OnNavigatedTo(e);
            suggestions = new ObservableCollection<SearchItemTemplate>();
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            this.navigationHelper.OnNavigatedFrom(e);
        }


        #region variables
        ObservableCollection<SearchItemTemplate> suggestions = new ObservableCollection<SearchItemTemplate>();
        #endregion
        StatusBar statusBar;
        private ApplicationDataContainer localStore = Windows.Storage.ApplicationData.Current.LocalSettings;
        async private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            //clear suggestions and add new ones
            statusBar = StatusBar.GetForCurrentView();
            await statusBar.ShowAsync();
            statusBar.ProgressIndicator.Text = "Searching...";
            await statusBar.ProgressIndicator.ShowAsync();
            try
            {
                suggestions.Clear();
                Uri searchUri = new Uri("http://autocomplete.wunderground.com/aq?query=" + SearchBox.Text + "&format=XML");
                HttpClient client = new HttpClient();
                Stream str = await client.GetStreamAsync(searchUri);
                populateSuggestions(XDocument.Load(str));
                statusBar.ProgressIndicator.HideAsync();
            }
            catch
            {
                statusBar.ProgressIndicator.ProgressValue = 0;
                statusBar.ProgressIndicator.Text = "There was a problem searching";
            }
        }
        private void populateSuggestions(XDocument doc)
        {
            //using the result of a wunderground query, populate the search box
            if (allowedToAutoFind())
            {
                suggestions.Add(new SearchItemTemplate() { locName = "Current Location", isCurrent = true, wUrl = "currLoc" });
            }
            List<String> locNames = new List<string>();
            List<String> locUrls = new List<string>();
            foreach (XElement elm in doc.Descendants().Elements("name"))
            {
                locNames.Add((string)elm.Value);
            }
            foreach (XElement elm in doc.Descendants().Elements("l"))
            {
                locUrls.Add((string)elm.Value);
            }
            for (int i = 0; i < locNames.Count && i < locUrls.Count; i++)
            {
                suggestions.Add(new SearchItemTemplate() { locName = locNames[i], wUrl = locUrls[i], isCurrent = false });
            }
            results.ItemsSource = suggestions;
        }
        async private void results_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ListBox lb = sender as ListBox;
            SearchItemTemplate item = (lb.SelectedItem as SearchItemTemplate);
            ObservableCollection<Location> locs = Serializer.get(Values.LOC_STORE, typeof(ObservableCollection<Location>), store) as ObservableCollection<Location>;
            if (locs == null)
            {
                locs = new ObservableCollection<Location>();
            }
            if (locIsNew(locs, item))
            {
                if (!item.isCurrent)
                {
                    GeoTemplate coordinates = await getCoordinates(item.locName);
                    if (!coordinates.fail)
                    {
                        locs.Add(new Location() { IsCurrent = item.isCurrent, IsDefault = false, LocName = item.locName, LocUrl = item.wUrl, Lat = coordinates.position.Position.Latitude, Lon = coordinates.position.Position.Longitude });
                    }
                }
                else
                {
                    locs.Add(new Location() { IsCurrent = item.isCurrent, IsDefault = false, LocName = item.locName, LocUrl = item.wUrl });
                }
                Serializer.save(locs, typeof(ObservableCollection<Location>), Values.LOC_STORE, store);
            }
            Frame.GoBack();
        }

        //helpers
        private bool locIsNew(ObservableCollection<Location> locs, SearchItemTemplate item)
        {
            //prevent locations from being added twice
            foreach (Location loc in locs)
            {
                if (item.locName == loc.LocName || (loc.IsCurrent && item.isCurrent))
                {
                    return false;
                }
            }
            return true;
        }

        //googling location
        async private Task<GeoTemplate> getCoordinates(string locName)
        {
            //get item coordinates from google
            Uri googleUri = new Uri(Values.GOOGLE_URL + locName + Values.GOOGLE_POST);
            HttpClient c = new HttpClient();
            Stream str = await c.GetStreamAsync(googleUri);
            return readCoordinates(XDocument.Load(str));
        }
        private GeoTemplate readCoordinates(XDocument doc)
        {
            //parse googles response
            try
            {
                var location = doc.Element("GeocodeResponse").Element("result").Element("geometry").Element("location");
                string lat = (string)location.Element("lat").Value;
                string lon = (string)location.Element("lng").Value;
                if (lat.Contains(","))
                {
                    lat = lat.Replace(',', '.');
                }
                if (lon.Contains(","))
                {
                    lon = lon.Replace(',', '.');
                }
                return new GeoTemplate() { fail = false, position = new Geopoint(new BasicGeoposition() { Latitude = Convert.ToDouble(lat, new CultureInfo("en-US")), Longitude = Convert.ToDouble(lon, new CultureInfo("en-US")) }) };
            }
            catch
            {
                return new GeoTemplate() { fail = true, errorMsg = "google returned invalid coordinates" };
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
    }
}
