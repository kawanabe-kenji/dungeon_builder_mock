using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DungeonBuilder
{
    public class ControlManager : MonoBehaviour
    {
        [SerializeField]
        private Camera _camera;

        [SerializeField]
        private TouchHandler _touchHandler;

        [SerializeField]
        private TouchHandler[] _minoViewPanels;

        private int _touchPanelLayer;

        private bool[] _isDragMinoViewPanels;

        public void Initialize()
        {
            _touchPanelLayer = LayerMask.GetMask("UI");
        }

        public Vector2Int GetFieldPosition(Vector2 screenPoint, bool isDebug = false)
        {
            // スクリーン座標を元にRayを取得
            var ray = _camera.ScreenPointToRay(screenPoint);
            if (!Physics.Raycast(ray, out RaycastHit hit, 300f, _touchPanelLayer)) return -Vector2Int.one;

            float distance = Vector3.Distance(ray.origin, hit.point);
            if (isDebug) Debug.DrawRay(ray.origin, ray.direction * distance, Color.red, 5);

            var index = FieldView.GetIndex(hit.point);
            if (isDebug) Debug.Log("index: " + index);

            return index;
        }
    }
}
