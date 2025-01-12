using Setting;

public class MonoInstaller : Zenject.MonoInstaller
{
    public override void InstallBindings()
    {
        Container.Bind<Settings>().AsSingle();   
        
        Container.Bind<GameData>().AsSingle();
    }
}