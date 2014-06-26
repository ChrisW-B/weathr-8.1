using System;
using System.Collections.Generic;

namespace FlickrInfo
{
   public class FlickrData
    {
        public bool fail { get; set; }
        public string errorMsg { get; set; }
        public List<FlickrImage> images { get; set; }
    }
   public class FlickrImage
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
}
