using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SwordHitToggle : MonoBehaviour
{
    [Header("Toggle Groups")]
    [SerializeField] private GameObject[] objectsToEnable;
    [SerializeField] private GameObject[] objectsToDisable;

    [Header("Shader Settings")]
    [SerializeField] private string weightProperty = "_Weight";
    [SerializeField] private float transitionDuration = 0.8f;

    [Header("Hit Protection")]
    [SerializeField] private float hitCooldown = 0.2f;

    private bool toggledState = false;
    private float lastHitTime = -999f;

    // Prevent multiple coroutines fighting over the same object
    private readonly Dictionary<GameObject, Coroutine> runningTransitions = new Dictionary<GameObject, Coroutine>();

    public void Toggle()
    {
        if (Time.time < lastHitTime + hitCooldown)
        {
            Debug.Log($"[{gameObject.name}] Toggle blocked by cooldown.");
            return;
        }

        lastHitTime = Time.time;
        toggledState = !toggledState;

        Debug.Log($"[{gameObject.name}] TOGGLED STATE = {toggledState}");

        foreach (GameObject obj in objectsToEnable)
        {
            if (obj == null) continue;
            StartStateTransition(obj, toggledState);
            
        }

        foreach (GameObject obj in objectsToDisable)
        {
            if (obj == null) continue;
            StartStateTransition(obj, !toggledState);
        }
    }

    private void StartStateTransition(GameObject obj, bool turnOn)
    {
        if (runningTransitions.TryGetValue(obj, out Coroutine existing) && existing != null)
        {
            StopCoroutine(existing);
        }

        Coroutine newRoutine = StartCoroutine(SetObjectStateWithShader(obj, turnOn));
        runningTransitions[obj] = newRoutine;
    }

    private IEnumerator SetObjectStateWithShader(GameObject obj, bool turnOn)
    {
        Debug.Log($"[{gameObject.name}] Transitioning {obj.name}, turnOn = {turnOn}");

        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>(true);
        Debug.Log($"[{gameObject.name}] {obj.name} renderer count = {renderers.Length}");

        if (turnOn)
        {
            obj.SetActive(true);

            SetWeight(renderers, 0f);

            float t = 0f;
            while (t < transitionDuration)
            {
                t += Time.deltaTime;
                float weight = Mathf.Lerp(0f, 1f, t / transitionDuration);
                SetWeight(renderers, weight);
                yield return null;
            }

            SetWeight(renderers, 1f);
        }
        else
        {
            SetWeight(renderers, 1f);

            float t = 0f;
            while (t < transitionDuration)
            {
                t += Time.deltaTime;
                float weight = Mathf.Lerp(1f, 0f, t / transitionDuration);
                SetWeight(renderers, weight);
                yield return null;
            }

            SetWeight(renderers, 0f);
            obj.SetActive(false);
        }

        if (runningTransitions.ContainsKey(obj))
        {
            runningTransitions[obj] = null;
        }
    }

    private void SetWeight(Renderer[] renderers, float value)
    {
        foreach (Renderer rend in renderers)
        {
            if (rend == null) continue;

            Material[] mats = rend.materials;
            foreach (Material mat in mats)
            {
                if (mat == null) continue;

                if (mat.HasProperty(weightProperty))
                {
                    mat.SetFloat(weightProperty, value);
                    Debug.Log($"[{gameObject.name}] Set {mat.name} {weightProperty} = {value}");
                }
                else
                {
                    Debug.LogWarning($"[{gameObject.name}] Material {mat.name} is missing property '{weightProperty}'");
                }
            }
        }
    }
}