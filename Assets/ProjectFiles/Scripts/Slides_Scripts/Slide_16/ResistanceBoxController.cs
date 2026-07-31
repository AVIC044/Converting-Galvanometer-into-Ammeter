using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ResistanceOption
{
    public int resistancevalue;
    public Animator plugAnimator;
}

public class ResistanceBoxController : MonoBehaviour
{
    [Header("Resistance Options")]
    [SerializeField] private ResistanceOption[] options;

    [Header("Animation")]
    [SerializeField] private float animationLockTime = 0.8f;

    // Fires whenever the total resistance changes (after the plug animation completes)
    public event Action<int> OnResistanceChanged;

    // Fires the moment a specific slot is toggled — index + whether it's now removed (selected)
    public event Action<int, bool> OnPlugToggled;

    private HashSet<int> removedPlugs = new HashSet<int>();

    

    public int OptionCount => options.Length;

    public int CurrentResistance
    {
        get
        {
            int total = 0;

            foreach (int index in removedPlugs)
            {
                total += options[index].resistancevalue;
            }
            // Debug.Log($"[{nameof(ResistanceBoxController)}] Current total resistance: {total} Ω");
            return total;
        }
    }

    private bool isAnimating;

    public void SelectResistance(int index)
    {
        if (isAnimating)
            return;

        if (index < 0 || index >= options.Length)
            return;

        StartCoroutine(ChangeRoutine(index));
    }

    private IEnumerator ChangeRoutine(int index)
    {
        isAnimating = true;

        Animator anim = options[index].plugAnimator;
        bool isNowRemoved;

        if (removedPlugs.Contains(index))
        {
            if (anim != null)
                anim.SetTrigger("Plug_In");

            removedPlugs.Remove(index);
            isNowRemoved = false;
        }
        else
        {
            if (anim != null)
                anim.SetTrigger("Plug_Out");

            removedPlugs.Add(index);
            isNowRemoved = true;
        }

        // Fire immediately so the UI (button color) responds right on click,
        // rather than waiting for the plug animation to finish.
        OnPlugToggled?.Invoke(index, isNowRemoved);

        yield return new WaitForSeconds(animationLockTime);

        isAnimating = false;

        OnResistanceChanged?.Invoke(CurrentResistance);
    }

    public void RestoreAllPlugs()
    {
        foreach (int index in removedPlugs)
        {
            Animator anim = options[index].plugAnimator;

            if (anim != null)
                anim.SetTrigger("Plug_In");

            OnPlugToggled?.Invoke(index, false);
        }

        removedPlugs.Clear();

        OnResistanceChanged?.Invoke(CurrentResistance);
    }
}
