using Setting;

public class MonoInstaller : Zenject.MonoInstaller
{
    public override void InstallBindings()
    {
        Container.Bind<Global>().AsSingle();   
        
        Container.Bind<GameData>().AsSingle();
    }
}