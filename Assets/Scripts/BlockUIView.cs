using UnityEngine;
using UnityEngine.UI;

namespace DungeonBuilder
{
    /// <summary>
    /// UI上でのマス表示クラス
    /// </summary>
    public class BlockUIView : MonoBehaviour
    {
        [SerializeField]
        private Image _panel;

        /// <summary> 床 </summary>
        public Image Panel => _panel;

        [SerializeField]
        private Image[] _walls;

        /// <summary> 壁 </summary>
        public Image[] Walls => _walls;
    }
}
