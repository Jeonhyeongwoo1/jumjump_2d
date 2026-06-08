namespace JumJump.Interface
{
    public interface IGameFlowState
    {
        GameStateType StateType { get; }

        void OnEnter();
        void OnUpdate(IGameFlowStateContext context);
        void OnExit();
        void OnTapRequested(IGameFlowStateContext context);
        void OnPlayerMissedLanding(IGameFlowStateContext context);
        void OnRestartRequested(IGameFlowStateContext context);
    }
}
