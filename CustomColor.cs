using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using QQS_UI.Core;

#nullable disable
namespace QQS_UI
{
    public class CustomColor
    {
        public RGBAColor[] Colors;
        public CustomColor(string colorFileName = "colors.json")
        {
            if (!File.Exists(colorFileName))
            {
                string colors = JsonSerializer.Serialize(Global.KeyColors);
                File.WriteAllText(colorFileName, colors);
                Colors = new RGBAColor[96];
                Array.Copy(Global.DefaultColors, Colors, 96);
            }
            else
            {
                try
                {
                    string colorData = File.ReadAllText(colorFileName);
                    Colors = JsonSerializer.Deserialize<RGBAColor[]>(colorData);
                }
                catch
                {
                    Console.WriteLine("Error loading colors configuration, default colors will be used...");
                    Colors = new RGBAColor[96];
                    Array.Copy(Global.DefaultColors, Colors, 96);
                }
            }
            Console.WriteLine("Colors loading is complete. There are {0} colors.", Colors.Length);
        }

        private CustomColor()
        {

        }
        /// <summary>
        /// Loads the color of the specified file into the current instance.
        /// </summary>
        /// <param name="colorFileName">Color file path.</param>
        /// <returns>If the file does not exist, -1; if there is a problem loading it, 1; if there is no error, 0.</returns>
        public int Load(string colorFileName)
        {
            if (!File.Exists(colorFileName))
            {
                return -1;
            }
            string colorData = File.ReadAllText(colorFileName);
            RGBAColor[] lastColors = Colors;
            try
            {
                Colors = JsonSerializer.Deserialize<RGBAColor[]>(colorData);
            }
            catch
            {
                Colors = lastColors;
                return 1;
            }
            return 0;
        }

        /// <summary>
        /// Copy colors owned by current instance to <see cref="Global.KeyColors"/>.
        /// </summary>
        /// <remarks>
        /// This is not a thread-safe operation.
        /// </remarks>
        /// <returns>
        /// If colors owned by <see langword="this"/> is null then -1 will be returned;<br/>
        /// If the color array owned by <see langword="this"/> is not null but its length equals 0, then 1 is returned;<br/>
        /// If the operation is successful, returns 0.
        /// </returns>
        public int SetGlobal()
        {
            if (Colors == null)
            {
                return -1;
            }
            if (Colors.Length == 0)
            {
                return 1;
            }
            Global.KeyColors = new RGBAColor[Colors.Length];
            Global.NoteColors = new RGBAColor[Colors.Length];
            Array.Copy(Colors, Global.KeyColors, Colors.Length);
            Array.Copy(Colors, Global.NoteColors, Colors.Length);
            return 0;
        }

        public void UseDefault()
        {
            Colors = new RGBAColor[96];
            Array.Copy(Global.DefaultColors, Colors, 96);
            Global.KeyColors = Colors;
        }

        public CustomColor Shuffle()
        {
            CustomColor shuffled = new()
            {
                Colors = new RGBAColor[Colors.Length]
            };
            Array.Copy(Colors, shuffled.Colors, Colors.Length);
            Random rand = new Random(DateTime.Now.Millisecond);
            for (int i = 0; i < shuffled.Colors.Length; i++)
            {
                int x, y;
                RGBAColor col;
                x = rand.Next(0, shuffled.Colors.Length);
                do
                {
                    y = rand.Next(0, shuffled.Colors.Length);
                } while (y == x);

                col = shuffled.Colors[x];
                shuffled.Colors[x] = shuffled.Colors[y];
                shuffled.Colors[y] = col;
            }

            return shuffled;
        }

        public CustomColor Exchange(RGBAColor[] colors)
        {
            CustomColor old = new()
            {
                Colors = new RGBAColor[Colors.Length]
            };
            Array.Copy(Colors, old.Colors, Colors.Length);
            Colors = new RGBAColor[colors.Length];
            colors.CopyTo(Colors, 0);
            return old;
        }
    }
}
