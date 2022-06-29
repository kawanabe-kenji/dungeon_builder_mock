using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DungeonBuilder
{
    public class EnemyView : MonoBehaviour
    {
        [SerializeField]
        private Transform _lookTransform;

        [SerializeField]
        private bool _isVisible;

        public UnknownObjectView UnknownView;

        public bool IsVisible
        {
            get => _isVisible;
            set
            {
                _isVisible = value;
                //SetVisible(value);
            }
        }

        public Vector3 lookAngles
        {
            get => _lookTransform.localEulerAngles;
            set => _lookTransform.localEulerAngles = value;
        }

        private void SetVisible(bool isVisible)
        {
            transform.GetChild(0).gameObject.SetActive(isVisible);
            if (UnknownView != null) UnknownView.SetVisible(!isVisible);
        }
    }
}
