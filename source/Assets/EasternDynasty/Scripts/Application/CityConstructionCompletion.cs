namespace EasternDynasty
{
    /// <summary>Immutable session-only evidence of a construction completed by Core.</summary>
    public sealed class CityConstructionCompletion
    {
        public readonly int plotIndex, previousLevel, targetLevel, previousEffect, completedEffect;
        public readonly CityBuilding building;
        public readonly bool completedWhileAway;

        internal CityConstructionCompletion(int plotIndex, CityBuilding building, int previousLevel, int targetLevel, bool completedWhileAway)
        {
            this.plotIndex = plotIndex; this.building = building;
            this.previousLevel = previousLevel; this.targetLevel = targetLevel;
            previousEffect = CityCatalog.EffectAtLevel(building, previousLevel);
            completedEffect = CityCatalog.EffectAtLevel(building, targetLevel);
            this.completedWhileAway = completedWhileAway;
        }
    }
}
