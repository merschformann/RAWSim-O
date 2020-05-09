using RAWSimO.Core.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.Core.Helper
{
    /// <summary>
    /// Exposes functionality to use GNUPlot.
    /// </summary>
    public class GnuPlotter
    {
        /// <summary>
        /// Plots points on a two dimensional plane.
        /// </summary>
        /// <param name="filename">The base filename to use.</param>
        /// <param name="points">The points that shall be plotted.</param>
        /// <param name="title">The title of the plot.</param>
        public static void Plot2DPoints(string filename, IEnumerable<Tuple<string, IEnumerable<Tuple<double, double>>>> points, string title)
        {
            // Write data file
            int datasetCounter = 0;
            foreach (var pointset in points)
            {
                using (StreamWriter sw = new StreamWriter(filename + (datasetCounter++) + ".dat"))
                {
                    sw.WriteLine(IOConstants.GNU_PLOT_COMMENT_LINE + " x y");
                    foreach (var point in pointset.Item2)
                        sw.WriteLine(point.Item1.ToString(IOConstants.FORMATTER) + IOConstants.GNU_PLOT_VALUE_SPLIT + point.Item2.ToString(IOConstants.FORMATTER));
                }
            }
            // Write plot script
            using (StreamWriter sw = new StreamWriter(filename + ".gp"))
            {
                sw.WriteLine("reset");
                sw.WriteLine("# Output definition");
                sw.WriteLine("set terminal pdfcairo enhanced size 7, 7 font \"Consolas, 12\"");
                sw.WriteLine("# Parameters");
                sw.WriteLine("set key left top Left");
                sw.WriteLine("set xlabel \"x\"");
                sw.WriteLine("set ylabel \"y\"");
                sw.WriteLine("set grid");
                sw.WriteLine("set style fill solid 0.25");
                sw.WriteLine("# Line-Styles");
                sw.WriteLine("set style line 1 linetype 1 linecolor rgb \"#474749\" linewidth 1 pt 1");
                sw.WriteLine("set style line 2 linetype 1 linecolor rgb \"#7090c8\" linewidth 1 pt 2");
                sw.WriteLine("set style line 3 linetype 1 linecolor rgb \"#42b449\" linewidth 1 pt 3");
                sw.WriteLine("set style line 4 linetype 1 linecolor rgb \"#f7cb38\" linewidth 1 pt 4");
                sw.WriteLine("set style line 5 linetype 1 linecolor rgb \"#db4a37\" linewidth 1 pt 5");
                sw.WriteLine("set title \"" + title + "\"");
                sw.WriteLine("set output \"" + filename + ".pdf\"");
                sw.WriteLine("plot \\");
                datasetCounter = 0;
                foreach (var pointset in points)
                {
                    string dataFilename = filename + (datasetCounter++) + ".dat";
                    if (datasetCounter < points.Count() - 1)
                        sw.WriteLine("\"" + dataFilename + "\" u 1:2 w points linestyle " + ((datasetCounter % 5) + 1) + " t \"" + pointset.Item1 + "\", \\");
                    else
                        sw.WriteLine("\"" + dataFilename + "\" u 1:2 w points linestyle " + ((datasetCounter % 5) + 1) + " t \"" + pointset.Item1 + "\"");
                }
                sw.WriteLine("reset");
                sw.WriteLine("exit");
            }
            using (StreamWriter sw = new StreamWriter(filename + ".cmd"))
            {
                sw.WriteLine("gnuplot " + filename + ".gp");
            }
        }
    }
}
