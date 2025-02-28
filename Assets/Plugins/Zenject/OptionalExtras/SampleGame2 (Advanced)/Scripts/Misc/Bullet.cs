using UnityEngine;

namespace Zenject.SpaceFighter
{
public enum BulletTypes
{
    FromEnemy,
    FromPlayer
}

public class Bullet : MonoBehaviour, IPoolable<float, float, BulletTypes, IMemoryPool>
{
    [SerializeField] private MeshRenderer _renderer;

    [SerializeField] private Material _playerMaterial;

    [SerializeField] private Material _enemyMaterial;

    private float _lifeTime;

    private IMemoryPool _pool;
    private float _speed;
    private float _startTime;

    public BulletTypes Type { get; private set; }

    public Vector3 MoveDirection => transform.right;

    public void Update()
    {
        transform.position -= transform.right * _speed * Time.deltaTime;

        if (Time.realtimeSinceStartup - _startTime > _lifeTime) _pool.Despawn(this);
    }

    public void OnTriggerEnter(Collider other)
    {
        var enemyView = other.GetComponent<EnemyView>();

        if (enemyView != null && Type == BulletTypes.FromPlayer)
        {
            enemyView.Facade.Die();
            _pool.Despawn(this);
        }
        else
        {
            var player = other.GetComponent<PlayerFacade>();

            if (player != null && Type == BulletTypes.FromEnemy)
            {
                player.TakeDamage(MoveDirection);
                _pool.Despawn(this);
            }
        }
    }

    public void OnSpawned(float speed, float lifeTime, BulletTypes type, IMemoryPool pool)
    {
        _pool = pool;
        Type = type;
        _speed = speed;
        _lifeTime = lifeTime;

        _renderer.material = type == BulletTypes.FromEnemy ? _enemyMaterial : _playerMaterial;

        _startTime = Time.realtimeSinceStartup;
    }

    public void OnDespawned()
    {
        _pool = null;
    }

    public class Factory : PlaceholderFactory<float, float, BulletTypes, Bullet>
    {
    }
}
}