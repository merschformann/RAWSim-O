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
    /// Stores a linear expression and handles all operations on it.
    /// </summary>
    public class LinearExpression
    {
        internal dynamic Expression;
        protected LinearModel Solver;

        #region Sum

        public static LinearExpression Sum(IEnumerable<double> coeffs, IEnumerable<Variable> variables)
        {
            LinearModel solver = variables.First().Solver;
            switch (solver.Type)
            {
                case SolverType.CPLEX: { return new LinearExpression() { Solver = solver, Expression = solver.CplexModel.ScalProd(coeffs.ToArray(), variables.Select(e => e.Expression as INumVar).ToArray()) }; }
                case SolverType.Gurobi:
                    {
                        GRBLinExpr expr = new GRBLinExpr();
                        expr.AddTerms(coeffs.ToArray(), variables.Select(e => e.Expression as GRBVar).ToArray());
                        return new LinearExpression() { Solver = solver, Expression = expr };
                    }
                default: throw new ArgumentException("Unknown solver type: " + solver.Type);
            }
        }

        public static LinearExpression Sum(IEnumerable<Variable> variables)
        {
            LinearModel solver = variables.First().Solver;
            switch (solver.Type)
            {
                case SolverType.CPLEX: { return new LinearExpression() { Solver = solver, Expression = solver.CplexModel.Sum(variables.Select(e => e.Expression as INumExpr).ToArray()) }; }
                case SolverType.Gurobi:
                    {
                        GRBLinExpr expr = new GRBLinExpr();
                        expr.AddTerms(Enumerable.Repeat(1.0, variables.Count()).ToArray(), variables.Select(e => e.Expression as GRBVar).ToArray());
                        return new LinearExpression() { Solver = solver, Expression = expr };
                    }
                default: throw new ArgumentException("Unknown solver type: " + solver.Type);
            }
        }

        public static LinearExpression Sum(IEnumerable<LinearExpression> expressions)
        {
            LinearModel solver = expressions.First().Solver;
            switch (solver.Type)
            {
                case SolverType.CPLEX: { return new LinearExpression() { Solver = solver, Expression = solver.CplexModel.Sum(expressions.Select(e => e.Expression as INumExpr).ToArray()) }; }
                case SolverType.Gurobi:
                    {
                        GRBLinExpr expr = new GRBLinExpr();
                        foreach (var exp in expressions)
                            expr += exp.Expression;
                        return new LinearExpression() { Solver = solver, Expression = expr };
                    }
                default: throw new ArgumentException("Unknown solver type: " + solver.Type);
            }
        }

        #endregion

        #region Add

        public static LinearExpression operator +(LinearExpression exp1, LinearExpression exp2)
        {
            switch (exp1.Solver.Type)
            {
                case SolverType.CPLEX: { return new LinearExpression() { Solver = exp1.Solver, Expression = exp1.Solver.CplexModel.Sum(exp1.Expression, exp2.Expression) }; }
                case SolverType.Gurobi: { return new LinearExpression() { Solver = exp1.Solver, Expression = exp1.Expression + exp2.Expression }; }
                default: throw new ArgumentException("Unknown solver type: " + exp1.Solver.Type);
            }
        }

        public static LinearExpression operator +(LinearExpression exp1, double exp2)
        {
            switch (exp1.Solver.Type)
            {
                case SolverType.CPLEX: { return new LinearExpression() { Solver = exp1.Solver, Expression = exp1.Solver.CplexModel.Sum(exp1.Expression, exp2) }; }
                case SolverType.Gurobi: { return new LinearExpression() { Solver = exp1.Solver, Expression = exp1.Expression + exp2 }; }
                default: throw new ArgumentException("Unknown solver type: " + exp1.Solver.Type);
            }
        }

        public static LinearExpression operator +(double exp1, LinearExpression exp2)
        {
            switch (exp2.Solver.Type)
            {
                case SolverType.CPLEX: { return new LinearExpression() { Solver = exp2.Solver, Expression = exp2.Solver.CplexModel.Sum(exp1, exp2.Expression) }; }
                case SolverType.Gurobi: { return new LinearExpression() { Solver = exp2.Solver, Expression = exp1 + exp2.Expression }; }
                default: throw new ArgumentException("Unknown solver type: " + exp2.Solver.Type);
            }
        }

        #endregion

        #region Subtract

        public static LinearExpression operator -(LinearExpression exp1, LinearExpression exp2)
        {
            switch (exp1.Solver.Type)
            {
                case SolverType.CPLEX: { return new LinearExpression() { Solver = exp1.Solver, Expression = exp1.Solver.CplexModel.Diff(exp1.Expression, exp2.Expression) }; }
                case SolverType.Gurobi: { return new LinearExpression() { Solver = exp1.Solver, Expression = exp1.Expression - exp2.Expression }; }
                default: throw new ArgumentException("Unknown solver type: " + exp1.Solver.Type);
            }
        }

        public static LinearExpression operator -(LinearExpression exp1, double exp2)
        {
            switch (exp1.Solver.Type)
            {
                case SolverType.CPLEX: { return new LinearExpression() { Solver = exp1.Solver, Expression = exp1.Solver.CplexModel.Diff(exp1.Expression, exp2) }; }
                case SolverType.Gurobi: { return new LinearExpression() { Solver = exp1.Solver, Expression = exp1.Expression - exp2 }; }
                default: throw new ArgumentException("Unknown solver type: " + exp1.Solver.Type);
            }
        }

        public static LinearExpression operator -(double exp1, LinearExpression exp2)
        {
            switch (exp2.Solver.Type)
            {
                case SolverType.CPLEX: { return new LinearExpression() { Solver = exp2.Solver, Expression = exp2.Solver.CplexModel.Diff(exp1, exp2.Expression) }; }
                case SolverType.Gurobi: { return new LinearExpression() { Solver = exp2.Solver, Expression = exp1 - exp2.Expression }; }
                default: throw new ArgumentException("Unknown solver type: " + exp2.Solver.Type);
            }
        }

        #endregion

        #region Multiply

        public static LinearExpression operator *(LinearExpression exp1, LinearExpression exp2)
        {
            switch (exp1.Solver.Type)
            {
                case SolverType.CPLEX: { return new LinearExpression() { Solver = exp1.Solver, Expression = exp1.Solver.CplexModel.Prod(exp1.Expression, exp2.Expression) }; }
                case SolverType.Gurobi: { return new LinearExpression() { Solver = exp1.Solver, Expression = exp1.Expression * exp2.Expression }; }
                default: throw new ArgumentException("Unknown solver type: " + exp1.Solver.Type);
            }
        }

        public static LinearExpression operator *(LinearExpression exp1, double exp2)
        {
            switch (exp1.Solver.Type)
            {
                case SolverType.CPLEX: { return new LinearExpression() { Solver = exp1.Solver, Expression = exp1.Solver.CplexModel.Prod(exp1.Expression, exp2) }; }
                case SolverType.Gurobi: { return new LinearExpression() { Solver = exp1.Solver, Expression = exp1.Expression * exp2 }; }
                default: throw new ArgumentException("Unknown solver type: " + exp1.Solver.Type);
            }
        }

        public static LinearExpression operator *(double exp1, LinearExpression exp2)
        {
            switch (exp2.Solver.Type)
            {
                case SolverType.CPLEX: { return new LinearExpression() { Solver = exp2.Solver, Expression = exp2.Solver.CplexModel.Prod(exp1, exp2.Expression) }; }
                case SolverType.Gurobi: { return new LinearExpression() { Solver = exp2.Solver, Expression = exp1 * exp2.Expression }; }
                default: throw new ArgumentException("Unknown solver type: " + exp2.Solver.Type);
            }
        }

        #endregion

        #region Lesser equals

        public static LinearExpression operator <=(LinearExpression exp1, LinearExpression exp2)
        {
            switch (exp1.Solver.Type)
            {
                case SolverType.CPLEX: return new LinearExpression() { Solver = exp1.Solver, Expression = exp1.Solver.CplexModel.Le(exp1.Expression, exp2.Expression) };
                case SolverType.Gurobi: return new LinearExpression() { Solver = exp1.Solver, Expression = exp1.Expression <= exp2.Expression };
                default: throw new ArgumentException("Unknown solver type: " + exp1.Solver.Type);
            }
        }

        public static LinearExpression operator <=(LinearExpression exp1, double exp2)
        {
            switch (exp1.Solver.Type)
            {
                case SolverType.CPLEX: return new LinearExpression() { Solver = exp1.Solver, Expression = exp1.Solver.CplexModel.Le(exp1.Expression, exp2) };
                case SolverType.Gurobi: return new LinearExpression() { Solver = exp1.Solver, Expression = exp1.Expression <= exp2 };
                default: throw new ArgumentException("Unknown solver type: " + exp1.Solver.Type);
            }
        }

        public static LinearExpression operator <=(double exp1, LinearExpression exp2)
        {
            switch (exp2.Solver.Type)
            {
                case SolverType.CPLEX: return new LinearExpression() { Solver = exp2.Solver, Expression = exp2.Solver.CplexModel.Le(exp1, exp2.Expression) };
                case SolverType.Gurobi: return new LinearExpression() { Solver = exp2.Solver, Expression = exp1 <= exp2.Expression };
                default: throw new ArgumentException("Unknown solver type: " + exp2.Solver.Type);
            }
        }

        #endregion

        #region Greater equals

        public static LinearExpression operator >=(LinearExpression exp1, LinearExpression exp2)
        {
            switch (exp1.Solver.Type)
            {
                case SolverType.CPLEX: return new LinearExpression() { Solver = exp1.Solver, Expression = exp1.Solver.CplexModel.Ge(exp1.Expression, exp2.Expression) };
                case SolverType.Gurobi: return new LinearExpression() { Solver = exp1.Solver, Expression = exp1.Expression >= exp2.Expression };
                default: throw new ArgumentException("Unknown solver type: " + exp1.Solver.Type);
            }
        }

        public static LinearExpression operator >=(LinearExpression exp1, double exp2)
        {
            switch (exp1.Solver.Type)
            {
                case SolverType.CPLEX: return new LinearExpression() { Solver = exp1.Solver, Expression = exp1.Solver.CplexModel.Ge(exp1.Expression, exp2) };
                case SolverType.Gurobi: return new LinearExpression() { Solver = exp1.Solver, Expression = exp1.Expression >= exp2 };
                default: throw new ArgumentException("Unknown solver type: " + exp1.Solver.Type);
            }
        }

        public static LinearExpression operator >=(double exp1, LinearExpression exp2)
        {
            switch (exp2.Solver.Type)
            {
                case SolverType.CPLEX: return new LinearExpression() { Solver = exp2.Solver, Expression = exp2.Solver.CplexModel.Ge(exp1, exp2.Expression) };
                case SolverType.Gurobi: return new LinearExpression() { Solver = exp2.Solver, Expression = exp1 >= exp2.Expression };
                default: throw new ArgumentException("Unknown solver type: " + exp2.Solver.Type);
            }
        }

        #endregion

        #region Equals

        public static LinearExpression operator ==(LinearExpression exp1, LinearExpression exp2)
        {
            switch (exp1.Solver.Type)
            {
                case SolverType.CPLEX: return new LinearExpression() { Solver = exp1.Solver, Expression = exp1.Solver.CplexModel.Eq(exp1.Expression, exp2.Expression) };
                case SolverType.Gurobi: return new LinearExpression() { Solver = exp1.Solver, Expression = exp1.Expression == exp2.Expression };
                default: throw new ArgumentException("Unknown solver type: " + exp1.Solver.Type);
            }
        }

        public static LinearExpression operator ==(LinearExpression exp1, double exp2)
        {
            switch (exp1.Solver.Type)
            {
                case SolverType.CPLEX: return new LinearExpression() { Solver = exp1.Solver, Expression = exp1.Solver.CplexModel.Eq(exp1.Expression, exp2) };
                case SolverType.Gurobi: return new LinearExpression() { Solver = exp1.Solver, Expression = exp1.Expression == exp2 };
                default: throw new ArgumentException("Unknown solver type: " + exp1.Solver.Type);
            }
        }

        public static LinearExpression operator ==(double exp1, LinearExpression exp2)
        {
            switch (exp2.Solver.Type)
            {
                case SolverType.CPLEX: return new LinearExpression() { Solver = exp2.Solver, Expression = exp2.Solver.CplexModel.Eq(exp1, exp2.Expression) };
                case SolverType.Gurobi: return new LinearExpression() { Solver = exp2.Solver, Expression = exp1 == exp2.Expression };
                default: throw new ArgumentException("Unknown solver type: " + exp2.Solver.Type);
            }
        }

        public static LinearExpression operator !=(LinearExpression exp1, LinearExpression exp2) { throw new InvalidOperationException("No unequality operator defined!"); }

        public static LinearExpression operator !=(LinearExpression exp1, double exp2) { throw new InvalidOperationException("No unequality operator defined!"); }

        public static LinearExpression operator !=(double exp1, LinearExpression exp2) { throw new InvalidOperationException("No unequality operator defined!"); }

        public override bool Equals(object obj) { return base.Equals(obj); }

        public override int GetHashCode() { return base.GetHashCode(); }

        #endregion
    }
}
