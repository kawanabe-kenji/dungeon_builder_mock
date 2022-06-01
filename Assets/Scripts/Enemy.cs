using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DungeonBuilder
{
    public class Enemy
    {
        private Vector2Int _fieldPos;

        public Vector2Int FieldPos => _fieldPos;

        private int _hp;

        public int HP => _hp;

        private int _waitTurn;

        public int WaitTurn => _waitTurn;
    }
}
