using UnityEngine;

public interface ITriggerCheckable
{
    bool IsAggroed { get; set; }
    bool IsWithinStrikingDistance { get; set; }
    void setAggroStatus(bool isAggroed);
    void setStrikingDistanceStatus(bool isWithinStrikingDistance);
}
