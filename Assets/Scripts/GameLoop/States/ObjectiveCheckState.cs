namespace DunGemCrawler
{
    public class ObjectiveCheckState : GameStateBase
    {
        public override void OnEnter()
        {
            LevelConfig level = Board.CurrentLevel;
            if (level == null) { FSM.Enter<IdleState>(); return; }

            switch (level.Objective)
            {
                case LevelObjectiveType.ScoreThreshold:
                    if (Board.Score >= level.TargetScore)
                    {
                        FSM.Enter<RoomExitState>();
                        return;
                    }
                    break;

                case LevelObjectiveType.KeystoneDelivery:
                    if (Board.Data.InBounds(level.GoalCell))
                    {
                        GemData gem = Board.Data.GetGem(level.GoalCell);
                        // Frozen gems don't count — they must be freed first
                        if (gem != null && gem.Color == level.KeystoneColor
                            && gem.Modifier?.Type != GemModifierType.Frozen)
                        {
                            FSM.Enter<RoomExitState>();
                            return;
                        }
                    }
                    break;

                case LevelObjectiveType.ReachDoor:
                default:
                    break;
            }

            FSM.Enter<IdleState>();
        }
    }
}
