using System.Collections.Generic;
using UnityEngine;

public class Elevator : MonoBehaviour
{
    [System.Serializable]
    public class ElevatorDestination
    {
        public string floorName;
        public string sceneName;
        public string spawnId;
    }

    public List<ElevatorDestination> destinations = new List<ElevatorDestination>();

    public void GoToFloor(int index)
    {
        if (index < 0 || index >= destinations.Count)
        {
            Debug.LogWarning("Índice de andar inválido.");
            return;
        }

        ElevatorDestination destination = destinations[index];

        if (SceneTransition.Instance == null)
        {
            Debug.LogWarning("SceneTransition não encontrado.");
            return;
        }

        SceneTransition.Instance.TransitionToScene(destination.sceneName, destination.spawnId);
    }
}