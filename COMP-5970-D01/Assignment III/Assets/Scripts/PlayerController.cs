using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    float moveSpeed = 5f;
    float minX = -2.4f;
    float maxX = 2.4f;
    float minY = -4.5f;
    float maxY = 4.5f;

    public GameObject bulletPrefab;
    public Transform firePoint;
    public float fireRate = 0.25f;

    private float nextFireTime = 0f;
    private Vector2 moveInput;

    public AudioSource audioSource;
    public AudioClip shootSound;    

    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    public void OnAttack(InputValue value)
    {
        if (GameManager.IsGameOver) return;

        if (Time.time >= nextFireTime)
        {
            Shoot();
            nextFireTime = Time.time + fireRate;
        }
    }

    void Shoot()
    {
        audioSource.PlayOneShot(shootSound);
        Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
    }


    private void Update()
    {
        if (GameManager.IsGameOver) return;

        Vector3 movement = new Vector3(moveInput.x, moveInput.y, 0f);
        transform.position += movement * moveSpeed * Time.deltaTime;
        Vector3 pos = transform.position;

        pos.x = Mathf.Clamp(pos.x, minX, maxX);
        pos.y = Mathf.Clamp(pos.y, minY, maxY);

        transform.position = pos;
    }
}