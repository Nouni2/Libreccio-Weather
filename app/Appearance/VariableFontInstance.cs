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

using System.Runtime.InteropServices;
using System.Windows.Media;
using Point = System.Windows.Point;

namespace LibreccioWeather.Appearance;

/// <summary>
/// A variable font at chosen axis values, read through DirectWrite. WPF's typefaces have no font variation settings, so
/// text in such an instance is drawn as the geometry of its glyph outlines. Glyphs are placed by their
/// advances without shaping, so ligatures and kerning do not apply.
/// </summary>
internal sealed class VariableFontInstance
{
    // DWRITE_FACTORY_TYPE_ISOLATED: the factory and the font loader registered with it are not shared with other users
    // of DirectWrite in the process.
    private const int IsolatedFactory = 1;

    // IDWriteFactory6, the first factory that creates font resources with axis values.
    private static readonly Guid Factory6Id = new("F3744D80-21F7-42EB-B35D-995BC72FC223");

    // Vtable slots, counted from the declarations in dwrite.h and dwrite_3.h, inherited methods first.
    private const int ReleaseSlot = 2;
    private const int RegisterFontFileLoaderSlot = 13;
    private const int CreateInMemoryFontFileLoaderSlot = 44;
    private const int CreateFontResourceSlot = 49;
    private const int CreateInMemoryFontFileReferenceSlot = 4;
    private const int CreateFontFaceSlot = 13;
    private const int GetMetricsSlot = 8;
    private const int GetDesignGlyphMetricsSlot = 10;
    private const int GetGlyphIndicesSlot = 11;
    private const int GetGlyphRunOutlineSlot = 14;

    // DWRITE_FONT_METRICS holds ten 16-bit fields, and DWRITE_GLYPH_METRICS seven 32-bit ones.
    private const int FontMetricsFields = 10;
    private const int GlyphMetricsFields = 7;

    private readonly IntPtr _fontFace;
    private readonly int _designUnitsPerEm;
    private readonly GetDesignGlyphMetricsMethod _getDesignGlyphMetrics;
    private readonly GetGlyphIndicesMethod _getGlyphIndices;
    private readonly GetGlyphRunOutlineMethod _getGlyphRunOutline;

    private VariableFontInstance(IntPtr fontFace)
    {
        _fontFace = fontFace;
        _getDesignGlyphMetrics = Method<GetDesignGlyphMetricsMethod>(fontFace, GetDesignGlyphMetricsSlot);
        _getGlyphIndices = Method<GetGlyphIndicesMethod>(fontFace, GetGlyphIndicesSlot);
        _getGlyphRunOutline = Method<GetGlyphRunOutlineMethod>(fontFace, GetGlyphRunOutlineSlot);
        var metrics = new ushort[FontMetricsFields];
        Method<GetMetricsMethod>(fontFace, GetMetricsSlot)(fontFace, metrics);
        _designUnitsPerEm = metrics[0];
    }

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate uint ReleaseMethod(IntPtr self);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int RegisterFontFileLoaderMethod(IntPtr self, IntPtr fontFileLoader);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int CreateInMemoryFontFileLoaderMethod(IntPtr self, out IntPtr newLoader);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int CreateInMemoryFontFileReferenceMethod(
        IntPtr self, IntPtr factory, byte[] fontData, uint fontDataSize, IntPtr ownerObject, out IntPtr fontFile);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int CreateFontResourceMethod(IntPtr self, IntPtr fontFile, uint faceIndex, out IntPtr fontResource);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int CreateFontFaceMethod(
        IntPtr self, int fontSimulations, AxisValue[] fontAxisValues, uint fontAxisValueCount, out IntPtr fontFace);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate void GetMetricsMethod(IntPtr self, [Out] ushort[] fontFaceMetrics);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int GetDesignGlyphMetricsMethod(
        IntPtr self, ushort[] glyphIndices, uint glyphCount, [Out] int[] glyphMetrics, int isSideways);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int GetGlyphIndicesMethod(IntPtr self, uint[] codePoints, uint codePointCount, [Out] ushort[] glyphIndices);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int GetGlyphRunOutlineMethod(
        IntPtr self, float emSize, ushort[] glyphIndices, float[]? glyphAdvances, IntPtr glyphOffsets, uint glyphCount,
        int isSideways, int isRightToLeft, IntPtr geometrySink);

