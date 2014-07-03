using Weathr81.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Display;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using SerializerClass;
using Windows.Storage;
using BackgroundTask;
using Windows.UI.Popups;
using System.Collections.ObjectModel;
using LocationHelper;
using Windows.UI.StartScreen;

// The Basic Page item template is documented at http://go.microsoft.com/fwlink/?LinkID=390556

namespace Weathr81.OtherPages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SettingsPivot : Page
    {
        #region navigation stuff
        private NavigationHelper navigationHelper;
        private ObservableDictionary defaultViewModel = new ObservableDictionary();

        public SettingsPivot()
        {
            this.InitializeComponent();

            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += this.NavigationHelper_LoadState;
            this.navigationHelper.SaveState += this.NavigationHelper_SaveState;
            NavigationCacheMode = Windows.UI.Xaml.Navigation.NavigationCacheMode.Required;
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
        #endregion

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
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            this.navigationHelper.OnNavigatedTo(e);
            updateSettings();
            doneSetting = true;
        }
        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            this.navigationHelper.OnNavigatedFrom(e);
        }
        #endregion
        #region variables
        private const string UNITS_CHANGED = "unitsChanged";
        private const string UNITS_ARE_SI = "unitsAreSI";
        private const string TILE_UNITS_ARE_SI = "tileUnitsAreSI";
        private const string TRANSPARENT_TILE = "tileIsTransparent";
        private const string ALLOW_BG_TASK = "allowBackground";
        private const string UPDATE_FREQ = "updateFreq";
        private const string TASK_NAME = "Weathr Tile Updater";
        private const string UPDATE_ON_CELL = "allowUpdateOnNetwork";
        private const string ALLOW_LOC = "allowAutoLocation";
        private const string LOC_STORE = "locList";
        private ApplicationDataContainer store = Windows.Storage.ApplicationData.Current.RoamingSettings;
        private ApplicationDataContainer localStore = Windows.Storage.ApplicationData.Current.LocalSettings;
        bool doneSetting = false;
        #endregion
        #region settings setup
        private void updateSettings()
        {
            setUpGeneralSection();
            setupLocationsSection();
            setUpTileSection();
        }

        private void setupLocationsSection()
        {
            if (localStore.Values.ContainsKey(ALLOW_LOC))
            {
                autoLocateToggle.IsOn = (bool)localStore.Values[ALLOW_LOC];
            }
            else
            {
                localStore.Values[ALLOW_LOC] = true;
                autoLocateToggle.IsOn = true;
            }
            if (store.Values.ContainsKey(LOC_STORE))
            {
                ObservableCollection<Location> list = (Serializer.get(LOC_STORE, typeof(ObservableCollection<Location>), store) as ObservableCollection<Location>);
                if (list != null)
                {
                    LocationTemplate locTemplate = new LocationTemplate() { locations = new LocationList() { locationList = list } };
                    locList.DataContext = locTemplate;
                }
            }
        }
        private void setUpGeneralSection()
        {
            if (store.Values.ContainsKey(UNITS_ARE_SI))
            {
                units.IsOn = (bool)store.Values[UNITS_ARE_SI];
            }
            else
            {
                store.Values[UNITS_ARE_SI] = true;
                units.IsOn = true;
            }
        }
        private void setUpTileSection()
        {
            if (localStore.Values.ContainsKey(ALLOW_BG_TASK))
            {
                bool allowed = (bool)localStore.Values[ALLOW_BG_TASK];
                enableBackground.IsChecked = allowed;
                if (allowed)
                {
                    backgroundPanel.Visibility = Windows.UI.Xaml.Visibility.Visible;
                }
                else
                {
                    backgroundPanel.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                }
            }
            else
            {
                localStore.Values[ALLOW_BG_TASK] = false;
                enableBackground.IsChecked = false;
                backgroundPanel.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            }
            if (localStore.Values.ContainsKey(UPDATE_ON_CELL))
            {
                wifiOnlyToggle.IsOn = !(bool)localStore.Values[UPDATE_ON_CELL];
            }
            else
            {
                wifiOnlyToggle.IsOn = false;
                localStore.Values[UPDATE_ON_CELL] = true;
            }
            if (localStore.Values.ContainsKey(TRANSPARENT_TILE))
            {
                transparentToggle.IsOn = !(bool)localStore.Values[TRANSPARENT_TILE];
            }
            else
            {
                transparentToggle.IsOn = true;
                localStore.Values[TRANSPARENT_TILE] = false;
            }

            if (localStore.Values.ContainsKey(UPDATE_FREQ))
            {
                updateFreq.Value = Convert.ToInt32(localStore.Values[UPDATE_FREQ]);
                currentRate.Text = printUpdateRate(updateFreq.Value);
            }
            else
            {
                updateFreq.Value = 120;
                currentRate.Text = printUpdateRate(updateFreq.Value);
                localStore.Values[UPDATE_FREQ] = (uint)120;
            }

            if (store.Values.ContainsKey(TILE_UNITS_ARE_SI))
            {
                tileUnits.IsOn = (bool)store.Values[TILE_UNITS_ARE_SI];
            }
            else
            {
                tileUnits.IsOn = true;
                store.Values[TILE_UNITS_ARE_SI] = true;
            }
        }
        #endregion

        #region general pivot
        private void Units_Toggled(object sender, RoutedEventArgs e)
        {
            if (doneSetting)
            {
                ToggleSwitch toggle = sender as ToggleSwitch;
                if (toggle != null)
                {
                    localStore.Values[UNITS_CHANGED] = true;
                    if (toggle.IsOn)
                    {
                        store.Values[UNITS_ARE_SI] = true;
                        return;
                    }
                    store.Values[UNITS_ARE_SI] = false;
                }
            }
        }
        #endregion

        #region tile pivot

        private void enableBackground_Checked(object sender, RoutedEventArgs e)
        {
            if (doneSetting)
            {
                localStore.Values[ALLOW_BG_TASK] = true;
                backgroundPanel.Visibility = Windows.UI.Xaml.Visibility.Visible;
                registerRecurringBG();
            }
            else
            {
                return;
            }
        }
        private void enableBackground_Unchecked(object sender, RoutedEventArgs e)
        {
            if (doneSetting)
            {
                localStore.Values[ALLOW_BG_TASK] = false;
                UpdateTiles.Unregister(TASK_NAME);
                backgroundPanel.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            }
            else
            {
                return;
            }
        }
        private void registerRecurringBG(uint mins = 120)
        {
            localStore.Values[UPDATE_FREQ] = mins;
            UpdateTiles.Register(mins);
        }

        //rate slider
        private string printUpdateRate(double p)
        {
            if (p < 60)
            {
                return "Update every " + p + " minutes";
            }
            else
            {
                double hours = (int)(p / 60);
                double mins = p % 60;
                string hrS;
                string minS;

                if (hours == 1)
                {
                    hrS = " hour ";
                }
                else
                {
                    hrS = " hours ";
                }
                if (mins == 1)
                {
                    minS = " minute";
                }
                else
                {
                    minS = " minutes";
                }
                return "Update every " + hours + hrS + " and " + mins + minS;
            }
        }
        private void updateFreq_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (doneSetting)
            {
                if (timer.IsEnabled)
                {
                    timer.Stop();
                    timer = null;
                    timer = new DispatcherTimer();
                }
                currentRate.Text = printUpdateRate((sender as Slider).Value);
                timer.Interval = new TimeSpan(0, 0, 0, 0, 500);
                timer.Start();
                timer.Tick += timer_Tick;

            }
            return;
        }
        DispatcherTimer timer = new DispatcherTimer();
        void timer_Tick(object sender, object e)
        {
            registerRecurringBG(Convert.ToUInt32(updateFreq.Value));
            timer.Stop();
            timer = null;
            timer = new DispatcherTimer();
        }

        private void TransparentTile_Toggled(object sender, RoutedEventArgs e)
        {
            if (doneSetting)
            {
                localStore.Values[TRANSPARENT_TILE] = !(sender as ToggleSwitch).IsOn;
            }
            return;
        }

        private void wifiOnlyToggle_Toggled(object sender, RoutedEventArgs e)
        {
            if (doneSetting)
            {
                ToggleSwitch t = sender as ToggleSwitch;
                if (t != null)
                {
                    localStore.Values[UPDATE_ON_CELL] = !t.IsOn;
                }
            }
        }

        private void tileUnits_Toggled(object sender, RoutedEventArgs e)
        {
            if (doneSetting)
            {
                ToggleSwitch t = sender as ToggleSwitch;
                if (t != null)
                {
                    if (t.IsOn)
                    {
                        store.Values[TILE_UNITS_ARE_SI] = true;
                        return;
                    }
                    store.Values[TILE_UNITS_ARE_SI] = false;
                }
            }
            return;
        }
        #endregion

        #region advanced pivot
        async private void Button_Click(object sender, RoutedEventArgs e)
        {
            MessageDialog dialog = new MessageDialog("This will remove all of your saved data and settings on for Weathr on all of your devices, and reset everything, including backups. Are you sure you want to delete?", "Are you sure?");
            dialog.Commands.Add(new UICommand("Reset Everything", delegate(IUICommand cmd)
            {
                store.Values.Clear();
                localStore.Values.Clear();
                UpdateTiles.Unregister(TASK_NAME);
                updateSettings();
            }));
            dialog.Commands.Add(new UICommand("No"));
            await dialog.ShowAsync();
        }
        #endregion

        #region location pivot
        private void autoLocateToggle_Toggled(object sender, RoutedEventArgs e)
        {
            if (doneSetting)
            {
                localStore.Values[ALLOW_LOC] = (sender as ToggleSwitch).IsOn;
            }
            return;
        }

        private void addLoc_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(AddLocation));
        }

        private void removeItem_Click(object sender, RoutedEventArgs e)
        {
            LocationTemplate list = locList.DataContext as LocationTemplate;
            Location item = findLocation((sender as MenuFlyoutItem).Tag.ToString());
            if (list != null && item != null)
            {
                foreach (Location old in list.locations.locationList)
                {
                    if (old == item)
                    {
                        list.locations.locationList.Remove(old);
                        break;
                    }
                }
            }
            locList.DataContext = null;
            LocationTemplate locTemp = new LocationTemplate() { locations = new LocationList() { locationList = list.locations.locationList } };
            locList.DataContext = locTemp;
            Serializer.save(locTemp.locations.locationList, typeof(ObservableCollection<Location>), LOC_STORE, store);
        }

        async private void pinItem_Click(object sender, RoutedEventArgs e)
        {
            MenuFlyoutItem locationUrl = sender as MenuFlyoutItem;
            if (locationUrl != null)
            {
                Location location = findLocation(locationUrl.Tag.ToString());
                if (location != null)
                {
                    SecondaryTile secondaryTile = new SecondaryTile() { Arguments = location.LocUrl, TileId = location.Lat + "_" + location.Lon, DisplayName = location.LocName, RoamingEnabled = true };
                    secondaryTile.VisualElements.ShowNameOnSquare150x150Logo = true;
                    secondaryTile.VisualElements.ShowNameOnWide310x150Logo = true;
                    secondaryTile.VisualElements.Square150x150Logo = new Uri("ms-appx:///Assets/Square71x71Logo.scale-240.png");
                    secondaryTile.VisualElements.Square310x310Logo = new Uri("ms-appx:///Assets/SmallLogo.scale-240.png");
                    secondaryTile.VisualElements.Wide310x150Logo = new Uri("ms-appx:///Assets/WideLogo.scale-240.png");
                    await secondaryTile.RequestCreateAsync();
                }
            }
        }

        private Location findLocation(string url)
        {
            LocationTemplate list = locList.DataContext as LocationTemplate;
            if (list != null)
            {
                foreach (Location loc in list.locations.locationList)
                {
                    if (loc.LocUrl == url)
                    {
                        return loc;
                    }
                }
            }
            return null;
        }

        private void makeItemDefault_Click(object sender, RoutedEventArgs e)
        {
            LocationTemplate list = locList.DataContext as LocationTemplate;
            Location item = findLocation((sender as MenuFlyoutItem).Tag.ToString());
            if (list != null && item != null)
            {
                foreach (Location old in list.locations.locationList)
                {
                    if (old == item)
                    {
                        old.IsDefault = true;
                        old.Image = "/Assets/favs.png";
                    }
                    else
                    {
                        old.IsDefault = false;
                        old.Image = null;
                    }
                }
            }
            locList.DataContext = null;
            LocationTemplate locTemp = new LocationTemplate() { locations = new LocationList() { locationList = list.locations.locationList } };
            locList.DataContext = locTemp;
            Serializer.save(locTemp.locations.locationList, typeof(ObservableCollection<Location>), LOC_STORE, store);
        }

        private void StackPanel_Holding(object sender, HoldingRoutedEventArgs e)
        {
            FrameworkElement senderElement = sender as FrameworkElement;
            FlyoutBase flyoutBase = FlyoutBase.GetAttachedFlyout(senderElement);
            flyoutBase.ShowAt(senderElement);
        }
        #endregion

        private void ContentRoot_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //show and hide the add location button/app menu
            if ((sender as Pivot).SelectedIndex == 1)
            {
                appBar.Visibility = Windows.UI.Xaml.Visibility.Visible;
            }
            else
            {
                appBar.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            }
        }
    }
}
