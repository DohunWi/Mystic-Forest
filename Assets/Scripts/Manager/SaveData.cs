using UnityEngine;

[System.Serializable] // 이게 있어야 JSON으로 변환 가능
public class SaveData
{
    public Vector3 playerPosition; // 플레이어 위치
    public int health;             // 현재 체력
    public int currentSceneIndex;  // 현재 맵 (나중에 씬 이동 구현 시 필요)
}