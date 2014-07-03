using System;
using System.Collections.Generic;

namespace FlickrInfo
{
   public class FlickrData
    {
        public bool fail { get; set; }
        public string errorMsg { get; set; }
        public List<FlickrImageData> images { get; set; }
    }
   public class FlickrImageData
   {
       public string farm { get; set; }
       public string server { get; set; }
       public string secret { get; set; }
       public string id { get; set; }
       public string owner { get; set; }
   }
   public class FlickrUser
   {
       public string userName { get; set; }
       public string realName { get; set; }
       public Uri profUri { get; set; }
       public bool fail {  get;set; }
       public string errorMsg { get; set; }
   }
   public class FlickrImage
   {
       public Uri uri { get; set; }
       public Uri artistUri { get; set; }
       public string title { get; set; }
       public string artist { get; set; }
   }

}
