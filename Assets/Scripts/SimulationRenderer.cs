using UnityEngine;

public class SimulationRenderer : MonoBehaviour
{
    public SandSimulation simulation;
    private RenderTexture renderTexture;
    private Material displayMaterial;
    private Color[] colorBuffer;
    private Texture2D updateTexture;
    private GameObject displayQuad;
    private int textureWidth;
    private int textureHeight;
    private Vector2Int simulationBounds;

    void Start()
    {
        CalculateSimulationBounds();
        InitializeRenderTexture();
        InitializeDisplayQuad();
    }

    private void CalculateSimulationBounds()
    {
        // Find the min and max chunk coordinates from the simulation
        int minX = int.MaxValue, minY = int.MaxValue;
        int maxX = int.MinValue, maxY = int.MinValue;

        foreach (var chunkPos in simulation.chunks.Keys)
        {
            minX = Mathf.Min(minX, chunkPos.x);
            minY = Mathf.Min(minY, chunkPos.y);
            maxX = Mathf.Max(maxX, chunkPos.x);
            maxY = Mathf.Max(maxY, chunkPos.y);
        }

        simulationBounds = new Vector2Int(minX, minY);

        // Calculate texture size based on chunks (each chunk is 16x16 particles)
        textureWidth = (maxX - minX + 1) * simulation.chunkSize;
        textureHeight = (maxY - minY + 1) * simulation.chunkSize;

        // Initialize textures
        colorBuffer = new Color[textureWidth * textureHeight];
        updateTexture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false);
        updateTexture.filterMode = FilterMode.Point;
    }

    private void InitializeRenderTexture()
    {
        renderTexture = new RenderTexture(textureWidth, textureHeight, 0);
        renderTexture.filterMode = FilterMode.Point;
        renderTexture.enableRandomWrite = true;
        renderTexture.Create();
        Graphics.Blit(updateTexture, renderTexture);
    }

    private void InitializeDisplayQuad()
    {
        if (displayQuad != null)
        {
            Destroy(displayQuad);
        }

        displayQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        displayQuad.transform.parent = transform;

        // Calculate the world width and height
        int chunksWide = textureWidth / simulation.chunkSize;
        int chunksHigh = textureHeight / simulation.chunkSize;

        // Position the quad at the center of the simulation area in world space
        Vector3 worldCenter = new Vector3(
            simulationBounds.x + chunksWide * 0.5f,
            simulationBounds.y + chunksHigh * 0.5f,
            0.1f
        );
        displayQuad.transform.position = worldCenter;

        // Scale the quad to match the simulation size in world units
        // Since 1 chunk = 1 world unit, we scale by the number of chunks
        displayQuad.transform.localScale = new Vector3(chunksWide, chunksHigh, 1);

        // Create and apply material
        displayMaterial = new Material(Shader.Find("Unlit/Texture"));
        displayMaterial.mainTexture = renderTexture;

        MeshRenderer quadRenderer = displayQuad.GetComponent<MeshRenderer>();
        quadRenderer.material = displayMaterial;

        // Make sure the quad faces the camera
        displayQuad.transform.forward = Camera.main.transform.forward;
    }

    void Update()
    {
        UpdateTextureFromSimulation();

        // Keep the quad facing the camera
        if (Camera.main != null)
        {
            displayQuad.transform.forward = Camera.main.transform.forward;
        }
    }

    private void UpdateTextureFromSimulation()
    {
        System.Array.Clear(colorBuffer, 0, colorBuffer.Length);

        foreach (var chunkPair in simulation.chunks)
        {
            Vector2Int chunkPos = chunkPair.Key;
            SandSimulation.Chunk chunk = chunkPair.Value;

            // Calculate base texture position for this chunk
            int baseX = (chunkPos.x - simulationBounds.x) * simulation.chunkSize;
            int baseY = (chunkPos.y - simulationBounds.y) * simulation.chunkSize;

            for (int x = 0; x < simulation.chunkSize; x++)
            {
                for (int y = 0; y < simulation.chunkSize; y++)
                {
                    var particle = chunk.elements[x, y];
                    if (particle != null)
                    {
                        int texX = baseX + x;
                        int texY = baseY + y;

                        if (texX >= 0 && texX < textureWidth && texY >= 0 && texY < textureHeight)
                        {
                            int index = texY * textureWidth + texX;
                            if (particle is SandSimulation.Water)
                            {
                                colorBuffer[index] = new Color(0.2f, 0.4f, 1f, 0.8f);
                            }
                            else if (particle is SandSimulation.Sand)
                            {
                                colorBuffer[index] = new Color(0.76f, 0.7f, 0.5f, 1f);
                            }
                        }
                    }
                }
            }
        }

        updateTexture.SetPixels(colorBuffer);
        updateTexture.Apply();
        Graphics.Blit(updateTexture, renderTexture);
    }

    private void OnDestroy()
    {
        if (renderTexture != null)
            renderTexture.Release();
        if (updateTexture != null)
            Destroy(updateTexture);
        if (displayMaterial != null)
            Destroy(displayMaterial);
    }
}