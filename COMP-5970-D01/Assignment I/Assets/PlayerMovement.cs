using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    float horizontalInput = 0f;

    float moveSpeed = 5f;

    SafeAreaManager safeArea;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        safeArea = FindFirstObjectByType<SafeAreaManager>();
    }

    // Update is called once per frame
    void Update()
    {
        if(Keyboard.current.leftArrowKey.isPressed || Keyboard.current.aKey.isPressed)
        {
            horizontalInput = -1f;
        }
        else if(Keyboard.current.rightArrowKey.isPressed || Keyboard.current.dKey.isPressed)
        {
            horizontalInput = 1f;
        }
        transform.position += Vector3.right * horizontalInput * moveSpeed * Time.deltaTime;
        float clampedX = Mathf.Clamp(transform.position.x, safeArea.LeftX, safeArea.RightX);
        transform.position = new Vector3(clampedX, transform.position.y, transform.position.z);
    }

    private void OnTriggerEnter2D(Collider2D other) {
        FindFirstObjectByType<GameManager>().GameOver();
    }
}
