using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DungeonBuilder
{
    public class Mino
    {
        #region Constants
        public enum ShapeType
        {
            /// <summary> 長い棒 </summary>
            A = 0,
            /// <summary> 正方形 </summary>
            B,
            /// <summary> S字 </summary>
            C1,
            /// <summary> S字(反転) </summary>
            C2,
            /// <summary> L字 </summary>
            D1,
            /// <summary> L字(反転) </summary>
            D2,
            /// <summary> T字 </summary>
            E,
            Max
        }

        private readonly static Dictionary<ShapeType, (int x, int y)[]> SHAPE_PATTERN = new Dictionary<ShapeType, (int x, int y)[]>()
        {
            { ShapeType.A, new (int x, int y)[] { (-1, 0), (1, 0), (2, 0) } },
            { ShapeType.B, new (int x, int y)[] { (0, 1), (1, 0), (1, 1) } },
            { ShapeType.C1, new (int x, int y)[] { (-1, 0), (0, 1), (1, 1) } },
            { ShapeType.C2, new (int x, int y)[] { (-1, 1), (0, 1), (1, 0) } },
            { ShapeType.D1, new (int x, int y)[] { (-1, 0), (1, 0), (1, 1) } },
            { ShapeType.D2, new (int x, int y)[] { (-1, 1), (-1, 0), (1, 0) } },
            { ShapeType.E, new (int x, int y)[] { (-1, 0), (1, 0), (0, 1) } },
        };
        #endregion // Constant

        #region Variables
        private Dictionary<(int x, int y), Block> _blocks = new Dictionary<(int x, int y), Block>();

        public Dictionary<(int x, int y), Block> Blocks => _blocks;

        private (int x, int y) _index;

        public (int x, int y) Index
        {
            get => _index;
            set => _index = value;
        }

        public int X
        {
            get => _index.x;
            set => _index = (value, _index.y);
        }

        public int Y
        {
            get => _index.y;
            set => _index = (_index.x, value);
        }

        private ShapeType _type;

        public ShapeType Type => _type;
        #endregion // Variables

        public static Mino Create(ShapeType type)
        {
            var mino = new Mino();
            mino.Respawn(type);
            return mino;
        }

        public static ShapeType RandomShapeType()
        {
            return (ShapeType)Random.Range(0, (int)ShapeType.Max);
        }

        public void Respawn(ShapeType type)
        {
            _type = type;

            Blocks.Clear();
            Blocks.Add((0, 0), new Block());

            var indexes = SHAPE_PATTERN[type];
            foreach (var index in indexes)
            {
                Blocks.Add(index, new Block());
            }
            CreateWalls();
        }

        private void CreateWalls()
        {
            List<(Block block, int wallIndex)> minoWalls = new List<(Block block, int wallIndex)>();

            // ミノの周囲を囲むように壁を作成
            foreach (var kvp in Blocks)
            {
                var index = kvp.Key;
                var block = kvp.Value;
                for(int i = 0; i < (int)Block.DirectionType.Max; i++)
                {
                    // チェックした方向にブロックがなければ壁を作成
                    var offset = Block.AROUND_OFFSET[i];
                    if (!Blocks.ContainsKey((index.x + offset.x, index.y + offset.y)))
                    {
                        block.Walls[i] = true;
                        minoWalls.Add((block, i));
                    }
                }
            }

            // ランダムな位置に2つ穴を開ける
            int count = 2;
            while (count-- > 0)
            {
                int index = Random.Range(0, minoWalls.Count);
                var block = minoWalls[index].block;
                var wallIndex = minoWalls[index].wallIndex;
                block.Walls[wallIndex] = false;
                minoWalls.RemoveAt(index);
            }
        }

        public void Rotate()
        {
            var newBlocks = new Dictionary<(int x, int y), Block>();
            foreach (var kvp in Blocks)
            {
                var index = kvp.Key;
                var block = kvp.Value;
                // 回転に合わせてブロックの相対的な位置を変える
                newBlocks.Add((index.y, -index.x), block);

                // 回転に合わせて壁の情報も更新
                var lastWall = block.Walls[block.Walls.Length - 1];
                for (int i = block.Walls.Length - 1; i > 0; i--)
                {
                    block.Walls[i] = block.Walls[i - 1];
                }
                block.Walls[0] = lastWall;
            }
            Blocks.Clear();
            _blocks = newBlocks;
        }

        public void PutKey()
		{
            var block = Blocks.Values.ElementAt(Random.Range(0, Blocks.Values.Count));
            block.HasKey = true;
        }
    }
}
