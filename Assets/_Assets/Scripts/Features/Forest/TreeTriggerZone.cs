using System.Collections;
using UnityEngine;

public class TreeTriggerZone : MonoBehaviour
{
    [SerializeField] private GameObject Tree;
    [SerializeField] private GameObject Bear;
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private float bearActivationDelaySeconds = 3f;

    private Coroutine bearActivationRoutine;

    private void Awake()
    {
        if (Tree != null)
        {
            Tree.SetActive(false);
        }

        if (Bear != null)
        {
            Bear.SetActive(false);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag))
        {
            return;
        }

        if (Tree != null)
        {
            Tree.SetActive(true);
        }

        if (Bear != null)
        {
            if (bearActivationRoutine != null)
            {
                StopCoroutine(bearActivationRoutine);
            }

            bearActivationRoutine = StartCoroutine(ActivateBearAfterDelay());
        }
        else
        {
            Debug.LogWarning($"{nameof(TreeTriggerZone)} on {gameObject.name} missing tree reference.", this);
        }
    }

    private IEnumerator ActivateBearAfterDelay()
    {
        yield return new WaitForSeconds(bearActivationDelaySeconds);

        Bear.SetActive(true);
        bearActivationRoutine = null;
    }
}