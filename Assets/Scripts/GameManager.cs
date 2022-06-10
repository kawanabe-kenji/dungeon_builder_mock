using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
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
        private FieldHUDManager _fieldHUDMgr;

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
                _playerHP = Mathf.Min(value, PlayerMaxHP);
                UpdatePlayerHPView();
            }
        }

        public int PlayerMaxHP => _playerHPViews.Length * PLAYER_HP_VIEW_PART;

        private int _playerStamina;

        private int PlayerStamina
        {
            get => _playerStamina;
            set
            {
                _playerStamina = Mathf.Min(value, PlayerStaminaMax);
                for(int i = 0; i < _playerStaminaViews.Length; i++) _playerStaminaViews[i].enabled = i < PlayerStamina;
            }
        }

        private int PlayerStaminaMax => _playerStaminaViews.Length;

        [SerializeField]
        private Image[] _playerStaminaViews;

        [SerializeField]
        private Renderer _possibleBlockPrefab;

        [SerializeField]
        private Transform _possibleBlocksParent;

        private List<Renderer> _possibleBlocks;

        private Node[] _possibleNodes;

        [SerializeField]
        private EnemyManager _enemyMgr;

        [SerializeField]
        private GameObject _loseLayerParent;

        [SerializeField]
        private CanvasGroup _loseBlackLayer;

        [SerializeField]
        private CanvasGroup _loseTextLayer;

        [SerializeField]
        private Button _loseResetButton;

        [SerializeField]
        private Image _blackLayer;

        private bool _isShowPossibleRange;
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

            _fieldHUDMgr.Initialize();

            _fieldMgr = new FieldManager(_fieldSize, startPos, _controlMgr.MinoViewPanels.Length);
            _routeCalc = new RouteCalculator(_fieldMgr.FieldSize);

            _fieldView.Initialize(_fieldMgr.FieldSize, startPos, _fieldHUDMgr);
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
            _controlMgr.ResetButton.onClick.AddListener(ResetScene);

            _enemyMgr.Initialize(_fieldHUDMgr);

            _playerHP = PlayerMaxHP;
            _playerStamina = PlayerStaminaMax;

            _possibleBlocks = new List<Renderer>();

            Fade(false);

            ShowPlayerPossibleRange();
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
            HilightLine();

            // TODO: 今回新たに揃った列に何かする
            foreach (var fieldPosY in _fieldMgr.LastAddHilightLine)
            {
                Debug.Log("Hilight Y: " + fieldPosY);
            }
        }

        private void HilightLine()
        {
            _fieldMgr.HilightLine(_playerPos);
            _fieldView.HilightLine(_fieldMgr.Blocks);
            _enemyMgr.HilightLine(_fieldMgr.Blocks);
        }

        private void TouchField(PointerEventData eventData)
        {
            var fieldPos = _controlMgr.GetFieldPosition(eventData.position, true);
            var isContainsPossibleNode = false;
            foreach (var node in _possibleNodes)
            {
                if (node.Position == fieldPos)
                {
                    isContainsPossibleNode = true;
                    break;
                }
            }
            if (!isContainsPossibleNode) return;

            var block = _fieldMgr.GetBlock(fieldPos);
            if(block == null) return;

            var route = _routeCalc.GetRoute(_playerPos, fieldPos, _fieldMgr.Blocks);
            _routeView.gameObject.SetActive(route != null);
            if(route == null) return;

            TraceRoute(route);
            HidePlayerPossibleRange();
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
                        enemyView.IsVisible = true;
                        // TODO: 攻撃演出
                    });
                    var prePos = FieldView.GetWorldPosition(route[i - 1]) + offset;
                    var movePos = prePos + (position - prePos) * 0.5f;
                    seq.Append(_player.DOMove(movePos, 0.1f));
                    seq.AppendCallback(() =>
                    {
                        enemy.HP--;
                        if(enemy.HP <= 0) _enemyMgr.RemoveEnemy(enemy);
                    });
                    seq.Append(_player.DOMove(prePos, 0.1f));
                    break;
                }
                if(isHitEnemy) break;

                seq.AppendCallback(() => PlayerStamina--);
                seq.Append(_player.DOMove(position, 0.1f).SetEase(Ease.Linear));
                seq.AppendCallback(() => _playerPos = nextPos);

                var nextBlock = _fieldMgr.GetBlock(nextPos);
                if (nextBlock != null && nextBlock.HasHealItem)
                {
                    // TODO: 回復アイテム拾った演出
                    nextBlock.HasHealItem = false;
                    var nextBlockView = _fieldView.GetBlock(nextPos);
                    if(nextBlockView != null && nextBlockView.HealItem != null)
                    {
                        var healItem = nextBlockView.HealItem;
                        seq.AppendCallback(() =>
                        {
                            healItem.gameObject.SetActive(true);
                            _fieldHUDMgr.RemoveUnknownView(healItem.gameObject);
                        });
                        var upVector = (Vector3.forward + Camera.main.transform.up).normalized;
                        seq.Append(healItem.DOMove(healItem.position + upVector * 3f, 1f).SetEase(Ease.OutCubic));
                        seq.AppendCallback(() =>
                        {
                            nextBlockView.HealItem = null;
                            Destroy(healItem.gameObject);
                            PlayerHP++;
                        });
                    }
                }
            }
            seq.OnComplete(() =>
            {
                HilightLine();
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
            HidePlayerPossibleRange();
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
                ShowPlayerPossibleRange();
                return;
            }

            PutMino(pickedMino);

            _fieldView.ReleaseMino(pickedMino);

            var shapeType = Mino.RandomShapeType();
            var mino = _fieldMgr.SpawnMino(index, shapeType);

            if (!mino.HasKey())
            {
                // 敵配置(60%の確率）
                if (ProbabilityCalclator.DetectFromPercent(60))
                {
                    Enemy enemy = null;
                    if (ProbabilityCalclator.DetectFromPercent(50))
                        enemy = new Enemy(
                            hp: 2,
                            power: 1,
                            moveDistance: 1,
                            searchRange: _fieldSize.x + _fieldSize.y,
                            looksType: 0
                        );
                    else
                        enemy = new Enemy(
                            hp: 1,
                            power: 1,
                            moveDistance: 3,
                            searchRange: 3,
                            looksType: 1
                        );

                    mino.PutEnemy(enemy);
                }
                // 回復アイテム配置(35%の確率)
                else if (ProbabilityCalclator.DetectFromPercent(35))
                {
                    mino.PutHealItem();
                }
            }

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
                var moveDistance = enemy.MoveDistance;
                if (FieldManager.Distance(enemy.FieldPos, _playerPos) > enemy.SearchRange) moveDistance = 1;

                var enemyView = _enemyMgr.EnemyViews[i];
                var route = _routeCalc.GetRoute(enemy.FieldPos, _playerPos, moveDistance, _fieldMgr.Blocks);
                if (route == null) continue;

                var seq = DOTween.Sequence();
                seq.AppendInterval(0.5f);
                var offset = Vector3.up * _fieldView.HeightFloor + Vector3.back;
                for(int j = 1; j < route.Length; j++)
                {
                    var currentPos = route[j - 1];
                    bool isHitPlayer = false;
                    for(int k = 0; k < (int)Block.DirectionType.Max; k++)
                    {
                        var targetPos = currentPos + Block.AROUND_OFFSET[k];
                        if (_playerPos != targetPos) continue;

                        var currentBlock = _fieldMgr.GetBlock(currentPos);
                        if (currentBlock.Walls[k]) continue;

                        var reverseDir = Block.GetReverseDirection((Block.DirectionType)k);
                        var targetBlock = _fieldMgr.GetBlock(targetPos);
                        if (targetBlock == null || targetBlock.Walls[(int)reverseDir]) continue;

                        isHitPlayer = true;
                        seq.AppendCallback(() =>
                        {
                            float angle = Quaternion.LookRotation(_player.position - enemyView.transform.position).eulerAngles.y;
                            enemyView.IsVisible = true;
                            enemyView.lookAngles = new Vector3(0f, angle, 0f);
                            // TODO: 攻撃モーション
                        });
                        var prePos = FieldView.GetWorldPosition(currentPos) + offset;
                        var movePos = prePos + (_player.position - prePos) * 0.5f;
                        seq.Append(enemyView.transform.DOMove(movePos, 0.1f));
                        seq.Append(enemyView.transform.DOMove(prePos, 0.1f));
                        seq.AppendCallback(() =>
                        {
                            enemyView.IsVisible = _fieldMgr.GetBlock(enemy.FieldPos).IsIlluminated;
                            PlayerHP -= enemy.Power;
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
                        if (isIlluminated) enemyView.IsVisible = isIlluminated;
                        enemy.FieldPos = nextPos;
                    });
                    seq.Append(enemyView.transform.DOMove(position, oneMoneDuration).SetEase(Ease.Linear));
                    if (!isIlluminated) seq.AppendCallback(() => enemyView.IsVisible = isIlluminated);
                }
                seq.Play();

                if(maxDuration < route.Length * oneMoneDuration)
                {
                    maxDuration = route.Length * oneMoneDuration;
                }
            }

            var controlSeq = DOTween.Sequence();
            controlSeq.AppendInterval(maxDuration);
            controlSeq.OnComplete(() =>
            {
                _controlMgr.interactable = true;
                PlayerStamina += 1;
                ShowPlayerPossibleRange();
            });
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
            seq.Append(_loseBlackLayer.DOFade(1f, 0.7f));
            seq.Append(_loseTextLayer.DOFade(1f, 0.5f));
            seq.AppendCallback(() =>
            {
                _loseResetButton.image.enabled = true;
                _loseResetButton.onClick.AddListener(ResetScene);
            });
            seq.Play();
        }

        private void ResetScene()
        {
            var seq = Fade(true);
            seq.AppendCallback(() => SceneManager.LoadScene("GameScene"));
            seq.Play();
        }

        private Sequence Fade(bool isOut)
        {
            _blackLayer.color = isOut ? Color.clear : Color.black;
            _blackLayer.enabled = true;
            var seq = DOTween.Sequence();
            seq.Append(_blackLayer.DOFade(isOut ? 1f : 0f, 0.5f));
            return seq;
        }

        private void ShowPlayerPossibleRange()
        {
            if (_isShowPossibleRange) return;

            _routeView.gameObject.SetActive(false);

            var enemiesPos = new Vector2Int[_enemyMgr.Enemies.Count];
            for (int i = 0; i < enemiesPos.Length; i++) enemiesPos[i] = _enemyMgr.Enemies[i].FieldPos;

            HidePlayerPossibleRange();

            _possibleNodes = _routeCalc.GetNodesAsPossible(_playerPos, PlayerStamina, _fieldMgr.Blocks/*, enemiesPos*/);
            for(int i = 0; i < _possibleNodes.Length; i++)
            {
                Renderer block;
                if (_possibleBlocks.Count <= i)
                {
                    block = Instantiate(_possibleBlockPrefab, _possibleBlocksParent);
                    _possibleBlocks.Add(block);
                }
                else
                {
                    block = _possibleBlocks[i];
                }
                block.transform.localPosition = FieldView.GetWorldPosition(_possibleNodes[i].Position);
                block.enabled = true;
            }

            _isShowPossibleRange = true;
        }

        private void HidePlayerPossibleRange()
        {
            if (!_isShowPossibleRange) return;
            foreach (var block in _possibleBlocks) block.enabled = false;
            _isShowPossibleRange = false;
        }
    }
}
