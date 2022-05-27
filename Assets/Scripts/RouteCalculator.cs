using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DungeonBuilder
{
    public class RouteCalculator
    {
        private Node[] _nodes;

        private Vector2Int fieldSize;

        private Node GetNode(Vector2Int fidldPos)
        {
            if (fidldPos.x < 0 || fidldPos.y < 0 || fidldPos.x >= fieldSize.x || fidldPos.y >= fieldSize.y) return null;
            return _nodes[fidldPos.y * fieldSize.x + fidldPos.x];
        }

        public RouteCalculator(Vector2Int fieldSize)
        {
            this.fieldSize = fieldSize;
            _nodes = new Node[fieldSize.x * fieldSize.y];
            for (int y = 0; y < fieldSize.y; y++)
            {
                for (int x = 0; x < fieldSize.x; x++)
                {
                    _nodes[y * fieldSize.x + x] = new Node(x, y);
                }
            }
        }

        public Vector2Int[] GetRoute(Vector2Int start, Vector2Int goal, Block[,] fieldData)
        {
            _nodes.ToList().ForEach(node => node.Initialize());

            var passedPositions = new List<Vector2Int>();
            var recentTargets = new List<Node>();
            var startNode = GetNode(start);
            startNode.Score(goal, null);
            passedPositions.Add(start);
            recentTargets.Add(GetNode(start));
            var adjacentInfos = new List<Node>();
            Node goalNode = null;

            while (true)
            {
                var currentTarget = recentTargets.OrderBy(info => info.Weight).FirstOrDefault();
                var currentPosition = currentTarget.Position;
                var currentBlock = GetBlock(fieldData, currentPosition);

                adjacentInfos.Clear();
                for (int i = 0; i < (int)Block.DirectionType.Max; i++)
                {
                    var offset = Block.AROUND_OFFSET[i];
                    Vector2Int targetPosition = new Vector2Int(currentPosition.x + offset.x, currentPosition.y + offset.y);
                    // 対象方向に対して移動できなければ対象外
                    if (currentBlock.Walls[i])
                    {
                        continue;
                    }
                    var reverseDir = Block.GetReverseDirection((Block.DirectionType)i);
                    var targetBlock = GetBlock(fieldData, targetPosition);
                    if (targetBlock == null || targetBlock.Walls[(int)reverseDir])
                    {
                        continue;
                    }

                    // 計算済みのセルは対象外
                    if (passedPositions.Contains(targetPosition))
                    {
                        continue;
                    }
                    var target = GetNode(targetPosition);
                    if (target == null)
                    {
                        continue;
                    }
                    target.Score(goal, GetNode(currentPosition));
                    adjacentInfos.Add(target);
                }

                // recentTargetsとpassedPositionsを更新
                recentTargets.Remove(currentTarget);
                recentTargets.AddRange(adjacentInfos);
                passedPositions.Add(currentPosition);

                // ゴールが含まれていたらそこで終了
                goalNode = adjacentInfos.FirstOrDefault(info => info.Position == goal);
                if (goalNode != null)
                {
                    break;
                }
                // recentTargetsがゼロだったら行き止まりなので終了
                if (recentTargets.Count == 0)
                {
                    break;
                }
            }

            // ゴールが結果に含まれていない場合は最短経路が見つからなかった
            if (goalNode == null)
            {
                return null;
            }

            // Previousを辿ってセルのリストを作成する
            var route = new List<Vector2Int>();
            route.Add(goal);
            var routeNode = goalNode;

            while (true)
            {
                if (routeNode.Step == 0)
                {
                    break;
                }
                route.Add(routeNode.Previous);
                routeNode = GetNode(routeNode.Previous);
            }
            route.Reverse();
            return route.ToArray();
        }

        private Block GetBlock(Block[,] data, Vector2Int position)
        {
            if (position.x < 0 || position.x >= data.GetLength(0) || position.y < 0 || position.y >= data.GetLength(1))
            {
                return null;
            }
            return data[position.x, position.y];
        }
    }
}