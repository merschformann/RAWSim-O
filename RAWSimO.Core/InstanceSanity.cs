using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAWSimO.Core
{
    public partial class Instance
    {
        /// <summary>
        /// Contains all error types that sanity checking is overwatching.
        /// </summary>
        public enum SanityError
        {
            /// <summary>
            /// No outgoing connection from a waypoint.
            /// </summary>
            DeadEnd,
            /// <summary>
            /// No outgoing unblockable connection from a waypoint.
            /// </summary>
            DeadEndByBlocking,
            /// <summary>
            /// A storage location that might be blocked throughout the simulation by other pods.
            /// </summary>
            BlockableStorageLocation,
            /// <summary>
            /// There are not enough resting positions for all bots of the instance.
            /// </summary>
            InsufficientRestingPositions,
        }
        /// <summary>
        /// Contains some description to better clarify all errors that might be detected.
        /// </summary>
        private Dictionary<SanityError, string> _sanityErrorDescriptions = new Dictionary<SanityError, string>
        {
            { SanityError.DeadEnd, "No outgoing connection from at least one waypoint detected. Ensure that the graph is completely connected." },
            { SanityError.DeadEndByBlocking, "No unblockable outgoing connection from a waypoint detected. Ensure that the graph is completely connected without relying on storage location waypoints." },
            { SanityError.BlockableStorageLocation, "Detected a storage locations that might be blocked throughout simulation. Ensure that storage locations cannot be blocked by pods, i.e. have a bi-directional connection to an aisle." },
            { SanityError.InsufficientRestingPositions, "Insufficient amount of resting positions for all robots. Remove some pods or some robots." },
        };
        /// <summary>
        /// Returns a describing string for the corresponding error type.
        /// </summary>
        /// <param name="errorType">The error type.</param>
        /// <returns>A string describing the error.</returns>
        public string SanityGetDescription(SanityError errorType) { return _sanityErrorDescriptions[errorType]; }
        /// <summary>
        /// Sanity checks the instance.
        /// </summary>
        /// <returns>All errors that were recognized.</returns>
        public List<Tuple<SanityError, string>> SanityCheck()
        {
            List<Tuple<SanityError, string>> errors = new List<Tuple<SanityError, string>>();
            // Check dead end waypoints
            foreach (var waypoint in Waypoints)
                if (!waypoint.Paths.Any())
                    errors.Add(new Tuple<SanityError, string>(SanityError.DeadEnd, waypoint.ToString()));
            // Check dead end waypoints by blocking
            foreach (var waypoint in Waypoints)
                if (!waypoint.Paths.Any(other => !other.PodStorageLocation))
                    errors.Add(new Tuple<SanityError, string>(SanityError.DeadEnd, waypoint.ToString()));
            // Check blockable storage locations
            foreach (var storageLocation in Waypoints.Where(w => w.PodStorageLocation))
                if (!Waypoints.Any(otherWP => !otherWP.PodStorageLocation && otherWP.ContainsPath(storageLocation)))
                    errors.Add(new Tuple<SanityError, string>(SanityError.BlockableStorageLocation, storageLocation.ToString()));
            // Check resting positions
            int storageLocations = Waypoints.Count(w => w.PodStorageLocation);
            if (storageLocations - Pods.Count < Bots.Count)
                errors.Add(new Tuple<SanityError, string>(SanityError.InsufficientRestingPositions, "StorageLocations-Pods<Bots:" + storageLocations + "-" + Pods.Count + "<" + Bots.Count));
            // Return the list
            return errors;
        }
    }
}
