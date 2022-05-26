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
        private static readonly Dictionary<(int x, int y), Block.DirectionType[]> BASE_SPACE = new Dictionary<(int x, int y), Block.DirectionType[]>() {
            { (-1, -1), new Block.DirectionType[] { Block.DirectionType.BACK, Block.DirectionType.LEFT } },
            { (0, -1), null },
            { (1, -1), new Block.DirectionType[] { Block.DirectionType.BACK, Block.DirectionType.RIGHT } },
            { (-1, 0), null },
            { (0, 0), null },
            { (1, 0), null },
            { (-1, 1), new Block.DirectionType[] { Block.DirectionType.FRONT, Block.DirectionType.LEFT } },
            { (0, 1), null },
            { (1, 1), new Block.DirectionType[] { Block.DirectionType.FRONT, Block.DirectionType.RIGHT } },
        };

        private const float CELL_SIZE = 4f;

        #region Variables
        // Data
        [SerializeField]
        private int _fieldX = 7;

        [SerializeField]
        private int _fieldY = 16;

        private Block[,] _field;

        private Mino _current;

        private Node[] _nodes;

        private (int x, int y) _playerPos;

        private Mino[] _putMinoPatterns;

        private int[] _putMinoRotateCounts;

        private bool _putKey;

        [SerializeField]
        private FieldUIView _fieldUIView;

        // View
        [SerializeField]
        private MinoView[] _minoPrefabs;

        private MinoView _currentView;

        [SerializeField]
        private Transform _minoViewParent;

        [SerializeField]
        private ParticleSystem _fogs;

        private MinoView.Block[,] _fieldView;

        [SerializeField]
        private MinoView.Block[] _startBlocks;

        // Control
        [SerializeField]
        private TouchHandler _touchHandler;

        [SerializeField]
        private Camera _camera;

        private int _touchPanelLayer;

        [SerializeField]
        private Transform _player;

        [SerializeField]
        private LineRenderer _routeLine;

        [SerializeField]
        private Transform _routeArrow;

        // UI View
        [SerializeField]
        private TouchHandler[] _minoViewPanels;

        [SerializeField]
        private RectTransform[] _minoViewPrefabs;

        private bool[] _isDragMinoViewPanels;
        #endregion // Variables

        #region CommonMethod
        public T[] GetComponentsInChildrenWithoutSelf<T>(GameObject self) where T : Component
        {
            return self.GetComponentsInChildren<T>().Where(c => self != c.gameObject).ToArray();
        }

        private (int x, int y) SpawnIndex => (_fieldX / 2, _fieldY - 3);

        private bool CanPlaced(Mino mino, int x, int y)
        {
            foreach(var kvp in mino.Blocks)
            {
                var offset = kvp.Key;
                int checkX = x + offset.x;
                int checkY = y + offset.y;
                // 指定したミノを指定した位置に置いたとき、壁や地面にぶつかっているかどうか
                if(checkY < 0 || checkX < 0 || checkX >= _fieldX)
                {
                    return false;
                }
                // 指定したミノを指定した位置に置いたとき、いずれかのブロックが設置済みブロックにぶつかっているかどうか
                if(GetBlock((checkX, checkY)) != null)
                {
                    return false;
                }
            }
            return true;
        }

        private Vector3 GetPositionMinoView((int x, int y) index)
        {
            return new Vector3(index.x, 0f, index.y) * CELL_SIZE;
        }
        #endregion // CommonMethod

        private void Awake()
        {
            _fieldUIView.Initialize(new Vector2Int(_fieldX, _fieldY));

            ////////////////////////////////////////
            // Data
            ////////////////////////////////////////
            _field = new Block[_fieldX, _fieldY];
            _putMinoPatterns = new Mino[_minoViewPanels.Length];
            _putMinoRotateCounts = new int[_minoViewPanels.Length];
            for(int i = 0; i < _putMinoPatterns.Length; i++)
            {
                _putMinoPatterns[i] = Mino.Create(Mino.RandomShapeType());
            }
            // ゲーム開始スペース
            (int x, int y) startIndex = (Mathf.CeilToInt(_fieldX / 2), 1);
            foreach(var kvp in BASE_SPACE)
            {
                var offset = kvp.Key;
                var block = new Block();
                _field[startIndex.x + offset.x, startIndex.y + offset.y] = block;
                if(kvp.Value == null) continue;
                foreach(var dir in kvp.Value)
                {
                    block.Walls[(int)dir] = true;
                }
            }
            _nodes = new Node[_fieldX * _fieldY];
            for(int y = 0; y < _fieldY; y++)
            {
                for(int x = 0; x < _fieldX; x++)
                {
                    _nodes[y * _fieldX + x] = new Node(x, y);
                }
            }
            _playerPos = startIndex;

            ////////////////////////////////////////
            // View
            ////////////////////////////////////////
            _fieldView = new MinoView.Block[_fieldX, _fieldY];
            // スタート地点の壁情報
            _fieldView[startIndex.x, startIndex.y] = _startBlocks[0];
            for(int i = 0; i < Block.EIGHT_AROUND_OFFSET.Length; i++)
            {
                var offset = Block.EIGHT_AROUND_OFFSET[i];
                (int x, int y) index = (startIndex.x + offset.x, startIndex.y + offset.y);
                _fieldView[index.x, index.y] = _startBlocks[i + 1];
            }

            _fogs.Simulate(100f);
            _fogs.Play();

            ////////////////////////////////////////
            // Control
            ////////////////////////////////////////
            _touchHandler.OnPointerUpEvent = TouchBlock;
            _isDragMinoViewPanels = new bool[_minoViewPanels.Length];
            for(int i = 0; i < _minoViewPanels.Length; i++)
            {
                int index = i;
                _minoViewPanels[i].OnPointerDownEvent = e => PickMino(index);
                _minoViewPanels[i].OnDragEvent = e => DragMino(e, index);
                _minoViewPanels[i].OnEndDragEvent = e => ReleaseMino(index);
                _minoViewPanels[i].OnPointerClickEvent = e => RotateMino(index);
                var minoView = Instantiate(_minoViewPrefabs[(int)_putMinoPatterns[i].Type], _minoViewPanels[i].transform);

                int count = 0;
                foreach(var kvp in _putMinoPatterns[i].Blocks)
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

            _touchPanelLayer = LayerMask.GetMask("UI");
        }

        private void RefleshMino()
        {
            RefleshField();
            _fieldUIView.DrawMino(_current);

            if(_currentView != null)
            {
                _currentView.transform.localPosition = GetPositionMinoView(_current.Index);
            }
        }

        private void FixMino()
        {
            // ミノと隣接するブロックの壁チェック
            foreach(var kvp in _current.Blocks)
            {
                var offset = kvp.Key;
                var block = kvp.Value;
                for(int i = 0; i < (int)Block.DirectionType.Max; i++)
                {
                    var dir = Block.AROUND_OFFSET[i];
                    // ミノ内の隣接は無視
                    if(_current.Blocks.ContainsKey((offset.x + dir.x, offset.y + dir.y))) continue;

                    (int x, int y) index = (_current.Index.x + offset.x + dir.x, _current.Index.y + offset.y + dir.y);
                    // 盤面外は無視
                    if(index.x < 0 || index.x >= _fieldX || index.y < 0 || index.y >= _fieldY) continue;

                    var fieldBlock = GetBlock((index.x, index.y));
                    // ブロックのない場所は無視
                    if(fieldBlock == null) continue;

                    // 隣接したブロックのどちらかが空いていれば道にする
                    var reverseDir = Block.GetReverseDirection((Block.DirectionType)i);
                    if(!block.Walls[i])
                    {
                        fieldBlock.Walls[(int)reverseDir] = false;
                    }
                    else if(!fieldBlock.Walls[(int)reverseDir])
                    {
                        block.Walls[i] = false;
                    }
                }
            }

            int blockCount = 0;
            foreach(var kvp in _current.Blocks)
            {
                var offset = kvp.Key;
                Block block = kvp.Value;
                int x = _current.X + offset.x;
                int y = _current.Y + offset.y;
                _field[x, y] = block;

                var blockView = _currentView.Blocks[blockCount];
                blockView.Fog.Play();
                _fieldView[x, y] = blockView;
                blockCount++;
            }

            RefleshMino();
        }

        private void RefleshField()
        {
            _fieldUIView.Reflesh(_field);

            for (int x = 0; x < _fieldX; x++)
            {
                for(int y = 0; y < _fieldY; y++)
                {
                    Block block = GetBlock((x, y));
                    var blockView = _fieldView[x, y];
                    if (blockView == null || blockView.Walls == null) continue;

                    for (int i = 0; i < blockView.Walls.Length; i++)
                    {
                        if (blockView.Walls[i] == null) continue;
                        blockView.Walls[i].gameObject.SetActive(block.Walls[i]);
                    }
                }
            }

            HighlightLine();
        }

        private void HighlightLine()
        {
            List<int> aligLines = new List<int>();
            for(int y = 0; y < _fieldY; y++)
            {
                int xCount = 0;
                for(int x = 0; x < _fieldX; x++)
                {
                    var block = _field[x, y];
                    if(block == null) continue;
                    block.IsIlluminated = false;
                    xCount++;
                }
                if(xCount == _fieldX) aligLines.Add(y);
            }

            foreach(int y in aligLines)
            {
                for(int x = 0; x < _fieldX; x++)
                {
                    _field[x, y].IsIlluminated = true;
                }
            }

            GetBlock(_playerPos).IsIlluminated = true;
            foreach(var offset in Block.EIGHT_AROUND_OFFSET)
            {
                (int x, int y) index = (_playerPos.x + offset.x, _playerPos.y + offset.y);
                var block = GetBlock(index);
                if(block != null) block.IsIlluminated = true;
            }

            for(int y = 0; y < _fieldY; y++)
            {
                for(int x = 0; x < _fieldX; x++)
                {
                    var block = _field[x, y];
                    if(block == null) continue;
                    var blockView = _fieldView[x, y];
                    var fog = blockView.Fog;
                    if(fog == null) continue;
                    if(block.IsIlluminated) fog.Stop(false, ParticleSystemStopBehavior.StopEmitting);
                    else fog.Play();
                    if(blockView.Key != null) blockView.Key.gameObject.SetActive(block.IsIlluminated);
                }
            }
        }

        private (int x, int y) GetTouchIndex(Vector2 position, bool isDebug = false)
        {
            // スクリーン座標を元にRayを取得
            var ray = _camera.ScreenPointToRay(position);
            if(!Physics.Raycast(ray, out RaycastHit hit, 300f, _touchPanelLayer))
            {
                return (-1, -1);
            }
            float distance = Vector3.Distance(ray.origin, hit.point);
            if(isDebug) Debug.DrawRay(ray.origin, ray.direction * distance, Color.red, 5);
            var index = GetIndex(hit.point);
            if(isDebug) Debug.Log("index: " + index);
            return index;
        }

        private void TouchBlock(PointerEventData eventData)
        {
            var index = GetTouchIndex(eventData.position, true);
            var block = GetBlock(index);
            if(block == null)
            {
                return;
            }
            var route = GetRoute(_playerPos, index);
            _routeArrow.parent.gameObject.SetActive(route != null);
            if(route == null)
            {
                return;
            }
            TraceRoute(route);
        }

        private void TraceRoute((int x, int y)[] route)
        {
            _playerPos = route[route.Length - 1];
            DrawRoute(route);

            var seq = DOTween.Sequence();
            var offset = Vector3.up * _minoViewParent.position.y + Vector3.back;
            var playerRotate = _player.GetChild(0);
            for(int i = 1; i < route.Length; i++)
            {
                var position = GetPosition(route[i]) + offset;
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

        private void PutMino((int x, int y) index)
        {
            _current.Index = index;
            RefleshMino();
        }

        private (int x, int y) GetIndex(Vector3 worldPosition)
        {
            var helfCellSize = CELL_SIZE / 2f;
            return (
                (int)((worldPosition.x + helfCellSize) / CELL_SIZE),
                (int)((worldPosition.z + helfCellSize) / CELL_SIZE)
            );
        }

        private Vector3 GetPosition((int x, int y) index)
        {
            return new Vector3(index.x * CELL_SIZE, 0f, index.y * CELL_SIZE);
        }

        private void DrawRoute((int x, int y)[] route)
        {
            Vector3[] routePositions = new Vector3[route.Length];
            for(int i = 0; i < route.Length; i++)
            {
                routePositions[i] = GetPosition(route[i]);
            }
            // 最後の座標は、最後から一つ前の方向に１m下げる（矢印表示のため）
            var backOffset = (routePositions[route.Length - 2] - routePositions[route.Length - 1]).normalized;
            routePositions[route.Length - 1] += backOffset;
            _routeLine.positionCount = routePositions.Length;
            _routeLine.SetPositions(routePositions);

            _routeArrow.localPosition = routePositions[route.Length - 1];
            _routeArrow.rotation = Quaternion.LookRotation(-backOffset);
        }

        private (int x, int y)[] GetRoute((int x, int y) start, (int x, int y) goal)
        {
            _nodes.ToList().ForEach(node => node.Initialize());

            var passedPositions = new List<(int x, int y)>();
            var recentTargets = new List<Node>();
            var startNode = GetNode(start);
            startNode.Score(goal, null);
            passedPositions.Add(start);
            recentTargets.Add(GetNode(start));
            var adjacentInfos = new List<Node>();
            Node goalNode = null;

            while(true)
            {
                var currentTarget = recentTargets.OrderBy(info => info.Weight).FirstOrDefault();
                var currentPosition = currentTarget.Position;
                var currentBlock = GetBlock(currentPosition);

                adjacentInfos.Clear();
                for(int i = 0; i < (int)Block.DirectionType.Max; i++)
                {
                    var offset = Block.AROUND_OFFSET[i];
                    (int x, int y) targetPosition = (currentPosition.x + offset.x, currentPosition.y + offset.y);
                    // 対象方向に対して移動できなければ対象外
                    if(currentBlock.Walls[i])
                    {
                        continue;
                    }
                    var reverseDir = Block.GetReverseDirection((Block.DirectionType)i);
                    var targetBlock = GetBlock(targetPosition);
                    if(targetBlock == null || targetBlock.Walls[(int)reverseDir])
                    {
                        continue;
                    }

                    // 計算済みのセルは対象外
                    if(passedPositions.Contains(targetPosition))
                    {
                        continue;
                    }
                    var target = GetNode(targetPosition);
                    if(target == null)
                    {
                        continue;
                    }
                    target.Score(goal, GetNode(currentPosition));
                    adjacentInfos.Add(target);
                }

                // recentTargetsとpassedPositionsを更新
                recentTargets.Remove(currentTarget);
                recentTargets.AddRange(adjacentInfos);
                passedPositions.Add(currentPosition);

                // ゴールが含まれていたらそこで終了
                goalNode = adjacentInfos.FirstOrDefault(info => info.Position == goal);
                if(goalNode != null)
                {
                    break;
                }
                // recentTargetsがゼロだったら行き止まりなので終了
                if(recentTargets.Count == 0)
                {
                    break;
                }
            }

            // ゴールが結果に含まれていない場合は最短経路が見つからなかった
            if(goalNode == null)
            {
                return null;
            }

            // Previousを辿ってセルのリストを作成する
            var route = new List<(int x, int y)>();
            route.Add(goal);
            var routeNode = goalNode;

            while(true)
            {
                if(routeNode.Step == 0)
                {
                    break;
                }
                route.Add(routeNode.Previous);
                routeNode = GetNode(routeNode.Previous);
            }
            route.Reverse();
            return route.ToArray();
        }

        private Node GetNode((int x, int y) position)
        {
            if(position.x < 0 || position.x >= _fieldX || position.y < 0 || position.y >= _fieldY)
            {
                return null;
            }
            return _nodes[position.y * _fieldX + position.x];
        }

        private Block GetBlock((int x, int y) position)
        {
            if(position.x < 0 || position.x >= _fieldX || position.y < 0 || position.y >= _fieldY)
            {
                return null;
            }
            return _field[position.x, position.y];
        }

        #region Control Mino
        private void PickMino(int index)
        {
            _current = _putMinoPatterns[index];

            if(_currentView != null)
            {
                Destroy(_currentView.gameObject);
            }
            _currentView = Instantiate(_minoPrefabs[(int)_current.Type], _minoViewParent);
            foreach(var block in _currentView.Blocks) block.Fog.Simulate(100f);

            _currentView.transform.position = Vector3.one * 1000f;
            for(int i = 0; i < _putMinoRotateCounts[index]; i++)
            {
                _currentView.Rotate();
            }

            _currentView.RefreshWalls(_current);
        }

        private void DragMino(PointerEventData eventData, int panelId)
        {
            _isDragMinoViewPanels[panelId] = true;
            if(_current == null)
            {
                return;
            }
            var index = GetTouchIndex(eventData.position);
            if(_current.Index == index)
            {
                return;
            }
            var block = GetBlock(index);
            if(block != null)
            {
                return;
            }
            PutMino(index);
        }

        private void ReleaseMino(int index)
        {
            _isDragMinoViewPanels[index] = false;

            if(_current == null)
            {
                return;
            }

            if(!CanPlaced(_current, _current.X, _current.Y))
            {
                _current = null;
                Destroy(_currentView.gameObject);
                _currentView = null;
                RefleshMino();
                return;
            }

            FixMino();

            for(int i = 0; i < _current.Blocks.Values.Count; i++)
            {
                var block = _current.Blocks.Values.ElementAt(i);
                if(block.HasKey)
                {
                    var blockView = _currentView.Blocks[i];
                    blockView.Key = Instantiate(_keyPrefab, blockView.Fog.transform.parent);
                    blockView.Key.eulerAngles = _keyPrefab.localEulerAngles;
                    blockView.Key.gameObject.SetActive(block.IsIlluminated);
                }
            }

            _current = null;
            _currentView = null;

            var shapeType = Mino.RandomShapeType();
            _putMinoPatterns[index] = Mino.Create(shapeType);
            if(!_putKey)
            {
                int blockCount = 0;
                foreach(var block in _field) if(block != null) blockCount++;
                if(blockCount > _fieldX * _fieldY * 0.3f)
                {
                    _putMinoPatterns[index].PutKey();
                    _putKey = true;
                }
            }

            var minoViewPanel = _minoViewPanels[index];
            Destroy(minoViewPanel.transform.GetChild(0).gameObject);
            var minoView = Instantiate(_minoViewPrefabs[(int)shapeType], minoViewPanel.transform);
            _putMinoRotateCounts[index] = 0;

            int count = 0;
            foreach(var kvp in _putMinoPatterns[index].Blocks)
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

        [SerializeField]
        private Transform _keyPrefab;

        private void RotateMino(int index)
        {
            if(_isDragMinoViewPanels[index])
            {
                return;
            }
            _current.Rotate();
            var rotateCount = _putMinoRotateCounts[index] + 1;
            if(rotateCount >= 4) rotateCount = 0;
            _putMinoRotateCounts[index] = rotateCount;
            _minoViewPanels[index].transform.GetChild(0).localEulerAngles = Vector3.forward * rotateCount * -90f;
        }
        #endregion // Control Mino
    }
}
