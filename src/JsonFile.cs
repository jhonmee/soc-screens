// MuroSOC - JsonFile
using System;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Xml;

namespace MuroSoc
{
    internal static class JsonFile
    {
        public static T Read<T>(string path) where T : class
        {
            using (FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                return (T)CreateSerializer(typeof(T)).ReadObject(stream);
            }
        }

        public static void Write<T>(string path, T value) where T : class
        {
            string temp = path + ".tmp";
            using (FileStream stream = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                using (XmlDictionaryWriter writer = JsonReaderWriterFactory.CreateJsonWriter(stream, new UTF8Encoding(false), false, true, "  "))
                {
                    CreateSerializer(typeof(T)).WriteObject(writer, value);
                    writer.Flush();
                }
                stream.Flush(true);
            }
            if (File.Exists(path))
            {
                try
                {
                    File.Replace(temp, path, null);
                    return;
                }
                catch (IOException)
                {
                    File.Delete(path);
                }
            }
            File.Move(temp, path);
        }

        private static DataContractJsonSerializer CreateSerializer(Type type)
        {
            DataContractJsonSerializerSettings settings = new DataContractJsonSerializerSettings();
            settings.UseSimpleDictionaryFormat = true;
            return new DataContractJsonSerializer(type, settings);
        }
    }
}
