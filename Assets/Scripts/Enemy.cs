using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DungeonBuilder
{
    public class Enemy
    {
        public Vector2Int FieldPos;

        private int _hp;

        public int HP => _hp;

        private int _waitTurn;

        public int WaitTurn => _waitTurn;

        private Vector2Int[] CreateRouteChase()
        {
            return null;
        }
    }
}
