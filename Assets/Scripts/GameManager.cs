using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
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

        private const int PLAYER_HP_VIEW_PART = 1;

        [SerializeField]
        private Image[] _playerHPViews;

        private int _playerHP;

        private int PlayerHP
        {
            get => _playerHP;
            set
            {
                _playerHP = value;
                UpdatePlayerHPView();
            }
        }

        [SerializeField]
        private EnemyManager _enemyMgr;

        [SerializeField]
        private GameObject _loseLayerParent;

        [SerializeField]
        private CanvasGroup _loseBlackLayer;

        [SerializeField]
        private CanvasGroup _loseTextLayer;
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

            _playerHP = _playerHPViews.Length * PLAYER_HP_VIEW_PART;
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
            _enemyMgr.HighlightLine(_fieldMgr.Blocks);
        }

        private void TouchField(PointerEventData eventData)
        {
            var fieldPos = _controlMgr.GetFieldPosition(eventData.position, true);
            var block = _fieldMgr.GetBlock(fieldPos);
            if(block == null) return;

            var route = _routeCalc.GetRoute(_playerPos, fieldPos, _fieldMgr.Blocks);
            _routeView.gameObject.SetActive(route != null);
            if(route == null) return;

            TraceRoute(route);
        }

        private void TraceRoute(Vector2Int[] route)
        {
            _routeView.DrawRoute(route);

            var seq = DOTween.Sequence();
            var offset = Vector3.up * _fieldView.HeightFloor + Vector3.back;
            var playerRotate = _player.GetChild(0);
            for(int i = 1; i < route.Length; i++)
            {
                var nextPos = route[i];

                var position = FieldView.GetWorldPosition(nextPos) + offset;
                seq.AppendCallback(() =>
                {
                    float angle = Quaternion.LookRotation(position - _player.position).eulerAngles.y;
                    playerRotate.localEulerAngles = new Vector3(0f, angle, 0f);
                });

                bool isHitEnemy = false;
                foreach(var enemy in _enemyMgr.Enemies)
                {
                    if(enemy.FieldPos != nextPos) continue;
                    isHitEnemy = true;
                    seq.AppendCallback(() =>
                    {
                        _routeView.gameObject.SetActive(false);
                        var enemyView = _enemyMgr.GetView(enemy);
                        enemyView.transform.GetChild(0).gameObject.SetActive(true);
                        // TODO: 攻撃演出
                    });
                    var prePos = FieldView.GetWorldPosition(route[i - 1]) + offset;
                    var movePos = prePos + (position - prePos) * 0.5f;
                    seq.Append(_player.DOMove(movePos, 0.1f));
                    seq.AppendCallback(() => _enemyMgr.RemoveEnemy(enemy));
                    seq.Append(_player.DOMove(prePos, 0.1f));
                    break;
                }
                if(isHitEnemy) break;

                seq.Append(_player.DOMove(position, 0.05f).SetEase(Ease.Linear));
                seq.AppendCallback(() => _playerPos = nextPos);
            }
            seq.OnComplete(() =>
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
            if(_fieldMgr.PickedMino == null) return;

            var fieldPos = _controlMgr.GetFieldPosition(eventData.position);
            if(_fieldMgr.PickedMino.FieldPos == fieldPos) return;

            var block = _fieldMgr.GetBlock(fieldPos);
            if(block != null) return;

            _fieldMgr.PickedMino.FieldPos = fieldPos;
            RefreshMino();
        }

        private void ReleaseMino(int index)
        {
            _controlMgr.IsDragMinoViewPanels[index] = false;

            var pickedMino = _fieldMgr.PickedMino;
            if(pickedMino == null) return;

            _fieldMgr.ReleaseMino();

            if(!_fieldMgr.CanPutMino(pickedMino))
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
                var route = _routeCalc.GetRouteAsPossibleRandom(enemy.FieldPos, 3, _fieldMgr.Blocks, _playerPos);

                var seq = DOTween.Sequence();
                seq.AppendInterval(0.5f);
                var offset = Vector3.up * _fieldView.HeightFloor + Vector3.back;
                for(int j = 1; j < route.Length; j++)
                {
                    var currentPos = route[j - 1];
                    bool isHitPlayer = false;
                    for(int k = 0; k < (int)Block.DirectionType.Max; k++)
                    {
                        if(_playerPos != currentPos + Block.AROUND_OFFSET[k]) continue;
                        isHitPlayer = true;
                        seq.AppendCallback(() =>
                        {
                            float angle = Quaternion.LookRotation(_player.position - enemyView.transform.position).eulerAngles.y;
                            enemyView.transform.GetChild(0).gameObject.SetActive(true);
                            enemyView.lookAngles = new Vector3(0f, angle, 0f);
                            // TODO: 攻撃モーション
                        });
                        var prePos = FieldView.GetWorldPosition(currentPos) + offset;
                        var movePos = prePos + (_player.position - prePos) * 0.5f;
                        seq.Append(enemyView.transform.DOMove(movePos, 0.1f));
                        seq.Append(enemyView.transform.DOMove(prePos, 0.1f));
                        seq.AppendCallback(() =>
                        {
                            enemyView.transform.GetChild(0).gameObject.SetActive(_fieldMgr.GetBlock(enemy.FieldPos).IsIlluminated);
                            PlayerHP--;
                        });
                        break;
                    }
                    if(isHitPlayer) break;

                    var nextPos = route[j];
                    var position = FieldView.GetWorldPosition(nextPos) + offset;
                    bool isIlluminated = _fieldMgr.GetBlock(nextPos).IsIlluminated;
                    seq.AppendCallback(() =>
                    {
                        float angle = Quaternion.LookRotation(position - enemyView.transform.position).eulerAngles.y;
                        enemyView.lookAngles = new Vector3(0f, angle, 0f);
                        enemyView.transform.GetChild(0).gameObject.SetActive(isIlluminated);
                        enemy.FieldPos = nextPos;
                    });
                    seq.Append(enemyView.transform.DOMove(position, oneMoneDuration).SetEase(Ease.Linear));
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

        private void UpdatePlayerHPView()
        {
            for(int i = 0; i < _playerHPViews.Length; i++)
            {
                if(_playerHP >= (i + 1) * PLAYER_HP_VIEW_PART)
                {
                    _playerHPViews[i].fillAmount = 1f;
                    continue;
                }
                _playerHPViews[i].fillAmount = (float)(_playerHP - i * PLAYER_HP_VIEW_PART) / PLAYER_HP_VIEW_PART;
            }

            if(_playerHP > 0) return;

            _controlMgr.interactable = false;

            _loseLayerParent.SetActive(true);
            var seq = DOTween.Sequence();
            seq.AppendInterval(0.5f);
            seq.Append(_loseBlackLayer.DOFade(1f, 1f));
            seq.Append(_loseTextLayer.DOFade(1f, 1f));
            seq.Play();
        }
    }
}
