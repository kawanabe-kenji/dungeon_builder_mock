using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DungeonBuilder
{
    public class Enemy
    {
        public Vector2Int FieldPos;

        public int HP = 2;

        private int _power;

        public int Power => _power;

        private int _moveDistance;

        public int MoveDistance => _moveDistance;

        private int _searchRange;

        public int SearchRange => _searchRange;

        private int _looksType;

        public int LooksType => _looksType;

        public bool IsBuried = false;

        public Enemy(int hp, int power, int moveDistance, int searchRange, int looksType)
        {
            HP = hp;
            _power = power;
            _moveDistance = moveDistance;
            _searchRange = searchRange;
            _looksType = looksType;
        }
    }
}
