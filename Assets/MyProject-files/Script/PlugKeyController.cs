using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class PlugKeyController : MonoBehaviour
{
    public enum AutoActionType { PlugIn, PlugOut }

    [System.Serializable]
    public class PageAutoPlugConfig
    {
        [Tooltip("The page index (0-based) where this auto action triggers")]
        public int pageIndex;

        [Tooltip("Type of automatic action to perform")]
        public AutoActionType actionType = AutoActionType.PlugIn;

        [Tooltip("Delay in seconds before triggering the auto move")]
        public float delay = 0.5f;

        [Tooltip("Event triggered specifically when this page auto-sequence fires")]
        public UnityEvent OnPageAutoSequenceTriggered;
    }

    [Header("Click Settings")]
    [SerializeField] private string clickableTag = "Plugkey";
    [SerializeField] private bool allowClickToMove = false;

    [Header("Plug Positions")]
    [SerializeField] private Transform plugInPosition;
    [SerializeField] private Transform plugOutPosition;

    [Header("Movement Settings")]
    [SerializeField] private float moveDuration = 0.5f;
    [SerializeField] private AnimationCurve moveCurve = AnimationCurve.Linear(0, 0, 1, 1);

    [Header("Initial State")]
    [SerializeField] private bool startPluggedIn = false;

    [Header("Events")]
    public UnityEvent OnPlugIn;
    public UnityEvent OnPlugOut;

    [Header("Automatic Page Sequence Settings")]
    [SerializeField] private List<PageAutoPlugConfig> pageAutoConfigs = new List<PageAutoPlugConfig>();

    [Tooltip("Global event when ANY automatic plug-in completes")]
    public UnityEvent OnAutoPlugIn;

    [Tooltip("Global event when ANY automatic plug-out completes")]
    public UnityEvent OnAutoPlugOut;

    private bool isPluggedIn;
    private bool isMoving = false;
    private Coroutine activeMoveRoutine;

    private void OnEnable()
    {
        PageNavigationController.OnPageChanged += HandlePageChanged;
    }

    private void OnDisable()
    {
        PageNavigationController.OnPageChanged -= HandlePageChanged;
    }

    private void Start()
    {
        isPluggedIn = startPluggedIn;
        SetImmediateState(isPluggedIn);
    }

    private void HandlePageChanged(int pageIndex)
    {
        foreach (var config in pageAutoConfigs)
        {
            if (config != null && config.pageIndex == pageIndex)
            {
                StartCoroutine(ExecuteAutoSequenceRoutine(config));
                break;
            }
        }
    }

    private IEnumerator ExecuteAutoSequenceRoutine(PageAutoPlugConfig config)
    {
        if (config.delay > 0f)
            yield return new WaitForSeconds(config.delay);

        config.OnPageAutoSequenceTriggered?.Invoke();

        if (config.actionType == AutoActionType.PlugIn)
        {
            if (!isPluggedIn)
            {
                yield return StartCoroutine(MovePlugRoutine(true));
                OnAutoPlugIn?.Invoke();
            }
        }
        else if (config.actionType == AutoActionType.PlugOut)
        {
            if (isPluggedIn)
            {
                yield return StartCoroutine(MovePlugRoutine(false));
                OnAutoPlugOut?.Invoke();
            }
        }
    }

    public void TogglePlug()
    {
        if (isMoving) return;

        bool targetState = !isPluggedIn;
        if (activeMoveRoutine != null) StopCoroutine(activeMoveRoutine);
        activeMoveRoutine = StartCoroutine(MovePlugRoutine(targetState));
    }

    public void PlugIn()
    {
        if (isMoving || isPluggedIn) return;
        if (activeMoveRoutine != null) StopCoroutine(activeMoveRoutine);
        activeMoveRoutine = StartCoroutine(MovePlugRoutine(true));
    }

    public void PlugOut()
    {
        if (isMoving || !isPluggedIn) return;
        if (activeMoveRoutine != null) StopCoroutine(activeMoveRoutine);
        activeMoveRoutine = StartCoroutine(MovePlugRoutine(false));
    }

    private IEnumerator MovePlugRoutine(bool plugIn)
    {
        isMoving = true;

        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;

        Transform targetTransform = plugIn ? plugInPosition : plugOutPosition;

        if (targetTransform != null)
        {
            Vector3 endPos = targetTransform.position;
            Quaternion endRot = targetTransform.rotation;

            float elapsed = 0f;
            while (elapsed < moveDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / moveDuration);
                float evalT = moveCurve.Evaluate(t);

                transform.position = Vector3.Lerp(startPos, endPos, evalT);
                transform.rotation = Quaternion.Slerp(startRot, endRot, evalT);

                yield return null;
            }

            transform.position = endPos;
            transform.rotation = endRot;
        }

        isPluggedIn = plugIn;
        isMoving = false;

        if (isPluggedIn)
            OnPlugIn?.Invoke();
        else
            OnPlugOut?.Invoke();
    }

    private void SetImmediateState(bool pluggedIn)
    {
        Transform targetTransform = pluggedIn ? plugInPosition : plugOutPosition;
        if (targetTransform != null)
        {
            transform.position = targetTransform.position;
            transform.rotation = targetTransform.rotation;
        }
    }

    private void OnMouseDown()
    {
        if (allowClickToMove && gameObject.CompareTag(clickableTag))
        {
            TogglePlug();
        }
    }
}