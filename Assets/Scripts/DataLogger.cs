using UnityEngine;
using System.IO;

public class DataLogger : MonoBehaviour
{
    private string filePath;

    private void Awake()
    {
        filePath = Application.dataPath + "/PlayerLog.csv";
        if (!File.Exists(filePath))
        {
            File.WriteAllText(filePath, "Time,Action,PosX,PosY,PosZ,Score\n");
        }
    }

    public void Log(string action, Vector3 position, int score)
    {
        string log = Time.time + "," + action + "," + position.x + "," + position.y + "," + position.z + "," + score;
        File.AppendAllText(filePath, log + "\n");
    }
}
