namespace EasternDynasty
{
    /// <summary>Persistence boundary. Load returns an independent, validated World snapshot.</summary>
    public interface ICampaignRepository
    {
        bool Exists(bool youzhou);
        World Load(bool youzhou, out bool restoredBackup);
        void Save(World world);
    }
}
