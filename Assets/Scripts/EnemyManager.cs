using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DungeonBuilder
{
    public class EnemyManager : MonoBehaviour
    {
        [SerializeField]
        private EnemyView _enemyViewPrefab;

        private List<Enemy> _enemies;

        public List<Enemy> Enemies => _enemies;

        public void Initialize()
        {
            _enemies = new List<Enemy>();
        }

        public void PutMino(Mino mino)
        {
            var enemy = mino.Enemy;
            if (enemy == null) return;

            enemy.FieldPos = mino.FieldPos + enemy.FieldPos;
            Enemies.Add(enemy);
        }
    }
}
