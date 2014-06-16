using LocationHelper;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Windows.Storage;

namespace Serializer
{
    public class SerializerClass
    {
        private static ApplicationDataContainer store = Windows.Storage.ApplicationData.Current.RoamingSettings;

        public static void save(ObservableCollection<Location> locations, string value)
        {
            String serialized = serialize(locations);
            if (serialized.Length > 0)
            {
                store.Values[value] = serialized;
            }
        }
        public static ObservableCollection<Location> get(string value)
        {
            string locAsXml = (string)store.Values[value];
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(ObservableCollection<Location>));
                ObservableCollection<Location> locs = new ObservableCollection<Location>();
                using (var reader = new StringReader(locAsXml))
                {
                    locs = (ObservableCollection<Location>)serializer.Deserialize(reader);
                }
                return locs;
            }
            catch
            {
                ObservableCollection<Location> emptyList = new ObservableCollection<Location>();
                return emptyList;
            }

        }
        private static string serialize(ObservableCollection<Location> locations)
        {
            try
            {
                XmlSerializer xmlIzer = new XmlSerializer(typeof(ObservableCollection<Location>));
                var writer = new StringWriter();
                xmlIzer.Serialize(writer, locations);
                return writer.ToString();
            }

            catch (Exception exc)
            {
                System.Diagnostics.Debug.WriteLine(exc);
                return String.Empty;
            }
        }
    }
}
