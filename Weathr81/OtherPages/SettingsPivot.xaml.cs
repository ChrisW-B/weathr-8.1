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

// The Basic Page item template is documented at http://go.microsoft.com/fwlink/?LinkID=390556

namespace Weathr81.OtherPages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SettingsPivot : Page
    {
        private NavigationHelper navigationHelper;
        private ObservableDictionary defaultViewModel = new ObservableDictionary();

        public SettingsPivot()
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

        private const string UNITS_CHANGED = "unitsChanged";
        private const string UNITS_ARE_SI = "unitsAreSI";
        private ApplicationDataContainer store = Windows.Storage.ApplicationData.Current.RoamingSettings;
        private ApplicationDataContainer localStore = Windows.Storage.ApplicationData.Current.LocalSettings;
        bool doneSetting = false;

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            this.navigationHelper.OnNavigatedTo(e);
            updateSettings();
            doneSetting = true;
        }

        private void updateSettings()
        {
            if (localStore.Values.ContainsKey("flickrTile"))
            {
                transparentToggle.IsOn = (bool)localStore.Values["flickrTile"];
            }
            if (store.Values.ContainsKey(UNITS_ARE_SI))
            {
                units.IsOn = (bool)Serializer.get(UNITS_ARE_SI, typeof(bool), store);
            }
            else
            {
                Serializer.save(true, typeof(bool), UNITS_ARE_SI, store);
                units.IsOn = true;
            }
            if (store.Values.ContainsKey("allowBackground"))
            {
                bool allowed = (bool)store.Values["allowBackground"];
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
                store.Values["allowBackground"] = true;
                enableBackground.IsChecked = true;
                backgroundPanel.Visibility = Windows.UI.Xaml.Visibility.Visible;
            }
            if (store.Values.ContainsKey("updateFreq"))
            {
                updateFreq.Value = Convert.ToInt32(store.Values["updateFreq"]);
                currentRate.Text = printUpdateRate(updateFreq.Value);
            }
            else
            {
                updateFreq.Value = 120;
                registerRecurringBG();
                currentRate.Text = printUpdateRate(updateFreq.Value);
            }
        }



        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            this.navigationHelper.OnNavigatedFrom(e);
        }

        #endregion

        private void Units_Toggled(object sender, RoutedEventArgs e)
        {
            if (doneSetting)
            {
                ToggleSwitch toggle = sender as ToggleSwitch;
                Serializer.save(true, typeof(bool), UNITS_CHANGED, localStore);
                if (toggle != null)
                {
                    if (toggle.IsOn)
                    {
                        Serializer.save(true, typeof(bool), UNITS_ARE_SI, store);
                    }
                    else
                    {
                        Serializer.save(false, typeof(bool), UNITS_ARE_SI, store);
                    }
                }
            }
        }

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
        DispatcherTimer timer = new DispatcherTimer();
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

        void timer_Tick(object sender, object e)
        {
            registerRecurringBG(Convert.ToUInt32(updateFreq.Value));
            timer.Stop();
            timer = null;
            timer = new DispatcherTimer();
        }


        private void enableBackground_Checked(object sender, RoutedEventArgs e)
        {
            if (doneSetting)
            {
                store.Values["allowBackground"] = true;
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
                store.Values["allowBackground"] = false;
                UpdateTiles.Unregister("UV level updater");
                backgroundPanel.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            }
            else
            {
                return;
            }
        }
        private void registerRecurringBG(uint mins = 120)
        {
            store.Values["updateFreq"] = mins;
            localStore.Values["updateFreq"] = mins;
            UpdateTiles.Register(mins);
        }

        private void TransparentTile_Toggled(object sender, RoutedEventArgs e)
        {
            if (doneSetting)
            {
                localStore.Values["flickrTile"] = (sender as ToggleSwitch).IsOn;
            }
            return;
        }
    }
}
