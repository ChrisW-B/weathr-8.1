using LocationHelper;

namespace Weathr81.HelperClasses
{
    class MapLaunchClass
    {
        public enum mapType
        {
            radar,
            satellite
        }
        public GeoTemplate loc { get; set; }
        public mapType type { get; set; }
    }
}
