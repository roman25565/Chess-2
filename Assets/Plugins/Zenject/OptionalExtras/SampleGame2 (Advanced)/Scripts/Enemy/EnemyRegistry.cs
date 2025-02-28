using System.Collections.Generic;

namespace Zenject.SpaceFighter
{
public class EnemyRegistry
{
    private readonly List<EnemyFacade> _enemies = new();

    public IEnumerable<EnemyFacade> Enemies => _enemies;

    public void AddEnemy(EnemyFacade enemy)
    {
        _enemies.Add(enemy);
    }

    public void RemoveEnemy(EnemyFacade enemy)
    {
        _enemies.Remove(enemy);
    }
}
}