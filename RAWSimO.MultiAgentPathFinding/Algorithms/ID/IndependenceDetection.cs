using RAWSimO.MultiAgentPathFinding.Algorithms.OD;
using RAWSimO.MultiAgentPathFinding.DataStructures;
using RAWSimO.MultiAgentPathFinding.Elements;
using RAWSimO.MultiAgentPathFinding.Toolbox;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.MultiAgentPathFinding.Algorithms.ID
{
    /// <summary>
    /// Finding Optimal Solutions to Cooperative Pathﬁnding Problems - T. Standley 2010
    /// </summary>
    public class IndependenceDetection
    {
        /// <summary>
        /// The low level solver for agent groups
        /// </summary>
        public LowLevelSolver LowLevSolver;

        /// <summary>
        /// The _graph
        /// </summary>
        private Graph _graph;

        /// <summary>
        /// The deadlock handler
        /// </summary>
        private DeadlockHandler _deadlockHandler;

        /// <summary>
        /// Initializes a new instance of the <see cref="IndependenceDetection" /> class.
        /// </summary>
        /// <param name="lowLevelSolver">The low level solver.</param>
        /// <param name="seed">The seed to use for the randomizer.</param>
        public IndependenceDetection(Graph graph, int seed, LowLevelSolver lowLevelSolver = null)
        {
            this.LowLevSolver = lowLevelSolver;
            this._graph = graph;
            this._deadlockHandler = new DeadlockHandler(_graph, seed);
        }

        /// <summary>
        /// Find the path for all the agents.
        /// </summary>
        /// <param name="currentTime">The current Time.</param>
        /// <param name="agents">agents</param>
        /// <param name="obstacleNodes">The way points of the obstacles.</param>
        /// <param name="lockedNodes"></param>
        /// <param name="nextReoptimization">The next re-optimization time.</param>
        /// <param name="communicator">The communicator used for communication with the corresponding instance.</param>
        public void FindPaths(double currentTime, List<Agent> agents, double runtimeLimit, PathPlanningCommunicator communicator)
        {
            if (agents.Count == 0)
                return;

            var stopwatch = new Stopwatch();
            stopwatch.Restart();

            //0: init low level solver
            LowLevSolver.Init(currentTime, agents);

            //1: assign each agent to a singleton group
            var agentGroups = new Dictionary<int, List<Agent>>(agents.ToDictionary(a => a.ID, a => new List<Agent>(new Agent[] { a })));
            var groundIdAssigments = new Dictionary<int, int>(agents.ToDictionary(a => a.ID, a => a.ID));

            //2: plan a path for each group
            var plannedPath = new Dictionary<int, List<PlannedPath>>(agentGroups.ToDictionary(ag => ag.Key, ag => LowLevSolver.FindPaths(ag.Value)));

            var reservationTable = new ReservationTable(_graph, true, true, false);

            //set fixed blockage
            var fixedBlockage = AgentInfoExtractor.getStartBlockage(agents, currentTime);
            foreach (var agent in fixedBlockage.Keys)
                foreach (var interval in fixedBlockage[agent])
                    reservationTable.Add(interval, agent.ID);

            //3: repeat
            while (agentGroups.Count > 1)
            {
                reservationTable.Clear();

                //4: simulate execution of all paths until a conflict occurs
                int conflictAgent1 = -1;
                int conflictGroup1 = -1;
                int conflictAgent2 = -1;
                int conflictGroup2 = -1;
                int conflictNode;
                var foundConflict = false;
                foreach (var groupId in agentGroups.Keys)
                {
                    for (var agentIndex = 0; agentIndex < agentGroups[groupId].Count && !foundConflict; agentIndex++)
                    {
                        foundConflict = !reservationTable.IntersectionFree(plannedPath[groupId][agentIndex].Reservations, out conflictNode, out conflictAgent1);
                        if (foundConflict)
                        {
                            //remember the conflict agent
                            conflictAgent2 = plannedPath[groupId][agentIndex].Agent.ID;
                            conflictGroup2 = groupId;
                            conflictGroup1 = groundIdAssigments[conflictAgent1];

                            if (conflictGroup1 == conflictGroup2)
                                foundConflict = false;
                        }
                        else
                        {
                            //just add intervals
                            reservationTable.Add(plannedPath[groupId][agentIndex].Reservations, plannedPath[groupId][agentIndex].Agent.ID);
                        }
                    }
                }


                if (stopwatch.ElapsedMilliseconds / 1000.0 > runtimeLimit * 0.9)
                {
                    communicator.SignalTimeout();
                    break;
                }

                if (foundConflict)
                {
                    //5: merge two conflicting groups into a single group
                    //merge
                    agentGroups[conflictGroup1].AddRange(agentGroups[conflictGroup2]);
                    //delete old group
                    agentGroups.Remove(conflictGroup2);
                    plannedPath.Remove(conflictGroup2);
                    //reset Assignment
                    for (int agentIndex = 0; agentIndex < agentGroups[conflictGroup1].Count; agentIndex++)
                        groundIdAssigments[agentGroups[conflictGroup1][agentIndex].ID] = conflictGroup1;

                    //6: cooperatively plan new group
                    plannedPath[conflictGroup1] = LowLevSolver.FindPaths(agentGroups[conflictGroup1]);
                }
                else
                {
                    //7: until no conflicts occur
                    break;
                }
            }

            //8: solution ← paths of all groups combined
            foreach (var groupId in agentGroups.Keys)
            {
                for (var agentIndex = 0; agentIndex < agentGroups[groupId].Count; agentIndex++)
                {
                    agentGroups[groupId][agentIndex].Path = plannedPath[groupId][agentIndex].Path;//9: return solution
                    if (agentGroups[groupId][agentIndex].Path.Count <= 1)
                        _deadlockHandler.RandomHop(agentGroups[groupId][agentIndex]);
                }
            }

        }

        /// <summary>
        /// A planned path for an agent.
        /// Including the agent, the path and the reservations.
        /// </summary>
        public class PlannedPath
        {
            /// <summary>
            /// The agent
            /// </summary>
            public Agent Agent;

            /// <summary>
            /// The path
            /// </summary>
            public Path Path;

            /// <summary>
            /// The reservations
            /// </summary>
            public List<ReservationTable.Interval> Reservations;
        }

        /// <summary>
        /// Low level solver for independence detection
        /// </summary>
        public interface LowLevelSolver
        {

            /// <summary>
            /// Initializes the low level solver.
            /// </summary>
            /// <param name="currentTime">The current time.</param>
            /// <param name="allAgents">All agents.</param>
            /// <param name="obstacleNodes">The obstacle nodes.</param>
            /// <param name="lockedNodes">The locked nodes.</param>
            void Init(double currentTime, List<Agent> allAgents);

            /// <summary>
            /// Finds the paths.
            /// </summary>
            /// <param name="agents">The agents in the current group.</param>
            /// <returns></returns>
            List<PlannedPath> FindPaths(List<Agent> agents);
        }

    }
}
