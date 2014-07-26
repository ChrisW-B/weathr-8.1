using System;
using System.IO;
using System.Xml.Serialization;
using Windows.Storage;

namespace SerializerClass
{
    public class Serializer
    {
        public static void save(Object obj, Type type, string value, ApplicationDataContainer store)
        {
            String serialized = serialize(obj, type);
            if (serialized.Length > 0)
            {
                store.Values[value] = serialized;
            }
        }
        public static Object get(string value, Type type, ApplicationDataContainer store)
        {
            Object val = null;
            try
            {
                string locAsXml = (string)store.Values[value];
                if (locAsXml != null)
                {
                    XmlSerializer serializer = new XmlSerializer(type);
                    using (var reader = new StringReader(locAsXml))
                    {
                        val = serializer.Deserialize(reader);
                    }
                }
            }
            catch
            {
                val = null;
            }
            return val;
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
