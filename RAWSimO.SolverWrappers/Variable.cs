using Gurobi;
using ILOG.Concert;
using ILOG.CPLEX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.SolverWrappers
{
    /// <summary>
    /// Represents a variable that wraps variables of the respective solver.
    /// </summary>
    public class Variable : LinearExpression
    {
        /// <summary>
        /// The type of the variable.
        /// </summary>
        private VariableType _variableType;

        /// <summary>
        /// Creates a new variable.
        /// </summary>
        /// <param name="solver">The model / solver instance this variable belongs to.</param>
        /// <param name="variableType">The type of the variable.</param>
        /// <param name="lb">The lower bound of the variable.</param>
        /// <param name="ub">The upper bound of the variable.</param>
        /// <param name="name">The name of the variable.</param>
        public Variable(LinearModel solver, VariableType variableType, double lb, double ub, string name)
        {
            // Set active solver reference
            Solver = solver;
            Solver.RegisterVariable(this);
            // Set parameters
            _variableType = variableType;
            // Instantiate the variable
            switch (solver.Type)
            {
                case SolverType.CPLEX:
                    Expression =
                        (_variableType == VariableType.Continuous) ? solver.CplexModel.NumVar(lb, ub, NumVarType.Float) :
                        (_variableType == VariableType.Binary) ? solver.CplexModel.BoolVar(name) :
                        solver.CplexModel.IntVar((int)lb, (int)ub, name);
                    break;
                case SolverType.Gurobi:
                    Expression =
                        solver.GurobiModel.AddVar(lb, ub, 0, (variableType == VariableType.Continuous) ? GRB.CONTINUOUS : (variableType == VariableType.Binary) ? GRB.BINARY : GRB.INTEGER, name);
                    break;
                default: throw new ArgumentException("Unknown solver type: " + solver.Type);
            }
        }

        /// <summary>
        /// A function able to retrieve an intermediate value (optimization still running) from the model. (The value has to be set by a callback before this call)
        /// </summary>
        internal Func<GRBVar, double> _gurobiIntermediateValueRetriever;
        /// <summary>
        /// An intermediate solution value that is used after a callback has happened.
        /// </summary>
        public double CallbackValue { get { return _gurobiIntermediateValueRetriever(Expression); } }

        /// <summary>
        /// Returns the variable's value.
        /// </summary>
        public double Value { get { return GetValue(); } }

        /// <summary>
        /// Returns the variable's value.
        /// </summary>
        public double GetValue()
        {
            switch (Solver.Type)
            {
                case SolverType.CPLEX: return Solver.CplexModel.GetValue(Expression);
                case SolverType.Gurobi: return Expression.Get(GRB.DoubleAttr.X);
                default: throw new ArgumentException("Unknown solver type: " + Solver.Type);
            }
        }

        /// <summary>
        /// The variable's lower bound.
        /// </summary>
        public double LB { get { return GetLB(); } set { SetLB(value); } }

        /// <summary>
        /// Returns the variable's lower bound.
        /// </summary>
        /// <returns>The lower bound.</returns>
        public double GetLB()
        {
            switch (Solver.Type)
            {
                case SolverType.CPLEX: return Expression.LB;
                case SolverType.Gurobi: return Expression.Get(GRB.DoubleAttr.LB);
                default: throw new ArgumentException("Unknown solver type: " + Solver.Type);
            }
        }

        /// <summary>
        /// Sets the lower bound of the variable.
        /// </summary>
        /// <param name="value">Value for the LB.</param>
        public void SetLB(double value)
        {
            switch (Solver.Type)
            {
                case SolverType.CPLEX: Expression.LB = value; break;
                case SolverType.Gurobi: Expression.Set(GRB.DoubleAttr.LB, value); break;
                default: throw new ArgumentException("Unknown solver type: " + Solver.Type);
            }
        }

        /// <summary>
        /// The variable's upper bound.
        /// </summary>
        public double UB { get { return GetUB(); } set { SetUB(value); } }

        /// <summary>
        /// Returns the variable's upper bound.
        /// </summary>
        /// <returns>The upper bound.</returns>
        public double GetUB()
        {
            switch (Solver.Type)
            {
                case SolverType.CPLEX: return Expression.UB;
                case SolverType.Gurobi: return Expression.Get(GRB.DoubleAttr.UB);
                default: throw new ArgumentException("Unknown solver type: " + Solver.Type);
            }
        }

        /// <summary>
        /// Sets the upper bound of the variable.
        /// </summary>
        /// <param name="value">Value for the UB.</param>
        public void SetUB(double value)
        {
            switch (Solver.Type)
            {
                case SolverType.CPLEX: Expression.UB = value; break;
                case SolverType.Gurobi: Expression.Set(GRB.DoubleAttr.UB, value); break;
                default: throw new ArgumentException("Unknown solver type: " + Solver.Type);
            }
        }
    }
}
