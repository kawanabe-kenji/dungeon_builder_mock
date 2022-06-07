using System.Collections.Generic;
using UnityEngine;

namespace DungeonBuilder
{
    public class FieldHUDManager : MonoBehaviour
    {
        [SerializeField]
        private UnknownObjectView _unknownViewPrefab;

        [SerializeField]
        private RectTransform _unknownViewsParent;

        private Dictionary<GameObject, UnknownObjectView> _unknownViews;

        public void Initialize()
        {
            _unknownViews = new Dictionary<GameObject, UnknownObjectView>();
        }

        public UnknownObjectView AddUnknownView(GameObject trackObject, Vector2 offset)
        {
            var unknownView = Instantiate(_unknownViewPrefab, _unknownViewsParent);
            unknownView.SetTrackObject(trackObject.transform, offset);
            _unknownViews.Add(trackObject, unknownView);
            return unknownView;
        }

        public UnknownObjectView AddUnknownView(GameObject trackObject)
        {
            return AddUnknownView(trackObject, Vector2.zero);
        }

        public void RemoveUnknownView(GameObject trackObject)
        {
            var view = _unknownViews[trackObject];
            _unknownViews.Remove(trackObject);
            Destroy(view.gameObject);
        }

        public UnknownObjectView GetUnknownView(GameObject trackObject)
        {
            return _unknownViews[trackObject];
        }
    }
}
