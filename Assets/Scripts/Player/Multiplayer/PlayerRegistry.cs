using System.Collections.Generic;
using UnityEngine;

public class PlayerRegistry : MonoBehaviour
{
    public static List<Transform> AllPlayers = new List<Transform>();

    public static void Register(Transform player)
    {
        if (!AllPlayers.Contains(player))
            AllPlayers.Add(player);
    }

    public static void Unregister(Transform player)
    {
        if (AllPlayers.Contains(player))
            AllPlayers.Remove(player);
    }

    public static Transform GetClosestPlayer(Vector3 fromPos)
    {
        Transform closest = null;
        float minDist = Mathf.Infinity;

        foreach (var player in AllPlayers)
        {
            if (player == null) continue;

            float dist = Vector3.Distance(fromPos, player.position);
            if (dist < minDist)
            {
                minDist = dist;
                closest = player;
            }
        }

        return closest;
    }

    public static Transform GetWeakestPlayer(Vector3 fromPos)
    {
        Transform weakest = null;
        float lowestHealth = float.MaxValue;

        foreach (var player in AllPlayers)
        {
            if (player == null) continue;

            var health = player.GetComponent<PlayerHealth>();
            if (health == null || health.currentHealth <= 0) continue;

            if (health.currentHealth < lowestHealth)
            {
                lowestHealth = health.currentHealth;
                weakest = player;
            }
        }

        return weakest;
    }

    public static Transform GetWeakestNearbyPlayer(Vector3 fromPos, float maxDistance)
    {
        Transform weakest = null;
        float lowestHealth = float.MaxValue;

        foreach (var player in AllPlayers)
        {
            if (player == null) continue;

            float distance = Vector3.Distance(fromPos, player.position);
            if (distance > maxDistance) continue;

            var health = player.GetComponent<PlayerHealth>();
            if (health == null || health.currentHealth <= 0) continue;

            if (health.currentHealth < lowestHealth)
            {
                lowestHealth = health.currentHealth;
                weakest = player;
            }
        }

        return weakest;
    }
}
