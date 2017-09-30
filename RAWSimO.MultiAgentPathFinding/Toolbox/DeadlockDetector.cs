using RAWSimO.MultiAgentPathFinding.DataStructures;
using RAWSimO.MultiAgentPathFinding.Elements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.MultiAgentPathFinding.Toolbox
{
    /// <summary>
    ///  Detect and handle Deadlocks
    /// </summary>
    public class DeadlockHandler
    {

        /// <summary>
        /// The maximum wait time
        /// </summary>
        public double MaximumWaitTime;

        /// <summary>
        /// The length of a wait step
        /// </summary>
        public double LengthOfAWaitStep = 5;

        /// <summary>
        /// The time of the agent last move
        /// </summary>
        private Dictionary<int, double> _waitingSince;

        /// <summary>
        /// The node of the agent last move
        /// </summary>
        private Dictionary<int, int> _waitNode;

        /// <summary>
        /// The graph
        /// </summary>
        private Graph _graph;

        /// <summary>
        /// The Random Component
        /// </summary>
        private Random _rnd;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeadlockHandler"/> class.
        /// </summary>
        /// <param name="graph">The graph.</param>
        /// <param name="seed">The seed value for the randomizer.</param>
        public DeadlockHandler(Graph graph, int seed)
        {
            _graph = graph;
            _waitingSince = new Dictionary<int, double>();
            _waitNode = new Dictionary<int, int>();
            _rnd = new Random(seed);
        }

        /// <summary>
        /// Updates the specified agents at the specified time.
        /// </summary>
        /// <param name="agents">The agents.</param>
        /// <param name="currentTime">The current time.</param>
        public void Update(List<Agent> agents, double currentTime)
        {
            //only measure waiting time for agents that want to move - just update the others
            foreach (var agent in agents.Where(a => a.FixedPosition))
            {
                // Update the waiting time, because this agent is actually not waiting
                _waitingSince[agent.ID] = currentTime;
                _waitNode[agent.ID] = agent.NextNode;
            }
            //check if the agent is moving
            foreach (var agent in agents.Where(a => !a.FixedPosition))
            {
                if (!_waitingSince.ContainsKey(agent.ID))
                {
                    _waitingSince.Add(agent.ID, currentTime);
                    _waitNode.Add(agent.ID, agent.NextNode);
                }
                else if (agent.ReservationsToNextNode.Count > 0 || agent.NextNode != _waitNode[agent.ID])
                {
                    //update on
                    //- Agent.NextNode changed
                    _waitingSince[agent.ID] = currentTime;
                    _waitNode[agent.ID] = agent.NextNode;
                }
            }
        }

        /// <summary>
        /// Determines whether [is in deadlock] [the specified agent].
        /// </summary>
        /// <param name="agent">The agent.</param>
        /// <param name="currentTime">The current time.</param>
        /// <returns>true, whether the agent is in a potential deadlock</returns>
        public bool IsInDeadlock(Agent agent, double currentTime)
        {
            return !agent.FixedPosition && currentTime - _waitingSince[agent.ID] > MaximumWaitTime;
        }

        /// <summary>
        /// Randoms the hop.
        /// </summary>
        /// <param name="agent">The agent.</param>
        /// <param name="reservationTable">The reservation table.</param>
        /// <param name="currentTime">The current time.</param>
        /// <param name="finalReservation">if set to <c>true</c> [final reservation].</param>
        /// <param name="insertReservation">if set to <c>true</c> [insert reservation].</param>
        /// <returns></returns>
        public bool RandomHop(Agent agent, ReservationTable reservationTable = null, double currentTime = 0.0, bool finalReservation = false, bool insertReservation = false)
        {

            //try to find a free hop
            var possibleEdges = new List<Edge>(_graph.Edges[agent.NextNode]);
            shuffle<Edge>(possibleEdges);
            foreach (var edge in possibleEdges.Where(e => !e.ToNodeInfo.IsLocked && (agent.CanGoThroughObstacles || !e.ToNodeInfo.IsObstacle)))
            {
                //create intervals
                if (reservationTable != null)
                {
                    var intervals = reservationTable.CreateIntervals(currentTime, currentTime, 0, agent.Physics, agent.NextNode, edge.To, finalReservation);

                    //check if a reservation is possible
                    if (reservationTable.IntersectionFree(intervals))
                    {
                        if (insertReservation)
                            reservationTable.Add(intervals);

                        //create a path
                        agent.Path.Clear();
                        agent.Path.AddLast(edge.To, true, _rnd.NextDouble() * LengthOfAWaitStep);

                        return true;
                    }
                }
                else
                {
                    //create a path
                    agent.Path.Clear();
                    agent.Path.AddLast(edge.To, true, _rnd.NextDouble() * LengthOfAWaitStep);

                    return true;
                }

            }

            return false;
        }

        /// <summary>
        /// Shuffles the specified list.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list">The list.</param>
        private void shuffle<T>(IList<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                int k = (_rnd.Next(0, n) % n);
                n--;
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }
    }
}
