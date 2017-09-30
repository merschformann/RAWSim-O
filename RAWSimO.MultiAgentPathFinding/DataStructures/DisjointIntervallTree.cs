using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.MultiAgentPathFinding.DataStructures
{
    /// <summary>
    /// a tree for disjunct intervals
    /// </summary>
    public class DisjointIntervalTree
    {

        /// <summary>
        /// The list of interval starting points
        /// </summary>
        protected List<double> _intervalStart;

        /// <summary>
        /// The list of interval ending points
        /// </summary>
        protected List<double> _intervalEnds;

        /// <summary>
        /// The list of the agentID reserving this interval
        /// </summary>
        protected List<int> _intervalAgentId;

        /// <summary>
        /// The list of the agentID reserving this interval
        /// </summary>
        protected List<int> _intervalPrio;

        /// <summary>
        /// Initializes a new instance of the <see cref="DisjointIntervalTree"/> class.
        /// </summary>
        public DisjointIntervalTree(bool storeAgentIds, bool storePrios)
        {
            _intervalStart = new List<double>();
            _intervalEnds = new List<double>();
            _intervalAgentId = storeAgentIds ? new List<int>() : null;
            _intervalPrio = storePrios ? new List<int>() : null;
        }

        /// <summary>
        /// Adds an interval.
        /// </summary>
        /// <param name="start">The start.</param>
        /// <param name="end">The end.</param>
        public virtual void Add(double start, double end, int agentId = -1, int prio = -1)
        {
            //no senseless intervals
            bool senselesseInterval = end - start < ReservationTable.TOLERANCE;
            if (senselesseInterval)
                return;

            var insertionIndex = -1;

            //check if intersection-free
            if (!_intersectionFree(start, end, out insertionIndex))
            {
                var sb = new StringBuilder();
                sb.Append("Insert: ").Append(start.ToString(CultureInfo.InvariantCulture)).Append(" - ").Append(end.ToString(CultureInfo.InvariantCulture)).Append("\n");
                sb.Append("Senseless interval evaluated to: " + senselesseInterval.ToString() + "\n");
                for (int i = 0; i < _intervalStart.Count; i++)
                    sb.Append("Entry: ").Append(_intervalStart[i].ToString(CultureInfo.InvariantCulture)).Append(" - ").Append(_intervalEnds[i].ToString(CultureInfo.InvariantCulture)).Append("\n");
                throw new IntervalIntersectionException(sb.ToString());
            }

            //insert interval
            _intervalStart.Insert(insertionIndex, start);
            _intervalEnds.Insert(insertionIndex, end);

            //store agent id
            if (_intervalAgentId != null)
                _intervalAgentId.Insert(insertionIndex, agentId);

            //store priority
            if (_intervalPrio != null)
                _intervalPrio.Insert(insertionIndex, prio);

        }

        /// <summary>
        /// Removes the interval with the specified index.
        /// </summary>
        /// <param name="index">The index.</param>
        public virtual void Remove(int index)
        {
            //remove Interval
            _intervalStart.RemoveAt(index);
            _intervalEnds.RemoveAt(index);
            if (_intervalAgentId != null)
                _intervalAgentId.RemoveAt(index);
            if (_intervalPrio != null)
                _intervalPrio.RemoveAt(index);
        }

        /// <summary>
        /// Removes the interval intersects with point in time.
        /// </summary>
        /// <param name="time">The time.</param>
        /// <returns>true, if removed</returns>
        public virtual bool RemoveIntersectionWithTime(double time)
        {
            //remove Interval
            var index = -1;
            var ifree = _intersectionFree(time, time, out index);

            if (ifree)
                return false;

            Remove(index - 1);

            return true;
        }

        /// <summary>
        /// Checks weather the given interval intersects with an existing interval.
        /// </summary>
        /// <param name="start">The start.</param>
        /// <param name="end">The end.</param>
        /// <returns>false, if there is an intersection</returns>
        public virtual bool IntersectionFree(double start, double end)
        {
            var index = -1;
            return _intersectionFree(start, end, out index);
        }

        /// <summary>
        /// Checks weather the given interval intersects with an existing interval.
        /// </summary>
        /// <param name="start">The start.</param>
        /// <param name="end">The end.</param>
        /// <param name="agentId">The agent identifier, who is reserving this time slot. -1 if there is none.</param>
        /// <returns>
        /// false, if there is an intersection
        /// </returns>
        public virtual bool IntersectionFree(double start, double end, out int agentId)
        {
            var index = -1;
            var ifree = _intersectionFree(start, end, out index);

            if (ifree)
            {
                //no collision
                agentId = -1;
            }
            else if (index - 1 >= 0 && _intervalStart[index - 1] <= start && start < _intervalEnds[index - 1])
            {
                //my start point is in interval with the index [index - 1]
                agentId = _intervalAgentId[index - 1];
            }
            else
            {
                //my start point is not intercepting. So give me the next interval
                agentId = _intervalAgentId[index];
            }
            return ifree;
        }

        /// <summary>
        /// Checks weather the given interval intersects with an existing interval.
        /// </summary>
        /// <param name="start">The start.</param>
        /// <param name="end">The end.</param>
        /// <param name="agentId">The agent identifier, who is reserving this time slot. -1 if there is none.</param>
        /// <returns>
        /// false, if there is an intersection
        /// </returns>
        public virtual bool IntersectionFree(double start, double end, int node, out List<Collision> agentsAndPrios)
        {
            agentsAndPrios = null;
            var index = -1;
            var ifree = _intersectionFree(start, end, out index);

            if (!ifree)
            {
                //maybe the interval end of the previous interval lays between start and end
                index = Math.Max(0, index - 1);

                //go through all following intervals as long as a collision occurs
                var first = true;
                agentsAndPrios = new List<Collision>();

                while (index < _intervalStart.Count)
                {

                    //intersections
                    if (_intervalStart[index] < end && start < _intervalEnds[index])
                        agentsAndPrios.Add(new Collision(node, _intervalAgentId[index], _intervalPrio[index], _intervalStart[index]));
                    else if (!first)
                        break;
                    else
                        first = false;

                    index++;
                }

                return false;
            }

            return ifree;
        }

        /// <summary>
        /// Checks weather the given interval intersects with an existing interval.
        /// </summary>
        /// <param name="start">The start.</param>
        /// <param name="end">The end.</param>
        /// <param name="index">The index, where a new interval should be inserted.</param>
        /// <returns>
        /// false, if there is an intersection
        /// </returns>
        protected virtual bool _intersectionFree(double start, double end, out int index)
        {
            var searchIntervalLeft = 0;
            var searchIntervalRight = _intervalStart.Count - 1;

            //index is the mid
            index = (searchIntervalLeft + searchIntervalRight) / 2;

            //binary search
            while (searchIntervalLeft <= searchIntervalRight)
            {
                if (start == _intervalStart[index])
                {
                    index++;
                    return false;
                }
                else if (start < _intervalStart[index])
                    searchIntervalRight = index - 1;
                else
                    searchIntervalLeft = index + 1;

                index = (searchIntervalLeft + searchIntervalRight) / 2;
            }

            //insertion index
            index = searchIntervalLeft;

            //now searchIntervalEnd < searchIntervalStart holds
            //The free interval is between searchIntervalEnd and searchIntervalStart

            //This would be better names
            //FreeIntervalStart = _intervalEnds[searchIntervalEnd];
            //FreeIntervalEnd = _intervalStart[searchIntervalStart];
            //FreeIntervalStart < start && end < FreeIntervalEnd
            return (index == 0 || _intervalEnds[index - 1] <= start + ReservationTable.TOLERANCE) && (index == _intervalStart.Count || end <= _intervalStart[index] + ReservationTable.TOLERANCE);
        }

        /// <summary>
        /// Clears this instance.
        /// </summary>
        public virtual void Clear()
        {
            _intervalStart.Clear();
            _intervalEnds.Clear();
            if (_intervalAgentId != null)
                _intervalAgentId.Clear();
            if (_intervalPrio != null)
                _intervalPrio.Clear();
        }

        /// <summary>
        /// Gets the count.
        /// </summary>
        /// <value>
        /// The count.
        /// </value>
        public virtual int Count
        {
            get { return _intervalStart.Count; }
        }

        /// <summary>
        /// Remove all entries up to the current time.
        /// </summary>
        /// <param name="currentTime">The current time.</param>
        public virtual void Reorganize(double currentTime)
        {
            while (_intervalEnds.Count > 0 && _intervalEnds[0] < currentTime)
            {
                _intervalStart.RemoveAt(0);
                _intervalEnds.RemoveAt(0);
                if (_intervalAgentId != null)
                    _intervalAgentId.RemoveAt(0);
                if (_intervalPrio != null)
                    _intervalPrio.RemoveAt(0);
            }

        }

        /// <summary>
        /// Gets the interval of the intersection with the intervals of this table and the given interval.
        /// </summary>
        /// <param name="interval">The interval that overlap with an existing one.</param>
        /// <returns>
        /// The overlapping interval
        /// </returns>
        public void GetOverlappingInterval(double start, double end, out double intersectionStart, out double intersectionEnd)
        {
            intersectionStart = -1;
            intersectionEnd = -1;

            //get the insertion index
            int index;
            var intersectionFree = _intersectionFree(start, end, out index);

            //this method should not be called, if the start and end do not collide
            if (intersectionFree)
                return;

            //if the collision occurred on the interval with index - 1 then decrease
            if (index > 0 && _intervalStart[index - 1] <= start && start < _intervalEnds[index - 1])
                index--;

            //Min Start, Max End
            intersectionStart = Math.Max(_intervalStart[index], start);
            intersectionEnd = Math.Min(_intervalEnds[index], end);

        }

        /// <summary>
        /// Interval intersection occurred.
        /// </summary>
        public class IntervalIntersectionException : Exception
        {
            public IntervalIntersectionException(string s) : base(s) { }
        };



    }

    /// <summary>
    /// Interval intersection occurred.
    /// </summary>
    public struct Collision
    {
        /// <summary>
        /// The collision node
        /// </summary>
        public int node;

        /// <summary>
        /// The collision agent identifier
        /// </summary>
        /// 
        public int agentId;

        /// <summary>
        /// The collision priority
        /// </summary>
        public int priority;

        /// <summary>
        /// The collision time
        /// </summary>
        public double time;

        // Constructor:
        public Collision(int node, int agentId, int priority, double time)
        {
            this.node = node;
            this.agentId = agentId;
            this.priority = priority;
            this.time = time;
        }
    }
}
