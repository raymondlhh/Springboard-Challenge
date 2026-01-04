using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private Joystick joystick;
    [SerializeField] private Animator animator;
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationSpeedDegrees = 720f;

    private static readonly int IsWalkingHash = Animator.StringToHash("IsWalking");

    private void Update()
    {
        if (joystick == null)
        {
            return;
        }

        Vector3 input = new Vector3(joystick.Horizontal, 0f, joystick.Vertical);
        float inputSqrMagnitude = input.sqrMagnitude;
        bool isWalking = inputSqrMagnitude > 0.0001f;

        if (inputSqrMagnitude > 1f)
        {
            input.Normalize();
        }

        if (isWalking)
        {
            Quaternion targetRotation = Quaternion.LookRotation(input, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeedDegrees * Time.deltaTime);
        }

        transform.Translate(input * moveSpeed * Time.deltaTime, Space.World);

        if (animator != null)
        {
            animator.SetBool(IsWalkingHash, isWalking);
        }
    }
}