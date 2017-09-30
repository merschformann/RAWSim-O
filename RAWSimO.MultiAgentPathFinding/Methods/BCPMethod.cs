using RAWSimO.MultiAgentPathFinding.Algorithms.AStar;
using RAWSimO.MultiAgentPathFinding.DataStructures;
using RAWSimO.MultiAgentPathFinding.Elements;
using RAWSimO.MultiAgentPathFinding.Physic;
using RAWSimO.MultiAgentPathFinding.Toolbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.MultiAgentPathFinding.Methods
{
    /// <summary>
    /// Flow Annotation Re-planning by Ko-Hsin Cindy Wang and Adi Botea 2008
    /// </summary>
    public class BCPMethod : PathFinder
    {

        /// <summary>
        /// Reservation Table
        /// </summary>
        public ReservationTable _reservationTable;

        /// <summary>
        /// amount of biased cost in seconds
        /// </summary>
        public double BiasedCostAmount = 6;

        /// <summary>
        /// The minimum time between two calculations
        /// </summary>
        public double MinimumCalcSpan = 5;

        /// <summary>
        /// The biased cost
        /// </summary>
        private Dictionary<int, Dictionary<int, double>> _biasedCost;

        /// <summary>
        /// The deadlock handler
        /// </summary>
        private DeadlockHandler _deadlockHandler;

        /// <summary>
        /// Initializes a new instance of the <see cref="FARMethod"/> class.
        /// </summary>
        /// <param name="graph">graph</param>
        /// <param name="seed">The seed to use for the randomizer.</param>
        /// <param name="logger">The logger to use.</param>
        public BCPMethod(Graph graph, int seed, PathPlanningCommunicator logger)
            : base(graph, seed, logger)
        {
            if (graph.BackwardEdges == null)
                graph.GenerateBackwardEgdes();
            _reservationTable = new ReservationTable(graph, true, true, false);
            _biasedCost = new Dictionary<int, Dictionary<int, double>>();
            _deadlockHandler = new DeadlockHandler(graph, seed);
        }

        public override void FindPaths(double currentTime, List<Agent> agents)
        {
            Stopwatch.Restart();

            var fixedBlockage = AgentInfoExtractor.getStartBlockage(agents, currentTime);

            //deadlock handling
            _deadlockHandler.LengthOfAWaitStep = LengthOfAWaitStep;
            _deadlockHandler.MaximumWaitTime = 30;
            _deadlockHandler.Update(agents, currentTime);

            //initiate biased costs
            _biasedCost.Clear();
            foreach (var agent in agents)
                _biasedCost.Add(agent.ID, new Dictionary<int, double>());

            while (true)
            {

                //clear reservation table
                _reservationTable.Clear();

                //set fixed blockage
                try
                {
                    foreach (var agent in agents)
                    {
                        //all intervals from now to the moment of stop
                        foreach (var interval in fixedBlockage[agent])
                            _reservationTable.Add(interval, agent.ID);

                        //reservation of next node
                        _reservationTable.Add(agent.NextNode, agent.ArrivalTimeAtNextNode, double.PositiveInfinity, agent.ID);
                    }
                }
                catch (RAWSimO.MultiAgentPathFinding.DataStructures.DisjointIntervalTree.IntervalIntersectionException) { }

                var collisionFound = false;
                foreach (var agent in agents.Where(a => !a.FixedPosition && a.NextNode != a.DestinationNode))
                {

                    if (Stopwatch.ElapsedMilliseconds / 1000.0 > RuntimeLimitPerAgent * agents.Count * 0.9 || Stopwatch.ElapsedMilliseconds / 1000.0 > RunTimeLimitOverall)
                    {
                        Communicator.SignalTimeout();
                        return;
                    }

                    //remove blocking next node
                    _reservationTable.RemoveIntersectionWithTime(agent.NextNode, agent.ArrivalTimeAtNextNode);


                    if (_deadlockHandler.IsInDeadlock(agent, currentTime))
                    {
                        _deadlockHandler.RandomHop(agent, _reservationTable, currentTime, true, true);
                        continue;
                    }

                    //Create A* search if necessary.
                    //Necessary if the agent has none or the agents destination has changed
                    var aStar = new SpaceAStar(Graph, agent.NextNode, agent.DestinationNode, agent.OrientationAtNextNode, agent.Physics, agent, _biasedCost[agent.ID]);

                    var found = aStar.Search();

                    //the search ended with no result => just wait a moment
                    if (!found)
                    {
                        //Can not find a path. Maybe the agent has to wait until the blocked nodes moved
                        waitStep(agent);
                        continue;
                    }

                    //get the path
                    List<ReservationTable.Interval> reservations;
                    agent.Path.Clear();
                    aStar.getReservationsAndPath(agent.ArrivalTimeAtNextNode, ref agent.Path, out reservations);
                    reservations.Last().End = double.PositiveInfinity;

                    //check whether the agent collides with an other agent on its way
                    int collidesWithAgent;
                    int collidesOnNode;
                    if (!_reservationTable.IntersectionFree(reservations, out collidesOnNode, out collidesWithAgent))
                    {
                        agent.Path.SetStopBeforeNode(collidesOnNode);

                        //collision => add biased cost for the current agent
                        //the other agent remains with the old cost. He has the higher priority
                        if (_biasedCost[agent.ID].ContainsKey(collidesOnNode))
                            _biasedCost[agent.ID][collidesOnNode] += BiasedCostAmount;
                        else
                            _biasedCost[agent.ID][collidesOnNode] = BiasedCostAmount;

                        collisionFound = true;
                    }
                    else
                    {
                        _reservationTable.Add(reservations, agent.ID);
                    }
                }

                if (!collisionFound)
                    return;
            }
        }

        /// <summary>
        /// Waits the step.
        /// </summary>
        /// <param name="agent">The agent.</param>
        /// <param name="waitForAgentId">The "wait for" agent identifier.</param>
        /// <returns>A path with a waiting step</returns>
        private void waitStep(Agent agent)
        {
            agent.Path.Clear();

            //wait a little while
            agent.Path.AddFirst(agent.NextNode, true, LengthOfAWaitStep);

            //add blocking next node
            if (_reservationTable.IntersectionFree(agent.NextNode, agent.ArrivalTimeAtNextNode, double.PositiveInfinity))
                _reservationTable.Add(agent.NextNode, agent.ArrivalTimeAtNextNode, double.PositiveInfinity, agent.ID);
        }
    }
}
