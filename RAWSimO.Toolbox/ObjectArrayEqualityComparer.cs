using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.Toolbox
{
    /// <summary>
    /// Provdides an equality comparer for object arrays
    /// </summary>
    /// <typeparam name="T">Type of the objects inside the array</typeparam>
    public class ObjectArrayEqualityComparer<T> : IEqualityComparer<T[]>
    {
        public bool Equals(T[] x, T[] y)
        {
            if (x.Length != y.Length)
            {
                return false;
            }
            for (int i = 0; i < x.Length; i++)
            {
                if (x[i] != null && y[i] != null)
                {
                    if (!x[i].Equals(y[i]))
                    {
                        return false;
                    }
                }
                else
                {
                    if (x[i] == null && y[i] != null ||
                        x[i] != null && y[i] == null)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        public int GetHashCode(T[] obj)
        {
            if (obj.Length == 0)
            {
                return 0;
            }
            int hash = obj[0].GetHashCode();
            foreach (var item in obj.Skip(1))
            {
                if (item != null)
                {
                    hash ^= item.GetHashCode();
                }
            }
            return hash;
        }
    }
}
