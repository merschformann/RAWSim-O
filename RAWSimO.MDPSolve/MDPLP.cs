using Atto.LinearWrap;
using RAWSimO.Toolbox;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.MDPSolve
{
    /// <summary>
    /// Implements a LP to evaluate the optimal policy determined by an MDP.
    /// </summary>
    public class MDPLP
    {
        /// <summary>
        /// Creates a new LP from a given file.
        /// </summary>
        /// <param name="file">The file to parse the model from.</param>
        /// <param name="logger">The logger to use.</param>
        /// <param name="solverType">The solver to use.</param>
        /// <param name="solverArgs">The args to pass to the solver.</param>
        /// <param name="roundToDecimals">The number of decimals to round all read data to (a negative value deactivates rounding).</param>
        public MDPLP(string file, Action<string> logger, SolverType solverType, string[] solverArgs, int roundToDecimals = -1)
        {
            // Init
            _solverType = solverType;
            _solverArgs =
                solverArgs == null ?
                _solverArgs = new KeyValuePair<string, string>[0] :
                solverArgs.Select(a => { string[] arg = a.Split('='); return new KeyValuePair<string, string>(arg[0], arg[1]); }).ToArray();
            Log = (string msg) => { logger?.Invoke(msg); _logBuffer.Append(msg); };
            LogLine = (string msg) => { logger?.Invoke(msg + Environment.NewLine); _logBuffer.AppendLine(msg); };
            // Read file
            Filename = Path.GetFileName(file);
            LogLine("Creating instance from file " + file + " ...");
            Read(file, roundToDecimals);
            // Output some model statistics
            LogLine("Model statistics:");
            LogLine("m: " + M);
            LogLine("n: " + N);
            LogLine("NNZs: " + NNZs);
        }

        /// <summary>
        /// The name of the file from which this model was created.
        /// </summary>
        public string Filename { get; private set; }
        /// <summary>
        /// Logs the string to the output.
        /// </summary>
        private Action<string> Log;
        /// <summary>
        /// Logs a string to the output and adds the character(s) for terminating the line.
        /// </summary>
        private Action<string> LogLine;
        /// <summary>
        /// Buffers all lines to enable dumping the log at a later time.
        /// </summary>
        private StringBuilder _logBuffer = new StringBuilder();
        /// <summary>
        /// The solver to use.
        /// </summary>
        private SolverType _solverType;
        /// <summary>
        /// Indicates whether a solution is available for being written to a file.
        /// </summary>
        public bool SolutionAvailable { get; private set; } = false;
        /// <summary>
        /// Some optional solver args to pass to the running solver.
        /// </summary>
        private KeyValuePair<string, string>[] _solverArgs;

        /// <summary>
        /// Contains all non-zeros of the coefficient matrix.
        /// </summary>
        private MultiKeyDictionary<double> _coefficientMatrix;
        /// <summary>
        /// The right hand side of the equation system.
        /// </summary>
        private List<double> _rhsVector;
        /// <summary>
        /// The cost vector of the objective function.
        /// </summary>
        private List<double> _objCoeffcientVector;
        /// <summary>
        /// The vector containing the solution after a successful solve operation.
        /// </summary>
        private List<double> _solutionVector;
        /// <summary>
        /// The actual solution value.
        /// </summary>
        private double _solutionValue = double.NaN;
        /// <summary>
        /// The m (number of rows).
        /// </summary>
        public int M { get; private set; }
        /// <summary>
        /// The n (number of columns).
        /// </summary>
        public int N { get; private set; }
        /// <summary>
        /// The number of non-zero elements in the coefficient matrix.
        /// </summary>
        public int NNZs { get; private set; }

        /// <summary>
        /// Reads the instance from the given file.
        /// </summary>
        /// <param name="file">The file to parse.</param>
        /// <param name="roundToDecimals">Indicates whether to round the read values (a negative value indicates no rounding).</param>
        private void Read(string file, int roundToDecimals = -1)
        {
            // Simply read all lines per blocks first
            Dictionary<string, List<string>> blocks = new Dictionary<string, List<string>>();
            // Keep track of current line and block
            string currentBlock = "";
            string line = "";
            // Init handling of one line
            Action handleLine = () =>
            {
                line = line.Trim();
                // Skip comments and empty lines
                if (line.StartsWith(Constants.COMMENT) || string.IsNullOrWhiteSpace(line)) { return; }
                // If it is a block start to set everything up
                if (line.StartsWith(Constants.IDENT_COEFF_MATRIX)) { blocks[Constants.IDENT_COEFF_MATRIX] = new List<string>(); currentBlock = Constants.IDENT_COEFF_MATRIX; return; }
                else if (line.StartsWith(Constants.IDENT_RHS_VECTOR)) { blocks[Constants.IDENT_RHS_VECTOR] = new List<string>(); currentBlock = Constants.IDENT_RHS_VECTOR; return; }
                else if (line.StartsWith(Constants.IDENT_OBJ_COEFF_VECTOR)) { blocks[Constants.IDENT_OBJ_COEFF_VECTOR] = new List<string>(); currentBlock = Constants.IDENT_OBJ_COEFF_VECTOR; return; }
                else { blocks[currentBlock].Add(line); }
            };
            // See which format of the file we have to handle
            if (Path.GetExtension(file) == ".gz")
            {
                using (FileStream originalFileStream = new FileStream(file, FileMode.Open))
                using (GZipStream decompressionStream = new GZipStream(originalFileStream, CompressionMode.Decompress))
                using (StreamReader sr = new StreamReader(decompressionStream))
                    while ((line = sr.ReadLine()) != null)
                        handleLine();
            }
            else
            {
                using (StreamReader sr = new StreamReader(file))
                    while ((line = sr.ReadLine()) != null)
                        handleLine();
            }
            // --> Parse the data
            _coefficientMatrix = new MultiKeyDictionary<double>();
            string[] coeffElements = string.Join(Constants.DELIMITER_MAIN.ToString(), blocks[Constants.IDENT_COEFF_MATRIX]).Split(Constants.DELIMITER_MAIN);
            foreach (var stringEle in coeffElements)
            {
                string[] element = stringEle.Split(Constants.DELIMITER_2ND_ORDER);
                string[] index = element[0].Replace(Constants.TUPLE_LEFT_SIDE, string.Empty).Replace(Constants.TUPLE_RIGHT_SIDE, string.Empty).Split(Constants.DELIMITER_TUPLE);
                double coeffValue = double.Parse(element[1], Constants.Formatter);
                if (roundToDecimals >= 0)
                    coeffValue = Math.Round(coeffValue, roundToDecimals);
                _coefficientMatrix[int.Parse(index[0], Constants.Formatter), int.Parse(index[1], Constants.Formatter)] = coeffValue;
            }
            NNZs = _coefficientMatrix.Values.Count();
            _rhsVector = blocks[Constants.IDENT_RHS_VECTOR].Single().Split(Constants.DELIMITER_MAIN).Select(v =>
            {
                double rhsValue = double.Parse(v, Constants.Formatter);
                if (roundToDecimals >= 0)
                    rhsValue = Math.Round(rhsValue, roundToDecimals);
                return rhsValue;
            }).ToList();
            M = _rhsVector.Count;
            _objCoeffcientVector = blocks[Constants.IDENT_OBJ_COEFF_VECTOR].Single().Split(Constants.DELIMITER_MAIN).Select(v =>
            {
                double objValue = double.Parse(v, Constants.Formatter);
                if (roundToDecimals >= 0)
                    objValue = Math.Round(objValue, roundToDecimals);
                return objValue;
            }).ToList();
            N = _objCoeffcientVector.Count;
        }

        /// <summary>
        /// Appends the model to the active string builder.
        /// </summary>
        /// <param name="sb">The string builder to use.</param>
        private void AppendModel(StringBuilder sb)
        {
            // Write coefficient matrix
            sb.AppendLine(Constants.IDENT_COEFF_MATRIX);
            sb.AppendLine(string.Join(Constants.DELIMITER_MAIN.ToString(), _coefficientMatrix.Keys.Select(indexPair =>
            {
                int i = (int)indexPair[0];
                int j = (int)indexPair[1];
                return
                    Constants.TUPLE_LEFT_SIDE + i.ToString(Constants.Formatter) + Constants.DELIMITER_TUPLE + j.ToString(Constants.Formatter) + Constants.TUPLE_RIGHT_SIDE +
                    Constants.DELIMITER_2ND_ORDER + _coefficientMatrix[indexPair].ToString(Constants.Formatter);
            })));
            // Write RHS vector
            sb.AppendLine(Constants.IDENT_RHS_VECTOR);
            sb.AppendLine(string.Join(Constants.DELIMITER_MAIN.ToString(), _rhsVector.Select(e => e.ToString(Constants.Formatter))));
            // Write objective coefficient vector
            sb.AppendLine(Constants.IDENT_OBJ_COEFF_VECTOR);
            sb.AppendLine(string.Join(Constants.DELIMITER_MAIN.ToString(), _objCoeffcientVector.Select(e => e.ToString(Constants.Formatter))));
        }
        /// <summary>
        /// Appends the solution to the active string builder.
        /// </summary>
        /// <param name="sb">The string builder to use.</param>
        private void AppendSolution(StringBuilder sb)
        {
            // Write solution vector
            sb.AppendLine(Constants.IDENT_SOLUTION_VECTOR);
            sb.AppendLine(string.Join(Constants.DELIMITER_MAIN.ToString(), _solutionVector.Select(e => e.ToString(Constants.Formatter))));
            // Write solution value
            sb.AppendLine(Constants.IDENT_SOLUTION_VALUE);
            sb.AppendLine(_solutionValue.ToString(Constants.Formatter));
        }
        /// <summary>
        /// Writes the instance including the solution to a file, if a solution is available.
        /// </summary>
        /// <param name="file">The file to write to.</param>
        public void Write(string file)
        {
            // Write to string builder first
            LogLine("Writing instance to " + file);
            StringBuilder sb = new StringBuilder();
            // Append model
            AppendModel(sb);
            // Append solution
            if (SolutionAvailable)
                AppendSolution(sb);

            // See which format of the file we have to handle
            if (Path.GetExtension(file) == ".gz")
            {
                using (FileStream outputFilestream = new FileStream(file, FileMode.Create))
                using (GZipStream compressedFileStream = new GZipStream(outputFilestream, CompressionMode.Compress, false))
                using (StreamWriter sw = new StreamWriter(compressedFileStream))
                    sw.WriteLine(sb.ToString());
            }
            else
            {
                using (StreamWriter sw = new StreamWriter(file))
                    sw.WriteLine(sb.ToString());
            }
        }

        /// <summary>
        /// Dumps the log to a file.
        /// </summary>
        /// <param name="file">The file to write to.</param>
        public void DumpLog(string file)
        {
            // Write to string builder first
            LogLine("Dumping log to file " + file);
            // See which format of the file we have to handle
            if (Path.GetExtension(file) == ".gz")
            {
                using (FileStream outputFilestream = new FileStream(file, FileMode.Create))
                using (GZipStream compressedFileStream = new GZipStream(outputFilestream, CompressionMode.Compress))
                using (StreamWriter sw = new StreamWriter(compressedFileStream))
                    sw.WriteLine(_logBuffer.ToString());
            }
            else
            {
                using (StreamWriter sw = new StreamWriter(file))
                    sw.WriteLine(_logBuffer.ToString());
            }
        }

        /// <summary>
        /// Logs a performance footprint to a consolidated file.
        /// </summary>
        /// <param name="start">The timestamp of instantiation.</param>
        /// <param name="name"></param>
        /// <param name="time"></param>
        /// <param name="solAvail"></param>
        /// <param name="obj"></param>
        /// <param name="solverParams"></param>
        public void LogPerformance(DateTime start, string name, TimeSpan time, bool solAvail, double obj, IEnumerable<string> solverParams)
        {
            // Append to existing footprints file - or create one, if it does not exist
            bool exists = File.Exists(Constants.CONSOLIDATED_FOOTPRINTS_FILE);
            using (StreamWriter sw = new StreamWriter(Constants.CONSOLIDATED_FOOTPRINTS_FILE, true))
            {
                // Write header, if the file was just created
                if (!exists)
                    sw.WriteLine(
                        "Timestamp" + Constants.DELIMITER_MAIN +
                        "Name" + Constants.DELIMITER_MAIN +
                        "Time" + Constants.DELIMITER_MAIN +
                        "SolAvail" + Constants.DELIMITER_MAIN +
                        "Obj" + Constants.DELIMITER_MAIN +
                        "Params");
                // Write result line
                sw.WriteLine(
                    start.ToString("yyyyMMdd-HHmmss") + Constants.DELIMITER_MAIN +
                    name + Constants.DELIMITER_MAIN +
                    time.TotalSeconds.ToString(Constants.Formatter) + Constants.DELIMITER_MAIN +
                    solAvail.ToString(Constants.Formatter) + Constants.DELIMITER_MAIN +
                    obj.ToString(Constants.Formatter) + Constants.DELIMITER_MAIN +
                    string.Join(Constants.DELIMITER_TUPLE.ToString(), solverParams));
            }
        }

        /// <summary>
        /// Solves this model.
        /// </summary>
        public void Solve()
        {
            // Init
            LogLine("Solving ...");
            DateTime before = DateTime.Now;
            LinearModel model = new LinearModel(SolverType.Gurobi, (string msg) => { Log(msg); });
            VariableCollection<int> xVar = new VariableCollection<int>(model, VariableType.Continuous, 0, double.PositiveInfinity, (int j) => { return "x" + j.ToString(); });

            // Modify some control parameters
            foreach (var param in _solverArgs)
                throw new NotSupportedException("Solver parameters currently not supported, request support in Atto.LinearWrap");
                // model.SetParam(param.Key, param.Value);

            // Prepare non-zero indices
            Dictionary<int, List<int>> nnzs = _coefficientMatrix.Keys.GroupBy(k => (int)k[0]).ToDictionary(k => k.Key, v => v.Select(e => (int)e[1]).ToList());

            // Add objective
            model.SetObjective(
                LinearExpression.Sum(Enumerable.Range(0, N).Select(j => _objCoeffcientVector[j] * xVar[j])),
                OptimizationSense.Minimize);
            // Add constraint
            foreach (var i in nnzs.Keys)
                model.AddConstr(LinearExpression.Sum(nnzs[i].Select(j => _coefficientMatrix[i, j] * xVar[j])) == _rhsVector[i],
                "Con" + i);
            // TODO remove debug
            model.Update();
            model.ExportMPS("mdp.mps");
            // Solve
            model.Optimize();
            TimeSpan solutionTime = DateTime.Now - before;
            // Get solution
            if (model.HasSolution())
            {
                LogLine("Solution found!");
                _solutionVector = Enumerable.Range(0, N).Select(j => xVar[j].Value).ToList();
                _solutionValue = model.GetObjectiveValue();
                SolutionAvailable = true;
            }
            else { LogLine("No solution!"); }
            // Log performance
            LogPerformance(before, Filename, solutionTime, SolutionAvailable, _solutionValue, _solverArgs.Select(a => a.Key + "=" + a.Value));
        }
    }
}
