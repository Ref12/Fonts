﻿// Copyright (c) Six Labors and contributors.
// Licensed under the Apache License, Version 2.0.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Numerics;
using SixLabors.Primitives;

namespace SixLabors.Fonts
{
    /// <summary>
    /// Encapulated logic for laying out and measuring text.
    /// </summary>
    public static class TextMeasurer
    {
        private static readonly GlyphMetric[] EmptyGlyphMetricArray = new GlyphMetric[0];

        /// <summary>
        /// Measures the text.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="options">The style.</param>
        /// <returns>The size of the text if it was to be rendered.</returns>
        public static SizeF Measure(string text, RendererOptions options)
            => TextMeasurerInt.Default.Measure(text, options);

        /// <summary>
        /// Measures the text.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="options">The style.</param>
        /// <returns>The size of the text if it was to be rendered.</returns>
        public static RectangleF MeasureBounds(string text, RendererOptions options)
            => TextMeasurerInt.Default.MeasureBounds(text, options);

        /// <summary>
        /// Measures the character bounds of the text. For each control character the list contains a <c>null</c> element.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="options">The style.</param>
        /// <param name="characterBounds">The list of character bounds of the text if it was to be rendered.</param>
        /// <returns>Whether any of the characters had non-empty bounds.</returns>
        public static bool TryMeasureCharacterBounds(string text, RendererOptions options, out IReadOnlyList<GlyphMetric> characterBounds)
            => TextMeasurerInt.Default.TryMeasureCharacterBounds(text, options, out characterBounds);

        internal static SizeF GetSize(ImmutableArray<GlyphLayout> glyphLayouts, Vector2 dpi)
        {
            if (glyphLayouts.IsEmpty)
            {
                return Size.Empty;
            }

            float left = glyphLayouts.Min(x => x.Location.X);
            float right = glyphLayouts.Max(x => x.Location.X + x.Width);

            // location is bottom left of the line
            float top = glyphLayouts.Min(x => x.Location.Y - x.LineHeight);
            float bottom = glyphLayouts.Max(x => x.Location.Y - x.LineHeight + x.Height);

            Vector2 topLeft = new Vector2(left, top) * dpi;
            Vector2 bottomRight = new Vector2(right, bottom) * dpi;

            var size = bottomRight - topLeft;
            return new RectangleF(topLeft.X, topLeft.Y, size.X, size.Y).Size;
        }

        internal static RectangleF GetBounds(ImmutableArray<GlyphLayout> glyphLayouts, Vector2 dpi)
        {
            if (glyphLayouts.IsEmpty)
            {
                return RectangleF.Empty;
            }

            bool hasSize = false;

            float left = int.MaxValue;
            float top = int.MaxValue;
            float bottom = int.MinValue;
            float right = int.MinValue;

            for (var i = 0; i < glyphLayouts.Length; i++)
            {
                var c = glyphLayouts[i];
                if (!c.IsControlCharacter)
                {
                    hasSize = true;
                    var box = c.BoundingBox(dpi);
                    if (left > box.Left)
                    {
                        left = box.Left;
                    }
                    if (top > box.Top)
                    {
                        top = box.Top;
                    }
                    if (bottom < box.Bottom)
                    {
                        bottom = box.Bottom;
                    }
                    if (right < box.Right)
                    {
                        right = box.Right;
                    }
                }
            }

            if (!hasSize)
            {
                return RectangleF.Empty;
            }

            var width = right - left;
            var height = bottom - top;

            return new RectangleF(left, top, width, height);
        }

        internal static bool TryGetCharacterBounds(ImmutableArray<GlyphLayout> glyphLayouts, Vector2 dpi, out IReadOnlyList<GlyphMetric> characterBounds)
        {
            bool hasSize = false;
            if (glyphLayouts.IsEmpty)
            {
                characterBounds = EmptyGlyphMetricArray;
                return hasSize;
            }

            List<GlyphMetric> characterBoundsList = new List<GlyphMetric>();

            for (var i = 0; i < glyphLayouts.Length; i++) {
                var c = glyphLayouts[i];
                if (c.IsControlCharacter)
                {
                    characterBoundsList.Add(new GlyphMetric(c.Character, c.BoundingBox(dpi), true));
                }
                else
                {
                    hasSize = true;
                    characterBoundsList.Add(new GlyphMetric(c.Character, c.BoundingBox(dpi), false));
                }
            }

            characterBounds = characterBoundsList;
            return hasSize;
        }

        internal class TextMeasurerInt
        {
            internal static TextMeasurerInt Default { get; set; } = new TextMeasurerInt();

            private TextLayout layoutEngine;

            internal TextMeasurerInt(TextLayout layoutEngine)
            {
                this.layoutEngine = layoutEngine;
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="TextMeasurerInt"/> class.
            /// </summary>
            internal TextMeasurerInt()
            : this(TextLayout.Default)
            {
            }

            /// <summary>
            /// Measures the text.
            /// </summary>
            /// <param name="text">The text.</param>
            /// <param name="options">The style.</param>
            /// <returns>The size of the text if it was to be rendered.</returns>
            internal RectangleF MeasureBounds(string text, RendererOptions options)
            {
                ImmutableArray<GlyphLayout> glyphsToRender = this.layoutEngine.GenerateLayout(text, options);

                return GetBounds(glyphsToRender, new Vector2(options.DpiX, options.DpiY));
            }

            /// <summary>
            /// Measures the text.
            /// </summary>
            /// <param name="text">The text.</param>
            /// <param name="options">The style.</param>
            /// <param name="characterBounds">The character bounds list.</param>
            /// <returns>The size of the text if it was to be rendered.</returns>
            internal bool TryMeasureCharacterBounds(string text, RendererOptions options, out IReadOnlyList<GlyphMetric> characterBounds)
            {
                ImmutableArray<GlyphLayout> glyphsToRender = this.layoutEngine.GenerateLayout(text, options);

                return TryGetCharacterBounds(glyphsToRender, new Vector2(options.DpiX, options.DpiY), out characterBounds);
            }

            /// <summary>
            /// Measures the text.
            /// </summary>
            /// <param name="text">The text.</param>
            /// <param name="options">The style.</param>
            /// <returns>The size of the text if it was to be rendered.</returns>
            internal SizeF Measure(string text, RendererOptions options)
            {
                ImmutableArray<GlyphLayout> glyphsToRender = this.layoutEngine.GenerateLayout(text, options);

                return GetSize(glyphsToRender, new Vector2(options.DpiX, options.DpiY));
            }
        }
    }
}