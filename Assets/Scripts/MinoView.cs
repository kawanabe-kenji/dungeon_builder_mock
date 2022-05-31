using System;
using System.Collections.Generic;
using UnityEngine;

namespace DungeonBuilder
{
    public class MinoView : MonoBehaviour
    {
        [Serializable]
        public class Block
        {
            [SerializeField]
            private GameObject[] _walls;

            public GameObject[] Walls => _walls;

            [SerializeField]
            private ParticleSystem _fog;

            public ParticleSystem Fog => _fog;

            public Transform Key;
        }

        [SerializeField]
        private List<Block> _blocks;

        public List<Block> Blocks => _blocks;

        public void RefreshWalls(Mino mino)
        {
            int blockIndex = 0;
            foreach (var kvp in mino.Blocks)
            {
                var block = kvp.Value;
                for (int i = 0; i < block.Walls.Length; i++)
                {
                    var wallView = Blocks[blockIndex].Walls[i];
                    if (wallView != null)
                    {
                        wallView.SetActive(block.Walls[i]);
                    }
                }
                blockIndex++;
            }
        }

        public void Rotate()
        {
            transform.localEulerAngles = new Vector3(0f, transform.localEulerAngles.y + 90f, 0f);

            foreach (var block in Blocks)
            {
                // 回転に合わせて壁の情報も更新
                var lastWall = block.Walls[block.Walls.Length - 1];
                for (int i = block.Walls.Length - 1; i > 0; i--)
                {
                    block.Walls[i] = block.Walls[i - 1];
                }
                block.Walls[0] = lastWall;
            }
        }
    }
}