    /// <summary>
    /// The first face of a font file at the given axis values, such as ("wght", 700), or null when Windows cannot make
    /// it. Axes the font lacks are ignored, and values outside an axis's range are clamped to it.
    /// </summary>
    public static VariableFontInstance? Create(byte[] fontData, IReadOnlyList<(string Tag, float Value)> axes)
    {
        IntPtr factory = IntPtr.Zero;
        IntPtr loader = IntPtr.Zero;
        IntPtr fontFile = IntPtr.Zero;
        IntPtr resource = IntPtr.Zero;
        bool created = false;
        try
        {
            Guid factoryId = Factory6Id;
            if (DWriteCreateFactory(IsolatedFactory, ref factoryId, out factory) < 0 || factory == IntPtr.Zero
                || Method<CreateInMemoryFontFileLoaderMethod>(factory, CreateInMemoryFontFileLoaderSlot)(factory, out loader) < 0
                || Method<RegisterFontFileLoaderMethod>(factory, RegisterFontFileLoaderSlot)(factory, loader) < 0
                // Without an owner object, DirectWrite keeps its own copy of the font data.
                || Method<CreateInMemoryFontFileReferenceMethod>(loader, CreateInMemoryFontFileReferenceSlot)(
                    loader, factory, fontData, (uint)fontData.Length, IntPtr.Zero, out fontFile) < 0
                || Method<CreateFontResourceMethod>(factory, CreateFontResourceSlot)(factory, fontFile, 0, out resource) < 0)
            {
                return null;
            }

            AxisValue[] values = axes.Select(axis => new AxisValue { AxisTag = Tag(axis.Tag), Value = axis.Value }).ToArray();
            if (Method<CreateFontFaceMethod>(resource, CreateFontFaceSlot)(resource, 0, values, (uint)values.Length, out IntPtr face) < 0
                || face == IntPtr.Zero)
            {
                return null;
            }

            // The factory and the loader are never released: a font file made by the loader stays valid only while the
            // loader is registered with its factory, and the face lives as long as the app.
            created = true;
            return new VariableFontInstance(face);
        }
        catch (Exception exception) when (exception is DllNotFoundException or EntryPointNotFoundException
            or MarshalDirectiveException or SEHException)
        {
            return null;
        }
        finally
        {
            Release(resource);
            Release(fontFile);
            if (!created)
            {
                Release(loader);
                Release(factory);
            }
        }
    }

    /// <summary>
    /// The advance width of text at an em size, or null when the font lacks one of its characters.
    /// </summary>
    public double? Measure(string text, double emSize)
    {
        if (GlyphIndices(text) is not { } glyphs)
        {
            return null;
        }

        var metrics = new int[glyphs.Length * GlyphMetricsFields];
        if (_getDesignGlyphMetrics(_fontFace, glyphs, (uint)glyphs.Length, metrics, 0) < 0)
        {
            return null;
        }

        long advance = 0;
        for (int i = 0; i < glyphs.Length; i++)
        {
            advance += (uint)metrics[i * GlyphMetricsFields + 1];
        }

        return advance * emSize / _designUnitsPerEm;
    }

    /// <summary>
    /// The outline of text at an em size, the pen starting at the origin on the baseline and y growing downwards, or
    /// null when the font lacks one of its characters.
    /// </summary>
    public Geometry? Outline(string text, double emSize)
    {
        if (GlyphIndices(text) is not { } glyphs)
        {
            return null;
        }

        var sink = new OutlineSink();
        IntPtr sinkInterface = Marshal.GetComInterfaceForObject<OutlineSink, IGeometrySink>(sink);
        try
        {
            if (_getGlyphRunOutline(_fontFace, (float)emSize, glyphs, null, IntPtr.Zero, (uint)glyphs.Length, 0, 0, sinkInterface) < 0)
            {
                return null;
            }
        }
        finally
        {
            Marshal.Release(sinkInterface);
        }

        sink.Geometry.Freeze();
        return sink.Geometry;
    }

    private ushort[]? GlyphIndices(string text)
    {
        uint[] codePoints = text.EnumerateRunes().Select(rune => (uint)rune.Value).ToArray();
        var glyphs = new ushort[codePoints.Length];
        if (_getGlyphIndices(_fontFace, codePoints, (uint)codePoints.Length, glyphs) < 0)
        {
            return null;
        }

        // Glyph 0 is the font's missing-glyph box.
        return glyphs.Contains((ushort)0) ? null : glyphs;
    }

