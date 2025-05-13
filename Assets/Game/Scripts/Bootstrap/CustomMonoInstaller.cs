using Setting;

public class CustomMonoInstaller : Zenject.MonoInstaller
{
    public override void InstallBindings()
    {
        Container.Bind<Global>().AsSingle();   
        
        Container.Bind<GameData>().AsSingle();
    }
}