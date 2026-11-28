using System;
using System.IO;
using System.Linq;
using RAWSimO.Core;
using RAWSimO.Core.Control;
using RAWSimO.Core.IO;
using RAWSimO.Core.Statistics;
using RAWSimO.Visualization.Avalonia.Generation;
using RAWSimO.Visualization.Avalonia.Rendering;
using SkiaSharp;
using Xunit;

namespace RAWSimO.Visualization.Avalonia.Tests
{
    public class HeatmapTests
    {
        private static string OutputDir =>
            Environment.GetEnvironmentVariable("RAWSIMO_SNAPSHOT_DIR") ?? Path.Combine(AppContext.BaseDirectory, "snapshots");

        /// <summary>
        /// Writes a location heat file with many points around (x, y) and a few scattered ones (all at time 10).
        /// </summary>
        private static string WriteLocationData(double x, double y, double length, double width)
        {
            string file = Path.Combine(Path.GetTempPath(), "rawsimo-heat-" + Guid.NewGuid().ToString("N") + ".heat");
            using var writer = new StreamWriter(file);
            writer.WriteLine(IOConstants.COMMENT_LINE + IOConstants.STAT_HEAT_TAG_START + HeatDataType.PolledLocation + IOConstants.STAT_HEAT_TAG_END + LocationDatapoint.GetCSVHeader());
            var random = new Random(42);
            for (int i = 0; i < 500; i++)
                writer.WriteLine(new LocationDatapoint { TimeStamp = 10, Tier = 0, X = x + random.NextDouble() - 0.5, Y = y + random.NextDouble() - 0.5, BotTask = BotTaskType.None }.ToCSV());
            for (int i = 0; i < 20; i++)
                writer.WriteLine(new LocationDatapoint { TimeStamp = 10, Tier = 0, X = random.NextDouble() * length, Y = random.NextDouble() * width, BotTask = BotTaskType.None }.ToCSV());
            return file;
        }

        private static string FindRepoFile(string relativePath)
        {
            for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir != null; dir = dir.Parent)
            {
                string candidate = Path.Combine(dir.FullName, relativePath);
                if (File.Exists(candidate))
                    return candidate;
            }
            throw new FileNotFoundException(relativePath);
        }

        [Fact]
        public void HottestTileIsWhereTheDataIs()
        {
            const double length = 50, width = 30;
            string file = WriteLocationData(10, 20, length, width);
            try
            {
                Assert.Equal(HeatDataType.PolledLocation, Heatmap.ParseHeatDataType(file));
                var parameters = new HeatmapParameters { TilesX = 50, Logarithmic = false };
                Heatmap heatmap = Heatmap.Calculate(length, width, 0, new HeatmapDataSelection { DataFile = file }, parameters);

                Assert.Equal(50, heatmap.Values.GetLength(0));
                Assert.Equal(30, heatmap.Values.GetLength(1)); // 1x1 tiles
                // Locate the maximum
                int maxX = 0, maxY = 0;
                for (int x = 0; x < heatmap.Values.GetLength(0); x++)
                    for (int y = 0; y < heatmap.Values.GetLength(1); y++)
                        if (heatmap.Values[x, y] > heatmap.Values[maxX, maxY]) { maxX = x; maxY = y; }
                Assert.Equal(1.0, heatmap.Values[maxX, maxY], 6);
                Assert.InRange(maxX, 9, 10);
                Assert.InRange(maxY, 19, 20);
                Assert.True(heatmap.Values[45, 5] < 0.1, "Far away tiles must be (nearly) cold");

                // A time window without data yields no heatmap
                Assert.Null(Heatmap.Calculate(length, width, 0, new HeatmapDataSelection { DataFile = file, TimeFilterLow = 100, TimeFilterHigh = 200 }, parameters));
            }
            finally { File.Delete(file); }
        }

        [Fact]
        public void ExportsInstanceWithHeatmap()
        {
            Instance instance = InstanceIO.ReadInstance(FindRepoFile(Path.Combine("Material", "Instances", "CoreBenchmark", "jenkinsinstance1.xinst")), null, null, true, true);
            instance.VisClearBots();
            instance.VisClearPods();
            var tier = instance.GetInfoTiers().First();
            double length = tier.GetInfoLength(), width = tier.GetInfoWidth();
            string file = WriteLocationData(length * 0.25, width * 0.5, length, width);
            try
            {
                var parameters = new HeatmapParameters();
                Heatmap heatmap = Heatmap.Calculate(length, width, tier.GetInfoID(), new HeatmapDataSelection { DataFile = file }, parameters);
                var options = new SceneOptions { Heatmap = heatmap.CreateOverlay(parameters, 1f) };
                Directory.CreateDirectory(OutputDir);
                string png = Path.Combine(OutputDir, "heatmap.png");
                var renderer = new SceneRenderer(instance, tier, DetailLevel.Debug);
                SceneExporter.Export(png, renderer, new SceneSnapshotBuilder(instance, () => 0).Build(), options, maxPixelSize: 1000);

                // The hot spot is red-ish, the opposite side blue-ish (rainbow coloring)
                using var bitmap = SKBitmap.Decode(png);
                var m = renderer.GetWorldToCanvas((1000 - 20) / (float)Math.Max(length, width), 10, 10);
                SKPoint hot = m.MapPoint((float)(length * 0.25), (float)(width * 0.5));
                SKPoint cold = m.MapPoint((float)(length * 0.9), (float)(width * 0.1));
                SKColor hotColor = bitmap.GetPixel((int)hot.X, (int)hot.Y), coldColor = bitmap.GetPixel((int)cold.X, (int)cold.Y);
                Assert.True(hotColor.Red > 200 && hotColor.Blue < 100, $"Hot spot is not red: {hotColor}");
                Assert.True(coldColor.Blue > 150 && coldColor.Red < 150, $"Cold area is not blue: {coldColor}");
            }
            finally { File.Delete(file); }
        }
    }
}
