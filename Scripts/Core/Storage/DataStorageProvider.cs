using System;

namespace Kuantech.Core
{
    [Serializable]
    public abstract class DataStorageProvider
    {
        public abstract string Id { get; }

        /// <summary>
        /// True if there is unsaved data that needs to be flushed.
        /// </summary>
        public abstract bool HasUnsavedChanges { get; }

        /// <summary>
        /// Initializes the provider and loads data from the source.
        /// Binary: reads file into cache. Database: establishes connection.
        /// </summary>
        public abstract void LoadData();

        /// <summary>
        /// Flushes unsaved changes to the storage backend.
        /// Binary: writes cache to disk. Database: executes queued queries.
        /// </summary>
        public abstract void SaveChanges();

        /// <summary>
        /// Clears all stored data.
        /// </summary>
        public abstract void Clear();
    }
}
