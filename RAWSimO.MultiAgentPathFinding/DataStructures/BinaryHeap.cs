using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.MultiAgentPathFinding.DataStructures
{

    public class BinaryHeap<T>
    {
        List<double> priorities;
        List<T> items;

        public HeapType Type { get; private set; }

        public int Size { get { return priorities.Count; } }

        public T Root
        {
            get { return items[0]; }
        }

        public BinaryHeap(HeapType type)
        {
            priorities  = new List<double>();
            items = new List<T>();
            this.Type = type;
        }

        public void Insert(double priority, T item)
        {
            items.Add(item);
            priorities.Add(priority);

            int i = items.Count - 1;

            bool flag = true;
            if (Type == HeapType.MaxHeap)
                flag = false;

            while (i > 0)
            {
                if ((priorities[i].CompareTo(priorities[(i - 1) / 2]) > 0) ^ flag)
                {
                    T temp = items[i];
                    items[i] = items[(i - 1) / 2];
                    items[(i - 1) / 2] = temp;

                    double tempd = priorities[i];
                    priorities[i] = priorities[(i - 1) / 2];
                    priorities[(i - 1) / 2] = tempd;

                    i = (i - 1) / 2;
                }
                else
                    break;
            }
        }

        public void DeleteRoot()
        {
            int i = priorities.Count - 1;

            items[0] = items[i];
            priorities[0] = priorities[i];
            items.RemoveAt(i);
            priorities.RemoveAt(i);

            i = 0;

            bool flag = true;
            if (Type == HeapType.MaxHeap)
                flag = false;

            while (true)
            {
                int leftInd = 2 * i + 1;
                int rightInd = 2 * i + 2;
                int largest = i;

                if (leftInd < priorities.Count)
                {
                    if ((priorities[leftInd].CompareTo(priorities[largest]) > 0) ^ flag)
                        largest = leftInd;
                }

                if (rightInd < priorities.Count)
                {
                    if ((priorities[rightInd].CompareTo(priorities[largest]) > 0) ^ flag)
                        largest = rightInd;
                }

                if (largest != i)
                {
                    T temp = items[largest];
                    items[largest] = items[i];
                    items[i] = temp;

                    double tempd = priorities[largest];
                    priorities[largest] = priorities[i];
                    priorities[i] = tempd;

                    i = largest;
                }
                else
                    break;
            }
        }

        public T PopRoot()
        {
            T result = items[0];

            DeleteRoot();

            return result;
        }

        public enum HeapType
        {
            MinHeap,
            MaxHeap
        }

    }
}
