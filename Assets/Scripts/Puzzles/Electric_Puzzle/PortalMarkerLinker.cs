using UnityEngine;

/// <summary>
/// Posiciona automaticamente o marcador visual do portal (tp.png)
/// baseado no nó lógico correspondente na grade.
/// Attach este script no GameObject que tem o SpriteRenderer do tp.png.
/// </summary>
public class PortalMarkerLinker : MonoBehaviour
{
    [Tooltip("Arraste aqui o RepairPuzzleNode do tipo Portal correspondente.")]
    public RepairPuzzleNode linkedNode;

    [Tooltip("Offset em unidades de mundo a partir do nó. " +
             "Ex: (0, 1.5, 0) para portal no topo da grade.")]
    public Vector3 worldOffset = new Vector3(0f, 1.5f, 0f);

    [Tooltip("Se verdadeiro, rotaciona o sprite automaticamente baseado na direção do offset.")]
    public bool autoRotate = true;

    private void Start()
    {
        if (linkedNode == null)
        {
            Debug.LogWarning("[PortalMarkerLinker] linkedNode não atribuído em: " + gameObject.name);
            return;
        }

        // Posiciona o marker
        transform.position = linkedNode.transform.position + worldOffset;

        // Rotaciona o sprite para apontar na direção do offset
        if (autoRotate && worldOffset != Vector3.zero)
        {
            float angle = Mathf.Atan2(worldOffset.y, worldOffset.x) * Mathf.Rad2Deg;
            // Subtrai 90° porque o sprite base do tp.png aponta para cima (90°)
            transform.rotation = Quaternion.Euler(0f, 0f, angle - 90f);
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (linkedNode == null) return;

        Gizmos.color = new Color(0.6f, 0.2f, 1f, 0.8f);
        Gizmos.DrawLine(transform.position, linkedNode.transform.position);
        Gizmos.DrawWireSphere(linkedNode.transform.position, 0.2f);
    }
#endif
}