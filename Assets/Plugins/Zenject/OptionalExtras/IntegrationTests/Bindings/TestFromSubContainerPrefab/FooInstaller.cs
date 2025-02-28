using UnityEngine;

#pragma warning disable 649

namespace Zenject.Tests.Bindings.FromSubContainerPrefab
{
public class FooInstaller : MonoInstaller
{
    [SerializeField] private Bar _bar;

    public override void InstallBindings()
    {
        Container.BindInstance(_bar);
        Container.Bind<Gorp>().WithId("gorp").AsSingle();
    }
}
}