using UnityEngine;
using System.Collections.Generic;

namespace DancingLineFanmade.Level.InvertedWorld
{
    [DisallowMultipleComponent]
    public class RuntimeRoadManager : MonoBehaviour
    {
        [SerializeField] private GameObject roadPrefab;
        [SerializeField] private float roadWidth = 2f;
        [SerializeField] private float roadHeight = 1f;
        [SerializeField] private int poolSize = 100;
        [SerializeField] private float recycleDistance = 150f;

        private readonly Queue<Transform> roadPool = new Queue<Transform>();
        private readonly List<Transform> activeRoads = new List<Transform>();
        private Transform roadHolder;
        private Transform currentRoad;
        private Vector3 segmentStartPosition;
        private MaterialPropertyBlock propBlock;
        private int startTimeID;

        private Vector3 PlayerGroundPosition => 
            new Vector3(Player.Instance.transform.position.x, 0f, Player.Instance.transform.position.z);

        private Vector3 VisualOffset => 
            new Vector3(0f, -0.5f * (roadHeight + 1f), 0f);

        private void Awake()
        {
            roadHolder = new GameObject("RuntimeRoadHolder").transform;
            propBlock = new MaterialPropertyBlock();
            startTimeID = Shader.PropertyToID("_StartTime");
            InitializePool();
        }

        private void Start()
        {
            segmentStartPosition = PlayerGroundPosition + VisualOffset;
            if (Player.Instance != null)
            {
                Player.Instance.OnTurn.AddListener(OnPlayerTurn);
            }
            SpawnSegment();
        }

        private void Update()
        {
            if (LevelManager.GameState != GameStatus.Playing) return;

            if (currentRoad != null)
            {
                Vector3 currentEndPos = PlayerGroundPosition + VisualOffset;
                UpdateSegmentTransform(currentRoad, segmentStartPosition, currentEndPos);
            }

            CheckRecycle();
        }

        private void InitializePool()
        {
            for (int i = 0; i < poolSize; i++)
            {
                GameObject obj = Instantiate(roadPrefab, roadHolder);
                obj.SetActive(false);
                roadPool.Enqueue(obj.transform);
            }
        }

        private Transform GetFromPool()
        {
            if (roadPool.Count == 0)
            {
                GameObject obj = Instantiate(roadPrefab, roadHolder);
                obj.SetActive(false);
                return obj.transform;
            }
            return roadPool.Dequeue();
        }

        private void ReturnToPool(Transform t)
        {
            t.gameObject.SetActive(false);
            roadPool.Enqueue(t);
        }

        private void OnPlayerTurn()
        {
            if (currentRoad != null)
            {
                Vector3 turnPos = PlayerGroundPosition + VisualOffset;
                UpdateSegmentTransform(currentRoad, segmentStartPosition, turnPos);
            }

            segmentStartPosition = PlayerGroundPosition + VisualOffset;
            SpawnSegment();
        }

        private void SpawnSegment()
        {
            currentRoad = GetFromPool();
            activeRoads.Add(currentRoad);
            
            currentRoad.position = segmentStartPosition;
            currentRoad.rotation = Player.Instance.transform.rotation;
            currentRoad.localScale = Vector3.zero;
            currentRoad.gameObject.SetActive(true);

            Renderer r = currentRoad.GetComponent<Renderer>();
            if (r != null)
            {
                r.GetPropertyBlock(propBlock);
                propBlock.SetFloat(startTimeID, Time.time);
                r.SetPropertyBlock(propBlock);
            }
        }

        private void UpdateSegmentTransform(Transform segment, Vector3 start, Vector3 end)
        {
            Vector3 center = (start + end) * 0.5f;
            center.y = start.y;
            float distance = Vector3.Distance(new Vector3(start.x, 0, start.z), new Vector3(end.x, 0, end.z));

            segment.position = center;
            segment.localScale = new Vector3(roadWidth, roadHeight, distance);
        }

        private void CheckRecycle()
        {
            if (activeRoads.Count > 0)
            {
                Transform oldest = activeRoads[0];
                if (oldest != currentRoad && Vector3.Distance(oldest.position, Player.Instance.transform.position) > recycleDistance)
                {
                    ReturnToPool(oldest);
                    activeRoads.RemoveAt(0);
                }
            }
        }
    }
}