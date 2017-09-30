using Gurobi;
using ILOG.Concert;
using ILOG.CPLEX;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.SolverWrappers
{
    /// <summary>
    /// Wrapper class that supplies unify functionality of creating both CPLEX and Gurobi models. This class represents the concrete model and the respective solver to optimize it altogether.
    /// </summary>
    public class LinearModel
    {
        /// <summary>
        /// The type of the solver to internally translate to.
        /// </summary>
        public SolverType Type { get; private set; }

        /// <summary>
        /// All variables of the model.
        /// </summary>
        private HashSet<Variable> _variables = new HashSet<Variable>();

        /// <summary>
        /// The active Gurobi model. (If solver-type is Gurobi)
        /// </summary>
        internal GRBModel GurobiModel;
        /// <summary>
        /// The active CPLEX instance. (If solver-type is CPLEX)
        /// </summary>
        internal Cplex CplexModel;

        /// <summary>
        /// The status callback of this instance.
        /// </summary>
        public IStatusCallback StatusCallback
        {
            get
            {
                switch (Type)
                {
                    case SolverType.CPLEX: return _cplexStatusCallback;
                    case SolverType.Gurobi: return _gurobiStatusCallback;
                    default: throw new ArgumentException("Unknown solver type: " + Type);
                }
            }
        }
        /// <summary>
        /// The callback for Gurobi.
        /// </summary>
        private GurobiStatusCallback _gurobiStatusCallback;
        /// <summary>
        /// The callback for CPLEX.
        /// </summary>
        private CplexStatusCallback _cplexStatusCallback;

        /// <summary>
        /// The logger.
        /// </summary>
        private Action<string> _logger;
        /// <summary>
        /// Logs the message, if a logger is present.
        /// </summary>
        /// <param name="msg">The message to log.</param>
        private void Log(string msg) { _logger?.Invoke(msg); }
        /// <summary>
        /// Logs the message, if a logger is present.
        /// </summary>
        /// <param name="msg">The message to log.</param>
        private void LogLine(string msg) { _logger?.Invoke(msg + Environment.NewLine); }

        public LinearModel(SolverType type, Action<string> logger, int threadCount = 0)
        {
            Type = type;
            _logger = logger;
            switch (type)
            {
                case SolverType.CPLEX:
                    {
                        CplexModel = new Cplex();
                        _cplexStatusCallback = new CplexStatusCallback(this) { };
                        CplexModel.SetOut(new CplexOutputHandler(logger));
                        CplexModel.Use(_cplexStatusCallback);
                        if (threadCount > 0)
                            CplexModel.SetParam(Cplex.IntParam.Threads, threadCount);
                    }
                    break;
                case SolverType.Gurobi:
                    {
                        GRBEnv gurobiEnvironment = new GRBEnv();
                        GurobiModel = new GRBModel(gurobiEnvironment);
                        GurobiModel.GetEnv().Set(GRB.IntParam.UpdateMode, 1); // Enable immediate updates to better support the lazy initialization of the wrappers (at minor performance and memory costs)
                        GurobiModel.GetEnv().Set(GRB.IntParam.OutputFlag, 0);
                        if (threadCount > 0)
                            GurobiModel.GetEnv().Set(GRB.IntParam.Threads, threadCount);
                        _gurobiStatusCallback = new GurobiStatusCallback(this) { Logger = logger };
                        GurobiModel.SetCallback(_gurobiStatusCallback);
                    }
                    break;
                default: throw new ArgumentException("Unknown solver type: " + Type);
            }
        }

        internal void RegisterVariable(Variable variable)
        {
            _variables.Add(variable);
        }

        #region Control

        /// <summary>
        /// Updates the model in order to integrate the latest changes.
        /// </summary>
        public void Update()
        {
            switch (Type)
            {
                case SolverType.CPLEX: /* Nothing to see here - move along */ break;
                case SolverType.Gurobi: /* Currently deactivated, because we are adding a variable only in the case when it is used by a constraint */ break;
                default: throw new ArgumentException("Unknown solver type: " + Type);
            }
        }

        /// <summary>
        /// Solves the model.
        /// </summary>
        public void Optimize()
        {
            // Indicate busyness
            _isBusy = true;
            // Update the model a final time
            Update();
            // Call the respective solve method
            switch (Type)
            {
                case SolverType.CPLEX: CplexModel.Solve(); break;
                case SolverType.Gurobi: GurobiModel.Optimize(); break;
                default: throw new ArgumentException("Unknown solver type: " + Type);
            }
            // Stop being busy
            _isBusy = false;
        }

        /// <summary>
        /// Aborts the current optimization process.
        /// </summary>
        public void Abort()
        {
            switch (Type)
            {
                case SolverType.CPLEX: _cplexStatusCallback.RequestStop(); break;
                case SolverType.Gurobi: GurobiModel.Terminate(); break;
                default: throw new ArgumentException("Unknown solver type: " + Type);
            }
        }

        #endregion

        #region Status

        /// <summary>
        /// Indicates whether a solution was found.
        /// </summary>
        /// <returns><code>true</code> if a solution was found, <code>false</code> otherwise.</returns>
        public bool HasSolution()
        {
            switch (Type)
            {
                case SolverType.CPLEX:
                    if ((CplexModel.IsMIP()) ? CplexModel.SolnPoolNsolns > 0 : (CplexModel.GetStatus() == ILOG.CPLEX.Cplex.Status.Optimal)) return true;
                    else return false;
                case SolverType.Gurobi:
                    if (GurobiModel.Get(GRB.IntAttr.SolCount) > 0) return true;
                    else return false;
                default: throw new ArgumentException("Unknown solver type: " + Type);
            }
        }

        /// <summary>
        /// Indicates whether the model was solved to optimality.
        /// </summary>
        /// <returns><code>true</code> if the model was solved to optimality, <code>false</code> otherwise.</returns>
        public bool IsOptimal()
        {
            switch (Type)
            {
                case SolverType.CPLEX: return CplexModel.GetStatus() == ILOG.CPLEX.Cplex.Status.Optimal;
                case SolverType.Gurobi: return GurobiModel.Get(GRB.IntAttr.Status) == GRB.Status.OPTIMAL;
                default: throw new ArgumentException("Unknown solver type: " + Type);
            }
        }

        /// <summary>
        /// Returns the current objective value (incumbent).
        /// </summary>
        /// <returns>The objective value (incumbent).</returns>
        public double GetObjectiveValue()
        {
            switch (Type)
            {
                case SolverType.CPLEX: return CplexModel.GetObjValue();
                case SolverType.Gurobi: return GurobiModel.Get(GRB.DoubleAttr.ObjVal);
                default: throw new ArgumentException("Unknown solver type: " + Type);
            }
        }

        /// <summary>
        /// Returns the best bound on the objective value.
        /// </summary>
        /// <returns>The best bound on the objective value.</returns>
        public double GetBestBound()
        {
            switch (Type)
            {
                case SolverType.CPLEX: return CplexModel.GetBestObjValue();
                case SolverType.Gurobi: return GurobiModel.Get(GRB.DoubleAttr.ObjBound);
                default: throw new ArgumentException("Unknown solver type: " + Type);
            }
        }

        /// <summary>
        /// Returns the relative gap between incumbent and best bound.
        /// </summary>
        /// <returns>The relative gap.</returns>
        public double GetGap()
        {
            switch (Type)
            {
                case SolverType.CPLEX: return CplexModel.GetMIPRelativeGap();
                case SolverType.Gurobi: return GurobiModel.Get(GRB.DoubleAttr.MIPGap);
                default: throw new ArgumentException("Unknown solver type: " + Type);
            }
        }

        /// <summary>
        /// Busyness helper.
        /// </summary>
        private bool _isBusy = false;
        /// <summary>
        /// Indicates whether the solver is busy.
        /// </summary>
        /// <returns><code>true</code> if the solver is busy, <code>false</code> otherwise.</returns>
        public bool IsBusy() { return _isBusy; }

        #endregion

        #region Model population

        /// <summary>
        /// Sets the objective function.
        /// </summary>
        /// <param name="expression">The objective function.</param>
        /// <param name="sense">The sense (optimization direction) of the objective function.</param>
        public void SetObjective(LinearExpression expression, OptimizationSense sense)
        {
            switch (Type)
            {
                case SolverType.CPLEX: CplexModel.AddObjective((sense == OptimizationSense.Minimize) ? ObjectiveSense.Minimize : ObjectiveSense.Maximize, expression.Expression); break;
                case SolverType.Gurobi: GurobiModel.SetObjective(expression.Expression, (sense == OptimizationSense.Minimize) ? GRB.MINIMIZE : GRB.MAXIMIZE); break;
                default: throw new ArgumentException("Unknown solver type: " + Type);
            }
        }

        /// <summary>
        /// Adds the constraint to the model.
        /// </summary>
        /// <param name="expression">The constraint to add to the model.</param>
        /// <param name="name">A unique name of the constraint.</param>
        public void AddConstr(LinearExpression expression, string name)
        {
            switch (Type)
            {
                case SolverType.CPLEX: CplexModel.Add(expression.Expression); break;
                case SolverType.Gurobi: GurobiModel.AddConstr(expression.Expression, name); break;
                default: throw new ArgumentException("Unknown solver type: " + Type);
            }
        }

        #endregion

        #region Parameters

        /// <summary>
        /// Sets a parameter to a given value by only specifying strings for both.
        /// </summary>
        /// <param name="paramName">The parameter to set.</param>
        /// <param name="paramValue">The value to set the parameter to.</param>
        public void SetParam(string paramName, string paramValue)
        {
            switch (Type)
            {
                case SolverType.CPLEX:
                    {
                        LogLine("Warning! Cannot set param by name for CPLEX - ignoring: " + paramName + "=" + paramValue);
                    }
                    break;
                case SolverType.Gurobi:
                    {
                        GurobiModel.GetEnv().Set(paramName, paramValue);
                        LogLine(paramName + " set to (set by name): " + paramValue);
                    }
                    break;
                default: throw new ArgumentException("Unknown solver type: " + Type);
            }
        }

        /// <summary>
        /// Sets the timelimit for the solve process.
        /// </summary>
        /// <param name="timelimit">A timelimit.</param>
        public void SetTimelimit(TimeSpan timelimit)
        {
            switch (Type)
            {
                case SolverType.CPLEX:
                    {
                        CplexModel.SetParam(ILOG.CPLEX.Cplex.DoubleParam.TiLim, timelimit.TotalSeconds);
                        LogLine("Timelimit set to: " + CplexModel.GetParam(ILOG.CPLEX.Cplex.DoubleParam.TimeLimit));
                    }
                    break;
                case SolverType.Gurobi:
                    {
                        GurobiModel.GetEnv().Set(GRB.DoubleParam.TimeLimit, timelimit.TotalSeconds);
                        LogLine("Timelimit set to: " + GurobiModel.GetEnv().Get(GRB.DoubleParam.TimeLimit));
                    }
                    break;
                default: throw new ArgumentException("Unknown solver type: " + Type);
            }
        }

        /// <summary>
        /// The different scaling behaviors.
        /// </summary>
        public enum ParamScaling
        {
            /// <summary>
            /// Disables scaling.
            /// </summary>
            None,
            /// <summary>
            /// Uses the default scaling behavior.
            /// </summary>
            Default,
            /// <summary>
            /// Enables aggressive scaling.
            /// </summary>
            Aggressive,
        }
        /// <summary>
        /// Sets the scaling behavior, if available.
        /// </summary>
        /// <param name="scaling">The scaling behavior.</param>
        public void SetScaling(ParamScaling scaling)
        {
            switch (Type)
            {
                case SolverType.CPLEX:
                    {
                        CplexModel.SetParam(ILOG.CPLEX.Cplex.IntParam.ScaInd, scaling == ParamScaling.None ? -1 : scaling == ParamScaling.Aggressive ? 1 : 0);
                        LogLine("Scaling set to: " + CplexModel.GetParam(ILOG.CPLEX.Cplex.IntParam.ScaInd));
                    }
                    break;
                case SolverType.Gurobi:
                    {
                        GurobiModel.GetEnv().Set(GRB.IntParam.ScaleFlag, scaling == ParamScaling.None ? 0 : scaling == ParamScaling.Aggressive ? 2 : 0);
                        LogLine("Scaling set to: " + GurobiModel.GetEnv().Get(GRB.IntParam.ScaleFlag));
                    }
                    break;
                default: throw new ArgumentException("Unknown solver type: ");
            }
        }

        #endregion

        #region I/O

        /// <summary>
        /// Exports the model to the specified path as an MPS-file.
        /// </summary>
        /// <param name="path">The path to the file (has to end with .mps).</param>
        public void ExportMPS(string path)
        {
            path = path.EndsWith(".mps") ? path : path + ".mps";
            switch (Type)
            {
                case SolverType.CPLEX: CplexModel.ExportModel(path); break;
                case SolverType.Gurobi: GurobiModel.Update(); GurobiModel.Write(path); break;
                default: throw new ArgumentException("Unknown solver type: " + Type);
            }
        }

        /// <summary>
        /// Exports the model to the specified path as an LP-file.
        /// </summary>
        /// <param name="path">The path to the file (has to end with .lp).</param>
        public void ExportLP(string path)
        {
            path = path.EndsWith(".lp") ? path : path + ".lp";
            switch (Type)
            {
                case SolverType.CPLEX: CplexModel.ExportModel(path); break;
                case SolverType.Gurobi: GurobiModel.Update(); GurobiModel.Write(path); break;
                default: throw new ArgumentException("Unknown solver type: " + Type);
            }
        }

        #endregion

        #region Output-helpers

        public class CplexOutputHandler : TextWriter
        {
            public CplexOutputHandler(Action<string> logger) : base() { _logger = logger; }
            private Action<string> _logger;
            public override void Write(char value) { _logger(value.ToString()); }
            public override void Write(string value) { _logger(value); }
            public override void WriteLine(string value) { _logger(value + Environment.NewLine); }
            public override Encoding Encoding { get { return Encoding.Default; } }
        }

        #endregion

        #region Callback

        public interface IStatusCallback
        {
            /// <summary>
            /// Logs messages of the solver.
            /// </summary>
            Action<string> Logger { get; }
            /// <summary>
            /// Notifies about new incumbents.
            /// </summary>
            Action NewIncumbent { get; set; }
            /// <summary>
            /// Logs new incumbent values.
            /// </summary>
            Action<double> LogIncumbent { get; set; }
        }

        public class GurobiStatusCallback : GRBCallback, IStatusCallback
        {
            /// <summary>
            /// Creates a new callback.
            /// </summary>
            /// <param name="solver">The active solver.</param>
            public GurobiStatusCallback(LinearModel solver) { _solver = solver; }
            /// <summary>
            /// The active solver.
            /// </summary>
            private LinearModel _solver;
            /// <summary>
            /// Logs messages of the solver.
            /// </summary>
            public Action<string> Logger { get; set; }
            /// <summary>
            /// Notifies about new incumbents.
            /// </summary>
            public Action NewIncumbent { get; set; }
            /// <summary>
            /// Logs new incumbent values.
            /// </summary>
            public Action<double> LogIncumbent { get; set; }

            protected override void Callback()
            {
                // If it's a message: log it
                if (where == GRB.Callback.MESSAGE)
                    if ((Logger != null))
                        Logger(this.GetStringInfo(GRB.Callback.MSG_STRING));

                // If it's a new solution: log and notify
                if (where == GRB.Callback.MIPSOL)
                {
                    if (LogIncumbent != null)
                        LogIncumbent(this.GetDoubleInfo(GRB.Callback.MIPSOL_OBJ));
                    if (NewIncumbent != null)
                    {
                        foreach (var variable in _solver._variables)
                            variable._gurobiIntermediateValueRetriever = this.GetSolution;
                        NewIncumbent();
                    }
                }
            }
        }

        public class CplexStatusCallback : Cplex.IncumbentCallback, IStatusCallback
        {
            /// <summary>
            /// Creates a new callback.
            /// </summary>
            /// <param name="solver">The active solver.</param>
            public CplexStatusCallback(LinearModel solver) : base() { _solver = solver; }
            /// <summary>
            /// The active solver.
            /// </summary>
            private LinearModel _solver;
            /// <summary>
            /// Logs messages of the solver.
            /// </summary>
            public Action<string> Logger { get; set; }
            /// <summary>
            /// Notifies about new incumbents.
            /// </summary>
            public Action NewIncumbent { get; set; }
            /// <summary>
            /// Logs new incumbent values.
            /// </summary>
            public Action<double> LogIncumbent { get; set; }
            /// <summary>
            /// Indicates whether a stop was requested.
            /// </summary>
            private bool _stopRequested = false;
            /// <summary>
            /// Instructs the solver to abort the current optimization.
            /// </summary>
            internal void RequestStop() { _stopRequested = true; }

            public override void Main()
            {
                // Log the new incumbent
                if (LogIncumbent != null)
                    LogIncumbent(this.ObjValue);
                // Stop optimization if requested
                if (_stopRequested)
                    Abort();
            }
        }

        //public class CplexStatusCallback : CpxIncumbentCallbackFunction, IStatusCallback
        //{
        //    /// <summary>
        //    /// Creates a new callback.
        //    /// </summary>
        //    /// <param name="solver">The active solver.</param>
        //    public CplexStatusCallback(SolverWrapper solver) { _solver = solver; }
        //    /// <summary>
        //    /// The active solver.
        //    /// </summary>
        //    private SolverWrapper _solver;
        //    /// <summary>
        //    /// Logs messages of the solver.
        //    /// </summary>
        //    public Action<string> Logger { get; set; }
        //    /// <summary>
        //    /// Notifies about new incumbents.
        //    /// </summary>
        //    public Action NewIncumbent { get; set; }
        //    /// <summary>
        //    /// Logs new incumbent values.
        //    /// </summary>
        //    public Action<double> LogIncumbent { get; set; }

        //    public int CallIt(IntPtr xenv, IntPtr cbdata, int wherefrom, object cbhandle, double objval, double[] x, ref int isfeas_p, ref int useraction)
        //    {

        //        return 0;
        //    }
        //}

        #endregion

        public static void Test(SolverType type)
        {
            Console.WriteLine("Is64BitProcess: " + Environment.Is64BitProcess);
            LinearModel wrapper = new LinearModel(type, (string s) => { Console.Write(s); });
            Console.WriteLine("Setting up model and optimizing it with " + wrapper.Type);
            string x = "x"; string y = "y"; string z = "z";
            List<string> variableNames = new List<string>() { x, y, z };
            VariableCollection<string> variables = new VariableCollection<string>(wrapper, VariableType.Binary, 0, 1, (string s) => { return s; });
            wrapper.SetObjective(0.5 * variables[x] + variables[y] + 4 * variables[z], OptimizationSense.Maximize);
            wrapper.AddConstr(LinearExpression.Sum(variableNames.Select(v => 2 * variables[v])) <= 4, "Summation");
            wrapper.AddConstr(variables[x] + variables[y] + variables[z] <= 2.0, "limit");
            wrapper.AddConstr(variables[x] == variables[z], "equal");
            wrapper.AddConstr(variables[x] * 2 == 2, "times");
            wrapper.Update();
            wrapper.Optimize();

            Console.WriteLine("Solution:");
            if (wrapper.HasSolution())
            {
                Console.WriteLine("Obj: " + wrapper.GetObjectiveValue());
                foreach (var variableName in variableNames)
                {
                    Console.WriteLine(variableName + ": " + variables[variableName].GetValue());
                }
            }
            else
            {
                Console.WriteLine("No solution!");
            }
        }
    }
}
