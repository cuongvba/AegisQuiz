using System;
using System.IO;
using AegisQuiz.Infrastructure.Services.Docx;
using Xunit;

namespace AegisQuiz.UnitTests
{
    public class MtefConversionTests
    {
        [Fact]
        public void Convert_Image122_ShouldExtractFractionWithRadical()
        {
            // image122.wmf: (a\sqrt{b})/c dm^3
            string path = Path.Combine(AppContext.BaseDirectory, "image122.wmf");
            if (!File.Exists(path))
            {
                // Fallback to project dir
                path = Path.GetFullPath("../../../image122.wmf");
            }

            Assert.True(File.Exists(path), $"File image122.wmf not found at {path}");
            byte[] bytes = File.ReadAllBytes(path);

            bool success = MtefToLatexConverter.TryConvert(bytes, out string katex);

            Assert.True(success, "MtefToLatexConverter failed to convert image122.wmf");
            Assert.StartsWith("$", katex);
            Assert.EndsWith("$", katex);
            Assert.Contains(@"\frac", katex);
            Assert.Contains(@"\sqrt", katex);
        }

        [Fact]
        public void Convert_Image12_ShouldExtractTrigFunction()
        {
            // image12.wmf: sin x = ...
            string path = Path.Combine(AppContext.BaseDirectory, "image12.wmf");
            if (!File.Exists(path))
            {
                path = Path.GetFullPath("../../../image12.wmf");
            }

            Assert.True(File.Exists(path), $"File image12.wmf not found at {path}");
            byte[] bytes = File.ReadAllBytes(path);

            bool success = MtefToLatexConverter.TryConvert(bytes, out string katex);

            Assert.True(success, "MtefToLatexConverter failed to convert image12.wmf");
            Assert.StartsWith("$", katex);
            Assert.EndsWith("$", katex);
            Assert.Contains(@"\sin", katex);
        }

        [Fact]
        public void Convert_Image34_ShouldExtractCoordinates()
        {
            string path = Path.Combine(AppContext.BaseDirectory, "image34.wmf");
            if (!File.Exists(path)) path = Path.GetFullPath("../../../image34.wmf");

            if (File.Exists(path))
            {
                byte[] bytes = File.ReadAllBytes(path);
                bool success = MtefToLatexConverter.TryConvert(bytes, out string katex);
                Assert.True(success);
                Assert.StartsWith("$", katex);
                Assert.EndsWith("$", katex);
            }
        }

        [Fact]
        public void Convert_Image121_ShouldConvertProperly()
        {
            string path = Path.Combine(AppContext.BaseDirectory, "image121.wmf");
            if (!File.Exists(path)) path = Path.GetFullPath("../../../image121.wmf");

            if (File.Exists(path))
            {
                byte[] bytes = File.ReadAllBytes(path);
                bool success = MtefToLatexConverter.TryConvert(bytes, out string katex);
                Assert.True(success);
                Assert.StartsWith("$", katex);
                Assert.EndsWith("$", katex);
            }
        }

        [Fact]
        public void Convert_CubicPolynomialOptions_ShouldNotSwallowSucceedingTerms()
        {
            var deToanPath = @"D:\Cuong\DuAn\mybank\2026-TNPT\DeToan.docx";
            if (!File.Exists(deToanPath)) return;

            using var archive = System.IO.Compression.ZipFile.OpenRead(deToanPath);
            var entry2 = archive.GetEntry("word/media/image2.wmf");
            Assert.NotNull(entry2);
            using var ms = new MemoryStream();
            using var s = entry2.Open();
            s.CopyTo(ms);
            byte[] bytes = ms.ToArray();

            bool success = MtefToLatexConverter.TryConvert(bytes, out string katex);
            Assert.True(success);
            Assert.Equal("$y=x^{3}-6x+2$", katex);
            Assert.DoesNotContain("3-6x", katex);
        }

        [Fact]
        public void Convert_NullOrInvalidBytes_ShouldReturnFalseSafely()
        {
            Assert.False(MtefToLatexConverter.TryConvert(null, out _));
            Assert.False(MtefToLatexConverter.TryConvert(new byte[10], out _));
            Assert.False(MtefToLatexConverter.TryConvert(new byte[100], out _));
        }
    }
}
