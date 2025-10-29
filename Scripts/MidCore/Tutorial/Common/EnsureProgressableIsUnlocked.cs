namespace Kuantech.Midcore.Tutorial.Common
{
    public class EnsureProgressableIsUnlocked : TutorialTask
    {
        public ProgressableDataAsset ProgressableDataAsset;
        public int MinRank;

        public override void StartTask()
        {
            base.StartTask();
            if (!ProgressionManager.IsProgressibleUnlocked(ProgressableDataAsset))
            {
                ProgressionManager.UnlockProgressible(ProgressableDataAsset);
            }

            int currRank = ProgressionManager.GetCurrentRank(ProgressableDataAsset);
            if (currRank < MinRank)
            {
                ProgressionManager.SetRank(ProgressableDataAsset, MinRank);
            }
            CompleteTask();
        }
    }
}