using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DungeonBuilder
{
    public class FieldView : MonoBehaviour
    {
        private const float CELL_SIZE = 4f;

        [SerializeField]
        private MinoView[] _minoPrefabs;

        private MinoView _currentView;

        [SerializeField]
        private Transform _minoViewParent;

        private MinoView.Block[,] _blocks;

        [SerializeField]
        private MinoView.Block[] _startBlocks;

        [SerializeField]
        private ParticleSystem _fogs;

        public void Initialize(Vector2Int fieldSize, Vector2Int startPos)
        {
            _blocks = new MinoView.Block[fieldSize.x, fieldSize.y];

            // スタート地点の壁情報
            _blocks[startPos.x, startPos.y] = _startBlocks[0];
            for (int i = 0; i < Block.EIGHT_AROUND_OFFSET.Length; i++)
            {
                var offset = Block.EIGHT_AROUND_OFFSET[i];
                (int x, int y) index = (startPos.x + offset.x, startPos.y + offset.y);
                _blocks[index.x, index.y] = _startBlocks[i + 1];
            }

            _fogs.Simulate(100f);
            _fogs.Play();
        }

        public void SetMinoPosition(Vector2Int fieldPos)
        {
            if (_currentView == null) return;
            _currentView.transform.localPosition = new Vector3(fieldPos.x, 0f, fieldPos.y) * CELL_SIZE;
        }

        public void PutMino(Mino mino)
        {
            int count = 0;
            foreach (var kvp in mino.Blocks)
            {
                var offset = kvp.Key;
                var x = mino.X + offset.x;
                var y = mino.Y + offset.y;

                var view = _currentView.Blocks[count++];
                _blocks[x, y] = view;

                view.Fog.Play();
            }
        }
    }
}
