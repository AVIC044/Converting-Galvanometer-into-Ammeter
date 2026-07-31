using UnityEngine;
using UnityEngine.InputSystem;

public class MagnetTestMove : MonoBehaviour
{
    [SerializeField] private float speed = 0.8f;

    private Mouse mouse;

    private void Awake()
    {
        mouse = Mouse.current;
    }

    private void Update()
    {
        if (mouse == null)
            return;

        if (mouse.leftButton.isPressed)
        {
            float delta = mouse.delta.ReadValue().y;

            transform.Translate(Vector3.up * delta * speed * Time.deltaTime, Space.World);
        }
    }
}