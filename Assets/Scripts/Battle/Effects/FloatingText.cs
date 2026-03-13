using PhalanxChronicle.Presentation;
using TMPro;
using UnityEngine;

namespace PhalanxChronicle.Battle.Effects
{
    public sealed class FloatingText : MonoBehaviour
    {
        private const float PixelSnapPpu = 64f;

        private TextMeshPro textMesh;
        private MeshRenderer meshRenderer;
        private Color startColor;
        private float elapsed;

        public static void Spawn(string content, Vector3 worldPosition, Color color)
        {
            GameObject gameObject = new GameObject("FloatingText", typeof(RectTransform));
            gameObject.transform.position = worldPosition;

            FloatingText floatingText = gameObject.AddComponent<FloatingText>();
            floatingText.Initialize(content, color);
        }

        private void Initialize(string content, Color color)
        {
            textMesh = gameObject.AddComponent<TextMeshPro>();
            textMesh.text = content;
            textMesh.font = RuntimeSpriteLibrary.GetWorldTmpFont(FontStyle.Bold);
            textMesh.fontSize = 7.4f;
            textMesh.fontStyle = FontStyles.Bold;
            textMesh.alignment = TextAlignmentOptions.Midline;
            textMesh.enableWordWrapping = false;
            textMesh.overflowMode = TextOverflowModes.Overflow;
            textMesh.rectTransform.sizeDelta = new Vector2(14f, 3.6f);
            textMesh.transform.localScale = new Vector3(0.085f, 0.085f, 1f);
            textMesh.color = color;
            meshRenderer = textMesh.GetComponent<MeshRenderer>();
            Material material = RuntimeSpriteLibrary.CreateTmpMaterialInstance(
                textMesh.font,
                color,
                new Color(0.16f, 0.08f, 0.02f, 0.92f),
                0.16f,
                new Color(0.04f, 0.02f, 0.01f, 0.7f),
                new Vector2(0.12f, -0.12f),
                0.06f);
            if (material != null)
            {
                meshRenderer.sharedMaterial = material;
            }

            meshRenderer.sortingOrder = 60;

            startColor = color;
            transform.position = SnapToPixelGrid(transform.position);
        }

        private void Update()
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / 0.75f);

            transform.position = SnapToPixelGrid(transform.position + Vector3.up * (0.8f * Time.deltaTime));
            textMesh.color = Color.Lerp(startColor, new Color(startColor.r, startColor.g, startColor.b, 0f), progress);
            transform.localScale = Vector3.one * Mathf.Lerp(1f, 1.16f, progress);

            if (progress >= 1f)
            {
                Destroy(gameObject);
            }
        }

        private static Vector3 SnapToPixelGrid(Vector3 position)
        {
            float pixelSize = 1f / PixelSnapPpu;
            return new Vector3(
                Mathf.Round(position.x / pixelSize) * pixelSize,
                Mathf.Round(position.y / pixelSize) * pixelSize,
                position.z);
        }
    }
}
