using UnityEngine;

namespace DungeonBuilder
{
    public class Block
    {
        public enum DirectionType
        {
            FRONT = 0,
            RIGHT,
            BACK,
            LEFT,
            Max
        }

        public readonly static Vector2Int[] AROUND_OFFSET = new Vector2Int[] {
            new Vector2Int(0, 1),
            new Vector2Int(1, 0),
            new Vector2Int(0, -1),
            new Vector2Int(-1, 0)
        };

        public readonly static Vector2Int[] EIGHT_AROUND_OFFSET = new Vector2Int[] {
            new Vector2Int(0, 1),
            new Vector2Int(1, 0),
            new Vector2Int(0, -1),
            new Vector2Int(-1, 0),
            new Vector2Int(1, 1),
            new Vector2Int(1, -1),
            new Vector2Int(-1, -1),
            new Vector2Int(-1, 1)
        };

        private bool[] _walls = new bool[(int)DirectionType.Max];

        public bool[] Walls => _walls;

        public bool IsIlluminated;

        public bool HasKey;

        public bool HasHealItem;

        public static DirectionType GetReverseDirection(DirectionType type)
        {
            switch (type)
            {
                case DirectionType.FRONT: return DirectionType.BACK;
                case DirectionType.RIGHT: return DirectionType.LEFT;
                case DirectionType.BACK: return DirectionType.FRONT;
                case DirectionType.LEFT: return DirectionType.RIGHT;
                default:
                    Debug.LogError("不正な方向タイプ: " + type);
                    return type;
            }
        }
    }
}
