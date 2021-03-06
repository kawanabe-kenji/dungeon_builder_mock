using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace DungeonBuilder
{
    /// <summary>
    /// UI上での盤面表示クラス
    /// </summary>
    public class FieldUIView : MonoBehaviour
    {
        [SerializeField]
        private BlockUIView _blockPrefab;

        [SerializeField]
        private GridLayoutGroup _layoutGroup;

        private BlockUIView[,] _blocks;

        private BlockUIView GetBlock(Vector2Int fieldPos)
        {
            if (fieldPos.x < 0 || fieldPos.y < 0 || fieldPos.x >= _blocks.GetLength(0) || fieldPos.y >= _blocks.GetLength(1)) return null;
            return _blocks[fieldPos.x, fieldPos.y];
        }

        public void Initialize(Vector2Int fieldSize)
        {
            var rectTransform = _layoutGroup.GetComponent<RectTransform>();

            _blocks = new BlockUIView[fieldSize.x, fieldSize.y];
            for(int y = 0; y < fieldSize.y; y++)
            {
                for (int x = 0; x < fieldSize.x; x++)
                {
                    var block = Instantiate(_blockPrefab, rectTransform);
                    _blocks[x, y] = block;
                    block.name = string.Format("Block [{0}, {1}]", x, y);
                    block.Panel.enabled = false;
                    foreach (var wall in block.Walls) wall.enabled = false;
                    block.Enemy.enabled = false;
                }
            }

            // デバッグ表示盤面のサイズ自動調整
            var cellSize = _layoutGroup.cellSize;
            var spacing = _layoutGroup.spacing;
            var padding = _layoutGroup.padding;
            rectTransform.sizeDelta = new Vector2(
                fieldSize.x * (cellSize.x + spacing.x) + padding.left,
                fieldSize.y * (cellSize.y + spacing.y) + padding.bottom
            );
        }

        public void Refresh(Block[,] fieldData, List<Enemy> enemies)
        {
            var fieldX = fieldData.GetLength(0);
            var fieldY = fieldData.GetLength(1);
            for (int x = 0; x < fieldX; x++)
            {
                for (int y = 0; y < fieldY; y++)
                {
                    var data = fieldData[x, y];
                    var view = _blocks[x, y];

                    view.Panel.enabled = data != null;
                    for (int i = 0; i < view.Walls.Length; i++)
                    {
                        view.Walls[i].enabled = view.Panel.enabled && data.Walls[i];
                    }

                    var fieldPos = new Vector2Int(x, y);
                    var enemy = enemies.FirstOrDefault(enemy => enemy.FieldPos == fieldPos);
                    view.Enemy.enabled = enemy != null;
                }
            }
        }

        public void DrawMino(Mino mino)
        {
            if (mino == null) return;

            foreach (var kvp in mino.Blocks)
            {
                var offset = kvp.Key;
                var view = GetBlock(mino.FieldPos + offset);
                if (view == null) continue;

                var data = kvp.Value;
                view.Panel.enabled = data != null;
                for (int i = 0; i < view.Walls.Length; i++)
                {
                    view.Walls[i].enabled = view.Panel.enabled && data.Walls[i];
                }

                view.Enemy.enabled = mino.Enemy != null && mino.Enemy.FieldPos == offset;
            }
        }
    }
}
