using System;
using System.IO;
using System.Xml.Serialization;
using Windows.Storage;

namespace Serializer
{
    public class SerializerClass
    {
        public static void save(Object locations, Type type, string value, ApplicationDataContainer store)
        {
            String serialized = serialize(locations, type);
            if (serialized.Length > 0)
            {
                store.Values[value] = serialized;
            }
        }
        public static Object get(string value, Type type, ApplicationDataContainer store)
        {
            string locAsXml = (string)store.Values[value];
            try
            {
                XmlSerializer serializer = new XmlSerializer(type);
                Object val = new Object();
                using (var reader = new StringReader(locAsXml))
                {
                    val = serializer.Deserialize(reader);
                }
                return val;
            }
            catch
            {
                Object empty = new Object();
                return empty;
            }

        }
        private static string serialize(Object obj, Type type)
        {
            try
            {
                XmlSerializer xmlIzer = new XmlSerializer(type);
                var writer = new StringWriter();
                xmlIzer.Serialize(writer, obj);
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
