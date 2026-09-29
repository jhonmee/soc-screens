// MuroSOC - PageScript
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace MuroSoc
{
    internal static class PageScript
    {
        private const string ResourceName = "MuroSoc.Scripts.murosoc.js";
        private const string Placeholder = "__MUROSOC_CONFIG__";

        private static string template;

        public static string Build(TabModel settings)
        {
            return Template.Replace(Placeholder, ConfigJson(settings));
        }

        public static string ConfigJson(TabModel settings)
        {
            PageConfig config = new PageConfig();
            config.HideScrollbars = settings.HideScrollbars;
            config.MinWidth = settings.MinWidth;
            config.Hide = new List<string>(settings.HideSelectors);
            config.Isolate = settings.IsolateSelector ?? string.Empty;
            config.Css = settings.CustomCss ?? string.Empty;
            config.CssPattern = settings.CustomCssPattern ?? string.Empty;
            config.Pan = settings.PanEnabled;
            config.PanX = settings.PanX;
            config.PanY = settings.PanY;
            config.PanSelector = settings.PanSelector ?? string.Empty;
            return Serialize(config);
        }

        public static T Parse<T>(string json) where T : class
        {
            if (string.IsNullOrEmpty(json) || json == "null")
            {
                return null;
            }
            try
            {
                using (MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(json)))
                {
                    return new DataContractJsonSerializer(typeof(T)).ReadObject(stream) as T;
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static string Serialize<T>(T value)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                new DataContractJsonSerializer(typeof(T)).WriteObject(stream, value);
                return Encoding.UTF8.GetString(stream.ToArray());
            }
        }

        private static string Template
        {
            get
            {
                if (template == null)
                {
                    using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(ResourceName))
                    {
                        if (stream == null)
                        {
                            throw new InvalidOperationException("Falta el recurso " + ResourceName);
                        }
                        using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                        {
                            template = reader.ReadToEnd();
                        }
                    }
                }
                return template;
            }
        }
    }

    [DataContract]
    internal sealed class PageConfig
    {
        [DataMember(Name = "hideScrollbars", Order = 1)]
        public bool HideScrollbars { get; set; }

        [DataMember(Name = "minWidth", Order = 2)]
        public int MinWidth { get; set; }

        [DataMember(Name = "hide", Order = 3)]
        public List<string> Hide { get; set; }

        [DataMember(Name = "isolate", Order = 4)]
        public string Isolate { get; set; }

        [DataMember(Name = "css", Order = 5)]
        public string Css { get; set; }

        [DataMember(Name = "cssPattern", Order = 6)]
        public string CssPattern { get; set; }

        [DataMember(Name = "pan", Order = 7)]
        public bool Pan { get; set; }

        [DataMember(Name = "panX", Order = 8)]
        public int PanX { get; set; }

        [DataMember(Name = "panY", Order = 9)]
        public int PanY { get; set; }

        [DataMember(Name = "panSelector", Order = 10)]
        public string PanSelector { get; set; }
    }

    [DataContract]
    internal sealed class PageMessage
    {
        [DataMember(Name = "type")]
        public string Type { get; set; }

        [DataMember(Name = "value")]
        public bool Value { get; set; }

        [DataMember(Name = "mode")]
        public string Mode { get; set; }

        [DataMember(Name = "selector")]
        public string Selector { get; set; }
    }

    [DataContract]
    internal sealed class BusyState
    {
        [DataMember(Name = "typed")]
        public bool Typed { get; set; }

        [DataMember(Name = "dialog")]
        public bool Dialog { get; set; }
    }

    [DataContract]
    internal sealed class PanState
    {
        [DataMember(Name = "x")]
        public int X { get; set; }

        [DataMember(Name = "y")]
        public int Y { get; set; }

        [DataMember(Name = "selector")]
        public string Selector { get; set; }
    }
}
