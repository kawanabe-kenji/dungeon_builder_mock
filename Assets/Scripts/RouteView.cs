using UnityEngine;

namespace DungeonBuilder
{
    public class RouteView : MonoBehaviour
    {
        [SerializeField]
        private LineRenderer _routeLine;

        [SerializeField]
        private Transform _routeArrow;

        public void DrawRoute(Vector2Int[] route)
        {
            Vector3[] routePositions = new Vector3[route.Length];
            for (int i = 0; i < route.Length; i++)
            {
                routePositions[i] = FieldView.GetWorldPosition(route[i]);
            }
            // 最後の座標は、最後から一つ前の方向に１m下げる（矢印表示のため）
            var backOffset = (routePositions[route.Length - 2] - routePositions[route.Length - 1]).normalized;
            routePositions[route.Length - 1] += backOffset;
            _routeLine.positionCount = routePositions.Length;
            _routeLine.SetPositions(routePositions);

            _routeArrow.localPosition = routePositions[route.Length - 1];
            _routeArrow.rotation = Quaternion.LookRotation(-backOffset);
        }
    }
}
