using RAWSimO.MultiAgentPathFinding.DataStructures;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.MultiAgentPathFinding.Test
{
    [TestClass]
    public class FibbonacciTest
    {
        public static int runs = 50000;
        /*
        [TestMethod]
        public void FibbonaciCCorrectness()
        {
            var fib = new FibonacciHeapC<string>();

            Assert.IsTrue(fib.Size == 0);

            fib.Add(2, "find");
            fib.Add(3, "das");
            fib.Add(1, "ich");
            fib.Add(4, "sehr");
            fib.Add(6, "gut");

            Assert.IsTrue(fib.Size == 5);
            Assert.IsTrue(fib.deleteMin().data == "ich");
            Assert.IsTrue(fib.deleteMin().data == "find");
            Assert.IsTrue(fib.deleteMin().data == "das");
            Assert.IsTrue(fib.deleteMin().data == "sehr");
            Assert.IsTrue(fib.deleteMin().data == "gut");

            Assert.IsTrue(fib.Size == 0);


        }*/

        [TestMethod]
        public void FibbonaciJavaCorrectness()
        {
            var fib = new FibonacciHeap<double, string>();

            Assert.IsTrue(fib.Count == 0);

            fib.Enqueue(2, "find");
            fib.Enqueue(3, "das");
            fib.Enqueue(1, "ich");
            fib.Enqueue(4, "sehr");
            fib.Enqueue(6, "gut");

            Assert.IsTrue(fib.Count == 5);
            Assert.IsTrue(fib.Dequeue().Value == "ich");
            Assert.IsTrue(fib.Dequeue().Value == "find");
            Assert.IsTrue(fib.Dequeue().Value == "das");
            Assert.IsTrue(fib.Dequeue().Value == "sehr");
            Assert.IsTrue(fib.Dequeue().Value == "gut");

            Assert.IsTrue(fib.Count == 0);


        }

        [TestMethod]
        public void BinaryCorrectness()
        {
            var fib = new BinaryHeap<string>(BinaryHeap<string>.HeapType.MinHeap);

            Assert.IsTrue(fib.Size == 0);

            fib.Insert(2, "find");
            fib.Insert(3, "das");
            fib.Insert(1, "ich");
            fib.Insert(4, "sehr");
            fib.Insert(6, "gut");

            Assert.IsTrue(fib.Size == 5);
            Assert.IsTrue(fib.PopRoot() == "ich");
            Assert.IsTrue(fib.PopRoot() == "find");
            Assert.IsTrue(fib.PopRoot() == "das");
            Assert.IsTrue(fib.PopRoot() == "sehr");
            Assert.IsTrue(fib.PopRoot() == "gut");

            Assert.IsTrue(fib.Size == 0);


        }

        [TestMethod]
        public void FibbonaciJCorrectness()
        {
            var fib = new FibonacciHeapJ<string>();

            Assert.IsTrue(fib.Count == 0);

            fib.Enqueue(2, "find");
            fib.Enqueue(3, "das");
            fib.Enqueue(1, "ich");
            fib.Enqueue(4, "sehr");
            fib.Enqueue(6, "gut");

            Assert.IsTrue(fib.Count == 5);
            Assert.IsTrue(fib.Dequeue().Value == "ich");
            Assert.IsTrue(fib.Dequeue().Value == "find");
            Assert.IsTrue(fib.Dequeue().Value == "das");
            Assert.IsTrue(fib.Dequeue().Value == "sehr");
            Assert.IsTrue(fib.Dequeue().Value == "gut");

            Assert.IsTrue(fib.Count == 0);


        }
        /*
        [TestMethod]
        public void FibonacciHeapCPerformance()
        {
            var fib = new FibonacciHeapC<string>();

            Assert.IsTrue(fib.isEmpty());

            var rnd = new Random();

            for (int i = 0; i < runs; i++)
                fib.Add(rnd.NextDouble(), "x");

            Assert.IsTrue(fib.Size == runs);

            for (int i = 0; i < runs; i++)
                Assert.IsTrue(fib.deleteMin().data == "x");

            Assert.IsTrue(fib.isEmpty());


        }*/

        [TestMethod]
        public void FibbonaciJavaPerformance()
        {
            var fib = new FibonacciHeap<double, string>();

            Assert.IsTrue(fib.Count == 0);

            var rnd = new Random(0);
            var ent = new FibonacciHeapHeapCell<double, string>[runs];

            for (int i = 0; i < runs; i++)
                ent[i] = fib.Enqueue(rnd.NextDouble(), "x");

            for (int i = 0; i < runs / 2; i++)
            {
                var nd = rnd.NextDouble();
                fib.ChangeKey(ent[i], nd);
            }

            for (int i = 0; i < runs / 2; i++)
            {
                ent[i] = fib.Enqueue(rnd.NextDouble(), "x");
                fib.Dequeue();
            }


            Assert.IsTrue(fib.Count == runs);

            for (int i = 0; i < runs; i++)
                Assert.IsTrue(fib.Dequeue().Value == "x");

            Assert.IsTrue(fib.Count == 0);


        }

        [TestMethod]
        public void BinaryHeapPerformance()
        {
            var fib = new BinaryHeap<string>(BinaryHeap<string>.HeapType.MinHeap);

            Assert.IsTrue(fib.Size == 0);

            var rnd = new Random(0);

            for (int i = 0; i < runs; i++)
                fib.Insert(rnd.NextDouble(), "x");

            Assert.IsTrue(fib.Size == runs);

            for (int i = 0; i < runs; i++)
                Assert.IsTrue(fib.PopRoot() == "x");

            Assert.IsTrue(fib.Size == 0);


        }

        [TestMethod]
        public void FibonacciHeapJPerformance()
        {
            var fib = new FibonacciHeapJ<string>();

            Assert.IsTrue(fib.Count == 0);

            var rnd = new Random(0);
            var ent = new FibonacciHeapJ<string>.Entry[runs];

            for (int i = 0; i < runs; i++)
                ent[i] = fib.Enqueue(rnd.NextDouble(), "x");

            for (int i = 0; i < runs / 2; i++)
            {
                var nd = rnd.NextDouble();
                fib.ChangeKey(ent[i], nd);
            }

            for (int i = 0; i < runs / 2; i++)
            {
                ent[i] = fib.Enqueue(rnd.NextDouble(), "x");
                fib.Dequeue();
            }

            Assert.IsTrue(fib.Count == runs);

            for (int i = 0; i < runs; i++)
                Assert.IsTrue(fib.Dequeue().Value == "x");

            Assert.IsTrue(fib.Count == 0);


        }
    }
}
