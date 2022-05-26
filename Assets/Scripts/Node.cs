using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DungeonBuilder
{
    public class Node
    {
        private (int x, int y) _position;

        public (int x, int y) Position => _position;

        /// <summary>スタート地点からの道のり、実コスト</summary>
        public int Step;

        /// <summary>ゴールまでの距離、推定コスト</summary>
        public int Distance;

        /// <summary>ノードの経路効率、スコア</summary>
        public int Weight => Distance + Step;

        /// <summary>1つ前のマス、親ノード</summary>
        public (int x, int y) Previous;

        public static int GetDistance((int x, int y) a, (int x, int y) b)
        {
            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
        }

        public Node(int x, int y)
        {
            _position = (x, y);
            Initialize();
        }

        public void Initialize()
        {
            Step = -1;
            Distance = -1;
            Previous = (-1, -1);
        }

        public void Score((int x, int y) goal, Node previous)
        {
            if (previous != null)
            {
                Step = previous.Step + 1;
                Previous = previous.Position;
            }
            else
            {
                Step = 0;
                Previous = (-1, -1);
            }
            Distance = GetDistance(Position, goal);
        }
    }
}