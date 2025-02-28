using System.Threading.Tasks;

namespace Zenject.Tests.TestAnimationStateBehaviourInject
{
public class DelayedInitializeKernel : BaseMonoKernelDecorator
{
    public override async void Initialize()
    {
        await Task.Delay(5000);
        DecoratedMonoKernel.Initialize();
    }
}
}