using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DungeonBuilder
{
    public class ControlManager : MonoBehaviour
    {
        [SerializeField]
        private Camera _camera;

        [SerializeField]
        private TouchHandler _touchFieldHandler;

        public TouchHandler TouchFieldHandler => _touchFieldHandler;

        [SerializeField]
        private TouchHandler[] _minoViewPanels;

        public TouchHandler[] MinoViewPanels => _minoViewPanels;

        [SerializeField]
        private RectTransform[] _minoViewPrefabs;

        private int _touchPanelLayer;

        private bool[] _isDragMinoViewPanels;

        public bool[] IsDragMinoViewPanels => _isDragMinoViewPanels;

        public void Initialize(Mino[] pickableMinos)
        {
            _touchPanelLayer = LayerMask.GetMask("UI");
            _isDragMinoViewPanels = new bool[_minoViewPanels.Length];

            for(int i = 0; i < pickableMinos.Length; i++)
            {
                var minoView = Instantiate(_minoViewPrefabs[(int)pickableMinos[i].Type], _minoViewPanels[i].transform);
                int count = 0;
                foreach(var kvp in pickableMinos[i].Blocks)
                {
                    var block = kvp.Value;
                    var blockView = minoView.GetChild(count);
                    for(int j = 0; j < block.Walls.Length; j++)
                    {
                        blockView.GetChild(j).GetComponent<Image>().enabled = block.Walls[j];
                    }
                    count++;
                }
            }
        }

        public Vector2Int GetFieldPosition(Vector2 screenPoint, bool isDebug = false)
        {
            // スクリーン座標を元にRayを取得
            var ray = _camera.ScreenPointToRay(screenPoint);
            if (!Physics.Raycast(ray, out RaycastHit hit, 300f, _touchPanelLayer)) return -Vector2Int.one;

            float distance = Vector3.Distance(ray.origin, hit.point);
            if (isDebug) Debug.DrawRay(ray.origin, ray.direction * distance, Color.red, 5);

            var fieldPos = FieldView.GetFieldPosition(hit.point);
            if (isDebug) Debug.Log("index: " + fieldPos);

            return fieldPos;
        }

        public void SpawnMino(int index, Mino.ShapeType shapeType, Dictionary<Vector2Int, Block> minoBlocks)
		{
            var minoViewPanel = _minoViewPanels[index];
            Destroy(minoViewPanel.transform.GetChild(0).gameObject);
            var minoView = Instantiate(_minoViewPrefabs[(int)shapeType], minoViewPanel.transform);

            int count = 0;
            foreach(var kvp in minoBlocks)
            {
                var block = kvp.Value;
                var blockView = minoView.GetChild(count);
                for(int j = 0; j < block.Walls.Length; j++)
                {
                    blockView.GetChild(j).GetComponent<Image>().enabled = block.Walls[j];
                }
                count++;
            }
        }

        public void RotateMino(int index, int rotateCount)
		{
            _minoViewPanels[index].transform.GetChild(0).localEulerAngles = Vector3.forward * rotateCount * -90f;
        }
    }
}
