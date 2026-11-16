/*
 * This file is part of Libreccio Weather.
 *
 * Libreccio Weather is free software: you can redistribute it and/or modify it
 * under the terms of the GNU General Public License as published by the Free
 * Software Foundation, version 3 of the License.
 *
 * Libreccio Weather is distributed in the hope that it will be useful, but
 * WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY
 * or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for
 * more details.
 *
 * You should have received a copy of the GNU General Public License along
 * with Libreccio Weather. If not, see <https://www.gnu.org/licenses/>.
 */

/*
 * Copyright 2022 Google LLC
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

namespace LibreccioWeather.Core.MaterialColors;

/// <summary>
/// A color role of a scheme, such as primary: the palette and tone it starts from, and the background and contrast
/// it must keep, from which its color is worked out for each scheme.
/// </summary>
public sealed class DynamicColor
{
    // Keyed by the scheme object, as the scheme defines no equality of its own.
    private readonly Dictionary<DynamicScheme, Hct> _hctCache = [];

    internal DynamicColor(
        string name,
        Func<DynamicScheme, TonalPalette> palette,
        Func<DynamicScheme, double> tone,
        bool isBackground,
        Func<DynamicScheme, double>? chromaMultiplier,
        Func<DynamicScheme, DynamicColor?>? background,
        Func<DynamicScheme, DynamicColor?>? secondBackground,
        Func<DynamicScheme, ContrastCurve?>? contrastCurve,
        Func<DynamicScheme, ToneDeltaPair?>? toneDeltaPair,
        Func<DynamicScheme, double?>? opacity)
    {
        Name = name;
        Palette = palette;
        Tone = tone;
        IsBackground = isBackground;
        ChromaMultiplier = chromaMultiplier;
        Background = background;
        SecondBackground = secondBackground;
        ContrastCurve = contrastCurve;
        ToneDeltaPair = toneDeltaPair;
        Opacity = opacity;
    }

    public string Name { get; }

    internal Func<DynamicScheme, TonalPalette> Palette { get; }

    internal Func<DynamicScheme, double> Tone { get; }

    /// <summary>
    /// Whether text or icons are drawn on the color, which keeps its tone out of the awkward tones 50 to 59.
    /// </summary>
    internal bool IsBackground { get; }

    internal Func<DynamicScheme, double>? ChromaMultiplier { get; }

    internal Func<DynamicScheme, DynamicColor?>? Background { get; }

    internal Func<DynamicScheme, DynamicColor?>? SecondBackground { get; }

    internal Func<DynamicScheme, ContrastCurve?>? ContrastCurve { get; }

    internal Func<DynamicScheme, ToneDeltaPair?>? ToneDeltaPair { get; }

    internal Func<DynamicScheme, double?>? Opacity { get; }

    /// <summary>
    /// The color's ARGB value in a scheme, with the role's opacity when it has one.
    /// </summary>
    public int GetArgb(DynamicScheme scheme)
    {
        int argb = GetHct(scheme).ToInt();
        if (Opacity is null || Opacity(scheme) is not { } percentage)
        {
            return argb;
        }

        int alpha = ColorMath.ClampInt(0, 255, (int)ColorMath.JavaRound(percentage * 255));
        return (argb & 0x00ffffff) | (alpha << 24);
    }

    public Hct GetHct(DynamicScheme scheme)
    {
        if (_hctCache.TryGetValue(scheme, out Hct? cachedAnswer))
        {
            return cachedAnswer;
        }

        Hct answer = ColorSpec2021.GetHct(scheme, this);
        if (_hctCache.Count > 4)
        {
            _hctCache.Clear();
        }

        _hctCache[scheme] = answer;
        return answer;
    }

    public double GetTone(DynamicScheme scheme)
    {
        return ColorSpec2021.GetTone(scheme, this);
    }

    /// <summary>
    /// The tone of a text or icon that reaches the ratio on a background tone, the lighter one when the background
    /// suits a light foreground.
    /// </summary>
    internal static double ForegroundTone(double bgTone, double ratio)
    {
        double lighterTone = Contrast.LighterUnsafe(bgTone, ratio);
        double darkerTone = Contrast.DarkerUnsafe(bgTone, ratio);
        double lighterRatio = Contrast.RatioOfTones(lighterTone, bgTone);
        double darkerRatio = Contrast.RatioOfTones(darkerTone, bgTone);
        bool preferLighter = TonePrefersLightForeground(bgTone);

        if (preferLighter)
        {
            // With a high ratio requested, neither tone may reach it; the lighter one is kept when both miss it by
            // about as much, which stops a light-mode on-primary-container from turning black between high and
            // maximum contrast.
            bool negligibleDifference = Math.Abs(lighterRatio - darkerRatio) < 0.1 && lighterRatio < ratio && darkerRatio < ratio;
            if (lighterRatio >= ratio || lighterRatio >= darkerRatio || negligibleDifference)
            {
                return lighterTone;
            }
            else
            {
                return darkerTone;
            }
        }
        else
        {
            return darkerRatio >= ratio || darkerRatio >= lighterRatio ? darkerTone : lighterTone;
        }
    }

    /// <summary>
    /// The tone moved to 49 when it suits a light foreground without allowing one, so that light text works on it.
    /// </summary>
    internal static double EnableLightForeground(double tone)
    {
        if (TonePrefersLightForeground(tone) && !ToneAllowsLightForeground(tone))
        {
            return 49.0;
        }

        return tone;
    }

    /// <summary>
    /// Whether text on the tone looks better light. Tones 50 to 59 suit light text, but only up to 49 does light
    /// text reach the needed contrast.
    /// </summary>
    internal static bool TonePrefersLightForeground(double tone)
    {
        return ColorMath.JavaRound(tone) < 60;
    }

    internal static bool ToneAllowsLightForeground(double tone)
    {
        return ColorMath.JavaRound(tone) <= 49;
    }

    internal static Func<DynamicScheme, double> GetInitialToneFromBackground(Func<DynamicScheme, DynamicColor?>? background)
    {
        if (background is null)
        {
            return _ => 50.0;
        }

        return s => background(s) is { } color ? color.GetTone(s) : 50.0;
    }

    internal sealed class Builder
    {
        private string _name = "";
        private Func<DynamicScheme, TonalPalette>? _palette;
        private Func<DynamicScheme, double>? _tone;
        private bool _isBackground;
        private Func<DynamicScheme, double>? _chromaMultiplier;
        private Func<DynamicScheme, DynamicColor?>? _background;
        private Func<DynamicScheme, DynamicColor?>? _secondBackground;
        private Func<DynamicScheme, ContrastCurve?>? _contrastCurve;
        private Func<DynamicScheme, ToneDeltaPair?>? _toneDeltaPair;
        private Func<DynamicScheme, double?>? _opacity;

        public Builder SetName(string name)
        {
            _name = name;
            return this;
        }

        public Builder SetPalette(Func<DynamicScheme, TonalPalette> palette)
        {
            _palette = palette;
            return this;
        }

        public Builder SetTone(Func<DynamicScheme, double> tone)
        {
            _tone = tone;
            return this;
        }

        public Builder SetIsBackground(bool isBackground)
        {
            _isBackground = isBackground;
            return this;
        }

        public Builder SetChromaMultiplier(Func<DynamicScheme, double> chromaMultiplier)
        {
            _chromaMultiplier = chromaMultiplier;
            return this;
        }

        public Builder SetBackground(Func<DynamicScheme, DynamicColor?> background)
        {
            _background = background;
            return this;
        }

        public Builder SetSecondBackground(Func<DynamicScheme, DynamicColor?> secondBackground)
        {
            _secondBackground = secondBackground;
            return this;
        }

        public Builder SetContrastCurve(Func<DynamicScheme, ContrastCurve?> contrastCurve)
        {
            _contrastCurve = contrastCurve;
            return this;
        }

        public Builder SetToneDeltaPair(Func<DynamicScheme, ToneDeltaPair?> toneDeltaPair)
        {
            _toneDeltaPair = toneDeltaPair;
            return this;
        }

        public Builder SetOpacity(Func<DynamicScheme, double?> opacity)
        {
            _opacity = opacity;
            return this;
        }

        public DynamicColor Build()
        {
            if (_background is null && _secondBackground is not null)
            {
                throw new InvalidOperationException("Color " + _name + " has secondBackground defined, but background is not defined.");
            }

            if (_background is null && _contrastCurve is not null)
            {
                throw new InvalidOperationException("Color " + _name + " has contrastCurve defined, but background is not defined.");
            }

            if (_background is not null && _contrastCurve is null)
            {
                throw new InvalidOperationException("Color " + _name + " has background defined, but contrastCurve is not defined.");
            }

            return new DynamicColor(
                _name,
                _palette ?? throw new InvalidOperationException("Color " + _name + " has no palette."),
                _tone ?? GetInitialToneFromBackground(_background),
                _isBackground,
                _chromaMultiplier,
                _background,
                _secondBackground,
                _contrastCurve,
                _toneDeltaPair,
                _opacity);
        }
    }
}
