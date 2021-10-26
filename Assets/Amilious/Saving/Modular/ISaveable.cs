namespace Amilious.Saving {
    
    /// <summary>
    /// This interface is used to make something saveable using
    /// the saving system.
    /// </summary>
    public interface ISaveable {
        
        /// <summary>
        /// This method is called when saving.
        /// </summary>
        /// <param name="saveData">The object that data should be added to.</param>
        void CaptureState(SaveData saveData);

        /// <summary>
        /// This method is called when loading.
        /// </summary>
        /// <param name="saveData">The object that contains the data that should
        /// be loaded.</param>
        void RestoreState(SaveData saveData);

        /// <summary>
        /// This method is called when loading and there
        /// is not a last save for this object.
        /// </summary>
        /// <param name="missingType">The type of data
        /// that was missing.</param>
        void MissingState(MissingStateType missingType);

        /// <summary>
        /// This method is used to check if loading and saving
        /// is enabled on an ISaveable component.
        /// </summary>
        /// <returns>True if loading and saving is enabled, otherwise
        /// returns false.</returns>
        bool IsSavingAndLoadingEnabled();
        
    }
}