using System.Collections.Generic;

namespace FlickrInfo
{
   public class FlickrData
    {
        public bool fail { get; set; }
        public string error { get; set; }
        public List<FlickrImage> images { get; set; }
    }
}
