namespace QCDC.Core
{
    /// <summary>
    /// Defines the standard rules for any state within the training application.
    /// </summary>
    public interface ITrainingState
    {
        // Triggers once when the state first starts
        void Enter(ARTrainingManager context);

        // Runs continuously while the state is active
        void Update(ARTrainingManager context);

        // Triggers once right before the state ends
        void Exit(ARTrainingManager context);
    }
}