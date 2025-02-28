namespace Zenject.Tests.Factories.BindFactoryFive
{
public class FooInstaller : MonoInstaller
{
    private double _param1;
    private int _param2;
    private float _param3;
    private string _param4;
    private char _param5;

    [Inject]
    public void Init(double p1, int p2, float p3, string p4, char p5)
    {
        _param1 = p1;
        _param2 = p2;
        _param3 = p3;
        _param4 = p4;
        _param5 = p5;
    }

    public override void InstallBindings()
    {
        Container.BindInstance(_param1).WhenInjectedInto<Foo>();
        Container.BindInstance(_param2).WhenInjectedInto<Foo>();
        Container.BindInstance(_param3).WhenInjectedInto<Foo>();
        Container.BindInstance(_param4).WhenInjectedInto<Foo>();
        Container.BindInstance(_param5).WhenInjectedInto<Foo>();

        Container.Bind<Foo>().FromNewComponentOnNewGameObject().AsTransient();
    }
}
}