﻿using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
//using System.Windows.Shapes;
using QQS_UI.Core;
using Path = System.IO.Path;
using System.Diagnostics;
using System.Threading;
using System.Drawing;
using System.Drawing.Imaging;
using Color = System.Windows.Media.Color;
using PixelFormat = System.Drawing.Imaging.PixelFormat;
using static System.Runtime.CompilerServices.RuntimeHelpers;

namespace QQS_UI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private RenderFile file = null;
        private bool isLoading = false;
        private Core.RenderOptions options = Core.RenderOptions.CreateRenderOptions();
        private CommonRenderer renderer = null;
        private readonly Config config;
        private readonly CustomColor customColors;
        private int keyHeightPercentage = 15;
        private const string DefaultVideoFilter = "Video (*.mp4, *.avi, *.mov)|*.mp4;*.avi;*.mov",
            TransparentVideoFilter = "Video (*.mov)|*.mov";
        public MainWindow()
        {
            InitializeComponent();
            config = new Config();
            customColors = new CustomColor();
            if (config.CachedMIDIDirectory == null)
            {
                config.CachedMIDIDirectory = new OpenFileDialog().InitialDirectory;
            }
            if (config.CachedVideoDirectory == null)
            {
                config.CachedVideoDirectory = new SaveFileDialog().InitialDirectory;
            }
            if (config.CachedColorDirectory == null)
            {
                config.CachedColorDirectory = config.CachedVideoDirectory;
            }
            config.SaveConfig();
            previewColor.Background = new SolidColorBrush(new Color
            {
                R = (byte)(options.DivideBarColor & 0xff),
                G = (byte)((options.DivideBarColor & 0xff00) >> 8),
                B = (byte)((options.DivideBarColor & 0xff0000) >> 16),
                A = 0xff
            });
            previewBackgroundColor.Background = new SolidColorBrush(new Color
            {
                R = 0,
                G = 0,
                B = 0,
                A = 255
            });

            if (!PFAConfigrationLoader.IsConfigurationAvailable)
            {
                loadPFAColors.IsEnabled = false;
            }

            renderWidth.Value = 1920;
            renderHeight.Value = 1080;
            noteSpeed.Value = 1.5;
#if DEBUG
            Title += " (Debug)";
#endif
            unpressedKeyboardGradientStrength.Value = Global.DefaultUnpressedWhiteKeyGradientScale;
            pressedKeyboardGradientStrength.Value = Global.DefaultPressedWhiteKeyGradientScale;
            noteGradientStrength.Value = Global.DefaultNoteGradientScale;
            separatorGradientStrength.Value = Global.DefaultSeparatorGradientScale;
            noteBorderWidth.Value = 1;
            noteBorderShade.Value = 5;
            denseNoteShade.Value = 5;
            noteAlpha.Value = 144;
            pressedNoteShadeDecrement.Value = 80;

            int processorCount = Environment.ProcessorCount;
            maxMidiLoaderConcurrency.Value = processorCount;
            maxRenderConcurrency.Value = processorCount;
            Global.MaxMIDILoaderConcurrency = -1;
            Global.MaxRenderConcurrency = -1;

            options.VideoQuality = 17;
        }

        private void openMidi_Click(object sender, RoutedEventArgs e)
        {
            if (!Directory.Exists(config.CachedMIDIDirectory))
            {
                config.CachedMIDIDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            }
            OpenFileDialog dialog = new()
            {
                Filter = "MIDI File (*.mid)|*.mid",
                InitialDirectory = config.CachedMIDIDirectory
            };
            if ((bool)dialog.ShowDialog())
            {
                string midiDirectory = Path.GetDirectoryName(Path.GetFullPath(dialog.FileName));
                config.CachedMIDIDirectory = midiDirectory;
                midiPath.Text = dialog.FileName;
                config.SaveConfig();
            }
        }

        private void loadButton_Click(object sender, RoutedEventArgs e)
        {
            if (isLoading)
            {
                return;
            }
            string fileName = midiPath.Text;
            if (!File.Exists(fileName) || !fileName.EndsWith(".mid"))
            {
                _ = MessageBox.Show("Incorrect MIDI path!" , "Unable to load MIDI file");
                return;
            }
            trackCount.Content = "Loading...";
            noteCount.Content = "Loading...";
            midiLen.Content = "Loading...";
            midiPPQ.Content = "Loading...";
            _ = Task.Run(() =>
            {
                isLoading = true;
                file = new RenderFile(fileName);
                isLoading = false;
                TimeSpan midilen = Global.GetTimeOf(file.MidiTime, file.Division, file.Tempos);
                Dispatcher.Invoke(() =>
                {
                    Resources["midiLoaded"] = true;
                    trackCount.Content = file.TrackCount.ToString();
                    noteCount.Content = file.NoteCount.ToString();
                    midiLen.Content = midilen.ToString("mm\\:ss\\.fff");
                    midiPPQ.Content = file.Division;
                });
            });
        }

        private void unloadButton_Click(object sender, RoutedEventArgs e)
        {
            int gen = GC.GetGeneration(file);
            file = null;
            GC.Collect(gen);
            Resources["midiLoaded"] = false;
            Console.WriteLine("Loaded.");
            noteCount.Content = "-";
            trackCount.Content = "-";
            midiLen.Content = "--:--.---";
            midiPPQ.Content = '-';
        }

        private void noteSpeed_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            options.NoteSpeed = noteSpeed.Value;
        }

        private void selectOutput_Click(object sender, RoutedEventArgs e)
        {
            if (!Directory.Exists(config.CachedVideoDirectory))
            {
                config.CachedVideoDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);
            }
            SaveFileDialog dialog = new()
            {
                Filter = options.TransparentBackground ? TransparentVideoFilter : DefaultVideoFilter,
                Title = "Select a location to save the output video",
                InitialDirectory = config.CachedVideoDirectory
            };
            if ((bool)dialog.ShowDialog())
            {
                config.CachedVideoDirectory = Path.GetDirectoryName(Path.GetFullPath(dialog.FileName));
                outputPath.Text = dialog.FileName;
                config.SaveConfig();
            }
        }

        private void startRender_Click(object sender, RoutedEventArgs e)
        {
            if (file == null)
            {
                _ = MessageBox.Show("Unable to render: \nMIDI file is empty. Please check if a MIDI file is loaded." , "No MIDI file");
                return;
            }
            options.Input = midiPath.Text;
            options.Output = outputPath.Text;
            options.PreviewMode = false;
            options.AdditionalFFMpegArgument = additionalFFArgs.Text;
            Resources["notRendering"] = Resources["notRenderingOrPreviewing"] = false;
            renderer = new CommonRenderer(file, options);
            _ = Task.Run(() =>
            {
                Console.WriteLine("Prepare for rendering...");
                renderer.Render();
                int gen = GC.GetGeneration(renderer);
                Dispatcher.Invoke(() =>
                {
                    renderer = null;
                    Resources["notRendering"] = Resources["notRenderingOrPreviewing"] = true;
                });
                GC.Collect(gen);
            });
        }

        private void interruptButton_Click(object sender, RoutedEventArgs e)
        {
            if (renderer != null)
            {
                renderer.Interrupt = true;
            }
        }

        private void crfSelect_ValueChanged(object sender, RoutedPropertyChangedEventArgs<decimal> e)
        {
            options.VideoQuality = (int)crfSelect.Value;
        }

        private void enableTranparentBackground_CheckToggled(object sender, RoutedPropertyChangedEventArgs<bool> e)
        {
            options.TransparentBackground = e.NewValue;
            if (options.TransparentBackground)
            {
                if (!outputPath.Text.EndsWith(".mov"))
                {
                    outputPath.Text = string.Concat(outputPath.Text.AsSpan(0, outputPath.Text.Length - 4), ".mov");
                }
                if (!additionalFFArgs.Text.ToLower().Contains("-vodec png"))
                {
                    additionalFFArgs.Text += " -vcodec png";
                }
            }
        }
        private void startPreview_Click(object sender, RoutedEventArgs e)
        {
            if (file == null)
            {
                _ = MessageBox.Show("Unable to render: \nMIDI file is empty. Please check if a MIDI file is loaded." , "No MIDI file");
                return;
            }
            options.Input = midiPath.Text;
            options.Output = outputPath.Text;
            options.PreviewMode = true;
            options.AdditionalFFMpegArgument = additionalFFArgs.Text;
            Resources["notPreviewing"] = Resources["notRenderingOrPreviewing"] = false;
            renderer = new CommonRenderer(file, options);
            _ = Task.Run(() =>
            {
                Console.WriteLine("Prepare for preview...");
                renderer.Render();
                int gen = GC.GetGeneration(renderer);
                Dispatcher.Invoke(() =>
                {
                    renderer = null;
                    Resources["notPreviewing"] = Resources["notRenderingOrPreviewing"] = true;
                });
                GC.Collect(gen);
            });
        }

        private void useDefaultColors_Click(object sender, RoutedEventArgs e)
        {
            customColors.UseDefault();
            customColors.SetGlobal();
            _ = MessageBox.Show("Color reset complete." , "Color reset complete.");
        }

        private void loadColors_Click(object sender, RoutedEventArgs e)
        {
            string filePath = colorPath.Text;
            if (!File.Exists(filePath))
            {
                _ = MessageBox.Show("Unable to load color file: file does not exist." , "Unable to load color");
                return;
            }
            int errCode = customColors.Load(filePath);
            if (errCode == 1)
            {
                _ = MessageBox.Show("An error occurred loading the color file: This file format is not compatible with supported color files." , "Unable to load color");
                return;
            }
            errCode = customColors.SetGlobal();
            if (errCode != 0)
            {
                _ = MessageBox.Show("An error occurred while setting the color: color is empty." , "Unable to set color");
                return;
            }
            _ = MessageBox.Show("Color loaded successfully. Total colors loaded: " + customColors.Colors.Length + " colors." , "Color loading complete");
        }

        private void openColorFile_Click(object sender, RoutedEventArgs e)
        {
            if (!Directory.Exists(config.CachedColorDirectory))
            {
                config.CachedColorDirectory = Directory.GetCurrentDirectory();
            }
            OpenFileDialog dialog = new()
            {
                Filter = "JSON File (*.json)|*.json",
                InitialDirectory = config.CachedColorDirectory
            };
            if ((bool)dialog.ShowDialog())
            {
                string colorDirectory = Path.GetDirectoryName(Path.GetFullPath(dialog.FileName));
                config.CachedColorDirectory = colorDirectory;
                colorPath.Text = dialog.FileName;
                config.SaveConfig();
            }
        }

        private void limitPreviewFPS_CheckToggled(object sender, RoutedPropertyChangedEventArgs<bool> e)
        {
            Global.LimitPreviewFPS = e.NewValue;
        }

        private void loadPFAColors_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                RGBAColor[] colors = PFAConfigrationLoader.LoadPFAConfigurationColors();
                customColors.Colors = colors;
                _ = customColors.SetGlobal();
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show($"Error loading PFA configuration colors: \n{ex.Message}\n Stack Trace: \n{ex.StackTrace}", "Unable to load PFA configuration");
            }
        }

        private void setbgColor_Click(object sender, RoutedEventArgs e)
        {
            string coltxt = bgColor.Text;
            if (coltxt.Length != 6)
            {
                _ = MessageBox.Show("The current color code does not conform to the specification . \n A color code should consist of 6 digits in hexadecimal representation." , "Unable to set color");
                return;
            }
            try
            {
                byte r = Convert.ToByte(coltxt[..2], 16);
                byte g = Convert.ToByte(coltxt.Substring(2, 2), 16);
                byte b = Convert.ToByte(coltxt.Substring(4, 2), 16);
                uint col = 0xff000000U | r | (uint)(g << 8) | (uint)(b << 16);
                options.BackgroundColor = col;
                previewBackgroundColor.Background = new SolidColorBrush(new Color()
                {
                    R = r,
                    G = g,
                    B = b,
                    A = 0xff
                });
            }
            catch
            {
                _ = MessageBox.Show("Error: Unable to parse color code. \nPlease check if the color code entered is correct." , "Unable to set color.");
            }
        }

        private void drawGreySquare_CheckToggled(object sender, RoutedPropertyChangedEventArgs<bool> e)
        {
            options.DrawGreySquare = e.NewValue;
        }

        private void enableNoteColorGradient_CheckToggled(object sender, RoutedPropertyChangedEventArgs<bool> e)
        {
            options.Gradient = e.NewValue;
        }

        private void shuffleColor_Click(object sender, RoutedEventArgs e)
        {
            customColors.Shuffle().SetGlobal();
        }

        private void enableSeparator_CheckToggled(object sender, RoutedPropertyChangedEventArgs<bool> e)
        {
            options.DrawSeparator = e.NewValue;
            if (e.NewValue)
            {
                if (betterBlackKeys != null && betterBlackKeys.IsChecked)
                {
                    options.BetterBlackKeys = true;
                }
            }
            else
            {
                options.BetterBlackKeys = false;
            }
        }

        private void thinnerNotes_CheckToggled(object sender, RoutedPropertyChangedEventArgs<bool> e)
        {
            options.ThinnerNotes = e.NewValue;
        }

        private void fps_ValueChanged(object sender, RoutedPropertyChangedEventArgs<decimal> e)
        {
            options.FPS = (int)e.NewValue;
        }

        private void renderWidth_ValueChanged(object sender, RoutedPropertyChangedEventArgs<decimal> e)
        {
            options.Width = (int)e.NewValue;
        }

        private void renderHeight_ValueChanged(object sender, RoutedPropertyChangedEventArgs<decimal> e)
        {
            options.Height = (int)e.NewValue;
            options.KeyHeight = options.Height * keyHeightPercentage / 100;
        }

        private void presetResolution_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch (presetResolution.SelectedIndex)
            {
                case 0:
                    renderWidth.Value = 640;
                    renderHeight.Value = 480;
                    break;
                case 1:
                    renderWidth.Value = 1280;
                    renderHeight.Value = 720;
                    break;
                case 2:
                    renderWidth.Value = 1920;
                    renderHeight.Value = 1080;
                    break;
                case 3:
                    renderWidth.Value = 2560;
                    renderHeight.Value = 1440;
                    break;
                default:
                    renderWidth.Value = 3840;
                    renderHeight.Value = 2160;
                    break;
            }
        }

        private void keyboardHeightPercentage_ValueChanged(object sender, RoutedPropertyChangedEventArgs<decimal> e)
        {
            keyHeightPercentage = (int)e.NewValue;
            options.KeyHeight = options.Height * keyHeightPercentage / 100;
        }

        private void delayStart_ValueChanged(object sender, RoutedPropertyChangedEventArgs<decimal> e)
        {
            options.DelayStartSeconds = (double)e.NewValue;
        }

        private void resetGradientScale_Click(object sender, RoutedEventArgs e)
        {
            unpressedKeyboardGradientStrength.Value = Global.DefaultUnpressedWhiteKeyGradientScale;
            pressedKeyboardGradientStrength.Value = Global.DefaultPressedWhiteKeyGradientScale;
            noteGradientStrength.Value = Global.DefaultNoteGradientScale;
            separatorGradientStrength.Value = Global.DefaultSeparatorGradientScale;

            unpressedKeyboardGradientStrength.slider.Value = Global.DefaultUnpressedWhiteKeyGradientScale;
            pressedKeyboardGradientStrength.slider.Value = Global.DefaultPressedWhiteKeyGradientScale;
            noteGradientStrength.slider.Value = Global.DefaultNoteGradientScale;
            separatorGradientStrength.slider.Value = Global.DefaultSeparatorGradientScale;

            options.KeyboardGradientDirection = VerticalGradientDirection.FromButtomToTop;
            options.SeparatorGradientDirection = VerticalGradientDirection.FromButtomToTop;
            options.NoteGradientDirection = HorizontalGradientDirection.FromLeftToRight;

            keyboardGradientDirection.SelectedIndex = 0;
            noteGradientDirection.SelectedIndex = 0;
            barGradientDirection.SelectedIndex = 0;
        }

        private void noteGradientStrength_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Global.NoteGradientScale = e.NewValue;
        }

        private void unpressedKeyboardGradientStrength_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Global.UnpressedWhiteKeyGradientScale = e.NewValue;
        }

        private void pressedKeyboardGradientStrength_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Global.PressedWhiteKeyGradientScale = e.NewValue;
        }

        private void separatorGradientStrength_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Global.SeparatorGradientScale = e.NewValue;
        }

        private void noteGradientDirection_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            options.NoteGradientDirection = (HorizontalGradientDirection)noteGradientDirection.SelectedIndex;
        }

        private void keyboardGradientDirection_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            options.KeyboardGradientDirection = (VerticalGradientDirection)keyboardGradientDirection.SelectedIndex;
        }

        private void barGradientDirection_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            options.SeparatorGradientDirection = (VerticalGradientDirection)barGradientDirection.SelectedIndex;
        }

        private void betterBlackKeys_CheckToggled(object sender, RoutedPropertyChangedEventArgs<bool> e)
        {
            options.BetterBlackKeys = e.NewValue;
        }

        private void drawKeyboard_CheckToggled(object sender, RoutedPropertyChangedEventArgs<bool> e)
        {
            if (e.NewValue)
            {
                options.KeyHeight = options.Height * keyHeightPercentage / 100;
                if (enableSeparator != null)
                {
                    options.DrawSeparator = enableSeparator.IsChecked;
                }
                if (drawGreySquare != null)
                {
                    options.DrawGreySquare = drawGreySquare.IsChecked;
                }

            }
            else
            {
                options.KeyHeight = 0;
                options.DrawGreySquare = false;
                options.DrawSeparator = false;
            }
        }

        private void maxRenderConcurrency_ValueChanged(object sender, RoutedPropertyChangedEventArgs<decimal> e)
        {
            if (useDefaultRenderConcurrency == null)
            {
                Global.MaxRenderConcurrency = -1;
                return;
            }
            Global.MaxRenderConcurrency = !useDefaultRenderConcurrency.IsChecked ? (int)e.NewValue : -1;
        }

        private void maxMidiLoaderConcurrency_ValueChanged(object sender, RoutedPropertyChangedEventArgs<decimal> e)
        {
            if (useDefaultMidiLoaderConcurrency == null)
            {
                Global.MaxMIDILoaderConcurrency = -1;
                return;
            }
            Global.MaxMIDILoaderConcurrency = !useDefaultMidiLoaderConcurrency.IsChecked ? (int)e.NewValue : -1;
        }

        private void useDefaultRenderConcurrency_CheckToggled(object sender, RoutedPropertyChangedEventArgs<bool> e)
        {
            Global.MaxRenderConcurrency = e.NewValue ? -1 : (int)maxRenderConcurrency.Value;
        }

        private void useDefaultMidiLoaderConcurrency_CheckToggled(object sender, RoutedPropertyChangedEventArgs<bool> e)
        {
            Global.MaxMIDILoaderConcurrency = e.NewValue ? -1 : (int)maxMidiLoaderConcurrency.Value;
        }

        private void videoQualityOptions_RadioChanged(object sender, RoutedEventArgs e)
        {
            if (sender == crfOptions)
            {
                options.QualityOptions = VideoQualityOptions.CRF;
                options.VideoQuality = crfSelect != null ? (int)crfSelect.Value : 17;
            }
            else
            {
                options.QualityOptions = VideoQualityOptions.Bitrate;
                options.VideoQuality = videoBitrate != null ? (int)videoBitrate.Value : 50000;
            }
        }

        private void videoBitrate_ValueChanged(object sender, RoutedPropertyChangedEventArgs<decimal> e)
        {
            options.VideoQuality = (int)e.NewValue;
        }

        private void pause_CheckToggled(object sender, RoutedPropertyChangedEventArgs<bool> e)
        {
            Global.PreviewPaused = e.NewValue;
        }

        private void denseNoteEffect_CheckToggled(object sender, RoutedPropertyChangedEventArgs<bool> e)
        {
            Global.EnableDenseNoteEffect = e.NewValue;
        }

        private void enableNoteBorder_CheckToggled(object sender, RoutedPropertyChangedEventArgs<bool> e)
        {
            Global.EnableNoteBorder = e.NewValue;
            if (!e.NewValue)
            {
                Global.EnableDenseNoteEffect = false;
                options.Gradient = false;
            }
            else
            {
                if (denseNoteEffect != null && denseNoteEffect.IsChecked)
                {
                    Global.EnableDenseNoteEffect = true;
                }
                if (enableNoteColorGradient != null && enableNoteColorGradient.IsChecked)
                {
                    options.Gradient = true;
                }
            }
        }

        private void noteBorderShade_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Global.NoteBorderShade = e.NewValue;
        }

        private void forceGC_Click(object sender, RoutedEventArgs e)
        {
            _ = Task.Run(() => GC.Collect());
        }

        private void whiteKeyShade_CheckToggled(object sender, RoutedPropertyChangedEventArgs<bool> e)
        {
            options.WhiteKeyShade = e.NewValue;
        }

        private void translucentNotes_CheckToggled(object sender, RoutedPropertyChangedEventArgs<bool> e)
        {
            Global.TranslucentNotes = e.NewValue;
            if (e.NewValue)
            {
                Global.NoteAlpha = (noteAlpha != null) ? (byte)noteAlpha.Value : (byte)144;
            }
            else
            {
                Global.NoteAlpha = 255;
            }
        }

        private void noteAlpha_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Global.NoteAlpha = (byte)e.NewValue;
        }

        private void noteBorderWidth_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Global.NoteBorderWidth = e.NewValue;
        }

        private void denseNoteShade_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Global.DenseNoteShade = e.NewValue;
        }

        private void brighterPressedNotes_CheckToggled(object sender, RoutedPropertyChangedEventArgs<bool> e)
        {
            options.BrighterNotesOnHit = e.NewValue;
        }

        private void pressedNoteShadeDecrement_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            options.PressedNotesShadeDecrement = 255 - (int)e.NewValue;
        }

        private void loadBmpColors_Click(object sender, RoutedEventArgs e)
        {
            string imagePath = colorBmpPath.Text;
            if (!File.Exists(imagePath))
            {
                _ = MessageBox.Show("An error occurred loading the color file: file does not exist", "Unable to load color");
                return;
            }
            try
            {
                using Bitmap bmp = new(imagePath);
                int bmpWidth = bmp.Width, bmpHeight = bmp.Height;
                BitmapData data = bmp.LockBits(new Rectangle(0, 0, bmpWidth, bmpHeight), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

                int numBytes = data.Stride * bmpHeight;
                RGBAColor[] pickedColors = new RGBAColor[numBytes / 4];

                unsafe
                {
                    fixed (RGBAColor* pDest = pickedColors)
                    {
                        RGBAColor* first = (RGBAColor*)data.Scan0;
                        Buffer.MemoryCopy(first, pDest, numBytes, numBytes);
                    }
                }
                
                bmp.UnlockBits(data);

                customColors.Exchange(pickedColors);
                int errCode = customColors.SetGlobal();
                if (errCode != 0)
                {
                    _ = MessageBox.Show("An error occurred while setting the color: Color is empty." , "Unable to set color");
                    return;
                }
                _ = MessageBox.Show("Color loaded successfully. Total colors loaded: " + customColors.Colors.Length + " colors." , "Color loading complete");
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show($"An error occurred loading the color file: \n{ex.Message}\n Stack Trace: \n{ex.StackTrace}", "Unable to convert color");
            }
        }

        private void openBmpColorFile_Click(object sender, RoutedEventArgs e)
        {
            if (!Directory.Exists(config.CachedColorDirectory))
            {
                config.CachedColorDirectory = Directory.GetCurrentDirectory();
            }
            OpenFileDialog dialog = new()
            {
                Filter = "Bitmap Images (*.bmp, *.jpg, *.png)|*.bmp;*.jpg;*.png",
                InitialDirectory = config.CachedColorDirectory
            };
            if ((bool)dialog.ShowDialog())
            {
                string colorDirectory = Path.GetDirectoryName(Path.GetFullPath(dialog.FileName));
                config.CachedColorDirectory = colorDirectory;
                colorBmpPath.Text = dialog.FileName;
                config.SaveConfig();
            }
        }

        private void setBarColor_Click(object sender, RoutedEventArgs e)
        {
            string coltxt = barColor.Text;
            if (coltxt.Length != 6)
            {
                _ = MessageBox.Show("The current color code does not conform to the specification . \n A color code should consist of 6 digits in hexadecimal representation." , "Unable to set color");
                return;
            }
            try
            {
                byte r = Convert.ToByte(coltxt.Substring(0, 2), 16);
                byte g = Convert.ToByte(coltxt.Substring(2, 2), 16);
                byte b = Convert.ToByte(coltxt.Substring(4, 2), 16);
                uint col = 0xFF000000U | r | (uint)(g << 8) | (uint)(b << 16);
                options.DivideBarColor = col;
                previewColor.Background = new SolidColorBrush(new Color()
                {
                    R = r,
                    G = g,
                    B = b,
                    A = 0xff
                });
            }
            catch
            {
                _ = MessageBox.Show("Error: Unable to parse color code. \nPlease check if the color code entered is correct." , "Unable to set color.");
            }
        }
    }

    internal class NotValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return !(bool)value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    internal class AndValueConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            bool b = true;
            foreach (object obj in values)
            {
                b &= (bool)obj;
            }
            return b;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
