using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RAWSimO.MultiAgentPathFinding.Methods;
using RAWSimO.MultiAgentPathFinding.Elements;
using RAWSimO.MultiAgentPathFinding;
using System.Collections.Generic;

namespace RAWSimO.MultiAgentPathFinding.Test
{
    [TestClass]
    public class EvadeProblemTest
    {
        [TestMethod]
        public void WHCAvStar()
        {
            var method = new WHCAvStarMethod(GetGraph(), 0, PathPlanningCommunicator.DUMMY_COMMUNICATOR);
            method.LengthOfAWaitStep = 1;
            var agents = Test(method);
            output(agents);
        }

        [TestMethod]
        public void WHCAnStar()
        {
            var method = new WHCAnStarMethod(GetGraph(), 0, new List<int>(new int[] { 0, 1 }), new List<int>(new int[] { 2, 1 }), PathPlanningCommunicator.DUMMY_COMMUNICATOR);
            method.LengthOfAWaitStep = 0.99;
            method.UseBias = false;
            var agents = Test(method);
            output(agents);
        }

        [TestMethod]
        public void FARrr()
        {
            var method = new FARMethod(GetGraph(), 0, FARMethod.EvadingStrategy.EvadeByRerouting, PathPlanningCommunicator.DUMMY_COMMUNICATOR);
            method.LengthOfAWaitStep = 1;
            var agents = Test(method);
            output(agents);
        }

        [TestMethod]
        public void FARnn()
        {
            var method = new FARMethod(GetGraph(), 0, FARMethod.EvadingStrategy.EvadeToNextNode, PathPlanningCommunicator.DUMMY_COMMUNICATOR);
            method.LengthOfAWaitStep = 1;
            var agents = Test(method);
            output(agents);
        }

        [TestMethod]
        public void PAS()
        {
            var method = new PASMethod(GetGraph(), 0, PathPlanningCommunicator.DUMMY_COMMUNICATOR);
            method.LengthOfAWaitStep = 1;
            var agents = Test(method);
            output(agents);
        }

        [TestMethod]
        public void ODIDf()
        {
            var method = new ODIDMethod(GetGraph(), 0, PathPlanningCommunicator.DUMMY_COMMUNICATOR);
            method.LengthOfAWaitStep = 2;
            method.UseFinalReservations = true;
            var agents = Test(method);
            output(agents);
        }

        [TestMethod]
        public void ODID()
        {
            var method = new ODIDMethod(GetGraph(), 0, PathPlanningCommunicator.DUMMY_COMMUNICATOR);
            method.LengthOfAWaitStep = 2;
            method.UseFinalReservations = false;
            var agents = Test(method);
            output(agents);
        }

        [TestMethod]
        public void CBS()
        {
            var method = new CBSMethod(GetGraph(), 0, PathPlanningCommunicator.DUMMY_COMMUNICATOR);
            method.LengthOfAWaitStep = 1;
            var agents = Test(method);
            output(agents);
        }

        [TestMethod]
        public void BCP()
        {
            var method = new BCPMethod(GetGraph(), 0, PathPlanningCommunicator.DUMMY_COMMUNICATOR);
            var agents = Test(method);
            output(agents);
        }

        private void output(List<Agent> agents)
        {
            Assert.IsTrue(agents[0].Path.Count > 0 && agents[1].Path.Count > 0);
            for (int i = 0; i <= 1; i++)
            {
                Console.WriteLine("Agent " + i + ": " + agents[i].NextNode + " -> " + agents[i].DestinationNode);
                Console.WriteLine(agents[i].Path);
            }
        }

        public Graph GetGraph()
        {
            var graph = new Graph(5);

            //Node 0
            graph.PositionX[0] = 0;
            graph.PositionY[0] = 1;
            graph.Edges[0] = new Edge[1];
            graph.Edges[0][0] = new Edge
            {
                From = 0,
                To = 1,
                Distance = 1,
                Angle = (short)0
            };

            //Node 1
            graph.PositionX[1] = 1;
            graph.PositionY[1] = 1;
            graph.Edges[1] = new Edge[2];
            graph.Edges[1][0] = new Edge
            {
                From = 1,
                To = 0,
                Distance = 1,
                Angle = (short)180
            };
            graph.Edges[1][1] = new Edge
            {
                From = 1,
                To = 2,
                Distance = 1,
                Angle = (short)0
            };

            //Node 2
            graph.PositionX[2] = 2;
            graph.PositionY[2] = 1;
            graph.Edges[2] = new Edge[3];
            graph.Edges[2][0] = new Edge
            {
                From = 2,
                To = 1,
                Distance = 1,
                Angle = (short)180
            };
            graph.Edges[2][1] = new Edge
            {
                From = 2,
                To = 3,
                Distance = 1,
                Angle = (short)0
            };
            graph.Edges[2][2] = new Edge
            {
                From = 2,
                To = 4,
                Distance = 1,
                Angle = (short)270
            };

            //Node 3
            graph.PositionX[3] = 3;
            graph.PositionY[3] = 1;
            graph.Edges[3] = new Edge[1];
            graph.Edges[3][0] = new Edge
            {
                From = 3,
                To = 2,
                Distance = 1,
                Angle = (short)180
            };

            //Node 4
            graph.PositionX[4] = 2;
            graph.PositionY[4] = 0;
            graph.Edges[4] = new Edge[1];
            graph.Edges[4][0] = new Edge
            {
                From = 4,
                To = 2,
                Distance = 1,
                Angle = (short)90
            };

            return graph;
        }

        public List<Agent> Test(PathFinder method)
        {
            //all needed variables
            var currentTime = 0.0;
            var agents = new List<Agent>();
            var paths = new Path[2];
            paths[0] = new Path();
            paths[1] = new Path();

            //Two agents
            agents.Add(new Agent
            {
                ID = 0,
                NextNode = 2,
                ReservationsToNextNode = new List<MultiAgentPathFinding.DataStructures.ReservationTable.Interval>(),
                ArrivalTimeAtNextNode = 0.0,
                OrientationAtNextNode = 0,
                DestinationNode = 0,
                FinalDestinationNode = 0,
                Path = paths[0], //path reference => will be filled
                FixedPosition = false,
                CanGoThroughObstacles = true,
                Physics = new MultiAgentPathFinding.Physic.Physics(double.MaxValue / 2.0, double.MaxValue / 2.0, 1, 0.0),
                RequestReoptimization = true
            });

            agents.Add(new Agent
            {
                ID = 1,
                NextNode = 1,
                ReservationsToNextNode = new List<MultiAgentPathFinding.DataStructures.ReservationTable.Interval>(),
                ArrivalTimeAtNextNode = 0.0,
                OrientationAtNextNode = 0,
                DestinationNode = 3,
                FinalDestinationNode = 3,
                Path = paths[1], //path reference => will be filled
                FixedPosition = false,
                CanGoThroughObstacles = true,
                Physics = new MultiAgentPathFinding.Physic.Physics(double.MaxValue / 2.0, double.MaxValue / 2.0, 1, 0.0),
                RequestReoptimization = true
            });

            method.FindPaths(currentTime, agents);

            return agents;
        }


    }
}
