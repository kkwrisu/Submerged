using UnityEngine;

public class ChangeColor : MonoBehaviour
{
    public Color color = Color.blue;

    private MeshRenderer meshRenderer;

    void Awake()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        ApplyColor();
    }

    void Start()
    {
        ApplyColor();
    }

    public void SetColor(Color newColor)
    {
        color = newColor;
        ApplyColor();
    }

    private void ApplyColor()
    {
        if (meshRenderer == null)
            meshRenderer = GetComponent<MeshRenderer>();

        if (meshRenderer != null)
            meshRenderer.material.color = color;
    }
}