using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.Toolbox
{
    /// <summary>
    /// This class exposes some helpers for handling enumerations.
    /// </summary>
    public class EnumerationHelpers
    {
        #region Cross product support

        /*-----------------------------
        The MIT License (MIT)
        Copyright (c) 2013 Avi Levin
        Permission is hereby granted, free of charge, to any person obtaining a copy
        of this software and associated documentation files (the "Software"), to deal
        in the Software without restriction, including without limitation the rights
        to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
        copies of the Software, and to permit persons to whom the Software is
        furnished to do so, subject to the following conditions:
        The above copyright notice and this permission notice shall be included in
        all copies or substantial portions of the Software.
        THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
        IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
        FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
        AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
        LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
        OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
        THE SOFTWARE.
        -------------------------------*/

        /// <summary>
        /// Gets a multiple-cross-product of all of the numbers from [0,0,0] to [i1, i2, ...]. <see href="https://github.com/Arithmomaniac/cs-tools/blob/master/CrossProduct.cs"/>
        /// </summary>
        /// <param name="maxIndices">A list of the maximum indices to iterate in each position (eg [2,4,1]</param>
        /// <returns>An iterator of cross-products ([0,0,0], [0,0,1], [0,1,0], etc)</returns>
        public static IEnumerable<List<int>> IndexCrossProduct(IReadOnlyList<int> maxIndices)
        {
            //don't do anything to an empty lsit
            if (maxIndices == null || maxIndices.Count == 0)
                yield break;

            //we repeatedly make reference to what's the index of the last digit in the enumeration
            var lastIndex = maxIndices.Count - 1;

            //initialize the iterable return value. Start with [0,0,0, etc]
            List<int> currentIndices = Enumerable.Repeat(0, maxIndices.Count).ToList();

            //increment the iteration by repeatedly adding one to the next index.
            for (; true; currentIndices[lastIndex]++)
            {
                //This could result in a value for the last place outside of the allowed range.
                //If so, we repeately "carry" the value over to the previous index, in a fashion similar to adding 1 to 099

                //for reach index in the set, working right to left
                for (int incrementIndex = lastIndex; true; incrementIndex--)
                {
                    //if no overflow, we can directly return the enumerator value
                    if (currentIndices[incrementIndex] <= maxIndices[incrementIndex])
                    {
                        yield return currentIndices.Select(i => i).ToList(); //copy the list
                        break;
                    }

                    //If addition would completely break range, exit
                    if (incrementIndex == 0)
                        yield break;

                    //else, carry the increment to the next index
                    currentIndices[incrementIndex] = 0;
                    currentIndices[incrementIndex - 1]++;
                }
            }
        }

        // <summary>
        /// Finds the multi-cross-product of an list of object lists. <see href="https://github.com/Arithmomaniac/cs-tools/blob/master/CrossProduct.cs"/>
        /// </summary>
        /// <typeparam name="T">The type of the object being enumerated.</typeparam>
        /// <param name="possibleValueArrays">The list of object lists to cross-product over.</param>
        /// <returns>An iterator of the cross products.</returns>
        public static IEnumerable<List<T>> CrossProduct<T>(IReadOnlyList<IReadOnlyList<T>> possibleValueArrays)
        {
            //The technique is to iterate over the possible index values, then use each generated
            //integer cross product as indices for the possibleValueArrays
            var maxIndices = possibleValueArrays.Select(arr => arr.Count - 1).ToList();

            //convert each generated possible index to the underlying values
            foreach (var indexList in IndexCrossProduct(maxIndices))
            {
                yield return possibleValueArrays.Select((values, i) => values[indexList[i]]).ToList();
            }
            yield break;
        }

        #endregion
    }
}