    // DWRITE_MAKE_FONT_AXIS_TAG: the four characters with the first in the lowest byte.
    private static uint Tag(string tag)
    {
        return tag[0] | ((uint)tag[1] << 8) | ((uint)tag[2] << 16) | ((uint)tag[3] << 24);
    }

    private static T Method<T>(IntPtr instance, int slot) where T : Delegate
    {
        IntPtr vtable = Marshal.ReadIntPtr(instance);
        return Marshal.GetDelegateForFunctionPointer<T>(Marshal.ReadIntPtr(vtable, slot * IntPtr.Size));
    }

    private static void Release(IntPtr instance)
    {
        if (instance != IntPtr.Zero)
        {
            Method<ReleaseMethod>(instance, ReleaseSlot)(instance);
        }
    }

    [DllImport("dwrite.dll", ExactSpelling = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static extern int DWriteCreateFactory(int factoryType, ref Guid iid, out IntPtr factory);

    [StructLayout(LayoutKind.Sequential)]
    private struct AxisValue
    {
        public uint AxisTag;
        public float Value;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct SinkPoint
    {
        public float X;
        public float Y;
    }

    /// <summary>
    /// ID2D1SimplifiedGeometrySink, which DirectWrite calls with the outline's figures.
    /// </summary>
    [ComImport]
    [Guid("2cd9069e-12e2-11dc-9fed-001143a055f9")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IGeometrySink
    {
        [PreserveSig]
        void SetFillMode(int fillMode);

        [PreserveSig]
        void SetSegmentFlags(int vertexFlags);

        [PreserveSig]
        void BeginFigure(SinkPoint startPoint, int figureBegin);

        [PreserveSig]
        void AddLines(IntPtr points, uint pointsCount);

        [PreserveSig]
        void AddBeziers(IntPtr beziers, uint beziersCount);

        [PreserveSig]
        void EndFigure(int figureEnd);

        [PreserveSig]
        int Close();
    }

    private sealed class OutlineSink : IGeometrySink
    {
        // D2D1_FIGURE_BEGIN_FILLED, D2D1_FIGURE_END_CLOSED and D2D1_FILL_MODE_WINDING.
        private const int FigureBeginFilled = 0;
        private const int FigureEndClosed = 1;
        private const int FillModeWinding = 1;

        private PathFigure? _figure;

        // TrueType outlines may overlap within a glyph, which a rasterizer fills by the nonzero rule, as a variable
        // font's instances often keep their contours' overlaps.
        public PathGeometry Geometry { get; } = new() { FillRule = FillRule.Nonzero };

        public void SetFillMode(int fillMode)
        {
            Geometry.FillRule = fillMode == FillModeWinding ? FillRule.Nonzero : FillRule.EvenOdd;
        }

        // The flags only change how a stroke joins segments, and the outline is only filled.
        public void SetSegmentFlags(int vertexFlags)
        {
        }

        public void BeginFigure(SinkPoint startPoint, int figureBegin)
        {
            _figure = new PathFigure { StartPoint = new Point(startPoint.X, startPoint.Y), IsFilled = figureBegin == FigureBeginFilled };
        }

        public void AddLines(IntPtr points, uint pointsCount)
        {
            var values = new float[pointsCount * 2];
            Marshal.Copy(points, values, 0, values.Length);
            for (int i = 0; i < values.Length; i += 2)
            {
                _figure?.Segments.Add(new LineSegment(new Point(values[i], values[i + 1]), true));
            }
        }

        // D2D1_BEZIER_SEGMENT: two control points and the end point.
        public void AddBeziers(IntPtr beziers, uint beziersCount)
        {
            var values = new float[beziersCount * 6];
            Marshal.Copy(beziers, values, 0, values.Length);
            for (int i = 0; i < values.Length; i += 6)
            {
                _figure?.Segments.Add(new BezierSegment(
                    new Point(values[i], values[i + 1]), new Point(values[i + 2], values[i + 3]),
                    new Point(values[i + 4], values[i + 5]), true));
            }
        }

        public void EndFigure(int figureEnd)
        {
            if (_figure is null)
            {
                return;
            }

            _figure.IsClosed = figureEnd == FigureEndClosed;
            Geometry.Figures.Add(_figure);
            _figure = null;
        }

        public int Close()
        {
            return 0;
        }
    }
}
