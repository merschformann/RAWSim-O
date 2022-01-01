using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace RAWSimO.Toolbox
{
    /// <summary>
    /// Exposes some useful extensions for working with enumerables.
    /// </summary>
    public static class EnumerableExtensions
    {
        #region HashSet

        /// <summary>
        /// Substracts an enumerable from a hash-set. This is identical to <see cref="HashSet{T}.ExceptWith"/>
        /// except that a new hash-set is returned and the first hash-set isn't modified in place.
        /// </summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <param name="set">The hash-set to subtract from.</param>
        /// <param name="other">The enumerable to subtract.</param>
        /// <returns>A new subtracted hash-set.</returns>
        public static HashSet<T> ExceptWithNew<T>(this HashSet<T> set, IEnumerable<T> other)
        {
            var clone = set.ToHashSet();
            clone.ExceptWith(other);
            return clone;
        }

        /// <summary>
        /// Creates a union of a hash-set and an enumerable. This is identical to <see cref="HashSet{T}.UnionWith"/>
        /// except that a new hash-set is returned and the first hash-set isn't modified in place.
        /// </summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <param name="set">The first hash-set of the union.</param>
        /// <param name="other">The second hash-set of the union.</param>
        /// <returns>A new hash-set that is the union of the two sets.</returns>
        public static HashSet<T> UnionWithNew<T>(this HashSet<T> set, IEnumerable<T> other)
        {
            var clone = set.ToHashSet();
            clone.UnionWith(other);
            return clone;
        }

        /// <summary>
        /// Creates an intersection of a hash-set and an enumerable. This is identical to <see cref="HashSet{T}.IntersectWith"/>
        /// except that a new hash-set is returned and the first hash-set isn't modified in place.
        /// </summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <param name="set">The first hash-set of the intersection.</param>
        /// <param name="other">The second hash-set of the intersection.</param>
        /// <returns>A new hash-set that is the intersection of the two sets.</returns>
        public static HashSet<T> IntersectWithNew<T>(this HashSet<T> set, IEnumerable<T> other)
        {
            var clone = set.ToHashSet();
            clone.IntersectWith(other);
            return clone;
        }

        #endregion

        #region Find rank

        /// <summary>
        /// Obtains the sorting rank of the given element without sorting.
        /// </summary>
        /// <typeparam name="T">The type of the element.</typeparam>
        /// <param name="source">The collection for which the order index of the element shall be obtained (the element does not have to be part of the collection).</param>
        /// <param name="element">The element to obtain the order index for.</param>
        /// <returns>The index of the element within the given collection.</returns>
        public static int RankOf<T>(this IEnumerable<T> source, T element) where T : IComparable
        {
            // Sanity check a bit
            if (ReferenceEquals(null, source))
                throw new ArgumentNullException("source");
            if (ReferenceEquals(null, element))
                throw new ArgumentNullException("element");
            // Get index for the element
            int smaller = 0;
            foreach (var e in source)
                if (element.CompareTo(e) > 0)
                    smaller++;
            return smaller;
        }
        /// <summary>
        /// Obtains the reverse sorting rank of the given element without sorting.
        /// </summary>
        /// <typeparam name="T">The type of the element.</typeparam>
        /// <param name="source">The collection for which the order index of the element shall be obtained (the element does not have to be part of the collection).</param>
        /// <param name="element">The element to obtain the order index for.</param>
        /// <returns>The index of the element within the given collection, if it was sorted descendingly.</returns>
        public static int RankOfDescending<T>(this IEnumerable<T> source, T element) where T : IComparable
        {
            // Sanity check a bit
            if (ReferenceEquals(null, source))
                throw new ArgumentNullException("source");
            if (ReferenceEquals(null, element))
                throw new ArgumentNullException("element");
            // Get index for the element
            int greater = 0;
            foreach (var e in source)
                if (element.CompareTo(e) < 0)
                    greater++;
            return greater;
        }
        /// <summary>
        /// Obtains the sorting rank of the given element without sorting.
        /// </summary>
        /// <typeparam name="T">The type of the element.</typeparam>
        /// <param name="source">The collection for which the order index of the element shall be obtained (the element does not have to be part of the collection).</param>
        /// <param name="element">The element to obtain the order index for.</param>
        /// <param name="elementSortValue">Obtains the value to sort the elements by.</param>
        /// <returns>The index of the element within the given collection.</returns>
        public static int RankOf<T>(this IEnumerable<T> source, T element, Func<T, double> elementSortValue)
        {
            // Sanity check a bit
            if (ReferenceEquals(null, source))
                throw new ArgumentNullException("source");
            if (ReferenceEquals(null, element))
                throw new ArgumentNullException("element");
            // Get index for the element
            int smaller = 0;
            double elementValue = elementSortValue(element);
            foreach (var e in source)
                if (elementSortValue(e) < elementValue)
                    smaller++;
            return smaller;
        }
        /// <summary>
        /// Obtains the reverse sorting rank of the given element without sorting.
        /// </summary>
        /// <typeparam name="T">The type of the element.</typeparam>
        /// <param name="source">The collection for which the order index of the element shall be obtained (the element does not have to be part of the collection).</param>
        /// <param name="element">The element to obtain the order index for.</param>
        /// <param name="elementSortValue">Obtains the value to sort the elements by.</param>
        /// <returns>The index of the element within the given collection, if it was sorted descendingly.</returns>
        public static int RankOfDescending<T>(this IEnumerable<T> source, T element, Func<T, double> elementSortValue)
        {
            // Sanity check a bit
            if (ReferenceEquals(null, source))
                throw new ArgumentNullException("source");
            if (ReferenceEquals(null, element))
                throw new ArgumentNullException("element");
            // Get index for the element
            int greater = 0;
            double elementValue = elementSortValue(element);
            foreach (var e in source)
                if (elementSortValue(e) > elementValue)
                    greater++;
            return greater;
        }

        #endregion

        #region AlphaNumericalSorting

        /// <summary>
        /// Orders the given sequence alpha-numerically. Many thanks to Matthew Horsley (see <see cref="http://stackoverflow.com/questions/248603/natural-sort-order-in-c-sharp"/>)
        /// </summary>
        /// <typeparam name="T">The type of the sequence.</typeparam>
        /// <param name="source">The sequence to order.</param>
        /// <param name="selector">The function projecting each element into a string.</param>
        /// <returns>The sequence ordered alpha-numerically.</returns>
        public static IEnumerable<T> OrderByAlphaNumeric<T>(this IEnumerable<T> source, Func<T, string> selector)
        {
            int max = source
                .SelectMany(i => Regex.Matches(selector(i), @"\d+").Cast<Match>().Select(m => (int?)m.Value.Length))
                .Max() ?? 0;
            return source.OrderBy(i => Regex.Replace(selector(i), @"\d+", m => m.Value.PadLeft(max, '0')));
        }

        #endregion

        #region Arg max / min

        /// <summary>
        /// Finds the element of an enumeration for which the selector returned the maximum value.
        /// </summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <param name="source">The enumeration to search.</param>
        /// <param name="selector">The function determining the value for the corresponding element.</param>
        /// <returns>The element with the maximal value.</returns>
        public static T ArgMax<T>(this IEnumerable<T> source, Func<T, double> selector)
        {
            // Sanity check a bit
            if (ReferenceEquals(null, source))
                throw new ArgumentNullException("source");
            if (ReferenceEquals(null, selector))
                throw new ArgumentNullException("selector");
            // Prepare values
            T maxValue = default(T);
            double max = double.NegativeInfinity;
            bool assigned = false;
            // Search for the maximum
            foreach (T item in source)
            {
                // Obtain value for the current element
                double v = selector(item);
                // Check for new maximum
                if ((max < v) || (!assigned))
                {
                    assigned = true;
                    max = v;
                    maxValue = item;
                }
            }
            // Return the maximum argument
            return maxValue;
        }
        /// <summary>
        /// Finds the element of an enumeration for which the selector returned the maximum value and the value itself.
        /// </summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <param name="source">The enumeration to search.</param>
        /// <param name="selector">The function determining the value for the corresponding element.</param>
        /// <returns>The element with the maximal value and the maximal value.</returns>
        public static KeyValuePair<T, double> ArgValueMax<T>(this IEnumerable<T> source, Func<T, double> selector)
        {
            // Sanity check a bit
            if (ReferenceEquals(null, source))
                throw new ArgumentNullException("source");
            if (ReferenceEquals(null, selector))
                throw new ArgumentNullException("selector");
            // Prepare values
            T maxValue = default(T);
            double max = double.NegativeInfinity;
            bool assigned = false;
            // Search for the maximum
            foreach (T item in source)
            {
                // Obtain value for the current element
                double v = selector(item);
                // Check for new maximum
                if ((max < v) || (!assigned))
                {
                    assigned = true;
                    max = v;
                    maxValue = item;
                }
            }
            // Return the maximum argument
            return new KeyValuePair<T, double>(maxValue, max);
        }
        /// <summary>
        /// Finds the element of an enumeration for which the selector returned the minimum value.
        /// </summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <param name="source">The enumeration to search.</param>
        /// <param name="selector">The function determining the value for the corresponding element.</param>
        /// <returns>The element with the minimal value.</returns>
        public static T ArgMin<T>(this IEnumerable<T> source, Func<T, double> selector)
        {
            // Sanity check a bit
            if (ReferenceEquals(null, source))
                throw new ArgumentNullException("source");
            if (ReferenceEquals(null, selector))
                throw new ArgumentNullException("selector");
            // Prepare values
            T minValue = default(T);
            double min = double.PositiveInfinity;
            bool assigned = false;
            // Search for the minimum
            foreach (T item in source)
            {
                // Obtain value for the current element
                double v = selector(item);
                // Check for new minimum
                if ((min > v) || (!assigned))
                {
                    assigned = true;
                    min = v;
                    minValue = item;
                }
            }
            // Return the maximum argument
            return minValue;
        }
        /// <summary>
        /// Finds the element of an enumeration for which the selector returned the minimum value and the value itself.
        /// </summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <param name="source">The enumeration to search.</param>
        /// <param name="selector">The function determining the value for the corresponding element.</param>
        /// <returns>The element with the minimal value and the minimal value.</returns>
        public static KeyValuePair<T, double> ArgValueMin<T>(this IEnumerable<T> source, Func<T, double> selector)
        {
            // Sanity check a bit
            if (ReferenceEquals(null, source))
                throw new ArgumentNullException("source");
            if (ReferenceEquals(null, selector))
                throw new ArgumentNullException("selector");
            // Prepare values
            T minValue = default(T);
            double min = double.PositiveInfinity;
            bool assigned = false;
            // Search for the minimum
            foreach (T item in source)
            {
                // Obtain value for the current element
                double v = selector(item);
                // Check for new minimum
                if ((min > v) || (!assigned))
                {
                    assigned = true;
                    min = v;
                    minValue = item;
                }
            }
            // Return the maximum argument
            return new KeyValuePair<T, double>(minValue, min);
        }
        /// <summary>
        /// Finds the element of an enumeration for which the selector returned the maximum value.
        /// </summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <param name="source">The enumeration to search.</param>
        /// <param name="selector">The function determining the value for the corresponding element.</param>
        /// <returns>The element with the maximal value.</returns>
        public static T ArgMax<T>(this IEnumerable<T> source, Func<T, int> selector)
        {
            // Sanity check a bit
            if (ReferenceEquals(null, source))
                throw new ArgumentNullException("source");
            if (ReferenceEquals(null, selector))
                throw new ArgumentNullException("selector");
            // Prepare values
            T maxValue = default(T);
            int max = int.MinValue;
            bool assigned = false;
            // Search for the maximum
            foreach (T item in source)
            {
                // Obtain value for the current element
                int v = selector(item);
                // Check for new maximum
                if ((max < v) || (!assigned))
                {
                    assigned = true;
                    max = v;
                    maxValue = item;
                }
            }
            // Return the maximum argument
            return maxValue;
        }
        /// <summary>
        /// Finds the element of an enumeration for which the selector returned the minimum value.
        /// </summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <param name="source">The enumeration to search.</param>
        /// <param name="selector">The function determining the value for the corresponding element.</param>
        /// <returns>The element with the minimal value.</returns>
        public static T ArgMin<T>(this IEnumerable<T> source, Func<T, int> selector)
        {
            // Sanity check a bit
            if (ReferenceEquals(null, source))
                throw new ArgumentNullException("source");
            if (ReferenceEquals(null, selector))
                throw new ArgumentNullException("selector");
            // Prepare values
            T minValue = default(T);
            int min = int.MaxValue;
            bool assigned = false;
            // Search for the minimum
            foreach (T item in source)
            {
                // Obtain value for the current element
                int v = selector(item);
                // Check for new minimum
                if ((min > v) || (!assigned))
                {
                    assigned = true;
                    min = v;
                    minValue = item;
                }
            }
            // Return the maximum argument
            return minValue;
        }
        /// <summary>
        /// Finds the element of an enumeration for which the selector returned the maximum value.
        /// </summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <param name="source">The enumeration to search.</param>
        /// <param name="selector">The function determining the value for the corresponding element.</param>
        /// <param name="filter">A filter that only allows elements that return <code>true</code> to be considered.</param>
        /// <returns>The element with the maximal value.</returns>
        public static T ArgMax<T>(this IEnumerable<T> source, Func<T, double> selector, Func<T, bool> filter)
        {
            // Sanity check a bit
            if (ReferenceEquals(null, source))
                throw new ArgumentNullException("source");
            if (ReferenceEquals(null, selector))
                throw new ArgumentNullException("selector");
            // Prepare values
            T maxValue = default(T);
            double max = double.NegativeInfinity;
            bool assigned = false;
            // Search for the maximum
            foreach (T item in source.Where(e => filter(e)))
            {
                // Obtain value for the current element
                double v = selector(item);
                // Check for new maximum
                if ((max < v) || (!assigned))
                {
                    assigned = true;
                    max = v;
                    maxValue = item;
                }
            }
            // Return the maximum argument
            return maxValue;
        }
        /// <summary>
        /// Finds the element of an enumeration for which the selector returned the minimum value.
        /// </summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <param name="source">The enumeration to search.</param>
        /// <param name="selector">The function determining the value for the corresponding element.</param>
        /// <param name="filter">A filter that only allows elements that return <code>true</code> to be considered.</param>
        /// <returns>The element with the minimal value.</returns>
        public static T ArgMin<T>(this IEnumerable<T> source, Func<T, double> selector, Func<T, bool> filter)
        {
            // Sanity check a bit
            if (ReferenceEquals(null, source))
                throw new ArgumentNullException("source");
            if (ReferenceEquals(null, selector))
                throw new ArgumentNullException("selector");
            // Prepare values
            T minValue = default(T);
            double min = double.PositiveInfinity;
            bool assigned = false;
            // Search for the minimum
            foreach (T item in source.Where(e => filter(e)))
            {
                // Obtain value for the current element
                double v = selector(item);
                // Check for new minimum
                if ((min > v) || (!assigned))
                {
                    assigned = true;
                    min = v;
                    minValue = item;
                }
            }
            // Return the maximum argument
            return minValue;
        }
        /// <summary>
        /// Finds the element of an enumeration for which the selector returned the maximum value.
        /// </summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <param name="source">The enumeration to search.</param>
        /// <param name="selector">The function determining the value for the corresponding element.</param>
        /// <param name="filter">A filter that only allows elements that return <code>true</code> to be considered.</param>
        /// <returns>The element with the maximal value.</returns>
        public static T ArgMax<T>(this IEnumerable<T> source, Func<T, int> selector, Func<T, bool> filter)
        {
            // Sanity check a bit
            if (ReferenceEquals(null, source))
                throw new ArgumentNullException("source");
            if (ReferenceEquals(null, selector))
                throw new ArgumentNullException("selector");
            // Prepare values
            T maxValue = default(T);
            int max = int.MinValue;
            bool assigned = false;
            // Search for the maximum
            foreach (T item in source.Where(e => filter(e)))
            {
                // Obtain value for the current element
                int v = selector(item);
                // Check for new maximum
                if ((max < v) || (!assigned))
                {
                    assigned = true;
                    max = v;
                    maxValue = item;
                }
            }
            // Return the maximum argument
            return maxValue;
        }
        /// <summary>
        /// Finds the element of an enumeration for which the selector returned the minimum value.
        /// </summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <param name="source">The enumeration to search.</param>
        /// <param name="selector">The function determining the value for the corresponding element.</param>
        /// <param name="filter">A filter that only allows elements that return <code>true</code> to be considered.</param>
        /// <returns>The element with the minimal value.</returns>
        public static T ArgMin<T>(this IEnumerable<T> source, Func<T, int> selector, Func<T, bool> filter)
        {
            // Sanity check a bit
            if (ReferenceEquals(null, source))
                throw new ArgumentNullException("source");
            if (ReferenceEquals(null, selector))
                throw new ArgumentNullException("selector");
            // Prepare values
            T minValue = default(T);
            int min = int.MaxValue;
            bool assigned = false;
            // Search for the minimum
            foreach (T item in source.Where(e => filter(e)))
            {
                // Obtain value for the current element
                int v = selector(item);
                // Check for new minimum
                if ((min > v) || (!assigned))
                {
                    assigned = true;
                    min = v;
                    minValue = item;
                }
            }
            // Return the maximum argument
            return minValue;
        }

        #endregion

        #region Arg max / min multi

        /// <summary>
        /// Finds the element of an enumeration for which the selector returned the maximum value.
        /// </summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <param name="source">The enumeration to search.</param>
        /// <param name="scorers">The functions scoring the respective elements. The first one is the main score. All subsequent ones are used as tie-breakers. The respective levels of tie-breaking are given by the order, i.e. the last one is used as the last tie-breaker, if everything else was equal. All scoring functions maximize.</param>
        /// <returns>The best element in the enumeration.</returns>
        public static T ArgMax<T>(this IEnumerable<T> source, params Func<T, double>[] scorers)
        {
            // Sanity check a bit
            if (ReferenceEquals(null, source))
                throw new ArgumentNullException("source");
            if (ReferenceEquals(null, scorers))
                throw new ArgumentNullException("selector");
            // Prepare values
            T maxArg = default(T);
            double[] max = Enumerable.Repeat(double.NegativeInfinity, scorers.Length).ToArray();
            double[] value = new double[scorers.Length];
            bool assigned = false;
            // Search for the maximum
            foreach (T item in source)
            {
                bool better = false;
                for (int scorerIndex = 0; scorerIndex < scorers.Length; scorerIndex++)
                {
                    // Obtain value for the current element
                    value[scorerIndex] = scorers[scorerIndex](item);
                    // If we already confirmed a new best one just keep iterating until all best values are updated for the new candidate
                    if (better)
                    {
                        // Update the value for the current scorer index for the new best one
                        max[scorerIndex] = value[scorerIndex];
                        continue;
                    }
                    // Check for new maximum
                    else if ((max[scorerIndex] < value[scorerIndex]) || (!assigned))
                    {
                        // New maximum found - store value for current scorer and the item itself - also indicate that we found a better one
                        assigned = true;
                        max[scorerIndex] = value[scorerIndex];
                        maxArg = item;
                        better = true;
                    }
                    else if (max[scorerIndex] == value[scorerIndex])
                    {
                        // The element is equal regarding this score - we need to go on with the examination of this element using the next tie breaker
                        // Also: no need to update the max - we have equality for this index
                        continue;
                    }
                    else
                    {
                        // The element is not better than the current best
                        break;
                    }
                }
            }
            // Return the maximum argument
            return maxArg;
        }

        #endregion

        #region Min / max with default

        /// <summary>
        /// Finds the minimum value in an enumeration or returns a default value, if the enumeration is empty.
        /// </summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <param name="source">The enumeration to search.</param>
        /// <param name="selector">The function determining the value for the corresponding element.</param>
        /// <param name="defaultValue">The default value to return, if the enumeration is empty.</param>
        /// <returns>The minimum or default value.</returns>
        public static double MinOrDefault<T>(this IEnumerable<T> source, Func<T, double> selector, double defaultValue)
        {
            // Sanity check a bit
            if (ReferenceEquals(null, source))
                throw new ArgumentNullException("source");
            // Prepare values
            double min = double.PositiveInfinity;
            bool assigned = false;
            // Search for the value
            foreach (T item in source)
            {
                // Obtain value for the current element
                double v = selector(item);
                // Check for new best
                if ((min > v) || (!assigned))
                {
                    assigned = true;
                    min = v;
                }
            }
            // Return the result
            return !assigned ? defaultValue : min;
        }

        /// <summary>
        /// Finds the maximum value in an enumeration or returns a default value, if the enumeration is empty.
        /// </summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <param name="source">The enumeration to search.</param>
        /// <param name="selector">The function determining the value for the corresponding element.</param>
        /// <param name="defaultValue">The default value to return, if the enumeration is empty.</param>
        /// <returns>The maximum or default value.</returns>
        public static double MaxOrDefault<T>(this IEnumerable<T> source, Func<T, double> selector, double defaultValue)
        {
            // Sanity check a bit
            if (ReferenceEquals(null, source))
                throw new ArgumentNullException("source");
            // Prepare values
            double max = double.NegativeInfinity;
            bool assigned = false;
            // Search for the value
            foreach (T item in source)
            {
                // Obtain value for the current element
                double v = selector(item);
                // Check for new best
                if ((max < v) || (!assigned))
                {
                    assigned = true;
                    max = v;
                }
            }
            // Return the result
            return !assigned ? defaultValue : max;
        }

        #endregion

        #region Variance

        /// <summary>
        /// Calculates the variance from the mean for the given collection.
        /// </summary>
        /// <param name="source">The enumeration to calculate the variance for.</param>
        /// <returns>The variance from the mean.</returns>
        public static double Variance(this IEnumerable<double> source)
        {
            if (source == null || !source.Any())
                return 0;
            double mean = source.Average();
            int count = 0;
            double value = 0;
            foreach (var element in source)
            {
                value += Math.Pow(element - mean, 2);
                count++;
            }
            return value / count;
        }
        /// <summary>
        /// Calculates the variance from the mean for the given collection.
        /// </summary>
        /// <param name="source">The enumeration to calculate the variance for.</param>
        /// <param name="selector">The function selecting the value to calculate the variance for.</param>
        /// <returns>The variance from the mean.</returns>
        public static double Variance<T>(this IEnumerable<T> source, Func<T, double> selector) { return source.Select(e => selector(e)).Variance(); }
        /// <summary>
        /// Calculates the variance from the mean for the given collection.
        /// </summary>
        /// <param name="source">The enumeration to calculate the variance for.</param>
        /// <returns>The variance from the mean.</returns>
        public static double Variance(this IEnumerable<int> source) { return source.Select(e => (double)e).Variance(); }
        /// <summary>
        /// Calculates the variance from the mean for the given collection.
        /// </summary>
        /// <param name="source">The enumeration to calculate the variance for.</param>
        /// <param name="selector">The function selecting the value to calculate the variance for.</param>
        /// <returns>The variance from the mean.</returns>
        public static double Variance<T>(this IEnumerable<T> source, Func<T, int> selector) { return source.Select(e => (double)selector(e)).Variance(); }

        #endregion

        #region Tuple iteration

        /// <summary>
        /// Applies a the check operation to all 2-tuples in the order of the enumeration.
        /// </summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <param name="source">The enumeration of elements.</param>
        /// <param name="check">The check operation to apply.</param>
        /// <returns><code>true</code> if all 2-tuple in the ordered enumeration are marked as true by the check operation applied, <code>false</code> otherwise.</returns>
        public static bool AllOrderedTuple<T>(this IEnumerable<T> source, Func<T, T, bool> check)
        {
            if (source == null || source.Count() < 2)
                return true;
            T prev = source.First();
            foreach (var element in source.Skip(1))
            {
                if (!check(prev, element))
                    return false;
                prev = element;
            }
            return true;
        }

        /// <summary>
        /// Applies a the check operation to all 2-tuples in the order of the enumeration.
        /// </summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <param name="source">The enumeration of elements.</param>
        /// <param name="check">The check operation to apply.</param>
        /// <returns><code>true</code> if any 2-tuple in the ordered enumeration is marked as true by the check operation applied, <code>false</code> otherwise.</returns>
        public static bool AnyOrderedTuple<T>(this IEnumerable<T> source, Func<T, T, bool> check)
        {
            if (source == null || source.Count() < 2)
                return false;
            T prev = source.First();
            foreach (var element in source.Skip(1))
            {
                if (check(prev, element))
                    return true;
                prev = element;
            }
            return true;
        }

        #endregion
    }
}
