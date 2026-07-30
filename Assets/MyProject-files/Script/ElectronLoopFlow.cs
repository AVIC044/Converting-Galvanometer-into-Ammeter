using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class SkipInterval
{
    public Transform pointA;
    public Transform pointB;
}

public class ElectronLoopFlow : MonoBehaviour
{
    [Header("Setup")]
    public GameObject spherePrefab;

    [Header("Electron Path")]
    public List<Transform> electronPath = new();

    [Header("Flow Settings")]
    public float moveSpeed = 0.3f;
    public float sphereSpacing = 0.15f;
    public int maxPoolSize = 100;
    public int minPoolSize = 5;

    [Header("Skip Intervals")]
    public List<SkipInterval> skipIntervals = new();

    private FlowData flow;

    private void Awake()
    {
        flow = new FlowData(this);
    }

    private void Start()
    {
        StartElectronFlow();
    }

    private void Update()
    {
        flow.Tick(moveSpeed);
    }

    public void StartElectronFlow()
    {
        flow.Build(electronPath);
    }

    public void StopElectronFlow()
    {
        flow.Disable();
    }

    //========================================================

    private class FlowData
    {
        private readonly ElectronLoopFlow owner;

        private readonly List<GameObject> spheres = new();
        private readonly List<float> distances = new();

        private readonly List<Vector3> path = new();
        private readonly List<float> segments = new();
        private readonly List<bool> segmentSkip = new();

        private float totalLength;
        private bool active;

        // Length used for skip segments so electrons cross them almost instantly
        private const float SkipSegmentLength = 0.001f;

        public FlowData(ElectronLoopFlow owner)
        {
            this.owner = owner;
        }

        public void Build(List<Transform> points)
        {
            Disable();

            path.Clear();
            segments.Clear();
            segmentSkip.Clear();
            distances.Clear();
            totalLength = 0f;

            if (points == null || points.Count < 2)
                return;

            // Filter valid transforms
            List<Transform> validPoints = new List<Transform>();
            foreach (Transform t in points)
            {
                if (t != null)
                    validPoints.Add(t);
            }

            if (validPoints.Count < 2)
                return;

            // Close the loop (reference back to the first transform)
            validPoints.Add(validPoints[0]);

            foreach (Transform t in validPoints)
                path.Add(t.position);

            // Calculate segment lengths, marking skip segments as near-zero length
            for (int i = 0; i < validPoints.Count - 1; i++)
            {
                Transform ta = validPoints[i];
                Transform tb = validPoints[i + 1];

                bool isSkip = IsSkipSegment(ta, tb);
                float len = isSkip
                    ? SkipSegmentLength
                    : Vector3.Distance(ta.position, tb.position);

                if (len > 0.0001f)
                {
                    segments.Add(len);
                    segmentSkip.Add(isSkip);
                    totalLength += len;
                }
            }

            if (totalLength <= 0.001f)
                return;

            if (owner.spherePrefab == null)
            {
                Debug.LogWarning("ElectronLoopFlow: spherePrefab not assigned.");
                return;
            }

            // Create pool if needed
            while (spheres.Count < owner.maxPoolSize)
            {
                GameObject s = GameObject.Instantiate(
                    owner.spherePrefab,
                    owner.transform);

                s.SetActive(false);
                spheres.Add(s);
            }

            int count = Mathf.Clamp(
                Mathf.FloorToInt(totalLength / owner.sphereSpacing),
                owner.minPoolSize,
                owner.maxPoolSize);

            float step = totalLength / count;

            // Offset electrons so none starts exactly on Startpoint.
            float offset = step * 0.5f;

            for (int i = 0; i < spheres.Count; i++)
                spheres[i].SetActive(i < count);

            for (int i = 0; i < count; i++)
            {
                float d = offset + step * i;

                if (d >= totalLength)
                    d -= totalLength;

                distances.Add(d);

                spheres[i].transform.position = GetPosition(d);
            }

            active = true;
        }

        public void Tick(float speed)
        {
            if (!active)
                return;

            float delta = speed * Time.unscaledDeltaTime;

            for (int i = 0; i < distances.Count; i++)
            {
                distances[i] += delta;

                if (distances[i] >= totalLength)
                    distances[i] -= totalLength;

                spheres[i].transform.position = GetPosition(distances[i]);
            }
        }

        public void Disable()
        {
            active = false;

            distances.Clear();

            foreach (GameObject s in spheres)
                s.SetActive(false);
        }

        private bool IsSkipSegment(Transform a, Transform b)
        {
            foreach (SkipInterval skip in owner.skipIntervals)
            {
                if (skip.pointA == null || skip.pointB == null)
                    continue;

                if ((skip.pointA == a && skip.pointB == b) ||
                    (skip.pointA == b && skip.pointB == a))
                    return true;
            }

            return false;
        }

        private Vector3 GetPosition(float distance)
        {
            float d = distance;

            for (int i = 0; i < segments.Count; i++)
            {
                float len = segments[i];

                if (d <= len)
                {
                    if (segmentSkip[i])
                        return path[i + 1];

                    return Vector3.Lerp(path[i], path[i + 1], d / len);
                }

                d -= len;
            }

            return path[0];
        }
    }
}