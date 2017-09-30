using RAWSimO.Toolbox;
using Gurobi;
using ILOG.CPLEX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.SolverWrappers
{
    /// <summary>
    /// Represents the basis of all variable collections.
    /// </summary>
    public class VarCollection
    {
        /// <summary>
        /// The type of the variables in this collection.
        /// </summary>
        protected VariableType _variableType;
        /// <summary>
        /// The lower bound to use if no other is given.
        /// </summary>
        protected double LB;
        /// <summary>
        /// The upper bound to use if no other is given.
        /// </summary>
        protected double UB;
        /// <summary>
        /// The model/solver to which this collection belongs.
        /// </summary>
        protected LinearModel _solver;
        /// <summary>
        /// The variables of this collection by their index.
        /// </summary>
        protected Dictionary<object[], Variable> _variables;
        /// <summary>
        /// The current number of variables in this collection.
        /// </summary>
        public int Count { get { return _variables.Count; } }
        /// <summary>
        /// Creates a new collection of variables.
        /// </summary>
        /// <param name="solver">The solver the variable collection belongs to.</param>
        /// <param name="variableType">The type of the variables in the collection.</param>
        /// <param name="lowerBound">The default lower bound when adding a variable on-the-fly.</param>
        /// <param name="upperBound">The default upper bound when adding a variable on-the-fly.</param>
        public VarCollection(LinearModel solver, VariableType variableType, double lowerBound, double upperBound)
        {
            // Set parameters
            _variableType = variableType;
            LB = lowerBound;
            UB = upperBound;
            // Reference active solver
            _solver = solver;
            // Instantiate the collection
            _variables = new Dictionary<object[], Variable>(new ObjectArrayEqualityComparer<object>());
        }
        /// <summary>
        /// Indicates whether this collection contains a variable at the given index.
        /// </summary>
        /// <param name="index">The index to look up.</param>
        /// <returns>Returns <code>true</code> if the variable was found, <code>false</code> otherwise.</returns>
        protected bool ContainsVariableAtIndex(params object[] index) { return _variables.ContainsKey(index); }
        /// <summary>
        /// Returns the respective variable at the given index.
        /// </summary>
        /// <param name="index">The index of the variable.</param>
        /// <returns>The variable at the index.</returns>
        protected Variable GetVariableByIndex(params object[] index) { return _variables[index]; }
        /// <summary>
        /// Sets the variable at the given index.
        /// </summary>
        /// <param name="value">The variable to set at the index.</param>
        /// <param name="index">The index of the variable.</param>
        protected void SetVariableByIndex(Variable value, params object[] index) { _variables[index] = value; }
    }
    /// <summary>
    /// A collection of variables with non-typed indeces.
    /// </summary>
    public class VariableCollection : VarCollection
    {
        /// <summary>
        /// Creates a new collection of variables.
        /// </summary>
        /// <param name="solver">The solver the variable collection belongs to.</param>
        /// <param name="variableType">The type of the variables in the collection.</param>
        /// <param name="lowerBound">The default lower bound when adding a variable on-the-fly.</param>
        /// <param name="upperBound">The default upper bound when adding a variable on-the-fly.</param>
        public VariableCollection(LinearModel solver, VariableType variableType, double lowerBound, double upperBound) : base(solver, variableType, lowerBound, upperBound) { }
        /// <summary>
        /// Creates a variable at the given index.
        /// </summary>
        /// <param name="lowerBound">The variable's lower bound.</param>
        /// <param name="upperBound">The variable's upper bound.</param>
        /// <param name="name">The variable's name.</param>
        /// <param name="index">The index.</param>
        public void Add(double lowerBound, double upperBound, string name, params object[] index)
        { this[index] = new Variable(_solver, _variableType, lowerBound, upperBound, name); }
        /// <summary>
        /// The variable at the specified index.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <returns>The variable.</returns>
        public Variable this[params object[] index] { get { return _variables[index]; } set { _variables[index] = value; } }
    }
    /// <summary>
    /// A collection of variables.
    /// </summary>
    /// <typeparam name="T1">Type of the first index.</typeparam>
    public class VariableCollection<T1> : VariableCollection
    {
        /// <summary>
        /// Function to name variables created on-the-fly.
        /// </summary>
        private Func<T1, string> _namingFunction;
        /// <summary>
        /// Creates a new collection of variables.
        /// </summary>
        /// <param name="solver">The solver the variable collection belongs to.</param>
        /// <param name="variableType">The type of the variables in the collection.</param>
        /// <param name="lowerBound">The variable's lower bound.</param>
        /// <param name="upperBound">The variable's upper bound.</param>
        /// <param name="namingFunction">Function to name variables created on-the-fly.</param>
        public VariableCollection(LinearModel solver, VariableType variableType, double lowerBound, double upperBound, Func<T1, string> namingFunction) : base(solver, variableType, lowerBound, upperBound) { _namingFunction = namingFunction; }
        /// <summary>
        /// Creates a variable at the given index.
        /// </summary>
        /// <param name="lowerBound">The variable's lower bound.</param>
        /// <param name="upperBound">The variable's upper bound.</param>
        /// <param name="name">The variable's name.</param>
        /// <param name="index1">The first index.</param>
        public void Add(double lowerBound, double upperBound, string name, T1 index)
        { this[index] = new Variable(_solver, _variableType, lowerBound, upperBound, name); }
        /// <summary>
        /// Creates variables for all indeces.
        /// </summary>
        /// <param name="lowerBound">The lower bound of all variables.</param>
        /// <param name="upperBound">The upper bound of all variables.</param>
        /// <param name="name">The naming function supplying a unique name per variable.</param>
        /// <param name="indeces">The indices to create variables for.</param>
        public void AddRange(double lowerBound, double upperBound, Func<T1, string> name, IEnumerable<T1> indeces)
        {
            foreach (var index in indeces)
                Add(lowerBound, upperBound, name(index), index);
        }
        /// <summary>
        /// The variable at the specified index.
        /// </summary>
        /// <param name="index1">The first index.</param>
        /// <returns>The variable.</returns>
        public Variable this[T1 index]
        {
            get
            {
                // Create the variable on-the-fly, if it does not exist
                if (!ContainsVariableAtIndex(index))
                {
                    Add(LB, UB, _namingFunction(index), index);
                    _solver.Update();
                }
                // Return it
                return GetVariableByIndex(index);
            }
            set { SetVariableByIndex(value, index); }
        }
    }
    /// <summary>
    /// A collection of variables.
    /// </summary>
    /// <typeparam name="T1">Type of the first index.</typeparam>
    /// <typeparam name="T2">Type of the second index.</typeparam>
    public class VariableCollection<T1, T2> : VariableCollection
    {
        /// <summary>
        /// Function to name variables created on-the-fly.
        /// </summary>
        private Func<T1, T2, string> _namingFunction;
        /// <summary>
        /// Creates a new collection of variables.
        /// </summary>
        /// <param name="solver">The solver the variable collection belongs to.</param>
        /// <param name="variableType">The type of the variables in the collection.</param>
        public VariableCollection(LinearModel solver, VariableType variableType, double lowerBound, double upperBound, Func<T1, T2, string> namingFunction) : base(solver, variableType, lowerBound, upperBound) { _namingFunction = namingFunction; }
        /// <summary>
        /// Creates a variable at the given index.
        /// </summary>
        /// <param name="lowerBound">The variable's lower bound.</param>
        /// <param name="upperBound">The variable's upper bound.</param>
        /// <param name="name">The variable's name.</param>
        /// <param name="index1">The first index.</param>
        /// <param name="index2">The second index.</param>
        public void Add(double lowerBound, double upperBound, string name, T1 index1, T2 index2)
        { this[index1, index2] = new Variable(_solver, _variableType, lowerBound, upperBound, name); }
        /// <summary>
        /// Creates variables for all indeces.
        /// </summary>
        /// <param name="lowerBound">The lower bound of all variables.</param>
        /// <param name="upperBound">The upper bound of all variables.</param>
        /// <param name="name">The naming function supplying a unique name per variable.</param>
        /// <param name="indeces1">The first indices to create variables for.</param>
        /// <param name="indeces2">The second indices to create variables for.</param>
        public void AddRange(double lowerBound, double upperBound, Func<T1, T2, string> name, IEnumerable<T1> indeces1, IEnumerable<T2> indeces2)
        {
            foreach (var index1 in indeces1)
                foreach (var index2 in indeces2)
                    Add(lowerBound, upperBound, name(index1, index2), index1, index2);
        }
        /// <summary>
        /// The variable at the specified index.
        /// </summary>
        /// <param name="index1">The first index.</param>
        /// <param name="index2">The second index.</param>
        /// <returns>The variable.</returns>
        public Variable this[T1 index1, T2 index2]
        {
            get
            {
                // Create the variable on-the-fly, if it does not exist
                if (!ContainsVariableAtIndex(index1, index2))
                {
                    Add(LB, UB, _namingFunction(index1, index2), index1, index2);
                    _solver.Update();
                }
                // Return it
                return GetVariableByIndex(index1, index2);
            }
            set { SetVariableByIndex(value, index1, index2); }
        }
    }
    /// <summary>
    /// A collection of variables.
    /// </summary>
    /// <typeparam name="T1">Type of the first index.</typeparam>
    /// <typeparam name="T2">Type of the second index.</typeparam>
    /// <typeparam name="T3">Type of the third index.</typeparam>
    public class VariableCollection<T1, T2, T3> : VariableCollection
    {
        /// <summary>
        /// Function to name variables created on-the-fly.
        /// </summary>
        private Func<T1, T2, T3, string> _namingFunction;
        /// <summary>
        /// Creates a new collection of variables.
        /// </summary>
        /// <param name="solver">The solver the variable collection belongs to.</param>
        /// <param name="variableType">The type of the variables in the collection.</param>
        public VariableCollection(LinearModel solver, VariableType variableType, double lowerBound, double upperBound, Func<T1, T2, T3, string> namingFunction) : base(solver, variableType, lowerBound, upperBound) { _namingFunction = namingFunction; }
        /// <summary>
        /// Creates a variable at the given index.
        /// </summary>
        /// <param name="lowerBound">The variable's lower bound.</param>
        /// <param name="upperBound">The variable's upper bound.</param>
        /// <param name="name">The variable's name.</param>
        /// <param name="index1">The first index.</param>
        /// <param name="index2">The second index.</param>
        /// <param name="index3">The third index.</param>
        public void Add(double lowerBound, double upperBound, string name, T1 index1, T2 index2, T3 index3)
        { this[index1, index2, index3] = new Variable(_solver, _variableType, lowerBound, upperBound, name); }
        /// <summary>
        /// Creates variables for all indeces.
        /// </summary>
        /// <param name="lowerBound">The lower bound of all variables.</param>
        /// <param name="upperBound">The upper bound of all variables.</param>
        /// <param name="name">The naming function supplying a unique name per variable.</param>
        /// <param name="indeces1">The first indices to create variables for.</param>
        /// <param name="indeces2">The second indices to create variables for.</param>
        /// <param name="indeces3">The third indices to create variables for.</param>
        public void AddRange(double lowerBound, double upperBound, Func<T1, T2, T3, string> name, IEnumerable<T1> indeces1, IEnumerable<T2> indeces2, IEnumerable<T3> indeces3)
        {
            foreach (var index1 in indeces1)
                foreach (var index2 in indeces2)
                    foreach (var index3 in indeces3)
                        Add(lowerBound, upperBound, name(index1, index2, index3), index1, index2, index3);
        }
        /// <summary>
        /// The variable at the specified index.
        /// </summary>
        /// <param name="index1">The first index.</param>
        /// <param name="index2">The second index.</param>
        /// <param name="index3">The third index.</param>
        /// <returns>The variable.</returns>
        public Variable this[T1 index1, T2 index2, T3 index3]
        {
            get
            {
                // Create the variable on-the-fly, if it does not exist
                if (!ContainsVariableAtIndex(index1, index2, index3))
                {
                    Add(LB, UB, _namingFunction(index1, index2, index3), index1, index2, index3);
                    _solver.Update();
                }
                // Return it
                return GetVariableByIndex(index1, index2, index3);
            }
            set { SetVariableByIndex(value, index1, index2, index3); }
        }
    }
    /// <summary>
    /// A collection of variables.
    /// </summary>
    /// <typeparam name="T1">Type of the first index.</typeparam>
    /// <typeparam name="T2">Type of the second index.</typeparam>
    /// <typeparam name="T3">Type of the third index.</typeparam>
    /// <typeparam name="T4">Type of the fourth index.</typeparam>
    public class VariableCollection<T1, T2, T3, T4> : VariableCollection
    {
        /// <summary>
        /// Function to name variables created on-the-fly.
        /// </summary>
        private Func<T1, T2, T3, T4, string> _namingFunction;
        /// <summary>
        /// Creates a new collection of variables.
        /// </summary>
        /// <param name="solver">The solver the variable collection belongs to.</param>
        /// <param name="variableType">The type of the variables in the collection.</param>
        public VariableCollection(LinearModel solver, VariableType variableType, double lowerBound, double upperBound, Func<T1, T2, T3, T4, string> namingFunction) : base(solver, variableType, lowerBound, upperBound) { _namingFunction = namingFunction; }
        /// <summary>
        /// Creates a variable at the given index.
        /// </summary>
        /// <param name="lowerBound">The variable's lower bound.</param>
        /// <param name="upperBound">The variable's upper bound.</param>
        /// <param name="name">The variable's name.</param>
        /// <param name="index1">The first index.</param>
        /// <param name="index2">The second index.</param>
        /// <param name="index3">The third index.</param>
        /// <param name="index4">The fourth index.</param>
        public void Add(double lowerBound, double upperBound, string name, T1 index1, T2 index2, T3 index3, T4 index4)
        { this[index1, index2, index3, index4] = new Variable(_solver, _variableType, lowerBound, upperBound, name); }
        /// <summary>
        /// Creates variables for all indeces.
        /// </summary>
        /// <param name="lowerBound">The lower bound of all variables.</param>
        /// <param name="upperBound">The upper bound of all variables.</param>
        /// <param name="name">The naming function supplying a unique name per variable.</param>
        /// <param name="indeces1">The first indices to create variables for.</param>
        /// <param name="indeces2">The second indices to create variables for.</param>
        /// <param name="indeces3">The third indices to create variables for.</param>
        /// <param name="indeces4">The fourth indices to create variables for.</param>
        public void AddRange(double lowerBound, double upperBound, Func<T1, T2, T3, T4, string> name, IEnumerable<T1> indeces1, IEnumerable<T2> indeces2, IEnumerable<T3> indeces3, IEnumerable<T4> indeces4)
        {
            foreach (var index1 in indeces1)
                foreach (var index2 in indeces2)
                    foreach (var index3 in indeces3)
                        foreach (var index4 in indeces4)
                            Add(lowerBound, upperBound, name(index1, index2, index3, index4), index1, index2, index3, index4);
        }
        /// <summary>
        /// The variable at the specified index.
        /// </summary>
        /// <param name="index1">The first index.</param>
        /// <param name="index2">The second index.</param>
        /// <param name="index3">The third index.</param>
        /// <param name="index4">The fourth index.</param>
        /// <returns>The variable.</returns>
        public Variable this[T1 index1, T2 index2, T3 index3, T4 index4]
        {
            get
            {
                // Create the variable on-the-fly, if it does not exist
                if (!ContainsVariableAtIndex(index1, index2, index3, index4))
                {
                    Add(LB, UB, _namingFunction(index1, index2, index3, index4), index1, index2, index3, index4);
                    _solver.Update();
                }
                // Return it
                return GetVariableByIndex(index1, index2, index3, index4);
            }
            set { SetVariableByIndex(value, index1, index2, index3, index4); }
        }
    }
    /// <summary>
    /// A collection of variables.
    /// </summary>
    /// <typeparam name="T1">Type of the first index.</typeparam>
    /// <typeparam name="T2">Type of the second index.</typeparam>
    /// <typeparam name="T3">Type of the third index.</typeparam>
    /// <typeparam name="T4">Type of the fourth index.</typeparam>
    /// <typeparam name="T5">Type of the fifth index.</typeparam>
    public class VariableCollection<T1, T2, T3, T4, T5> : VariableCollection
    {
        /// <summary>
        /// Function to name variables created on-the-fly.
        /// </summary>
        private Func<T1, T2, T3, T4, T5, string> _namingFunction;
        /// <summary>
        /// Creates a new collection of variables.
        /// </summary>
        /// <param name="solver">The solver the variable collection belongs to.</param>
        /// <param name="variableType">The type of the variables in the collection.</param>
        public VariableCollection(LinearModel solver, VariableType variableType, double lowerBound, double upperBound, Func<T1, T2, T3, T4, T5, string> namingFunction) : base(solver, variableType, lowerBound, upperBound) { _namingFunction = namingFunction; }
        /// <summary>
        /// Creates a variable at the given index.
        /// </summary>
        /// <param name="lowerBound">The variable's lower bound.</param>
        /// <param name="upperBound">The variable's upper bound.</param>
        /// <param name="name">The variable's name.</param>
        /// <param name="index1">The first index.</param>
        /// <param name="index2">The second index.</param>
        /// <param name="index3">The third index.</param>
        /// <param name="index4">The fourth index.</param>
        /// <param name="index5">The fifth index.</param>
        public void Add(double lowerBound, double upperBound, string name, T1 index1, T2 index2, T3 index3, T4 index4, T5 index5)
        { this[index1, index2, index3, index4, index5] = new Variable(_solver, _variableType, lowerBound, upperBound, name); }
        /// <summary>
        /// Creates variables for all indeces.
        /// </summary>
        /// <param name="lowerBound">The lower bound of all variables.</param>
        /// <param name="upperBound">The upper bound of all variables.</param>
        /// <param name="name">The naming function supplying a unique name per variable.</param>
        /// <param name="indeces1">The first indices to create variables for.</param>
        /// <param name="indeces2">The second indices to create variables for.</param>
        /// <param name="indeces3">The third indices to create variables for.</param>
        /// <param name="indeces4">The fourth indices to create variables for.</param>
        /// <param name="indeces5">The fifth indices to create variables for.</param>
        public void AddRange(double lowerBound, double upperBound, Func<T1, T2, T3, T4, T5, string> name, IEnumerable<T1> indeces1, IEnumerable<T2> indeces2, IEnumerable<T3> indeces3, IEnumerable<T4> indeces4, IEnumerable<T5> indeces5)
        {
            foreach (var index1 in indeces1)
                foreach (var index2 in indeces2)
                    foreach (var index3 in indeces3)
                        foreach (var index4 in indeces4)
                            foreach (var index5 in indeces5)
                                Add(lowerBound, upperBound, name(index1, index2, index3, index4, index5), index1, index2, index3, index4, index5);
        }
        /// <summary>
        /// The variable at the specified index.
        /// </summary>
        /// <param name="index1">The first index.</param>
        /// <param name="index2">The second index.</param>
        /// <param name="index3">The third index.</param>
        /// <param name="index4">The fourth index.</param>
        /// <param name="index5">The fifth index.</param>
        /// <returns>The variable.</returns>
        public Variable this[T1 index1, T2 index2, T3 index3, T4 index4, T5 index5]
        {
            get
            {
                // Create the variable on-the-fly, if it does not exist
                if (!ContainsVariableAtIndex(index1, index2, index3, index4, index5))
                {
                    Add(LB, UB, _namingFunction(index1, index2, index3, index4, index5), index1, index2, index3, index4, index5);
                    _solver.Update();
                }
                // Return it
                return GetVariableByIndex(index1, index2, index3, index4, index5);
            }
            set { SetVariableByIndex(value, index1, index2, index3, index4, index5); }
        }
    }
    /// <summary>
    /// A collection of variables.
    /// </summary>
    /// <typeparam name="T1">Type of the first index.</typeparam>
    /// <typeparam name="T2">Type of the second index.</typeparam>
    /// <typeparam name="T3">Type of the third index.</typeparam>
    /// <typeparam name="T4">Type of the fourth index.</typeparam>
    /// <typeparam name="T5">Type of the fifth index.</typeparam>
    /// <typeparam name="T6">Type of the sixth index.</typeparam>
    public class VariableCollection<T1, T2, T3, T4, T5, T6> : VariableCollection
    {
        /// <summary>
        /// Function to name variables created on-the-fly.
        /// </summary>
        private Func<T1, T2, T3, T4, T5, T6, string> _namingFunction;
        /// <summary>
        /// Creates a new collection of variables.
        /// </summary>
        /// <param name="solver">The solver the variable collection belongs to.</param>
        /// <param name="variableType">The type of the variables in the collection.</param>
        public VariableCollection(LinearModel solver, VariableType variableType, double lowerBound, double upperBound, Func<T1, T2, T3, T4, T5, T6, string> namingFunction) : base(solver, variableType, lowerBound, upperBound) { _namingFunction = namingFunction; }
        /// <summary>
        /// Creates a variable at the given index.
        /// </summary>
        /// <param name="lowerBound">The variable's lower bound.</param>
        /// <param name="upperBound">The variable's upper bound.</param>
        /// <param name="name">The variable's name.</param>
        /// <param name="index1">The first index.</param>
        /// <param name="index2">The second index.</param>
        /// <param name="index3">The third index.</param>
        /// <param name="index4">The fourth index.</param>
        /// <param name="index5">The fifth index.</param>
        /// <param name="index6">The sixth index.</param>
        public void Add(double lowerBound, double upperBound, string name, T1 index1, T2 index2, T3 index3, T4 index4, T5 index5, T6 index6)
        { this[index1, index2, index3, index4, index5, index6] = new Variable(_solver, _variableType, lowerBound, upperBound, name); }
        /// <summary>
        /// Creates variables for all indeces.
        /// </summary>
        /// <param name="lowerBound">The lower bound of all variables.</param>
        /// <param name="upperBound">The upper bound of all variables.</param>
        /// <param name="name">The naming function supplying a unique name per variable.</param>
        /// <param name="indeces1">The first indices to create variables for.</param>
        /// <param name="indeces2">The second indices to create variables for.</param>
        /// <param name="indeces3">The third indices to create variables for.</param>
        /// <param name="indeces4">The fourth indices to create variables for.</param>
        /// <param name="indeces5">The fifth indices to create variables for.</param>
        /// <param name="indeces6">The sixth indices to create variables for.</param>
        public void AddRange(double lowerBound, double upperBound, Func<T1, T2, T3, T4, T5, T6, string> name, IEnumerable<T1> indeces1, IEnumerable<T2> indeces2, IEnumerable<T3> indeces3, IEnumerable<T4> indeces4, IEnumerable<T5> indeces5, IEnumerable<T6> indeces6)
        {
            foreach (var index1 in indeces1)
                foreach (var index2 in indeces2)
                    foreach (var index3 in indeces3)
                        foreach (var index4 in indeces4)
                            foreach (var index5 in indeces5)
                                foreach (var index6 in indeces6)
                                    Add(lowerBound, upperBound, name(index1, index2, index3, index4, index5, index6), index1, index2, index3, index4, index5, index6);
        }
        /// <summary>
        /// The variable at the specified index.
        /// </summary>
        /// <param name="index1">The first index.</param>
        /// <param name="index2">The second index.</param>
        /// <param name="index3">The third index.</param>
        /// <param name="index4">The fourth index.</param>
        /// <param name="index5">The fifth index.</param>
        /// <param name="index6">The sixth index.</param>
        /// <returns>The variable.</returns>
        public Variable this[T1 index1, T2 index2, T3 index3, T4 index4, T5 index5, T6 index6]
        {
            get
            {
                // Create the variable on-the-fly, if it does not exist
                if (!ContainsVariableAtIndex(index1, index2, index3, index4, index5, index6))
                {
                    Add(LB, UB, _namingFunction(index1, index2, index3, index4, index5, index6), index1, index2, index3, index4, index5, index6);
                    _solver.Update();
                }
                // Return it
                return GetVariableByIndex(index1, index2, index3, index4, index5, index6);
            }
            set { SetVariableByIndex(value, index1, index2, index3, index4, index5, index6); }
        }
    }
    /// <summary>
    /// A collection of variables.
    /// </summary>
    /// <typeparam name="T1">Type of the first index.</typeparam>
    /// <typeparam name="T2">Type of the second index.</typeparam>
    /// <typeparam name="T3">Type of the third index.</typeparam>
    /// <typeparam name="T4">Type of the fourth index.</typeparam>
    /// <typeparam name="T5">Type of the fifth index.</typeparam>
    /// <typeparam name="T6">Type of the sixth index.</typeparam>
    /// <typeparam name="T7">Type of the seventh index.</typeparam>
    public class VariableCollection<T1, T2, T3, T4, T5, T6, T7> : VariableCollection
    {
        /// <summary>
        /// Function to name variables created on-the-fly.
        /// </summary>
        private Func<T1, T2, T3, T4, T5, T6, T7, string> _namingFunction;
        /// <summary>
        /// Creates a new collection of variables.
        /// </summary>
        /// <param name="solver">The solver the variable collection belongs to.</param>
        /// <param name="variableType">The type of the variables in the collection.</param>
        public VariableCollection(LinearModel solver, VariableType variableType, double lowerBound, double upperBound, Func<T1, T2, T3, T4, T5, T6, T7, string> namingFunction) : base(solver, variableType, lowerBound, upperBound) { _namingFunction = namingFunction; }
        /// <summary>
        /// Creates a variable at the given index.
        /// </summary>
        /// <param name="lowerBound">The variable's lower bound.</param>
        /// <param name="upperBound">The variable's upper bound.</param>
        /// <param name="name">The variable's name.</param>
        /// <param name="index1">The first index.</param>
        /// <param name="index2">The second index.</param>
        /// <param name="index3">The third index.</param>
        /// <param name="index4">The fourth index.</param>
        /// <param name="index5">The fifth index.</param>
        /// <param name="index6">The sixth index.</param>
        /// <param name="index7">The seventh index.</param>
        public void Add(double lowerBound, double upperBound, string name, T1 index1, T2 index2, T3 index3, T4 index4, T5 index5, T6 index6, T7 index7)
        { this[index1, index2, index3, index4, index5, index6, index7] = new Variable(_solver, _variableType, lowerBound, upperBound, name); }
        /// <summary>
        /// Creates variables for all indeces.
        /// </summary>
        /// <param name="lowerBound">The lower bound of all variables.</param>
        /// <param name="upperBound">The upper bound of all variables.</param>
        /// <param name="name">The naming function supplying a unique name per variable.</param>
        /// <param name="indeces1">The first indices to create variables for.</param>
        /// <param name="indeces2">The second indices to create variables for.</param>
        /// <param name="indeces3">The third indices to create variables for.</param>
        /// <param name="indeces4">The fourth indices to create variables for.</param>
        /// <param name="indeces5">The fifth indices to create variables for.</param>
        /// <param name="indeces6">The sixth indices to create variables for.</param>
        /// <param name="indeces7">The seventh indices to create variables for.</param>
        public void AddRange(double lowerBound, double upperBound, Func<T1, T2, T3, T4, T5, T6, T7, string> name, IEnumerable<T1> indeces1, IEnumerable<T2> indeces2, IEnumerable<T3> indeces3, IEnumerable<T4> indeces4, IEnumerable<T5> indeces5, IEnumerable<T6> indeces6, IEnumerable<T7> indeces7)
        {
            foreach (var index1 in indeces1)
                foreach (var index2 in indeces2)
                    foreach (var index3 in indeces3)
                        foreach (var index4 in indeces4)
                            foreach (var index5 in indeces5)
                                foreach (var index6 in indeces6)
                                    foreach (var index7 in indeces7)
                                        Add(lowerBound, upperBound, name(index1, index2, index3, index4, index5, index6, index7), index1, index2, index3, index4, index5, index6, index7);
        }
        /// <summary>
        /// The variable at the specified index.
        /// </summary>
        /// <param name="index1">The first index.</param>
        /// <param name="index2">The second index.</param>
        /// <param name="index3">The third index.</param>
        /// <param name="index4">The fourth index.</param>
        /// <param name="index5">The fifth index.</param>
        /// <param name="index6">The sixth index.</param>
        /// <param name="index7">The seventh index.</param>
        /// <returns>The variable.</returns>
        public Variable this[T1 index1, T2 index2, T3 index3, T4 index4, T5 index5, T6 index6, T7 index7]
        {
            get
            {
                // Create the variable on-the-fly, if it does not exist
                if (!ContainsVariableAtIndex(index1, index2, index3, index4, index5, index6, index7))
                {
                    Add(LB, UB, _namingFunction(index1, index2, index3, index4, index5, index6, index7), index1, index2, index3, index4, index5, index6, index7);
                    _solver.Update();
                }
                // Return it
                return GetVariableByIndex(index1, index2, index3, index4, index5, index6, index7);
            }
            set { SetVariableByIndex(value, index1, index2, index3, index4, index5, index6, index7); }
        }
    }
}
