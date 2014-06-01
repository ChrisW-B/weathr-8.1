using FlickrInfo;
using System;
using WeatherData;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using WundergroundData;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=391641

namespace Weathr81
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();

            this.NavigationCacheMode = NavigationCacheMode.Required;
        }

        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.
        /// This parameter is typically used to configure the page.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            // TODO: Prepare page for display here.

            // TODO: If your application contains multiple pages, ensure that you are
            // handling the hardware Back button by registering for the
            // Windows.Phone.UI.Input.HardwareButtons.BackPressed event.
            // If you are using the NavigationHelper provided by some templates,
            // this event is handled for you.
            checkForecast();
        }

        async private void checkForecast()
        {
            String apiKey = "102b8ec7fbd47a05";
            GetWundergroundData wData = new GetWundergroundData(apiKey, 41, -74);
            WeatherInfo data = await wData.getConditions();
            string info = data.currentConditions;
            String flickrApiKey = "2781c025a4064160fc77a52739b552ff";
            GetFlickrInfo i = new GetFlickrInfo(flickrApiKey);
            FlickrData d = await i.getFromYahooWeatherGroup("thunderstorm", true, 41, -74);
            if (!d.fail && d.images.Count > 0)
            {
                Random r = new Random();
                int randVal = r.Next(d.images.Count);
                Uri uri = i.getImageUri(d.images[randVal]);
                Uri newUri = uri;
            }
        }
    }
}
