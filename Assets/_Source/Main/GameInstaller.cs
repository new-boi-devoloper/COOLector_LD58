using GameControllers;
using Zenject;

namespace Main
{
    public class GameInstaller : MonoInstaller
    {
        private GameManager _gameManager;
        private UiManager _uiManager;

        public override void InstallBindings()
        {
            Container.Bind<UiManager>().FromComponentInHierarchy().AsSingle().Lazy();
            Container.Bind<GameManager>().FromComponentInHierarchy().AsSingle().NonLazy();
        }
    }
}