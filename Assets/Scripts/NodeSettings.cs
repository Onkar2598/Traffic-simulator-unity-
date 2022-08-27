
using UnityEngine;


[CreateAssetMenu(fileName = "NodeSettings", menuName = "NodeSettings")]
public class NodeSettings : ScriptableObject
{

    public KeyCode addNode = KeyCode.LeftAlt;
    public KeyCode connectNode = KeyCode.C;
    public KeyCode createDiversion = KeyCode.V;

    public KeyCode markSpeedTier1 = KeyCode.Alpha1;
    public KeyCode markSpeedTier2 = KeyCode.Alpha2;
    public KeyCode markSpeedTier3 = KeyCode.Alpha3;
    public KeyCode markSpeedTier4 = KeyCode.Alpha4;

    [Header("Connecting gizmo color")]
    public Color lowSpeedTierColor = new Color(1, 0.2f, 0, 0.25f);
    public Color mediumSpeedTierColor = new Color(1f, 0.5f, 0, 0.25f);
    public Color highSpeedTierColor = new Color(1, 1, 0, 0.25f);
    public Color expressSpeedTierColor = new Color(0.5f, 1, 0, 0.25f);

    [Header("Node Color")]
    public Color normalNodeColor = Color.yellow;
    public Color diversionNodeColor = new Color(1, 0.5f, 0);
    public Color parkingNodeColor = Color.red;

}
