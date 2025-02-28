using UnityEngine;

namespace Zenject.Tests.Factories.BindFactoryOne
{
//[CreateAssetMenu(fileName = "Bar", menuName = "Installers/Bar")]
public class Bar : ScriptableObject
{
    public string Value { get; private set; }

    [Inject]
    public void Init(string value)
    {
        Value = value;
    }

    public class Factory : PlaceholderFactory<string, Bar>
    {
    }
}
}