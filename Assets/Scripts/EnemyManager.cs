using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DungeonBuilder
{
    public class EnemyManager : MonoBehaviour
    {

        private List<Enemy> _enemies;

        public List<Enemy> Enemies => _enemies;

        [SerializeField]
        private EnemyView _enemyViewPrefab;

        private List<EnemyView> _enemyViews;

        public List<EnemyView> EnemyViews => _enemyViews;

        [SerializeField]
        private Transform _viewParent;

        public void Initialize()
        {
            _enemies = new List<Enemy>();
            _enemyViews = new List<EnemyView>();
        }

        public void PutMino(Mino mino)
        {
            var enemy = mino.Enemy;
            if (enemy == null) return;

            enemy.FieldPos = mino.FieldPos + enemy.FieldPos;
            Enemies.Add(enemy);

            var enemyView = CreateEnemyView(enemy);
            enemyView.transform.GetChild(0).gameObject.SetActive(false);
        }

        private EnemyView CreateEnemyView(Enemy enemy)
        {
            var enemyView = Instantiate(_enemyViewPrefab, _viewParent);
            _enemyViews.Add(enemyView);

            var worldPos = FieldView.GetWorldPosition(enemy.FieldPos) + Vector3.back;
            enemyView.transform.localPosition = worldPos;

            enemyView.lookAngles = Vector3.up * Random.Range(0, 3) * 90f;
            return enemyView;
        }

        public void RemoveEnemy(Enemy enemy)
        {
            var index = Enemies.IndexOf(enemy);
            Enemies.RemoveAt(index);

            var view = EnemyViews[index];
            Destroy(view.gameObject);
            EnemyViews.RemoveAt(index);
        }

        public void HighlightLine(Block[,] fieldData)
        {
            for(int i = 0; i < Enemies.Count; i++)
			{
                var fieldPos = Enemies[i].FieldPos;
                var block = fieldData[fieldPos.x, fieldPos.y];
                EnemyViews[0].gameObject.SetActive(block.IsIlluminated);
			}
        }

        public EnemyView GetView(Enemy enemy)
		{
            var index = Enemies.IndexOf(enemy);
            return EnemyViews[index];
        }
    }
}
