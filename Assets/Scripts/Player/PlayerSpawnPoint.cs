using UnityEngine;

public class PlayerSpawnPoint : MonoBehaviour
{
    [SerializeField] private string spawnID = "default";
    public string SpawnID => spawnID;
}