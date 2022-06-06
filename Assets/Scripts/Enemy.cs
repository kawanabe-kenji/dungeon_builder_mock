using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DungeonBuilder
{
    public class Enemy
    {
        public Vector2Int FieldPos;

        public int HP = 2;

        private int _waitTurn;

        public int WaitTurn => _waitTurn;
    }
}
