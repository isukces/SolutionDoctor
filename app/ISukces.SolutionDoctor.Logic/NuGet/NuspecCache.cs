using System;
using System.Collections.Generic;
using System.IO;
using isukces.json;
using Newtonsoft.Json;

namespace ISukces.SolutionDoctor.Logic.NuGet
{
    internal class NuspecCache
    {
        #region Static Methods

        public static Dictionary<string, Nuspec> GetForDirectory(DirectoryInfo directory)
        {
            var fn     = GetFileName(directory);
            var result = new Dictionary<string, Nuspec>(StringComparer.OrdinalIgnoreCase);
            if (!fn.Exists)
                return result;
            var fromFile = Json1.Utils.Load<Dictionary<string, Nuspec>>(fn);
            if (fromFile != null)
                foreach (var i in fromFile)
                    result[i.Key] = i.Value;
            return result;
        }

        public static void Save(DirectoryInfo directory, Dictionary<string, Nuspec> cache1)
        {
            var fn = GetFileName(directory);
            Json1.Utils.Save(fn, cache1);
        }

        private static FileInfo GetFileName(DirectoryInfo directory)
        {
            return new FileInfo(Path.Combine(directory.FullName, "$solutionDoctorPackage.cache"));
        }

        #endregion
    }

    public class Json1
    {
        private static JsonSerializer MySerializerFactory()
        {
            var sf = JsonUtils.DefaultSerializerFactory();
            sf.Converters.Add(new MyVersionConverter());
            return sf;
        }

        public static JsonUtils Utils
        {
            get { return new JsonUtils(MySerializerFactory); }
        }

        private class MyVersionConverter : JsonConverter
        {
            public override bool CanConvert(Type objectType)
            {
                return objectType == typeof(Version);
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue,
                JsonSerializer serializer)
            {
                var value = reader.Value;
                return value == null ? null : Version.Parse(value.ToString());
            }

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                var txt = ((Version)value).ToString();
                writer.WriteValue(txt);
            }
        }
    }
}