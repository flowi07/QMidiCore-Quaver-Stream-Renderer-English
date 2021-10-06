﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace QQS_UI.Core
{
    /// <summary>
    /// 表示渲染的设置. 请使用 <see cref="CreateRenderOptions"/> 来创建一个 <see cref="RenderOptions"/> 结构.
    /// </summary>
    public struct RenderOptions
    {
        /// <summary>
        /// 此成员没有被使用过.<br/>
        /// This member is wasted.
        /// </summary>
        public bool TickBased;
        public bool TransparentBackground;
        public bool ThinnerNotes;
        public bool DrawSeparator;
        public bool PreviewMode;
        public bool DrawGreySquare;
        public bool Gradient;
        public bool BetterBlackKeys;
        public bool WhiteKeyShade;
        public bool BrighterNotesOnHit;
        public int Width, Height, FPS, VideoQuality, KeyHeight;
        public VideoQualityOptions QualityOptions;
        public VerticalGradientDirection KeyboardGradientDirection;
        public VerticalGradientDirection SeparatorGradientDirection;
        public HorizontalGradientDirection NoteGradientDirection;
        public RGBAColor DivideBarColor;
        public RGBAColor BackgroundColor;
        public double NoteSpeed;
        public double DelayStartSeconds;
        public string Input;
        public string Output;
        public string AdditionalFFMpegArgument;
        public static RenderOptions CreateRenderOptions()
        {
            return new RenderOptions
            {
                Width = 1920,
                Height = 1080,
                FPS = 60,
                VideoQuality = 17,
                KeyHeight = 162,
                NoteSpeed = 1,
                DivideBarColor = 0xFF0000A0,
                TickBased = true,
                TransparentBackground = false,
                WhiteKeyShade = true,
                DrawSeparator = true,
                PreviewMode = false,
                AdditionalFFMpegArgument = string.Empty,
                DrawGreySquare = false,
                Gradient = true,
                ThinnerNotes = true,
                DelayStartSeconds = 0,
                BrighterNotesOnHit = true,
                BetterBlackKeys = true,
                KeyboardGradientDirection = VerticalGradientDirection.FromButtomToTop,
                NoteGradientDirection = HorizontalGradientDirection.FromLeftToRight,
                SeparatorGradientDirection = VerticalGradientDirection.FromButtomToTop,
                QualityOptions = VideoQualityOptions.CRF,
                BackgroundColor = new RGBAColor
                {
                    A = 0xFF,
                    G = 0,
                    R = 0,
                    B = 0
                }
            };
        }
    }
}
