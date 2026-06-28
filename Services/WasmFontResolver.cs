using System;
using System.IO;
using PdfSharpCore.Fonts;

namespace IntakeApp.Services
{
    public class WasmFontResolver : IFontResolver
    {
        public static byte[]? FontBytes { get; set; }

        public string DefaultFontName => "Roboto";

        public FontResolverInfo ResolveTypeface(string familyName, bool isBold, bool isItalic)
        {
            // For simplicity, map all typeface resolutions to our single loaded font
            return new FontResolverInfo("Roboto");
        }

        public byte[] GetFont(string faceName)
        {
            if (FontBytes == null)
            {
                throw new InvalidOperationException("FontBytes has not been loaded and initialized in WasmFontResolver.");
            }
            return FontBytes;
        }
    }
}
