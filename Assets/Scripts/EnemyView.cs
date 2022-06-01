using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DungeonBuilder
{
    public class EnemyView : MonoBehaviour
    {
        [SerializeField]
        private Transform _lookTransform;

        public Vector3 lookAngles
        {
            get => _lookTransform.localEulerAngles;
            set => _lookTransform.localEulerAngles = value;
        }
    }
}
