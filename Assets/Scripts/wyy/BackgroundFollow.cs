using UnityEngine;

public class BackgroundFollow : MonoBehaviour
{
    public Transform player; // 角色的Transform
    public float parallaxFactor; // 背景跟随因子

    void Update()
    {
        // 根据角色位置和跟随因子调整背景位置
        Vector3 newPosition = new Vector3(player.position.x * parallaxFactor, transform.position.y, transform.position.z);
        transform.position = newPosition;
    }
}
