using UnityEngine;
using UnityEngine.Events;

public class SimpleWireDrawer : MonoBehaviour
{
    [Header("Clickable Objects")]
    public GameObject object1;
    public GameObject object2;

    [Header("Sprites To Enable")]
    public GameObject sprite1;
    public GameObject sprite2;

    [Header("Camera")]
    public Camera mainCamera;
    public Transform camPosAfterObj1;
    public Transform camPosAfterObj2;
    public Transform camPosFinal;
    public float cameraMoveSpeed = 2f;

    bool obj1Clicked = false;
    bool obj2Clicked = false;

    // ========== ORIGINAL VARIABLES ==========
    [Header("Points (assign in Inspector)")]
    public Transform startPoint;
    public Transform[] midPoints;
    public Transform endPoint;

    [Header("Line & Glow")]
    public LineRenderer line;
    public ParticleSystem glowEffect;

    [Header("Raycast Settings")]
    public LayerMask clickableLayers;

    [Header("Drawing")]
    public float drawDuration = 1.5f;

    [Header("On Finish Event")]
    public UnityEvent onWireFinished;

    bool isDrawing = false;
    bool hasDrawn = false;
    float elapsed = 0f;

    Vector3[] pathPoints;
    float[] segmentLengths;
    float totalLength;
    bool allowStart = false;
    // =============================================


    void Awake()
    {
        if (line == null)
        {
            Debug.LogError("LineRenderer NOT assigned in Inspector!");
            return;
        }

        line.enabled = false;

        if (glowEffect)
        {
            glowEffect.Stop();
            glowEffect.Clear();
            glowEffect.gameObject.SetActive(false);
        }

        sprite1.SetActive(false);
        sprite2.SetActive(false);
        object1.SetActive(true);
        object2.SetActive(false);
    }


    void Update()
    {
        HandleClick();
        DrawUpdate();
    }


    // ================= CLICK SYSTEM =================
    void HandleClick()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Debug.Log("Raycast Fired");

            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, Mathf.Infinity, clickableLayers))
            {
                // -------- OBJ1 CLICK --------
                if (hit.collider.gameObject.CompareTag("Obj1"))
                {
                    Debug.Log("Obj1 Clicked");
                    obj1Clicked = true;

                    object1.SetActive(false);
                    sprite1.SetActive(true);
                    object2.SetActive(true);

                    StartCoroutine(MoveCamera(camPosAfterObj1));
                }

                // -------- OBJ2 CLICK --------
                else if (hit.collider.gameObject == object2 && !obj2Clicked)
                {
                    Debug.Log("Obj2 Clicked");
                    obj2Clicked = true;

                    object2.SetActive(false);
                    sprite2.SetActive(true);

                    StartCoroutine(MoveCamera(camPosAfterObj2));
                }

                CheckBoth();
            }
        }
    }


    void CheckBoth()
    {
        if (obj1Clicked && obj2Clicked && !isDrawing && !hasDrawn)
        {
            StartCoroutine(StartFinal());
        }
    }


    System.Collections.IEnumerator StartFinal()
    {
        yield return MoveCamera(camPosFinal);
        allowStart = true;
        OnEnable();
    }


    System.Collections.IEnumerator MoveCamera(Transform target)
    {
        Vector3 startPos = mainCamera.transform.position;
        Quaternion startRot = mainCamera.transform.rotation;

        float t = 0;

        while (t < 1)
        {
            t += Time.deltaTime * cameraMoveSpeed;
            mainCamera.transform.position = Vector3.Lerp(startPos, target.position, t);
            mainCamera.transform.rotation = Quaternion.Lerp(startRot, target.rotation, t);
            yield return null;
        }
    }


    // =============== ORIGINAL LOGIC ===============
    void OnEnable()
    {
        if (!allowStart) return;

        if (hasDrawn)
        {
            BuildPath();
            SetFinalLine();
            return;
        }

        if (!startPoint || !endPoint)
        {
            Debug.LogWarning("Start or End point missing.");
            return;
        }

        BuildPath();
        PrepareLine();
        StartDrawing();
    }


    void OnDisable()
    {
        isDrawing = false;
    }


    void BuildPath()
    {
        int midCount = midPoints != null ? midPoints.Length : 0;

        pathPoints = new Vector3[midCount + 2];
        pathPoints[0] = startPoint.position;

        for (int i = 0; i < midCount; i++)
        {
            pathPoints[i + 1] = midPoints[i] ? midPoints[i].position : pathPoints[i];
        }

        pathPoints[pathPoints.Length - 1] = endPoint.position;

        segmentLengths = new float[pathPoints.Length - 1];
        totalLength = 0f;

        for (int i = 0; i < segmentLengths.Length; i++)
        {
            segmentLengths[i] = Vector3.Distance(pathPoints[i], pathPoints[i + 1]);
            totalLength += segmentLengths[i];
        }
    }


    void PrepareLine()
    {
        line.positionCount = pathPoints.Length;

        for (int i = 0; i < pathPoints.Length; i++)
            line.SetPosition(i, pathPoints[0]);

        line.enabled = true;
    }


    void StartDrawing()
    {
        elapsed = 0f;
        isDrawing = true;
        hasDrawn = false;

        if (glowEffect)
        {
            glowEffect.gameObject.SetActive(true);
            glowEffect.Play();
        }
    }


    void DrawUpdate()
    {
        if (!isDrawing) return;

        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / drawDuration);

        float targetDistance = t * totalLength;

        Vector3 tipPosition = pathPoints[0];
        float acc = 0f;
        int currentSeg = 0;

        // Find segment
        for (int i = 0; i < segmentLengths.Length; i++)
        {
            if (acc + segmentLengths[i] >= targetDistance)
            {
                currentSeg = i;
                float segT = (targetDistance - acc) / segmentLengths[i];
                tipPosition = Vector3.Lerp(pathPoints[i], pathPoints[i + 1], segT);
                break;
            }
            acc += segmentLengths[i];
        }

        // Build temporary visual
        Vector3[] linePts = new Vector3[pathPoints.Length];

        for (int i = 0; i < linePts.Length; i++)
        {
            if (i <= currentSeg)
                linePts[i] = pathPoints[i];
            else
                linePts[i] = tipPosition;
        }

        line.SetPositions(linePts);

        if (glowEffect)
            glowEffect.transform.position = tipPosition;

        if (t >= 1f)
            FinishDrawing();
    }


    void FinishDrawing()
    {
        isDrawing = false;
        hasDrawn = true;

        SetFinalLine();

        if (glowEffect)
        {
            glowEffect.Stop();
            glowEffect.Clear();
            glowEffect.gameObject.SetActive(false);
        }

        sprite1.SetActive(false);
        sprite2.SetActive(false);

        onWireFinished?.Invoke();
    }


    void SetFinalLine()
    {
        line.positionCount = pathPoints.Length;
        line.SetPositions(pathPoints);
        line.enabled = true;
    }
}
