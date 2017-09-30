using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.MDPSolve
{
    /// <summary>
    /// Some constant definitions used by this project.
    /// </summary>
    public class Constants
    {
        /// <summary>
        /// The identifier of the coefficient matrix block.
        /// </summary>
        public const string IDENT_COEFF_MATRIX = "A";
        /// <summary>
        /// The identifier of the right hand side vector block.
        /// </summary>
        public const string IDENT_RHS_VECTOR = "b";
        /// <summary>
        /// The identifier of the objective coefficients vector block.
        /// </summary>
        public const string IDENT_OBJ_COEFF_VECTOR = "c";
        /// <summary>
        /// The identifier of the solution vector block.
        /// </summary>
        public const string IDENT_SOLUTION_VECTOR = "x";
        /// <summary>
        /// The identifier of the solution value block.
        /// </summary>
        public const string IDENT_SOLUTION_VALUE = "z";
        /// <summary>
        /// Identifies a comment line.
        /// </summary>
        public const string COMMENT = "#";
        /// <summary>
        /// The main delimiter between values.
        /// </summary>
        public const char DELIMITER_MAIN = ';';
        /// <summary>
        /// The second order delimiter to use.
        /// </summary>
        public const char DELIMITER_2ND_ORDER = ':';
        /// <summary>
        /// The delimiter to use to separate tuple elements.
        /// </summary>
        public const char DELIMITER_TUPLE = ',';
        /// <summary>
        /// The left side characters of a tuple.
        /// </summary>
        public const string TUPLE_LEFT_SIDE = "(";
        /// <summary>
        /// The right side characters of a tuple.
        /// </summary>
        public const string TUPLE_RIGHT_SIDE = ")";
        /// <summary>
        /// The formatter to use for I/O.
        /// </summary>
        public static readonly CultureInfo Formatter = CultureInfo.InvariantCulture;
        /// <summary>
        /// The file used for consolidated results.
        /// </summary>
        public const string CONSOLIDATED_FOOTPRINTS_FILE = "footprints.csv";
    }
}
