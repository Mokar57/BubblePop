using UnityEngine;

public class CameraMFollow : MonoBehaviour
{
    public GameObject player;      // Oyuncu objesi
    public Vector3 offset;         // Kameranın oyuncudan uzaklığı
    public float focusSpeed = 5f;  // Sağ tıka basıldığında yaklaşma hızı

    private Camera cam;
    private bool isRightClicking = false;
    private Vector3 targetPosition;

    void Start()
    {
        cam = Camera.main;
    }

    void Update()
    {
        // Sağ tık durumunu kontrol et
        if (Input.GetMouseButtonDown(2))
            isRightClicking = true;
        if (Input.GetMouseButtonUp(2))
            isRightClicking = false;
    }

    void LateUpdate()
    {
        Vector3 playerPos = player.transform.position;

        if (isRightClicking)
        {
            // Farenin dünya konumunu al
            Vector3 mousePos = cam.ScreenToWorldPoint(Input.mousePosition);
            mousePos.z = 0f;

            // Oyuncu ile fare arasında bir nokta hesapla
            Vector3 focusPoint = Vector3.Lerp(playerPos, mousePos, 0.5f);

            // Offset ekle ve kamerayı yumuşak şekilde oraya taşı
            targetPosition = focusPoint + offset;
            transform.position = Vector3.Lerp(transform.position, targetPosition, focusSpeed * Time.deltaTime);
        }
        else
        {
            // Kamera oyuncunun tam üstünde olsun (gecikmesiz)
            transform.position = playerPos + offset;
        }
    }
}