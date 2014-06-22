using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace FlickrInfo
{
    public class GetFlickrInfo
    {
        private const string URL_PRE = "https://api.flickr.com/services/rest/?method=flickr.photos.search&api_key=";
        private const string GROUP_PRE = "&group_id=";
        private const string LAT_PRE = "&lat=";
        private const string LON_PRE = "&lon=";
        private const string TAGS_PRE = "&tags=";
        private const string URL_POST = "&per_page=500&tag_mode=any&content_type=1&media=photos&radius=32&format=rest";
        private const string YAHOO_GROUP = "1463451@N25";
        private string apiKey;
        public enum ImageSize
        {
            smallSq,
            largeSq,
            thumb,
            small240,
            small320,
            medium500,
            medium640,
            medium800,
            large
        }

        public GetFlickrInfo(string apiKey)
        {
            this.apiKey = apiKey;
        }


        public async Task<FlickrData> getImages(string searchTerms, bool useGroup, bool useLoc, string lat = "0", string lon = "0")
        {
            //asynchronously returns a list of photos following the given parameters
            Uri uri = new Uri(URL_PRE + apiKey + GROUP_PRE + YAHOO_GROUP + LAT_PRE + lat + LON_PRE + lon + TAGS_PRE + searchTerms + URL_POST, UriKind.Absolute);
            if (!useLoc)
            {
                uri = new Uri(URL_PRE + apiKey + GROUP_PRE + YAHOO_GROUP + TAGS_PRE + searchTerms + URL_POST, UriKind.Absolute);
            }
            if (!useGroup)
            {
                uri = new Uri(URL_PRE + apiKey + LAT_PRE + lat + LON_PRE + lon + TAGS_PRE + searchTerms + URL_POST, UriKind.Absolute);
                if (!useLoc)
                {
                    uri = new Uri(URL_PRE + apiKey + TAGS_PRE + searchTerms + URL_POST, UriKind.Absolute);
                }
            }
            return getPhotoList(await getXml(uri));
        }
        public async Task<FlickrData> getImages(string searchTerms, bool useGroup, bool useLoc, double lat = 0, double lon = 0)
        {
            //asynchronously returns a list of photos following the given parameters
            Uri uri = new Uri(URL_PRE + apiKey + GROUP_PRE + YAHOO_GROUP + LAT_PRE + lat + LON_PRE + lon + TAGS_PRE + searchTerms + URL_POST, UriKind.Absolute);
            if (!useLoc)
            {
                uri = new Uri(URL_PRE + apiKey + GROUP_PRE + YAHOO_GROUP + TAGS_PRE + searchTerms + URL_POST, UriKind.Absolute);
            }
            if (!useGroup)
            {
                uri = new Uri(URL_PRE + apiKey + LAT_PRE + lat + LON_PRE + lon + TAGS_PRE + searchTerms + URL_POST, UriKind.Absolute);
                if (!useLoc)
                {
                    uri = new Uri(URL_PRE + apiKey + TAGS_PRE + searchTerms + URL_POST, UriKind.Absolute);
                }
            }
            return getPhotoList(await getXml(uri));
        }

        public Uri getImageUri(FlickrImage img, ImageSize sz)
        {
            //public task to get the url of an image from the image
            string url = "https://farm" + img.farm + ".staticflickr.com/" + img.server + "/" + img.id + "_" + img.secret;
            switch (sz)
            {
                case ImageSize.smallSq:
                    url += "_s";
                    break;
                case ImageSize.largeSq:
                    url += "_q";
                    break;
                case ImageSize.thumb:
                    url += "_t";
                    break;
                case ImageSize.small240:
                    url += "_m";
                    break;
                case ImageSize.small320:
                    url += "_n";
                    break;
                case ImageSize.medium500:
                    break;
                case ImageSize.medium640:
                    url += "_z";
                    break;
                case ImageSize.medium800:
                    url += "_c";
                    break;
                case ImageSize.large:
                    url += "_b";
                    break;
            }
            return new Uri(url + ".jpg", UriKind.Absolute);
        }

        async private Task<XDocument> getXml(Uri uri)
        {
            //gets an xml document from a uri
            try
            {
                HttpClient client = new HttpClient();
                Stream str = await client.GetStreamAsync(uri);
                return XDocument.Load(str);
            }
            catch
            {
                return null;
            }
        }
        private FlickrData getPhotoList(XDocument doc)
        {
            if (doc != null)
            {
                //converts a xml document into a flickr data class, with a list of photos
                FlickrData d = new FlickrData();
                XElement rsp = doc.Element("rsp");
                if (rsp != null)
                {
                    XAttribute stat = rsp.Attribute("stat");
                    if ((string)stat == "fail")
                    {
                        d.fail = true;
                        d.error = "error getting images in xml";
                        return d;
                    }
                    else
                    {
                        d.fail = false;
                        d.error = "all clear!";
                    }
                    d.images = new List<FlickrImage>();
                    XElement photos = doc.Element("rsp").Element("photos");
                    if (photos != null)
                    {
                        foreach (XElement photo in photos.Elements("photo"))
                        {
                            d.images.Add(new FlickrImage { farm = photo.Attribute("farm").Value, server = photo.Attribute("server").Value, secret = photo.Attribute("secret").Value, id = photo.Attribute("id").Value });
                        }
                        return d;
                    }
                }
            }
            return new FlickrData() { fail = true, error = "document had invalid data" };
        }
    }
}
