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
        private EnemyView[] _enemyViewPrefabs;

        private List<EnemyView> _enemyViews;

        public List<EnemyView> EnemyViews => _enemyViews;

        [SerializeField]
        private Transform _viewParent;

        private FieldHUDManager _fieldHUDMgr;

        public void Initialize(FieldHUDManager fieldHUDMgr)
        {
            _enemies = new List<Enemy>();
            _enemyViews = new List<EnemyView>();
            _fieldHUDMgr = fieldHUDMgr;
        }

        public void PutMino(Mino mino)
        {
            var enemy = mino.Enemy;
            if (enemy == null) return;

            var fieldPos = mino.FieldPos + enemy.FieldPos;
            AddEnemy(enemy, fieldPos);
        }

        public void AddEnemy(Enemy enemy, Vector2Int fieldPos)
        {
            enemy.FieldPos = fieldPos;
            Enemies.Add(enemy);

            var enemyView = CreateEnemyView(enemy);
            //enemyView.IsVisible = false;
        }

        private EnemyView CreateEnemyView(Enemy enemy)
        {
            var enemyView = Instantiate(_enemyViewPrefabs[enemy.LooksType], _viewParent);
            _enemyViews.Add(enemyView);

            var worldPos = FieldView.GetWorldPosition(enemy.FieldPos) + Vector3.back;
            enemyView.transform.localPosition = worldPos;

            enemyView.lookAngles = Vector3.up * Random.Range(0, 3) * 90f;

            //enemyView.UnknownView = _fieldHUDMgr.AddUnknownView(enemyView.gameObject, new Vector2(0f, 50f));

            return enemyView;
        }

        public void RemoveEnemy(Enemy enemy)
        {
            var index = Enemies.IndexOf(enemy);
            Enemies.RemoveAt(index);

            var view = EnemyViews[index];

            _fieldHUDMgr.RemoveUnknownView(view.gameObject);

            Destroy(view.gameObject);
            EnemyViews.RemoveAt(index);
        }

        public void HilightLine(Block[,] fieldData)
        {
            for(int i = 0; i < Enemies.Count; i++)
			{
                var fieldPos = Enemies[i].FieldPos;
                var block = fieldData[fieldPos.x, fieldPos.y];
                EnemyViews[i].IsVisible = block.IsIlluminated;
			}
        }

        public EnemyView GetView(Enemy enemy)
		{
            var index = Enemies.IndexOf(enemy);
            return EnemyViews[index];
        }

        public Enemy GetEnemy(Vector2Int fieldPos)
        {
            foreach (var enemy in Enemies) if (enemy.FieldPos == fieldPos) return enemy;
            return null;
        }
    }
}
