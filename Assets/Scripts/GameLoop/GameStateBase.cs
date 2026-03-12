namespace DunGemCrawler
{
    public abstract class GameStateBase
    {
        protected GameStateMachine FSM;
        protected BoardManager Board;

        public void Setup(GameStateMachine fsm, BoardManager board)
        {
            FSM = fsm;
            Board = board;
        }

        public virtual void OnEnter() { }
        public virtual void OnExit() { }
        public virtual void Tick() { }
    }
}
