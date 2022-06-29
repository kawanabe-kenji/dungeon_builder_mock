using System.Collections.Generic;
using UnityEngine;

namespace DungeonBuilder
{
    public class FieldHUDManager : MonoBehaviour
    {
        [SerializeField]
        private HUDViewBase _hudViewPrefab;

        [SerializeField]
        private RectTransform _hudViewsParent;

        private Dictionary<GameObject, HUDViewBase> _hudViews;

        public void Initialize()
        {
            _hudViews = new Dictionary<GameObject, HUDViewBase>();
        }

        public HUDViewBase AddHUDView(GameObject trackObject, Vector2 offset)
        {
            var hudView = Instantiate(_hudViewPrefab, _hudViewsParent);
            hudView.SetTrackObject(trackObject.transform, offset);
            _hudViews.Add(trackObject, hudView);
            return hudView;
        }

        public HUDViewBase AddHUDView(GameObject trackObject)
        {
            return AddHUDView(trackObject, Vector2.zero);
        }

        public void RemoveHUDView(GameObject trackObject)
        {
            var view = _hudViews[trackObject];
            _hudViews.Remove(trackObject);
            Destroy(view.gameObject);
        }

        public HUDViewBase GetHUDView(GameObject trackObject)
        {
            return _hudViews[trackObject];
        }
    }
}
