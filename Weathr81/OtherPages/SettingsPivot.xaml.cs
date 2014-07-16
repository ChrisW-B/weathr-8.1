using BackgroundTask;
using LocationHelper;
using SerializerClass;
using StoreLabels;
using System;
using System.Collections.ObjectModel;
using Weathr81.Common;
using Windows.Storage;
using Windows.UI.Popups;
using Windows.UI.StartScreen;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;

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
            
        }
        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            this.navigationHelper.OnNavigatedFrom(e);
        }
        #endregion
        #region variables
        private ApplicationDataContainer store = Windows.Storage.ApplicationData.Current.RoamingSettings;
        private ApplicationDataContainer localStore = Windows.Storage.ApplicationData.Current.LocalSettings;
        bool doneSetting = false;
        #endregion
        #region settings setup
        private void updateSettings()
        {
            doneSetting = false;
            setUpGeneralSection();
            setupLocationsSection();
            setUpTileSection();
            doneSetting = true;
        }

        private void setupLocationsSection()
        {
            if (localStore.Values.ContainsKey(Values.ALLOW_LOC))
            {
                autoLocateToggle.IsOn = (bool)localStore.Values[Values.ALLOW_LOC];
            }
            else
            {
                localStore.Values[Values.ALLOW_LOC] = true;
                autoLocateToggle.IsOn = true;
            }
            if (store.Values.ContainsKey(Values.LOC_STORE))
            {
                ObservableCollection<Location> list = (Serializer.get(Values.LOC_STORE, typeof(ObservableCollection<Location>), store) as ObservableCollection<Location>);
                if (list != null)
                {
                    LocationTemplate locTemplate = new LocationTemplate() { locations = new LocationList() { locationList = list } };
                    locList.DataContext = locTemplate;
                }
            }
        }
        private void setUpGeneralSection()
        {
            if (store.Values.ContainsKey(Values.TWENTY_FOUR_HR_TIME))
            {
                timeType.IsOn = (bool)store.Values[Values.TWENTY_FOUR_HR_TIME];
            }
            else
            {
                timeType.IsOn = false;
                store.Values[Values.TWENTY_FOUR_HR_TIME] = false;
            }
            if (store.Values.ContainsKey(Values.UNITS_ARE_SI))
            {
                units.IsOn = (bool)store.Values[Values.UNITS_ARE_SI];
            }
            else
            {
                store.Values[Values.UNITS_ARE_SI] = true;
                units.IsOn = true;
            }

            if (store.Values.ContainsKey(Values.ALLOW_MAIN_BG))
            {
                getImages.IsOn = (bool)store.Values[Values.ALLOW_MAIN_BG];
                if (getImages.IsOn)
                {
                    getImagesOn.Visibility = Windows.UI.Xaml.Visibility.Visible;
                }
                else
                {
                    getImagesOn.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                }
            }
            else
            {
                store.Values[Values.ALLOW_MAIN_BG] = true;
                getImages.IsOn = true;
                getImagesOn.Visibility = Windows.UI.Xaml.Visibility.Visible;
            }

            if (store.Values.ContainsKey(Values.MAIN_BG_WIFI_ONLY))
            {
                bgOnWifi.IsOn = (bool)store.Values[Values.MAIN_BG_WIFI_ONLY];
                
            }
            else
            {
                store.Values[Values.MAIN_BG_WIFI_ONLY] = false;
                bgOnWifi.IsOn = true;
            }


        }
        private void setUpTileSection()
        {
            
            if (localStore.Values.ContainsKey(Values.ALLOW_BG_TASK))
            {
                bool allowed = (bool)localStore.Values[Values.ALLOW_BG_TASK];
                enableBackground.IsOn = allowed;
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
                localStore.Values[Values.ALLOW_BG_TASK] = false;
                enableBackground.IsOn = false;
                backgroundPanel.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            }
            if (localStore.Values.ContainsKey(Values.UPDATE_ON_CELL))
            {
                wifiOnlyToggle.IsOn = !(bool)localStore.Values[Values.UPDATE_ON_CELL];
            }
            else
            {
                wifiOnlyToggle.IsOn = false;
                localStore.Values[Values.UPDATE_ON_CELL] = true;
            }
            if (localStore.Values.ContainsKey(Values.TRANSPARENT_TILE))
            {
                transparentToggle.IsOn = !(bool)localStore.Values[Values.TRANSPARENT_TILE];
            }
            else
            {
                transparentToggle.IsOn = false;
                localStore.Values[Values.TRANSPARENT_TILE] = true;
            }

            if (localStore.Values.ContainsKey(Values.UPDATE_FREQ))
            {
                updateFreq.Value = Convert.ToInt32(localStore.Values[Values.UPDATE_FREQ]);
                currentRate.Text = printUpdateRate(updateFreq.Value);
            }
            else
            {
                updateFreq.Value = 120;
                currentRate.Text = printUpdateRate(updateFreq.Value);
                localStore.Values[Values.UPDATE_FREQ] = (uint)120;
            }

            if (store.Values.ContainsKey(Values.TILE_UNITS_ARE_SI))
            {
                tileUnits.IsOn = (bool)store.Values[Values.TILE_UNITS_ARE_SI];
            }
            else
            {
                tileUnits.IsOn = true;
                store.Values[Values.TILE_UNITS_ARE_SI] = true;
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
                    localStore.Values[Values.UNITS_CHANGED] = true;
                    if (toggle.IsOn)
                    {
                        store.Values[Values.UNITS_ARE_SI] = true;
                        return;
                    }
                    store.Values[Values.UNITS_ARE_SI] = false;
                }
            }
        }

        private void getImages_Toggled(object sender, RoutedEventArgs e)
        {
            if (doneSetting)
            {
                ToggleSwitch toggle = sender as ToggleSwitch;
                if (toggle != null)
                {
                    if (toggle.IsOn)
                    {
                        store.Values[Values.ALLOW_MAIN_BG] = true;
                        getImagesOn.Visibility = Windows.UI.Xaml.Visibility.Visible;
                        return;
                    }
                    store.Values[Values.ALLOW_MAIN_BG] = false;
                    getImagesOn.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                }
            }
            return;
        }

        private void bgOnWifi_Toggled(object sender, RoutedEventArgs e)
        {
            if (doneSetting)
            {
                ToggleSwitch toggle = sender as ToggleSwitch;
                if (toggle != null)
                {
                    if (toggle.IsOn)
                    {
                        store.Values[Values.MAIN_BG_WIFI_ONLY] = true;
                        return;
                    }
                    store.Values[Values.MAIN_BG_WIFI_ONLY] = false;
                }
            }
            return;
        }

        private void timeType_Toggled(object sender, RoutedEventArgs e)
        {
            if (doneSetting)
            {
                ToggleSwitch t = sender as ToggleSwitch;
                if (t != null)
                {
                    store.Values[Values.TWENTY_FOUR_HR_TIME] = t.IsOn;
                }
            }
        }
        #endregion

        #region tile pivot
        private void enableBackground_Toggled(object sender, RoutedEventArgs e)
        {
            if (doneSetting)
            {
                ToggleSwitch enabledBg = sender as ToggleSwitch;
                if (enabledBg != null)
                {
                    if (enabledBg.IsOn)
                    {
                        localStore.Values[Values.ALLOW_BG_TASK] = true;
                        backgroundPanel.Visibility = Windows.UI.Xaml.Visibility.Visible;
                        if (localStore.Values.ContainsKey(Values.UPDATE_FREQ))
                        {
                            registerRecurringBG((uint)localStore.Values[Values.UPDATE_FREQ]);
                        }
                        else
                        {
                            registerRecurringBG();
                        }
                    }
                    else
                    {
                        localStore.Values[Values.ALLOW_BG_TASK] = false;
                        UpdateTiles.Unregister(Values.TASK_NAME);
                        backgroundPanel.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                    }
                }
            }
            return;
        }
       
        private void registerRecurringBG(uint mins = 120)
        {
            localStore.Values[Values.UPDATE_FREQ] = mins;
            UpdateTiles.Register(Values.TASK_NAME, mins);
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
                localStore.Values[Values.TRANSPARENT_TILE] = !(sender as ToggleSwitch).IsOn;
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
                    localStore.Values[Values.UPDATE_ON_CELL] = !t.IsOn;
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
                        store.Values[Values.TILE_UNITS_ARE_SI] = true;
                        return;
                    }
                    store.Values[Values.TILE_UNITS_ARE_SI] = false;
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
                DateTime firstRun = (DateTime)Serializer.get(Values.FIRST_START, typeof(DateTime), store);
                store.Values.Clear();
                localStore.Values.Clear();
                UpdateTiles.Unregister(Values.TASK_NAME);
                Serializer.save(firstRun, typeof(DateTime), Values.FIRST_START, store);
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
                localStore.Values[Values.ALLOW_LOC] = (sender as ToggleSwitch).IsOn;
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
            Serializer.save(locTemp.locations.locationList, typeof(ObservableCollection<Location>), Values.LOC_STORE, store);
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
            Serializer.save(locTemp.locations.locationList, typeof(ObservableCollection<Location>), Values.LOC_STORE, store);
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
