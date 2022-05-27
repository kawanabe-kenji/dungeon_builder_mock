using UnityEngine;

namespace DungeonBuilder
{
    public class Node
    {
        private Vector2Int _position;

        public Vector2Int Position => _position;

        /// <summary>スタート地点からの道のり、実コスト</summary>
        public int Step;

        /// <summary>ゴールまでの距離、推定コスト</summary>
        public int Distance;

        /// <summary>ノードの経路効率、スコア</summary>
        public int Weight => Distance + Step;

        /// <summary>1つ前のマス、親ノード</summary>
        public Vector2Int Previous;

        public static int GetDistance(Vector2Int a, Vector2Int b)
        {
            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
        }

        public Node(int x, int y)
        {
            _position = new Vector2Int(x, y);
            Initialize();
        }

        public void Initialize()
        {
            Step = -1;
            Distance = -1;
            Previous = -Vector2Int.one;
        }

        public void Score(Vector2Int goal, Node previous)
        {
            if (previous != null)
            {
                Step = previous.Step + 1;
                Previous = previous.Position;
            }
            else
            {
                Step = 0;
                Previous = -Vector2Int.one;
            }
            Distance = GetDistance(Position, goal);
        }
    }
}
