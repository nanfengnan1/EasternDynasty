namespace EasternDynasty
{
    internal enum MobileBackAction { CancelConfirmation, CloseCard, CloseDraw, Recruit, Map, ClearLand, ConfirmExit, TrainingOrigin, TrainingPlan, CityOverview, CityFormationOrigin, BattleReports, BattleMap, CloseQuestReward, ReinforcementOrigin, ReinforcementPlan, CityQuestObjective }
    internal static class MobileNavigation
    {
        internal static MobileBackAction Back(bool confirmation,bool card,bool draw,string tab,int selected,bool trainingFormation=false,string cityPage="overview",bool cityFormation=false,bool questReward=false,bool reinforcementFormation=false,bool trainingHistory=false,bool cityPlanning=false,bool cityExpanded=false,bool questLandOrigin=false)
        {
            if(confirmation)return MobileBackAction.CancelConfirmation;
            if(card)return MobileBackAction.CloseCard;
            if(draw)return MobileBackAction.CloseDraw;
            if(questReward)return MobileBackAction.CloseQuestReward;
            if(tab=="领地"&&questLandOrigin)return MobileBackAction.CityQuestObjective;
            if(tab=="征兵")return MobileBackAction.ReinforcementOrigin;
            if(tab=="武将"&&reinforcementFormation)return MobileBackAction.ReinforcementPlan;
            if(tab=="战斗回放")return MobileBackAction.BattleReports;
            if(tab=="战斗等待")return MobileBackAction.BattleMap;
            if(tab=="武将"&&trainingFormation)return MobileBackAction.TrainingPlan;
            if(tab=="练兵")return trainingHistory ? MobileBackAction.TrainingPlan : MobileBackAction.TrainingOrigin;
            if(tab=="武将"&&cityFormation)return MobileBackAction.CityFormationOrigin;
            if(tab=="主城"&&(cityPage!="overview"||cityPlanning||cityExpanded))return MobileBackAction.CityOverview;
            if(tab=="图鉴")return MobileBackAction.Recruit;
            if(tab!="领地")return MobileBackAction.Map;
            if(selected>=0)return MobileBackAction.ClearLand;
            return MobileBackAction.ConfirmExit;
        }
    }
}
