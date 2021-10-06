﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using QQSAPI;
using SharpExtension.Collections;

namespace QQS_UI.Core
{
    /// <summary>
    /// 存放全局数据.
    /// </summary>
    public static class Global
    {
        public static readonly short[] GenKeyX = {
            0, 12, 18, 33, 36, 54, 66, 72, 85, 90, 105, 108
        };
        public static readonly short[] DrawMap = {
            0, 2, 4, 5, 7, 9, 11, 12, 14, 16, 17, 19, 21, 23, 24, 26, 28, 29,
            31, 33, 35, 36, 38, 40, 41, 43, 45, 47, 48, 50, 52, 53, 55, 57,
            59, 60, 62, 64, 65, 67, 69, 71, 72, 74, 76, 77, 79, 81, 83, 84,
            86, 88, 89, 91, 93, 95, 96, 98, 100, 101, 103, 105, 107, 108,
            110, 112, 113, 115, 117, 119, 120, 122, 124, 125, 127, 1, 3,
            6, 8, 10, 13, 15, 18, 20, 22, 25, 27, 30, 32, 34, 37, 39, 42, 44,
            46, 49, 51, 54, 56, 58, 61, 63, 66, 68, 70, 73, 75, 78, 80, 82,
            85, 87, 90, 92, 94, 97, 99, 102, 104, 106, 109, 111, 114, 116,
            118, 121, 123, 126
        };
        public static readonly RGBAColor[] DefaultColors = {
            0xFF3366FF, 0xFFFF7E33, 0xFF33FF66, 0xFFFF3381, 0xFF33E1E1, 0xFFE433E1,
            0xFF99E133, 0xFF4B33E1, 0xFFFFCC33, 0xFF33B4FF, 0xFFFF3333, 0xFF33FFB1,
            0xFFFF33CC, 0xFF4EFF33, 0xFF9933FF, 0xFFE7FF33, 0xFF3366FF, 0xFFFF7E33,
            0xFF33FF66, 0xFFFF3381, 0xFF33E1E1, 0xFFE433E1, 0xFF99E133, 0xFF4B33E1,
            0xFFFFCC33, 0xFF33B4FF, 0xFFFF3333, 0xFF33FFB1, 0xFFFF33CC, 0xFF4EFF33,
            0xFF9933FF, 0xFFE7FF33, 0xFF3366FF, 0xFFFF7E33, 0xFF33FF66, 0xFFFF3381,
            0xFF33E1E1, 0xFFE433E1, 0xFF99E133, 0xFF4B33E1, 0xFFFFCC33, 0xFF33B4FF,
            0xFFFF3333, 0xFF33FFB1, 0xFFFF33CC, 0xFF4EFF33, 0xFF9933FF, 0xFFE7FF33,
            0xFF3366FF, 0xFFFF7E33, 0xFF33FF66, 0xFFFF3381, 0xFF33E1E1, 0xFFE433E1,
            0xFF99E133, 0xFF4B33E1, 0xFFFFCC33, 0xFF33B4FF, 0xFFFF3333, 0xFF33FFB1,
            0xFFFF33CC, 0xFF4EFF33, 0xFF9933FF, 0xFFE7FF33, 0xFF3366FF, 0xFFFF7E33,
            0xFF33FF66, 0xFFFF3381, 0xFF33E1E1, 0xFFE433E1, 0xFF99E133, 0xFF4B33E1,
            0xFFFFCC33, 0xFF33B4FF, 0xFFFF3333, 0xFF33FFB1, 0xFFFF33CC, 0xFF4EFF33,
            0xFF9933FF, 0xFFE7FF33, 0xFF3366FF, 0xFFFF7E33, 0xFF33FF66, 0xFFFF3381,
            0xFF33E1E1, 0xFFE433E1, 0xFF99E133, 0xFF4B33E1, 0xFFFFCC33, 0xFF33B4FF,
            0xFFFF3333, 0xFF33FFB1, 0xFFFF33CC, 0xFF4EFF33, 0xFF9933FF, 0xFFE7FF33
        };
        /// <summary>
        /// 渲染器对象实际使用的键盘颜色.<br/>
        /// Key colors that renderer actually uses.
        /// </summary>
        public static RGBAColor[] KeyColors;
        /// <summary>
        /// 渲染器对象实际使用的音符颜色.<br/>
        /// Note colors that renderer actually uses.
        /// </summary>
        public static RGBAColor[] NoteColors;
        static Global()
        {
            KeyColors = new RGBAColor[96];
            NoteColors = new RGBAColor[96];
            Array.Copy(DefaultColors, KeyColors, 96);
            Array.Copy(DefaultColors, NoteColors, 96);
        }
        /// <summary>
        /// 将 Midi 时间转换为 <see cref="TimeSpan"/>.<br/>
        /// Converts midi time to a new <see cref="TimeSpan"/> instance.
        /// </summary>
        public static TimeSpan GetTimeOf(uint midiTime, ushort ppq, UnmanagedList<Tempo> tempos)
        {
            if (tempos == null)
            {
                return new TimeSpan((long)(5000000.0 * midiTime / ppq));
            }
            if (tempos.Count == 0)
            {
                return new TimeSpan((long)(5000000.0 * midiTime / ppq));
            }
            double ticks = 0;
            double tempo = 500000.0;
            uint lastEventTime = 0;
            IIterator<Tempo> iterator = tempos.GetIterator();
            while (iterator.MoveNext())
            {
                if (iterator.Current.Tick > midiTime)
                {
                    break;
                }
                uint dtTime = iterator.Current.Tick - lastEventTime;
                ticks += tempo * 10.0 * dtTime / ppq;
                lastEventTime = iterator.Current.Tick;
                tempo = iterator.Current.Value;
            }
            ticks += tempo * 10.0 * (midiTime - lastEventTime) / ppq;
            return new TimeSpan((long)ticks);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe uint ParseVLInt(ref byte* p)
        {
            uint result = 0;
            uint b;
            do
            {
                b = *p++;
                result = (result << 7) | (b & 0x7F);
            }
            while ((b & 0b10000000) != 0);
            return result;
        }

        /// <summary>
        /// 表示预览时渲染FPS是否被控制不高于目标FPS.<br/>
        /// Determines whether render FPS cannot be greater than target FPS.
        /// </summary>
        public static bool LimitPreviewFPS = true;
        public static bool PreviewPaused = false;

        public static double PressedWhiteKeyGradientScale = 1.0025;
        public const double DefaultPressedWhiteKeyGradientScale = 1.0025;

        public static double NoteGradientScale = 1.08;
        public const double DefaultNoteGradientScale = 1.08;

        public static double UnpressedWhiteKeyGradientScale = 1.002;
        public const double DefaultUnpressedWhiteKeyGradientScale = 1.002;

        public static double SeparatorGradientScale = 1.08;
        public const double DefaultSeparatorGradientScale = 1.08;

        public static int MaxMIDILoaderConcurrency = -1;
        public static int MaxRenderConcurrency = -1;

        public static bool EnableNoteBorder = true;
        public static double NoteBorderWidth = 1;
        public static bool EnableDenseNoteEffect = true;
        public static double NoteBorderShade = 5;
        public static double DenseNoteShade = 5;

        public static bool TranslucentNotes = false;
        public static byte NoteAlpha = 255;
    }

    public struct PreviewEvent
    {
        public uint Time;
        public uint Value;
    }

    public enum HorizontalGradientDirection
    {
        FromLeftToRight,
        FromRightToLeft
    }
    public enum VerticalGradientDirection
    {
        FromButtomToTop,
        FromTopToButtom
    }
    public enum VideoQualityOptions
    {
        CRF,
        Bitrate
    }
}
