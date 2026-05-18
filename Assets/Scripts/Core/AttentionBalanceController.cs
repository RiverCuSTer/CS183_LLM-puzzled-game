using UnityEngine;
using System;
using System.Collections;

public class AttentionBalanceController : MonoBehaviour
{
    [Header("Balance Root")]
    public GameObject balanceRoot;

    [Header("Trays")]
    public AttentionTray trayWho;
    public AttentionTray trayAm;
    public AttentionTray trayI;

    [Header("Balance Visual")]
    public Transform balanceBeam;
    public float wobbleDuration = 2.0f;
    public float maxAngle = 8.0f;
    public float wobbleSpeed = 12.0f;

    private Action stableCallback;
    private Coroutine balanceRoutine;

    void Start()
    {
        if (balanceRoot == null)
            balanceRoot = gameObject;

        if (balanceRoot != null)
            balanceRoot.SetActive(false);
    }

    public void ShowBalance()
    {
        if (balanceRoot != null)
            balanceRoot.SetActive(true);
    }

    public void HideBalance()
    {
        if (balanceRoot != null)
            balanceRoot.SetActive(false);
    }

    public void ShowBalanceWithWeights(int whoWeight, int amWeight, int iWeight, System.Action onStable)
    {
        Debug.Log("ShowBalanceWithWeights called");
        Debug.Log("whoWeight = " + whoWeight);
        Debug.Log("amWeight = " + amWeight);
        Debug.Log("iWeight = " + iWeight);

        stableCallback = onStable;

        if (balanceRoot == null)
            balanceRoot = gameObject;

        if (balanceRoot != null)
            balanceRoot.SetActive(true);

        if (trayWho != null)
        {
            Debug.Log("trayWho assigned");
            trayWho.SetWeightCount(whoWeight);
        }
        else
        {
            Debug.LogError("trayWho is NULL");
        }

        if (trayAm != null)
        {
            Debug.Log("trayAm assigned");
            trayAm.SetWeightCount(amWeight);
        }
        else
        {
            Debug.LogError("trayAm is NULL");
        }

        if (trayI != null)
        {
            Debug.Log("trayI assigned");
            trayI.SetWeightCount(iWeight);
        }
        else
        {
            Debug.LogError("trayI is NULL");
        }

        if (balanceRoutine != null)
            StopCoroutine(balanceRoutine);

        balanceRoutine = StartCoroutine(BalanceStableRoutine());
    }
    IEnumerator BalanceStableRoutine()
    {
        float timer = 0f;

        while (timer < wobbleDuration)
        {
            timer += Time.deltaTime;

            float normalized = timer / wobbleDuration;
            float currentAngle = Mathf.Lerp(maxAngle, 0f, normalized);
            float z = Mathf.Sin(timer * wobbleSpeed) * currentAngle;

            if (balanceBeam != null)
                balanceBeam.localRotation = Quaternion.Euler(0f, 0f, z);

            yield return null;
        }

        if (balanceBeam != null)
            balanceBeam.localRotation = Quaternion.identity;

        Debug.Log("Balance is stable");

        if (stableCallback != null)
            stableCallback.Invoke();
    }
}