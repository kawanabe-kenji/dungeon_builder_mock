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

        private BlockUIView GetBlock(int x, int y)
        {
            if (x < 0 || y < 0 || x >= _blocks.GetLength(0) ||y >= _blocks.GetLength(1)) return null;
            return _blocks[x, y];
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

        public void Reflesh(Block[,] fieldData)
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
                }
            }
        }

        public void DrawMino(Mino mino)
        {
            if (mino == null) return;

            foreach (var kvp in mino.Blocks)
            {
                var offset = kvp.Key;
                var view = GetBlock(mino.X + offset.x, mino.Y + offset.y);
                if (view == null) continue;

                var data = kvp.Value;
                view.Panel.enabled = data != null;
                for (int i = 0; i < view.Walls.Length; i++)
                {
                    view.Walls[i].enabled = view.Panel.enabled && data.Walls[i];
                }
            }
        }
    }
}
