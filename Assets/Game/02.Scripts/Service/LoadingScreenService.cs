using JumJump.Presenter;

namespace JumJump.Service
{
    public sealed class LoadingScreenService
    {
        private readonly UI_Loading _view;
        private readonly UnityEngine.Camera _gameCamera;

        public LoadingScreenService(
            UI_Loading view,
            UnityEngine.Camera gameCamera)
        {
            _view = view;
            _gameCamera = gameCamera;
        }

        public void Show()
        {
            _view.Show(_gameCamera);
        }

        public void SetProgress(float ratio)
        {
            _view.SetProgress(ratio);
        }

        public void Hide()
        {
            _view.Hide();
        }
    }
}
