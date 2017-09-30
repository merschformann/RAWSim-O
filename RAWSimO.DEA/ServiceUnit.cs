using RAWSimO.Core.Statistics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.DEA
{
    /// <summary>
    /// Represents one decision making unit (DMU) and all measured values for it.
    /// </summary>
    public class ServiceUnit
    {
        /// <summary>
        /// The delimiter to use to separate idents.
        /// </summary>
        public const string DELIMITER = "/";

        public ServiceUnit(IEnumerable<Tuple<FootprintDatapoint.FootPrintEntry, string>> idents, IEnumerable<FootprintDatapoint> footprints)
        {
            _idents = idents.ToList();
            _datapoints = footprints.ToList();
        }

        /// <summary>
        /// The complete datapoints of this service unit.
        /// </summary>
        private List<FootprintDatapoint> _datapoints = new List<FootprintDatapoint>();
        /// <summary>
        /// The idents representing the service unit.
        /// </summary>
        private List<Tuple<FootprintDatapoint.FootPrintEntry, string>> _idents;
        /// <summary>
        /// The idents represented by a string.
        /// </summary>
        private string _stringIdents = null;
        /// <summary>
        /// The name of this service unit.
        /// </summary>
        public string Ident
        {
            get
            {
                if (_stringIdents == null)
                    _stringIdents = string.Join(DELIMITER, _idents.Select(i => i.Item2));
                return _stringIdents;
            }
        }
        /// <summary>
        /// The idents representing the service unit.
        /// </summary>
        public IEnumerable<Tuple<FootprintDatapoint.FootPrintEntry, string>> Idents { get { return _idents; } }
        /// <summary>
        /// Returns the measured value regarding the given characteristic averaged across all datapoints corresponding to this service unit.
        /// </summary>
        /// <param name="entry">The characteristic to look up.</param>
        /// <returns>The value measured for the characteristic (average across corresponding datapoints).</returns>
        public double this[FootprintDatapoint.FootPrintEntry entry] { get { return _datapoints.Average(d => (double)d[entry]); } }
        /// <summary>
        /// Returns the measured value regarding the given characteristic averaged across all datapoints corresponding to this service unit and the current group indicated by the supplied filter function.
        /// </summary>
        /// <param name="entry">The characteristic to look up.</param>
        /// <param name="datapointFilter">A function filtering for the datapoints of the current group.</param>
        /// <returns>The value measured for the characteristic (average across corresponding datapoints).</returns>
        public double GetValue(FootprintDatapoint.FootPrintEntry entry, Func<FootprintDatapoint, bool> datapointFilter)
        {
            return _datapoints.Where(d => datapointFilter(d)).Average(d => Convert.ToDouble(d[entry]));
        }
    }
}
