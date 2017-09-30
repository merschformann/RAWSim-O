using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.Core.Remote
{
    /// <summary>
    /// Implements functionality to integrate real world components in the simulation.
    /// </summary>
    public class RemoteControlAdapter
    {
        /// <summary>
        /// Creates a new instance of the adapter.
        /// </summary>
        /// <param name="instance">The instance the adapter belongs to.</param>
        public RemoteControlAdapter(Instance instance) { _instance = instance; }

        /// <summary>
        /// The instance this remote-controller belongs to.
        /// </summary>
        private Instance _instance;
        /// <summary>
        /// The callback for submitting a path for a robot.
        /// </summary>
        public Action<int, double, List<int>> PathSubmissionCallback { get; set; }
        /// <summary>
        /// The callback for telling a robot to pickup a pod.
        /// </summary>
        public Action<int> PickupSubmissionCallback { get; set; }
        /// <summary>
        /// The callback for telling a robot to setdown a pod.
        /// </summary>
        public Action<int> SetdownSubmissionCallback { get; set; }
        /// <summary>
        /// The callback for telling a robot to rest.
        /// </summary>
        public Action<int> RestSubmissionCallback { get; set; }
        /// <summary>
        /// The callback for telling a robot to get an item.
        /// </summary>
        public Action<int> GetItemSubmissionCallback { get; set; }
        /// <summary>
        /// The callback for telling a robot to put an item.
        /// </summary>
        public Action<int> PutItemSubmissionCallback { get; set; }
        /// <summary>
        /// Submits a path to a real world robot.
        /// </summary>
        /// <param name="robotID">The ID of the robot.</param>
        /// <param name="waitTime">The waittime before the execution starts.</param>
        /// <param name="path">The path to follow.</param>
        public void RobotSubmitPath(int robotID, double waitTime, List<int> path)
        {
            // Submit the path if we have a callback
            if (PathSubmissionCallback != null)
                PathSubmissionCallback(robotID, waitTime, path);
        }
        /// <summary>
        /// Tells a robot to pickup a pod.
        /// </summary>
        /// <param name="robotID">The robot ID</param>
        public void RobotSubmitPickupCommand(int robotID)
        {
            // Issue a pickup command to the given robot if we have a callback
            if (PickupSubmissionCallback != null)
                PickupSubmissionCallback(robotID);
        }
        /// <summary>
        /// Tells a robot to setdown a pod.
        /// </summary>
        /// <param name="robotID">The robot ID</param>
        public void RobotSubmitSetdownCommand(int robotID)
        {
            // Issue a setdown command to the given robot if we have a callback
            if (SetdownSubmissionCallback != null)
                SetdownSubmissionCallback(robotID);
        }
        /// <summary>
        /// Tells a robot to rest.
        /// </summary>
        /// <param name="robotID">The robot ID</param>
        public void RobotSubmitRestCommand(int robotID)
        {
            // Issue a rest command to the given robot if we have a callback
            if (RestSubmissionCallback != null)
                RestSubmissionCallback(robotID);
        }
        /// <summary>
        /// Tells a robot to get an item.
        /// </summary>
        /// <param name="robotID">The robot ID</param>
        public void RobotSubmitGetItemCommand(int robotID)
        {
            // Issue a getitem command to the given robot if we have a callback
            if (GetItemSubmissionCallback != null)
                GetItemSubmissionCallback(robotID);
        }
        /// <summary>
        /// Tells a robot to put an item.
        /// </summary>
        /// <param name="robotID">The robot ID</param>
        public void RobotSubmitPutItemCommand(int robotID)
        {
            // Issue a putitem command to the given robot if we have a callback
            if (PutItemSubmissionCallback != null)
                PutItemSubmissionCallback(robotID);
        }
        /// <summary>
        /// This method is called whenever a robot updates its location.
        /// </summary>
        /// <param name="robotID">The robot ID.</param>
        /// <param name="waypointID">The new location of the robot indicated by the waypoint ID.</param>
        /// <param name="orientation">The orientation of the robot.</param>
        public void RobotLocationUpdateCallback(int robotID, int waypointID, double orientation)
        {
            // Notify instance about the new position of a robot (catch and log possible exceptions)
            try { _instance.GetBotByID(robotID).OnReachedWaypoint(_instance.GetWaypointByID(waypointID)); }
            catch (Exception ex)
            {
                _instance.LogSevere(
                    "Error when remotely updating location of bot " + robotID.ToString() +
                    " to waypoint " + waypointID.ToString() +
                    " and orientation " + orientation.ToString() +
                    ": " + ex.Message);
            }
        }
        /// <summary>
        /// This method is called whenever a robot finishes a pickup operation.
        /// </summary>
        /// <param name="robotID">The robot ID.</param>
        /// <param name="success">Indicates whether the robot was successful.</param>
        public void RobotPickupFinishedCallback(int robotID, bool success)
        {
            // Notify instance about finished pickup operation (catch and log possible exceptions)
            try { _instance.GetBotByID(robotID).OnPickedUpPod(); }
            catch (Exception ex)
            {
                _instance.LogSevere(
                    "Error when remotely finishing pickup operation of bot " + robotID.ToString() +
                    ": " + ex.Message);
            }
        }
        /// <summary>
        /// This method is called whenever a robot finishes a setdown operation.
        /// </summary>
        /// <param name="robotID">The robot ID.</param>
        /// <param name="success">Indicates whether the robot was successful.</param>
        public void RobotSetdownFinishedCallback(int robotID, bool success)
        {
            // Notify instance about finished setdown operation (catch and log possible exceptions)
            try { _instance.GetBotByID(robotID).OnSetDownPod(); }
            catch (Exception ex)
            {
                _instance.LogSevere(
                    "Error when remotely finishing setdown operation of bot " + robotID.ToString() +
                    ": " + ex.Message);
            }
        }
    }
}
