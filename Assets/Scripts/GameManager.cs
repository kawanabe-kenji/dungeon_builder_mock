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
        /*
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
        */

        private const float CELL_SIZE = 4f;

        #region Variables
        [SerializeField]
        private Vector2Int _fieldSize = new Vector2Int(7, 12);

        private FieldManager _fieldMgr;
        /*
        // Data
        [SerializeField]
        private int _fieldX = 7;

        [SerializeField]
        private int _fieldY = 16;

        private Block[,] _field;

        private Mino _current;
        */

        private Vector2Int _playerPos;

        //private Mino[] _putMinoPatterns;

        private int[] _putMinoRotateCounts;

        private bool _putKey;

        [SerializeField]
        private FieldUIView _fieldUIView;

        [SerializeField]
        private FieldView _fieldView;

        private RouteCalculator _routeCalc;

        // Control
        [SerializeField]
        private ControlManager _controlMgr;

        [SerializeField]
        private TouchHandler _touchHandler;

        //[SerializeField]
        //private Camera _camera;

        //private int _touchPanelLayer;

        [SerializeField]
        private Transform _player;

        [SerializeField]
        private RouteView _routeView;

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

        /*
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
        */
        #endregion // CommonMethod

        private void Awake()
        {
            ////////////////////////////////////////
            // Data
            ////////////////////////////////////////
            // ゲーム開始のフィールド座標
            Vector2Int startPos = new Vector2Int(Mathf.CeilToInt(_fieldSize.x / 2), 1);
            _fieldMgr = new FieldManager(_fieldSize, startPos, _minoViewPanels.Length);
            /*
            _field = new Block[_fieldX, _fieldY];
            _putMinoPatterns = new Mino[_minoViewPanels.Length];
            */
            _putMinoRotateCounts = new int[_minoViewPanels.Length];
            /*
            for(int i = 0; i < _putMinoPatterns.Length; i++)
            {
                _putMinoPatterns[i] = Mino.Create(Mino.RandomShapeType());
            }
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
            */
            _playerPos = startPos;

            // UI View
            _fieldUIView.Initialize(_fieldMgr.FieldSize);

            _routeCalc = new RouteCalculator(_fieldMgr.FieldSize);

            ////////////////////////////////////////
            // View
            ////////////////////////////////////////
            _fieldView.Initialize(_fieldMgr.FieldSize, startPos);

            ////////////////////////////////////////
            // Control
            ////////////////////////////////////////
            _controlMgr.Initialize();

            _touchHandler.OnPointerUpEvent = TouchBlock;
            _isDragMinoViewPanels = new bool[_minoViewPanels.Length];
            for(int i = 0; i < _minoViewPanels.Length; i++)
            {
                int index = i;
                _minoViewPanels[i].OnPointerDownEvent = e => PickMino(index);
                _minoViewPanels[i].OnDragEvent = e => DragMino(e, index);
                _minoViewPanels[i].OnEndDragEvent = e => ReleaseMino(index);
                _minoViewPanels[i].OnPointerClickEvent = e => RotateMino(index);
                var minoView = Instantiate(_minoViewPrefabs[(int)_fieldMgr.PickableMinos[i].Type], _minoViewPanels[i].transform);

                int count = 0;
                foreach(var kvp in _fieldMgr.PickableMinos[i].Blocks)
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

        private void FixMino()
        {
            _fieldMgr.PutMino(_fieldMgr.PickedMino);
            /*
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

            foreach(var kvp in _current.Blocks)
            {
                var offset = kvp.Key;
                Block block = kvp.Value;
                int x = _current.X + offset.x;
                int y = _current.Y + offset.y;
                _field[x, y] = block;
            }
            */

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
            /*
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

            GetBlock((_playerPos.x, _playerPos.y)).IsIlluminated = true;
            foreach(var offset in Block.EIGHT_AROUND_OFFSET)
            {
                (int x, int y) index = (_playerPos.x + offset.x, _playerPos.y + offset.y);
                var block = GetBlock(index);
                if(block != null) block.IsIlluminated = true;
            }
            */

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

        public static Vector3 GetPosition(Vector2Int fieldPosition)
        {
            return new Vector3(fieldPosition.x * CELL_SIZE, 0f, fieldPosition.y * CELL_SIZE);
        }

        /*
        private Block GetBlock((int x, int y) position)
        {
            if(position.x < 0 || position.x >= _fieldX || position.y < 0 || position.y >= _fieldY) return null;
            return _field[position.x, position.y];
        }
        */

        #region Control Mino
        private void PickMino(int index)
        {
            _fieldMgr.PickMino(index);
            //_current = _putMinoPatterns[index];
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
            _isDragMinoViewPanels[index] = false;

            var pickedMino = _fieldMgr.PickedMino;
            if (pickedMino == null) return;

            if(!_fieldMgr.CanPutMino(pickedMino))
            {
                _fieldMgr.ReleaseMino();
                //_current = null;
                _fieldView.DestroyCurrentMino();
                RefreshMino();
                return;
            }

            FixMino();

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

            var minoViewPanel = _minoViewPanels[index];
            Destroy(minoViewPanel.transform.GetChild(0).gameObject);
            var minoView = Instantiate(_minoViewPrefabs[(int)shapeType], minoViewPanel.transform);
            _putMinoRotateCounts[index] = 0;

            int count = 0;
            foreach(var kvp in _fieldMgr.PickableMinos[index].Blocks)
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

        private void RotateMino(int index)
        {
            if(_isDragMinoViewPanels[index]) return;

            _fieldMgr.PickedMino.Rotate();
            var rotateCount = _putMinoRotateCounts[index] + 1;
            if(rotateCount >= 4) rotateCount = 0;
            _putMinoRotateCounts[index] = rotateCount;
            _minoViewPanels[index].transform.GetChild(0).localEulerAngles = Vector3.forward * rotateCount * -90f;
        }
        #endregion // Control Mino
    }
}
