using QQS_UI.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace QQS_UI
{
    public static class PFAConfigrationLoader
    {
        public static string ConfigurationPath
        {
            get
            {
                string path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                path += "\\Piano From Above\\Config.xml";
                return File.Exists(path) ? path : null;
            }
        }

        /// <summary>
        /// Determines whether PFA configuration is available.
        /// </summary>
        public static bool IsConfigurationAvailable => ConfigurationPath != null;

        /// <summary>
        /// Load colors from PFA configuration if possible.
        /// </summary>
        /// <returns>
        /// If it fails to load PFA configuration, <see langword="null"/> will be returned.<br/>
        /// If it succeeds in loading config, then an array containing these colors will be returned.
        /// </returns>
        public static RGBAColor[] LoadPFAConfigurationColors()
        {
            if (!IsConfigurationAvailable)
            {
                return null;
            }
            XmlDocument doc = new();
            doc.Load(ConfigurationPath);
            XmlNode rootNode = doc.SelectSingleNode("PianoFromAbove");
            XmlNode visualNode = rootNode.SelectSingleNode("Visual");
            XmlNode colors = visualNode.SelectSingleNode("Colors");
            XmlNodeList actualColors = colors.SelectNodes("Color");
            List<RGBAColor> retColors = new();
            foreach (XmlNode node in actualColors)
            {
                byte r = byte.Parse(node.Attributes[0].Value);
                byte g = byte.Parse(node.Attributes[1].Value);
                byte b = byte.Parse(node.Attributes[2].Value);
                retColors.Add(new RGBAColor
                {
                    R = r,
                    G = g,
                    B = b,
                    A = 0xFF
                });
            }
            Console.WriteLine("PFA configuration color parsing complete. There are {0} colors.", retColors.Count);
            return retColors.ToArray();
        }
    }
}
