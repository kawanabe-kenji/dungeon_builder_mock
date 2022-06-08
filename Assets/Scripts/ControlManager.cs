using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DungeonBuilder
{
    public class ControlManager : MonoBehaviour
    {
        [SerializeField]
        private bool _interactable = true;

        public bool interactable
        {
            get => _interactable;
            set => OnUpdateInteractable(value);
        }

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

        [SerializeField]
        private Image _objectIconPrefab;

        private Image[] _objectIcons;

        [SerializeField]
        private Sprite _spriteMonster;

        [SerializeField]
        private Sprite _spriteKey;

        [SerializeField]
        private Sprite _spriteHealItem;

        [SerializeField]
        private Button _resetButton;

        public Button ResetButton => _resetButton;

        private void OnValidate()
        {
            interactable = _interactable;
        }

        private void OnUpdateInteractable(bool value)
        {
            _interactable = value;

            if(_touchFieldHandler != null)
            {
                _touchFieldHandler.GetComponent<Graphic>().raycastTarget = value;
            }

            if(_minoViewPanels != null)
            {
                foreach (var panel in _minoViewPanels)
                {
                    if (panel == null) continue;
                    panel.GetComponent<Graphic>().raycastTarget = value;
                }
            }
        }

        public void Initialize(Mino[] pickableMinos)
        {
            _touchPanelLayer = LayerMask.GetMask("UI");
            _isDragMinoViewPanels = new bool[_minoViewPanels.Length];
            _objectIcons = new Image[_minoViewPanels.Length];

            for (int i = 0; i < pickableMinos.Length; i++)
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
            if (isDebug) Debug.Log("fieldPos: " + fieldPos);

            return fieldPos;
        }

        public void SpawnMino(int index, Mino.ShapeType shapeType, Mino mino)
		{
            var minoViewPanel = _minoViewPanels[index];
            Destroy(minoViewPanel.transform.GetChild(0).gameObject);
            var minoView = Instantiate(_minoViewPrefabs[(int)shapeType], minoViewPanel.transform);

            int count = 0;
            foreach(var kvp in mino.Blocks)
            {
                var block = kvp.Value;
                var blockView = minoView.GetChild(count);
                for(int j = 0; j < block.Walls.Length; j++)
                {
                    blockView.GetChild(j).GetComponent<Image>().enabled = block.Walls[j];
                }
                var offset = kvp.Key;
                if (mino.Enemy != null && offset == mino.Enemy.FieldPos || block.HasKey || block.HasHealItem)
                {
                    var icon = Instantiate(_objectIconPrefab, blockView.transform);
                    _objectIcons[index] = icon;
                    icon.rectTransform.localPosition = Vector3.zero;

                    if (block.HasKey)
                    {
                        icon.sprite = _spriteKey;
                        icon.color = new Color32(255, 140, 0, 255);
                    }
                    else if (block.HasHealItem)
                    {
                        icon.sprite = _spriteHealItem;
                        icon.color = new Color32(255, 110, 210, 255);
                    }
                    else
                    {
                        icon.sprite = _spriteMonster;
                        icon.color = new Color32(255, 75, 75, 255);
                    }
                }
                count++;
            }
        }

        public void RotateMino(int index, int rotateCount)
		{
            _minoViewPanels[index].transform.GetChild(0).localEulerAngles = Vector3.forward * rotateCount * -90f;
            if (_objectIcons[index] != null) _objectIcons[index].rectTransform.rotation = Quaternion.identity;
        }
    }
}
