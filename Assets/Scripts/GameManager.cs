using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using DG.Tweening;
using DigitalRuby.LightningBolt;

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

        //public int PlayerMaxHP => _playerHPViews.Length * PLAYER_HP_VIEW_PART;
        public const int PlayerMaxHP = 5;

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

        private int _playerPower;

        private int PlayerPower
        {
            get => _playerPower;
            set
            {
                _playerPower = Mathf.Min(value, 10);
                UpdatePlayerPowerView();
            }
        }

        [SerializeField]
        private Image[] _playerStaminaViews;

        [SerializeField]
        private Text _playerHPView;

        [SerializeField]
        private Text _playerPowerView;

        [SerializeField]
        private Text _enemyCountView;

        private int _enemyStartCount;

        private StatusHUDView _playerHUD;

        [SerializeField]
        private ParticleSystem _efExplosionPrefab;

        private Node[] _possibleNodes;

        [SerializeField]
        private LightningBoltScript _efBoltPrefab;

        private List<LightningBoltScript> _efBolts;

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
            Vector2Int startPos = new Vector2Int(Mathf.CeilToInt(_fieldSize.x / 2), 0);
            _playerPos = startPos;

            _fieldHUDMgr.Initialize();

            _fieldMgr = new FieldManager(_fieldSize, startPos, _controlMgr.MinoViewPanels.Length);
            _routeCalc = new RouteCalculator(_fieldMgr.FieldSize);

            _fieldView.Initialize(_fieldMgr.FieldSize, startPos, _fieldHUDMgr);
            _fieldUIView.Initialize(_fieldMgr.FieldSize);

            _controlMgr.Initialize(_fieldMgr.PickableMinos);

            //_controlMgr.TouchFieldHandler.OnPointerUpEvent = TouchField;
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
            _playerPower = 1;

            _playerHUD = (StatusHUDView)_fieldHUDMgr.AddHUDView(_player.gameObject, new Vector2(0f, 50f));
            UpdatePlayerHPView();
            UpdatePlayerPowerView();

            _efBolts = new List<LightningBoltScript>();

            int enemyCount = 6;
            int buriedEnemyCount = 4;
            CreateEnemies(enemyCount, startPos);
            CreateBuriedEnemies(buriedEnemyCount, startPos);

            _enemyStartCount = enemyCount + buriedEnemyCount;
            UpdateEnemyCountView();

            Fade(false);

            ShowPlayerPossibleRange();
        }

        private void RefreshMino()
        {
            RefreshField();
            _fieldUIView.DrawMino(_fieldMgr.PickedMino);
            if (_fieldMgr.PickedMino == null) return;
            _fieldView.SetMino(_fieldMgr.PickedMino);
        }

        private void PutMino(Mino mino)
        {
            _fieldMgr.PutMino(mino);
            _enemyMgr.PutMino(mino);
            _fieldView.PutMino(mino);

            foreach(var kvp in mino.Blocks)
            {
                var enemy =_enemyMgr.GetEnemy(mino.FieldPos + kvp.Key);
                if (enemy == null || !enemy.IsBuried) continue;
                enemy.IsBuried = false;
                _enemyMgr.GetView(enemy).IsVisible = true;
            }

            RefreshMino();

            PlayerPower += _fieldMgr.LastStickSideCount;
            int count = _fieldMgr.LastAddHilightLine.Count();
            if(count >= 1)
            {
                PlayerPower *= _fieldMgr.LastAddHilightLine.Count() * 2;
            }
        }

        private void RefreshField()
        {
            _fieldUIView.Refresh(_fieldMgr.Blocks, _enemyMgr.Enemies);
            _fieldView.Refresh(_fieldMgr.Blocks);
            HilightLine();

            // TODO: 今回新たに揃った列に何かする
            for(int i = 0; i < _fieldMgr.LastAddHilightLine.Count; i++)
            {
                var fieldPosY = _fieldMgr.LastAddHilightLine[i];

                /*
                for(int x = 0; x < _fieldMgr.FieldSize.x; x++)
                {
                    var fieldPos = new Vector2Int(x, fieldPosY);
                    var enemy = _enemyMgr.GetEnemy(fieldPos);
                    if (enemy == null) continue;
                    enemy.HP -= 2;
                    if (enemy.HP <= 0) _enemyMgr.RemoveEnemy(enemy);
                }

                if (i <= _efBolts.Count) _efBolts.Add(Instantiate(_efBoltPrefab));
                var efBolt = _efBolts[i];
                var efBoltLine = efBolt.GetComponent<LineRenderer>();
                efBolt.StartPosition = efBolt.EndPosition = Vector3.zero;
                efBolt.enabled = efBoltLine.enabled = true;

                efBolt.StartPosition = FieldView.GetWorldPosition(new Vector2Int(0, fieldPosY));
                efBolt.EndPosition = FieldView.GetWorldPosition(new Vector2Int(_fieldMgr.FieldSize.x - 1, fieldPosY));
                var seq = DOTween.Sequence();
                seq.AppendInterval(1.2f);
                seq.AppendCallback(() => efBolt.enabled = efBoltLine.enabled = false);
                */
            }
        }

        private void HilightLine()
        {
            _fieldMgr.HilightLine(_playerPos);
            _fieldView.HilightLine(_fieldMgr.Blocks);
            //_enemyMgr.HilightLine(_fieldMgr.Blocks);
        }

        private void TouchField(PointerEventData eventData)
        {
            var fieldPos = _controlMgr.GetFieldPosition(eventData.position, true);
            MoveTargetPosition(fieldPos);
        }

        private void MoveTargetPosition(Vector2Int fieldPos)
        {
            /*
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
            */

            var block = _fieldMgr.GetBlock(fieldPos);
            if (block == null) return;

            var route = _routeCalc.GetRoute(_playerPos, fieldPos, _fieldMgr.Blocks, true, _enemyMgr.GetEnemyPositions());
            _routeView.gameObject.SetActive(route != null);
            if (route == null) return;

            TraceRoute(route);
        }

        private void TraceRoute(Vector2Int[] route)
        {
            _routeView.DrawRoute(route);

            var seq = DOTween.Sequence();
            var offset = Vector3.up * _fieldView.HeightFloor + Vector3.back;
            var playerRotate = _player.GetChild(0);

            bool isAttacked = AttackForEnemy(route[0], seq);

            for (int i = 1; i < route.Length; i++)
            {
                var currentPos = route[i - 1];
                var nextPos = route[i];

                var position = FieldView.GetWorldPosition(nextPos) + offset;
                seq.AppendCallback(() =>
                {
                    float angle = Quaternion.LookRotation(position - _player.position).eulerAngles.y;
                    playerRotate.localEulerAngles = new Vector3(0f, angle, 0f);
                });

                //seq.AppendCallback(() => PlayerStamina--);
                seq.Append(_player.DOMove(position, 0.2f).SetEase(Ease.Linear));
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
                            _fieldHUDMgr.RemoveHUDView(healItem.gameObject);
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

                bool ret = AttackForEnemy(nextPos, seq);
                if (ret) isAttacked = ret;
            }

            PlaySkill(route[route.Length - 1], seq);

            seq.OnComplete(() =>
            {
                if(isAttacked) PlayerPower = 1;
                HilightLine();
                UpdateEnemyCountView();
                PlayEnemyTurn();
            });
            seq.Play();
        }

        private bool AttackForEnemy(Vector2Int currentPos, Sequence seq)
        {
            bool isAttacked = false;
            var offset = Vector3.up * _fieldView.HeightFloor + Vector3.back;
            foreach (var enemy in _enemyMgr.Enemies)
            {
                bool isHitEnemy = false;
                for (int j = 0; j < (int)Block.DirectionType.Max; j++)
                {
                    var targetPos = currentPos + Block.AROUND_OFFSET[j];
                    if (!enemy.IsBuried && enemy.FieldPos == targetPos)
                    {
                        isHitEnemy = true;
                        break;
                    }
                }
                if (!isHitEnemy) continue;
                var enemyView = _enemyMgr.GetView(enemy);
                seq.AppendCallback(() =>
                {
                    _routeView.gameObject.SetActive(false);
                    //enemyView.IsVisible = true;
                });
                var prePos = FieldView.GetWorldPosition(currentPos) + offset;
                var movePos = prePos + (enemyView.transform.position - prePos) * 0.5f;
                seq.Append(_player.DOMove(movePos, 0.1f));
                seq.AppendCallback(() =>
                {
                    enemy.HP -= _playerPower;
                    enemyView.HUDView.SetHP(enemy.HP);
                    if (enemy.HP <= 0) _enemyMgr.RemoveEnemy(enemy);
                    UpdateEnemyCountView();
                });
                seq.Append(_player.DOMove(prePos, 0.1f));
                isAttacked = true;
            }
            return isAttacked;
        }

        private void PlaySkill(Vector2Int currentPos, Sequence seq)
        {
            var stickCount = _fieldMgr.LastStickSideCount;
            var effectInterval = 0.12f;

            if (stickCount < 3) return;

            var offset = Vector3.up * _fieldView.HeightFloor + Vector3.back;
            var prePos = FieldView.GetWorldPosition(currentPos) + offset;
            seq.Append(_player.DOMove(prePos + Vector3.up * 2, 0.1f));
            seq.Append(_player.DOMove(prePos, 0.1f));

            for (int i = 0; i < 2; i++)
            {
                seq.AppendInterval(effectInterval);
                var offsetWeight = i + 1;
                seq.AppendCallback(() =>
                {
                    for (int j = 0; j < (int)Block.DirectionType.Max; j++)
                    {
                        var offsetPos = currentPos + Block.AROUND_OFFSET[j] * offsetWeight;
                        var offsetFieldPos = FieldView.GetWorldPosition(offsetPos);
                        Instantiate(_efExplosionPrefab, offsetFieldPos, Quaternion.identity, _player.parent);
                        var enemy = _enemyMgr.GetEnemy(offsetPos);
                        if (enemy != null && !enemy.IsBuried)
                        {
                            enemy.HP -= _playerPower;
                            var enemyView = _enemyMgr.GetView(enemy);
                            enemyView.HUDView.SetHP(enemy.HP);
                            if (enemy.HP <= 0) _enemyMgr.RemoveEnemy(enemy);
                        }
                    }
                });
            }

            if (stickCount < 4) return;

            seq.AppendInterval(effectInterval);
            seq.AppendCallback(() =>
            {
                for (int i = 0; i < (int)Block.DirectionType.Max; i++)
                {
                    var offsetPos = currentPos + Block.AROUND_OFFSET[i] * 3;
                    var offsetFieldPos = FieldView.GetWorldPosition(offsetPos);
                    Instantiate(_efExplosionPrefab, offsetFieldPos, Quaternion.identity, _player.parent);
                    var enemy = _enemyMgr.GetEnemy(offsetPos);
                    if (enemy != null && !enemy.IsBuried)
                    {
                        enemy.HP -= _playerPower;
                        var enemyView = _enemyMgr.GetView(enemy);
                        enemyView.HUDView.SetHP(enemy.HP);
                        if (enemy.HP <= 0) _enemyMgr.RemoveEnemy(enemy);
                    }
                }
            });

            if (stickCount < 5) return;

            seq.AppendInterval(effectInterval);
            seq.AppendCallback(() =>
            {
                var offsets = new Vector2Int[]
                {
                    new Vector2Int(0, 4),
                    new Vector2Int(4, 0),
                    new Vector2Int(0, -4),
                    new Vector2Int(-4, 0),
                    new Vector2Int(1, 1),
                    new Vector2Int(1, -1),
                    new Vector2Int(-1, -1),
                    new Vector2Int(-1, 1)
                };
                for (int i = 0; i < offsets.Length; i++)
                {
                    var offsetPos = currentPos + offsets[i];
                    var offsetFieldPos = FieldView.GetWorldPosition(offsetPos);
                    Instantiate(_efExplosionPrefab, offsetFieldPos, Quaternion.identity, _player.parent);
                    var enemy = _enemyMgr.GetEnemy(offsetPos);
                    if (enemy != null && !enemy.IsBuried)
                    {
                        enemy.HP -= _playerPower;
                        var enemyView = _enemyMgr.GetView(enemy);
                        enemyView.HUDView.SetHP(enemy.HP);
                        if (enemy.HP <= 0) _enemyMgr.RemoveEnemy(enemy);
                    }
                }
            });
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
                ShowPlayerPossibleRange();
                return;
            }

            PutMino(pickedMino);

            _fieldView.ReleaseMino(pickedMino);

            var shapeType = Mino.RandomShapeType();
            var mino = _fieldMgr.SpawnMino(index, shapeType);

            #region ミノ上へのオブジェクト配置
            /*
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
            */
            #endregion // ミノ上へのオブジェクト配置

            _fieldMgr.PickableMinoRotateCounts[index] = 0;

            _controlMgr.SpawnMino(index, shapeType, mino);

            var targetPos = pickedMino.FieldPos + pickedMino.GetMovePoint();
            MoveTargetPosition(targetPos);
            //PlayEnemyTurn();
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
                //var route = _routeCalc.GetRoute(enemy.FieldPos, _playerPos, moveDistance, _fieldMgr.Blocks);

                var diff = _playerPos - enemy.FieldPos;
                var route = new Vector2Int[] {
                    enemy.FieldPos,
                    enemy.FieldPos + (Mathf.Abs(diff.x) >= Mathf.Abs(diff.y) ? new Vector2Int((int)Mathf.Sign(diff.x), 0) : new Vector2Int(0, (int)Mathf.Sign(diff.y)))
                };

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

                        //var currentBlock = _fieldMgr.GetBlock(currentPos);
                        //if (currentBlock.Walls[k]) continue;

                        //var reverseDir = Block.GetReverseDirection((Block.DirectionType)k);
                        //var targetBlock = _fieldMgr.GetBlock(targetPos);
                        //if (targetBlock == null || targetBlock.Walls[(int)reverseDir]) continue;

                        isHitPlayer = true;
                        seq.AppendCallback(() =>
                        {
                            float angle = Quaternion.LookRotation(_player.position - enemyView.transform.position).eulerAngles.y;
                            //enemyView.IsVisible = true;
                            enemyView.lookAngles = new Vector3(0f, angle, 0f);
                            // TODO: 攻撃モーション
                        });
                        var prePos = FieldView.GetWorldPosition(currentPos) + offset;
                        if (_fieldMgr.GetBlock(currentPos) == null) prePos += GetOnWallOffset();
                        var movePos = prePos + (_player.position - prePos) * 0.5f;
                        seq.Append(enemyView.transform.DOMove(movePos, 0.1f));
                        seq.Append(enemyView.transform.DOMove(prePos, 0.1f));
                        seq.AppendCallback(() =>
                        {
                            //enemyView.IsVisible = _fieldMgr.GetBlock(enemy.FieldPos).IsIlluminated;
                            PlayerHP -= enemy.Power;
                        });
                        break;
                    }
                    if(isHitPlayer) break;

                    var nextPos = route[j];
                    var position = FieldView.GetWorldPosition(nextPos) + offset;
                    if (_fieldMgr.GetBlock(nextPos) == null) position += GetOnWallOffset();
                    //bool isIlluminated = _fieldMgr.GetBlock(nextPos).IsIlluminated;
                    seq.AppendCallback(() =>
                    {
                        float angle = Quaternion.LookRotation(position - enemyView.transform.position).eulerAngles.y;
                        enemyView.lookAngles = new Vector3(0f, angle, 0f);
                        //if (isIlluminated) enemyView.IsVisible = isIlluminated;
                        enemy.FieldPos = nextPos;
                    });
                    seq.Append(enemyView.transform.DOMove(position, oneMoneDuration).SetEase(Ease.Linear));
                    //if (!isIlluminated) seq.AppendCallback(() => enemyView.IsVisible = isIlluminated);
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
                PlayerStamina += 2;
                ShowPlayerPossibleRange();
            });
            controlSeq.Play();
        }

        private void UpdatePlayerHPView()
        {
            //for(int i = 0; i < _playerHPViews.Length; i++)
            //{
            //    if(_playerHP >= (i + 1) * PLAYER_HP_VIEW_PART)
            //    {
            //        _playerHPViews[i].fillAmount = 1f;
            //        continue;
            //    }
            //    _playerHPViews[i].fillAmount = (float)(_playerHP - i * PLAYER_HP_VIEW_PART) / PLAYER_HP_VIEW_PART;
            //}
            _playerHPView.text = _playerHP.ToString();
            _playerHUD.SetHP(_playerHP);

            if (_playerHP > 0) return;

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

        private void UpdatePlayerPowerView()
        {
            _playerPowerView.text = _playerPower.ToString();
            _playerHUD.SetPower(_playerPower);
        }

        private void UpdateEnemyCountView()
        {
            _enemyCountView.text = string.Format("{0}/{1}", _enemyStartCount - _enemyMgr.Enemies.Count, _enemyStartCount);
        }

        private void ResetScene()
        {
            var seq = Fade(true);
            seq.AppendCallback(() => SceneManager.LoadScene("SideStickScene"));
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
            _routeView.gameObject.SetActive(false);

            var enemiesPos = new Vector2Int[_enemyMgr.Enemies.Count];
            for (int i = 0; i < enemiesPos.Length; i++) enemiesPos[i] = _enemyMgr.Enemies[i].FieldPos;

            _possibleNodes = _routeCalc.GetNodesAsPossible(_playerPos, PlayerStamina, _fieldMgr.Blocks/*, enemiesPos*/);
        }

        private void CreateEnemies(int count, Vector2Int startPos)
        {
            var creatablePosList = new List<Vector2Int>();
            for(int y = 3; y < _fieldSize.y; y++)
            {
                for (int x = 0; x < _fieldSize.x; x++)
                {
                    creatablePosList.Add(new Vector2Int(x, y));
                }
            }
            creatablePosList.Remove(startPos);

            for (int i = 0; i < count; i++)
            {
                Enemy enemy = null;
                if (ProbabilityCalclator.DetectFromPercent(0))
                    ;
                else
                    enemy = new Enemy(
                        hp: 15,
                        power: 2,
                        moveDistance: 1,
                        searchRange: _fieldSize.x + _fieldSize.y,
                        looksType: 0
                    );

                var fieldPos = creatablePosList[Random.Range(0, creatablePosList.Count)];
                creatablePosList.Remove(fieldPos);
                _enemyMgr.AddEnemy(enemy, fieldPos);

                if (_fieldMgr.GetBlock(fieldPos) == null) _enemyMgr.GetView(enemy).transform.position += GetOnWallOffset();
            }
        }

        private void CreateBuriedEnemies(int count, Vector2Int startPos)
        {
            var creatablePosList = new List<Vector2Int>();
            for (int y = 3; y < _fieldSize.y; y++)
            {
                for (int x = 0; x < _fieldSize.x; x++)
                {
                    creatablePosList.Add(new Vector2Int(x, y));
                }
            }
            creatablePosList.Remove(startPos);

            foreach (var enemy in _enemyMgr.Enemies)
            {
                creatablePosList.Remove(enemy.FieldPos);
            }

            for (int i = 0; i < count; i++)
            {
                Enemy enemy = null;
                if (ProbabilityCalclator.DetectFromPercent(0))
                    ;
                else
                    enemy = new Enemy(
                        hp: 15,
                        power: 2,
                        moveDistance: 1,
                        searchRange: _fieldSize.x + _fieldSize.y,
                        looksType: 0
                    );

                var fieldPos = creatablePosList[Random.Range(0, creatablePosList.Count)];
                creatablePosList.Remove(fieldPos);
                var view = _enemyMgr.AddEnemy(enemy, fieldPos);

                if (_fieldMgr.GetBlock(fieldPos) == null) view.transform.position += GetOnWallOffset();

                enemy.IsBuried = true;
                view.IsVisible = false;
            }
        }

        private Vector3 GetOnWallOffset()
        {
            return Vector3.up * 4f;
        }
    }
}
