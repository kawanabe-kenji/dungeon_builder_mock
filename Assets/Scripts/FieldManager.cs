using System.Collections.Generic;
using UnityEngine;

namespace DungeonBuilder
{
    public class FieldManager
    {
        private static readonly Dictionary<(int x, int y), Block.DirectionType[]> BASE_SPACE = new Dictionary<(int x, int y), Block.DirectionType[]>() {
            { (-1, -1), new Block.DirectionType[] { Block.DirectionType.BACK, Block.DirectionType.LEFT } },
            { (0, -1), null },
            { (1, -1), new Block.DirectionType[] { Block.DirectionType.BACK, Block.DirectionType.RIGHT } },
            { (-1, 0), null },
            { (0, 0), null },
            { (1, 0), null },
            { (-1, 1), new Block.DirectionType[] { Block.DirectionType.FRONT, Block.DirectionType.LEFT } },
            { (0, 1), null },
            { (1, 1), new Block.DirectionType[] { Block.DirectionType.FRONT, Block.DirectionType.RIGHT } },
        };

        private Vector2Int _fieldSize;

        public Vector2Int FieldSize => _fieldSize;

        private Block[,] _field;

        public Block[,] Blocks => _field;

        private Mino _pickedMino;

        public Mino PickedMino => _pickedMino;

        private Mino[] _pickableMinos;

        public Mino[] PickableMinos => _pickableMinos;

        private int[] _pickableMinoRotateCounts;

        public int[] PickableMinoRotateCounts => _pickableMinoRotateCounts;

        private bool _isPutKey;

        public Block GetBlock(Vector2Int fieldPos)
        {
            if (fieldPos.x < 0 || fieldPos.x >= FieldSize.x || fieldPos.y < 0 || fieldPos.y >= FieldSize.y) return null;
            return _field[fieldPos.x, fieldPos.y];
        }

        public FieldManager(Vector2Int fieldSize, Vector2Int startPos, int pickableMinoCount)
        {
            _fieldSize = fieldSize;
            _field = new Block[FieldSize.x, FieldSize.y];
            _pickableMinos = new Mino[pickableMinoCount];
            for (int i = 0; i < _pickableMinos.Length; i++)
            {
                _pickableMinos[i] = Mino.Create(Mino.RandomShapeType());
            }
            // ゲーム開始スペース
            foreach (var kvp in BASE_SPACE)
            {
                var offset = kvp.Key;
                var block = new Block();
                _field[startPos.x + offset.x, startPos.y + offset.y] = block;
                if (kvp.Value == null) continue;
                foreach (var dir in kvp.Value)
                {
                    block.Walls[(int)dir] = true;
                }
            }
            _pickableMinoRotateCounts = new int[pickableMinoCount];
        }

        public bool CanPutMino(Mino mino, int x, int y)
        {
            foreach (var kvp in mino.Blocks)
            {
                var offset = kvp.Key;
                var fieldPos = new Vector2Int(x + offset.x, y + offset.y);
                // 指定したミノを指定した位置に置いたとき、壁や地面にぶつかっているかどうか
                if (fieldPos.x < 0 || fieldPos.y < 0 || fieldPos.x >= FieldSize.x) return false;
                // 指定したミノを指定した位置に置いたとき、いずれかのブロックが設置済みブロックにぶつかっているかどうか
                if (GetBlock(fieldPos) != null) return false;
            }
            return true;
        }

        public bool CanPutMino(Mino mino)
        {
            return CanPutMino(mino, mino.X, mino.Y);
        }

        public void PutMino(Mino mino)
        {
            // ミノと隣接するブロックの壁チェック
            foreach (var kvp in mino.Blocks)
            {
                var offset = kvp.Key;
                var block = kvp.Value;
                for (int i = 0; i < (int)Block.DirectionType.Max; i++)
                {
                    var dir = Block.AROUND_OFFSET[i];
                    // ミノ内の隣接は無視
                    if (mino.Blocks.ContainsKey((offset.x + dir.x, offset.y + dir.y))) continue;

                    Vector2Int fieldPos = new Vector2Int(mino.Index.x + offset.x + dir.x, mino.Index.y + offset.y + dir.y);
                    // 盤面外は無視
                    if (fieldPos.x < 0 || fieldPos.x >= FieldSize.x || fieldPos.y < 0 || fieldPos.y >= FieldSize.y) continue;

                    var fieldBlock = GetBlock(fieldPos);
                    // ブロックのない場所は無視
                    if (fieldBlock == null) continue;

                    // 隣接したブロックのどちらかが空いていれば道にする
                    var reverseDir = Block.GetReverseDirection((Block.DirectionType)i);
                    if (!block.Walls[i])
                    {
                        fieldBlock.Walls[(int)reverseDir] = false;
                    }
                    else if (!fieldBlock.Walls[(int)reverseDir])
                    {
                        block.Walls[i] = false;
                    }
                }
            }

            foreach (var kvp in mino.Blocks)
            {
                var offset = kvp.Key;
                Block block = kvp.Value;
                int x = mino.X + offset.x;
                int y = mino.Y + offset.y;
                _field[x, y] = block;
            }
        }

        public void HighlightLine(Vector2Int playerPos)
        {
            List<int> aligLines = new List<int>();
            for (int y = 0; y < FieldSize.y; y++)
            {
                int xCount = 0;
                for (int x = 0; x < FieldSize.x; x++)
                {
                    var block = _field[x, y];
                    if (block == null) continue;
                    block.IsIlluminated = false;
                    xCount++;
                }
                if (xCount == FieldSize.x) aligLines.Add(y);
            }

            foreach (int y in aligLines)
            {
                for (int x = 0; x < FieldSize.x; x++)
                {
                    _field[x, y].IsIlluminated = true;
                }
            }

            GetBlock(playerPos).IsIlluminated = true;
            foreach (var offset in Block.EIGHT_AROUND_OFFSET)
            {
                Vector2Int fieldPos = new Vector2Int(playerPos.x + offset.x, playerPos.y + offset.y);
                var block = GetBlock(fieldPos);
                if (block != null) block.IsIlluminated = true;
            }
        }

        public void PickMino(int index)
        {
            _pickedMino = _pickableMinos[index];
        }

        public void ReleaseMino()
        {
            _pickedMino = null;
        }

        public Mino SpawnMino(int index, Mino.ShapeType shapeType)
        {
            var mino = Mino.Create(shapeType);
            _pickableMinos[index] = mino;

            if (!_isPutKey)
            {
                int blockCount = 0;
                foreach (var block in Blocks) if (block != null) blockCount++;
                if (blockCount > FieldSize.x * FieldSize.y * 0.3f)
                {
                    mino.PutKey();
                    _isPutKey = true;
                }
            }

            return mino;
        }

        public int PickableMinoRotate(int index)
        {
            var rotateCount = _pickableMinoRotateCounts[index] + 1;
            if (rotateCount >= 4) rotateCount = 0;
            _pickableMinoRotateCounts[index] = rotateCount;
            return rotateCount;
        }
    }
}
