using System.Collections;
using System.Collections.Generic;
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

        public readonly static (int x, int y)[] AROUND_OFFSET = new (int x, int y)[] { (0, 1), (1, 0), (0, -1), (-1, 0) };

        public readonly static (int x, int y)[] EIGHT_AROUND_OFFSET = new (int x, int y)[] { (0, 1), (1, 0), (0, -1), (-1, 0), (1, 1), (1, -1), (-1, -1), (-1, 1) };

        private bool[] _walls = new bool[(int)DirectionType.Max];

        public bool[] Walls => _walls;

        public bool IsIlluminated;

        public bool HasKey;

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