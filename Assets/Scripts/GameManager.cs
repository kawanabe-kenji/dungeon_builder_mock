using System.Collections.Generic;
using System.Linq;
using UnityEngine;
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

        [SerializeField]
        private EnemyManager _enemyMgr;
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

            _controlMgr.TouchFieldHandler.OnPointerUpEvent = TouchField;
            var minoViewPanels = _controlMgr.MinoViewPanels;
            for(int i = 0; i < minoViewPanels.Length; i++)
            {
                int index = i;
                minoViewPanels[i].OnPointerDownEvent = e => PickMino(index);
                minoViewPanels[i].OnDragEvent = e => DragMino(e, index);
                minoViewPanels[i].OnEndDragEvent = e => ReleaseMino(index);
                minoViewPanels[i].OnPointerClickEvent = e => RotateMino(index);
            }

            _enemyMgr.Initialize();
        }

        private void RefreshMino()
        {
            RefreshField();
            _fieldUIView.DrawMino(_fieldMgr.PickedMino);
            if(_fieldMgr.PickedMino != null)
            {
                var minoPosition = _fieldMgr.PickedMino.FieldPos;
                _fieldView.SetMinoPosition(new Vector2Int(minoPosition.x, minoPosition.y));
            }
        }

        private void PutMino(Mino mino)
        {
            _fieldMgr.PutMino(mino);
            _enemyMgr.PutMino(mino);
            _fieldView.PutMino(mino);
            RefreshMino();
        }

        private void RefreshField()
        {
            _fieldUIView.Refresh(_fieldMgr.Blocks, _enemyMgr.Enemies);
            _fieldView.Refresh(_fieldMgr.Blocks);
            HighlightLine();
        }

        private void HighlightLine()
        {
            _fieldMgr.HighlightLine(_playerPos);
            _fieldView.HighlightLine(_fieldMgr.Blocks);
        }

        private void TouchField(PointerEventData eventData)
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
            seq.OnComplete(()=>
            {
                HighlightLine();
                PlayEnemyTurn();
            });
            seq.Play();
        }

        #region Control Mino
        private void PickMino(int index)
        {
            _fieldMgr.PickMino(index);
            _fieldView.PickMino(_fieldMgr.PickedMino, _fieldMgr.PickableMinoRotateCounts[index]);
        }

        private void DragMino(PointerEventData eventData, int panelId)
        {
            _controlMgr.IsDragMinoViewPanels[panelId] = true;
            if(_fieldMgr.PickedMino == null)　return;

            var fieldPos = _controlMgr.GetFieldPosition(eventData.position);
            if(_fieldMgr.PickedMino.FieldPos == fieldPos)　return;

            var block = _fieldMgr.GetBlock(fieldPos);
            if(block != null)　return;

            _fieldMgr.PickedMino.FieldPos = fieldPos;
            RefreshMino();
        }

        private void ReleaseMino(int index)
        {
            _controlMgr.IsDragMinoViewPanels[index] = false;

            var pickedMino = _fieldMgr.PickedMino;
            if (pickedMino == null) return;

            _fieldMgr.ReleaseMino();

            if (!_fieldMgr.CanPutMino(pickedMino))
            {
                _fieldView.DestroyCurrentMino();
                RefreshMino();
                return;
            }

            PutMino(pickedMino);

            _fieldView.ReleaseMino(pickedMino);

            var shapeType = Mino.RandomShapeType();
            var mino = _fieldMgr.SpawnMino(index, shapeType);

            // FIXME: 敵配置
            var enemy = new Enemy();
            mino.PutEnemy(enemy);

            _fieldMgr.PickableMinoRotateCounts[index] = 0;

            _controlMgr.SpawnMino(index, shapeType, mino);

            PlayEnemyTurn();
        }

        private void RotateMino(int index)
        {
            if(_controlMgr.IsDragMinoViewPanels[index]) return;

            _fieldMgr.PickedMino.Rotate();
            var rotateCount = _fieldMgr.PickableMinoRotate(index);
            _controlMgr.RotateMino(index, rotateCount);
        }
        #endregion // Control Mino

        private void PlayEnemyTurn()
        {
            _controlMgr.interactable = false;
            var maxDuration = 0f;
            var oneMoneDuration = 0.3f;

            for(int i = 0; i < _enemyMgr.Enemies.Count; i++)
            {
                var enemy = _enemyMgr.Enemies[i];
                var enemyView = _enemyMgr.EnemyViews[i];
                var route = _routeCalc.GetRouteAsPossibleRandom(enemy.FieldPos, 3, _fieldMgr.Blocks);
                enemy.FieldPos = route[route.Length - 1];

                var seq = DOTween.Sequence();
                var offset = Vector3.up * _fieldView.HeightFloor + Vector3.back;
                for (int j = 1; j < route.Length; j++)
                {
                    var position = FieldView.GetWorldPosition(route[j]) + offset;
                    seq.AppendCallback(() =>
                    {
                        float angle = Quaternion.LookRotation(position - enemyView.transform.position).eulerAngles.y;
                        enemyView.lookAngles = new Vector3(0f, angle, 0f);
                    });
                    seq.Append(enemyView.transform.DOMove(position, oneMoneDuration).SetEase(Ease.Linear));
                    bool inFog = !_fieldMgr.GetBlock(route[j]).IsIlluminated;
                    seq.AppendCallback(() => enemyView.transform.GetChild(0).gameObject.SetActive(!inFog));
                }
                seq.Play();

                if(maxDuration < route.Length * oneMoneDuration)
                {
                    maxDuration = route.Length * oneMoneDuration;
                }
            }

            var controlSeq = DOTween.Sequence();
            controlSeq.AppendInterval(maxDuration);
            controlSeq.OnComplete(() => _controlMgr.interactable = true);
            controlSeq.Play();
        }
    }
}
