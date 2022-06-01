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

        public void Initialize()
        {
            _enemies = new List<Enemy>();
        }
    }
}
