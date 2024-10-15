using UnityEngine;

public class BackgroundFollow : MonoBehaviour
{
    public Transform player; // ��ɫ��Transform
    public float parallaxFactor; // ������������

    void Update()
    {
        // ���ݽ�ɫλ�ú͸������ӵ�������λ��
        Vector3 newPosition = new Vector3(player.position.x * parallaxFactor, transform.position.y, transform.position.z);
        transform.position = newPosition;
    }
}
