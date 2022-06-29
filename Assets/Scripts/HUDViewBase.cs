using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DungeonBuilder
{
    public class HUDViewBase : MonoBehaviour
    {
        [SerializeField]
        private RectTransform _rect;

        private Transform _trackObject;

        private Vector2 _offset;

        private Behaviour[] _views;

        private void Awake()
        {
            var graphics = _rect.GetComponentsInChildren<Graphic>();
            _views = new Behaviour[graphics.Length];
            for(int i = 0; i < _views.Length; i++) _views[i] = graphics[i];
        }

        public void SetTrackObject(Transform trackObject, Vector2 offset)
        {
            _trackObject = trackObject;
            _offset = offset;
            LateUpdate();
        }

        public void SetTrackObject(Transform trackObject)
        {
            SetTrackObject(trackObject, Vector2.zero);
        }

        private void LateUpdate()
        {
            if (_trackObject == null) return;
            _rect.position = RectTransformUtility.WorldToScreenPoint(Camera.main, _trackObject.position);
            _rect.localPosition += (Vector3)_offset;
        }

        public void SetVisible(bool isVisible)
        {
            foreach (var view in _views) view.enabled = isVisible;
        }
    }
}
