using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;

namespace DungeonBuilder
{
    public class GameManager : MonoBehaviour
    {
        #region Variables
        [SerializeField]
        private Vector2Int _fieldSize = new Vector2Int(7, 12);

        private FieldManager _fieldMgr;

        private Vector2Int _playerPos;

        [SerializeField]
        private FieldView _fieldView;

        [SerializeField]
        private FieldUIView _fieldUIView;

        [SerializeField]
        private RouteView _routeView;

        private RouteCalculator _routeCalc;

        [SerializeField]
        private ControlManager _controlMgr;

        [SerializeField]
        private Transform _player;

        private bool _putKey;

        private int[] _putMinoRotateCounts;

        /*
        [SerializeField]
        private TouchHandler _touchHandler;

        [SerializeField]
        private TouchHandler[] _minoViewPanels;

        [SerializeField]
        private RectTransform[] _minoViewPrefabs;

        private bool[] _isDragMinoViewPanels;
        */
        #endregion // Variables

        #region CommonMethod
        public T[] GetComponentsInChildrenWithoutSelf<T>(GameObject self) where T : Component
        {
            return self.GetComponentsInChildren<T>().Where(c => self != c.gameObject).ToArray();
        }
        #endregion // CommonMethod

        private void Awake()
        {
            // ゲーム開始のフィールド座標
            Vector2Int startPos = new Vector2Int(Mathf.CeilToInt(_fieldSize.x / 2), 1);
            _playerPos = startPos;

            _fieldMgr = new FieldManager(_fieldSize, startPos, _controlMgr.MinoViewPanels.Length);
            _routeCalc = new RouteCalculator(_fieldMgr.FieldSize);

            _fieldView.Initialize(_fieldMgr.FieldSize, startPos);
            _fieldUIView.Initialize(_fieldMgr.FieldSize);

            _controlMgr.Initialize(_fieldMgr.PickableMinos);

            _controlMgr.TouchHandler.OnPointerUpEvent = TouchBlock;
            var minoViewPanels = _controlMgr.MinoViewPanels;
            for(int i = 0; i < minoViewPanels.Length; i++)
            {
                int index = i;
                minoViewPanels[i].OnPointerDownEvent = e => PickMino(index);
                minoViewPanels[i].OnDragEvent = e => DragMino(e, index);
                minoViewPanels[i].OnEndDragEvent = e => ReleaseMino(index);
                minoViewPanels[i].OnPointerClickEvent = e => RotateMino(index);
            }

            _putMinoRotateCounts = new int[minoViewPanels.Length];
        }

        private void RefreshMino()
        {
            RefreshField();
            _fieldUIView.DrawMino(_fieldMgr.PickedMino);
            if(_fieldMgr.PickedMino != null)
            {
                var minoPosition = _fieldMgr.PickedMino.Index;
                _fieldView.SetMinoPosition(new Vector2Int(minoPosition.x, minoPosition.y));
            }
        }

        private void PutMino()
        {
            _fieldMgr.PutMino(_fieldMgr.PickedMino);
            _fieldView.PutMino(_fieldMgr.PickedMino);
            RefreshMino();
        }

        private void RefreshField()
        {
            _fieldUIView.Refresh(_fieldMgr.Blocks);
            _fieldView.Refresh(_fieldMgr.Blocks);
            HighlightLine();
        }

        private void HighlightLine()
        {
            _fieldMgr.HighlightLine(_playerPos);
            _fieldView.HighlightLine(_fieldMgr.Blocks);
        }

        private void TouchBlock(PointerEventData eventData)
        {
            var fieldPos = _controlMgr.GetFieldPosition(eventData.position, true);
            var block = _fieldMgr.GetBlock(fieldPos);
            if(block == null) return;

            var route = _routeCalc.GetRoute(_playerPos, fieldPos, _fieldMgr.Blocks);
            _routeView.gameObject.SetActive(route != null);
            if (route == null) return;

            TraceRoute(route);
        }

        private void TraceRoute(Vector2Int[] route)
        {
            _playerPos = route[route.Length - 1];
            _routeView.DrawRoute(route);

            var seq = DOTween.Sequence();
            var offset = Vector3.up * _fieldView.HeightFloor + Vector3.back;
            var playerRotate = _player.GetChild(0);
            for(int i = 1; i < route.Length; i++)
            {
                var position = FieldView.GetWorldPosition(route[i]) + offset;
                seq.AppendCallback(() =>
                {
                    float angle = Quaternion.LookRotation(position - _player.position).eulerAngles.y;
                    playerRotate.localEulerAngles = new Vector3(0f, angle, 0f);
                });
                seq.Append(_player.DOMove(position, 0.05f).SetEase(Ease.Linear));
            }
            seq.OnComplete(HighlightLine);
            seq.Play();
        }

        #region Control Mino
        private void PickMino(int index)
        {
            _fieldMgr.PickMino(index);
            _fieldView.PickMino(_fieldMgr.PickedMino, _putMinoRotateCounts[index]);
        }

        private void DragMino(PointerEventData eventData, int panelId)
        {
            _isDragMinoViewPanels[panelId] = true;
            if(_fieldMgr.PickedMino == null)　return;

            var fieldPos = _controlMgr.GetFieldPosition(eventData.position);
            if(_fieldMgr.PickedMino.Index == (fieldPos.x, fieldPos.y))　return;

            var block = _fieldMgr.GetBlock(fieldPos);
            if(block != null)　return;

            _fieldMgr.PickedMino.Index = (fieldPos.x, fieldPos.y);
            RefreshMino();
        }

        private void ReleaseMino(int index)
        {
            _controlMgr.IsDragMinoViewPanels[index] = false;

            var pickedMino = _fieldMgr.PickedMino;
            if (pickedMino == null) return;

            if(!_fieldMgr.CanPutMino(pickedMino))
            {
                _fieldMgr.ReleaseMino();
                _fieldView.DestroyCurrentMino();
                RefreshMino();
                return;
            }

            PutMino();

            _fieldView.ReleaseMino(pickedMino);

            _fieldMgr.ReleaseMino();

            var shapeType = Mino.RandomShapeType();
            var spawnedMino = _fieldMgr.SpawnMino(index, shapeType);
            if(!_putKey)
            {
                int blockCount = 0;
                foreach(var block in _fieldMgr.Blocks) if(block != null) blockCount++;
                if(blockCount > _fieldMgr.FieldSize.x * _fieldMgr.FieldSize.y * 0.3f)
                {
                    spawnedMino.PutKey();
                    _putKey = true;
                }
            }

            _putMinoRotateCounts[index] = 0;

            _controlMgr.SpawnMino(index, shapeType, _fieldMgr.PickableMinos[index].Blocks);
        }

        private void RotateMino(int index)
        {
            if(_controlMgr.IsDragMinoViewPanels[index]) return;

            _fieldMgr.PickedMino.Rotate();
            var rotateCount = _putMinoRotateCounts[index] + 1;
            if(rotateCount >= 4) rotateCount = 0;
            _putMinoRotateCounts[index] = rotateCount;
            _controlMgr.RotateMino(index, rotateCount);
        }
        #endregion // Control Mino
    }
}
