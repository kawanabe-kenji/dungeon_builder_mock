using System.Linq;
using UnityEngine;
using DG.Tweening;

namespace DungeonBuilder
{
    public class FieldView : MonoBehaviour
    {
        private const float CELL_SIZE = 4f;

        [SerializeField]
        private MinoView[] _minoPrefabs;

        private MinoView _currentView;

        [SerializeField]
        private Transform _minoViewParent;

        private MinoView.Block[,] _blocks;

        [SerializeField]
        private MinoView.Block[] _startBlocks;

        [SerializeField]
        private ParticleSystem _fogs;

        [SerializeField]
        private Transform _keyPrefab;

        [SerializeField]
        private Transform _healItemPrefab;

        public float HeightFloor => _minoViewParent.position.y;

        public static Vector2Int GetFieldPosition(Vector3 worldPos)
        {
            var helfCellSize = CELL_SIZE / 2f;
            return new Vector2Int(
                (int)((worldPos.x + helfCellSize) / CELL_SIZE),
                (int)((worldPos.z + helfCellSize) / CELL_SIZE)
            );
        }

        public static Vector3 GetWorldPosition(Vector2Int fieldPos)
        {
            return new Vector3(fieldPos.x * CELL_SIZE, 0f, fieldPos.y * CELL_SIZE);
        }

        public MinoView.Block GetBlock(Vector2Int fieldPos)
        {
            var fieldSize = new Vector2Int(_blocks.GetLength(0), _blocks.GetLength(1));
            if (fieldPos.x < 0 || fieldPos.x >= fieldSize.x || fieldPos.y < 0 || fieldPos.y >= fieldSize.y) return null;
            return _blocks[fieldPos.x, fieldPos.y];
        }

        public void Initialize(Vector2Int fieldSize, Vector2Int startPos)
        {
            _blocks = new MinoView.Block[fieldSize.x, fieldSize.y];

            // スタート地点の壁情報
            _blocks[startPos.x, startPos.y] = _startBlocks[0];
            for (int i = 0; i < Block.EIGHT_AROUND_OFFSET.Length; i++)
            {
                var offset = Block.EIGHT_AROUND_OFFSET[i];
                Vector2Int fieldPos = startPos + offset;
                _blocks[fieldPos.x, fieldPos.y] = _startBlocks[i + 1];
            }

            _fogs.Simulate(100f);
            _fogs.Play();
        }

        public void SetMinoPosition(Vector2Int fieldPos)
        {
            if (_currentView == null) return;
            _currentView.transform.localPosition = new Vector3(fieldPos.x, 0f, fieldPos.y) * CELL_SIZE;
        }

        public void PutMino(Mino mino)
        {
            int count = 0;
            foreach (var kvp in mino.Blocks)
            {
                var offset = kvp.Key;
                var fieldPos = mino.FieldPos + offset;

                var view = _currentView.Blocks[count++];
                _blocks[fieldPos.x, fieldPos.y] = view;

                view.Fog.Play();
                view.IsPlayingFog = true;
            }
        }

        public void Refresh(Block[,] fieldData)
        {
            var fieldX = fieldData.GetLength(0);
            var fieldY = fieldData.GetLength(1);
            for (int x = 0; x < fieldX; x++)
            {
                for (int y = 0; y < fieldY; y++)
                {
                    var data = fieldData[x, y];
                    if (data == null) continue;

                    var view = _blocks[x, y];
                    if (view == null || view.Walls == null) continue;

                    for (int i = 0; i < view.Walls.Length; i++)
                    {
                        if (view.Walls[i] == null) continue;
                        view.Walls[i].gameObject.SetActive(data.Walls[i]);
                    }
                }
            }
        }

        public void HighlightLine(Block[,] fieldData)
        {
            var _fieldX = fieldData.GetLength(0);
            var _fieldY = fieldData.GetLength(1);
            for (int y = 0; y < _fieldY; y++)
            {
                for (int x = 0; x < _fieldX; x++)
                {
                    var data = fieldData[x, y];
                    if (data == null) continue;

                    var view = _blocks[x, y];
                    if (view == null) continue;

                    var fog = view.Fog;
                    if (fog == null) continue;

                    if(data.IsIlluminated == view.IsPlayingFog)
                    {
                        view.IsPlayingFog = !data.IsIlluminated;
                        if (data.IsIlluminated)
                            fog.Stop(false, ParticleSystemStopBehavior.StopEmitting);
                        else
                            fog.Play();

                        var seq = DOTween.Sequence();
                        seq.AppendCallback(() =>
                        {
                            var main = fog.main;
                            main.simulationSpeed = 7.5f;
                        });
                        seq.AppendInterval(0.5f);
                        seq.AppendCallback(() =>
                        {
                            var main = fog.main;
                            main.simulationSpeed = 1f;
                        });
                    }

                    if (view.Key != null)
                    {
                        view.Key.gameObject.SetActive(data.IsIlluminated);
                    }
                }
            }
        }

        public void DestroyCurrentMino()
        {
            if (_currentView == null) return;
            Destroy(_currentView.gameObject);
            _currentView = null;
        }

        public void PickMino(Mino mino, int rotateCount)
        {
            DestroyCurrentMino();

            _currentView = Instantiate(_minoPrefabs[(int)mino.Type], _minoViewParent);
            foreach (var block in _currentView.Blocks) block.Fog.Simulate(100f);

            _currentView.transform.position = Vector3.one * 1000f;
            for (int i = 0; i < rotateCount; i++)
            {
                _currentView.Rotate();
            }

            _currentView.RefreshWalls(mino);
        }

        public void ReleaseMino(Mino mino)
        {
            for (int i = 0; i < mino.Blocks.Values.Count; i++)
            {
                var view = _currentView.Blocks[i];
                var data = mino.Blocks.Values.ElementAt(i);
                if (data.HasKey)
                {
                    view.Key = Instantiate(_keyPrefab, view.Fog.transform.parent);
                    view.Key.eulerAngles = _keyPrefab.localEulerAngles;
                    view.Key.gameObject.SetActive(data.IsIlluminated);
                }
                if (data.HasHealItem)
                {
                    view.HealItem = Instantiate(_healItemPrefab, view.Fog.transform.parent);
                    view.HealItem.eulerAngles = _healItemPrefab.localEulerAngles;
                    view.HealItem.gameObject.SetActive(data.IsIlluminated);
                }
            }

            _currentView = null;
        }
    }
}
