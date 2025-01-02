using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;
using Random = UnityEngine.Random;

public class SandSimulation : MonoBehaviour
{
    public static SandSimulation Instance { get; set; }

    public enum ChunkGenerationMode
    {
        TilemapOutline,
        TilemapRooms,
        Independent // not implemented yet
    }

    public enum ChunkUpdateMode
    {
        Sequential,
        FourSample,
    }

    [Header("Simulation Settings")]
    public bool initOnStart = false;
    public bool isSimPaused = false;
    public float updateInterval = 0.1f; //how often to update (in seconds)
    public float gravity = 0.2f;
    public int chunkSize = 16;
    public ChunkGenerationMode chunkGenerationMode = ChunkGenerationMode.TilemapOutline;
    public ChunkUpdateMode chunkUpdateMode = ChunkUpdateMode.Sequential;

    [Header("Interaction Settings")]
    public bool enableDrawing = false;
    public string selectedElement = "sand";
    [Range(0, 15)] public float drawingBrushSize = 0;

    [Header("References")]
    public SimulationRenderer simulationRenderer;
    public Tilemap referenceTilemap;
    [HideInInspector] public Dictionary<Vector2Int, Chunk> chunks = new Dictionary<Vector2Int, Chunk>();

    [Header("Debug")]
    public bool drawDebug = false;
    public bool renderChunkBorders = true;
    public bool showActiveChunks = true;
    public bool renderElements = false;

    private void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if(initOnStart) InitSimulation();
    }

    private float timeSinceLastUpdate = 0f;
    void Update()
    {
        if (!isSimPaused)
        {
            timeSinceLastUpdate += Time.deltaTime;

            if (timeSinceLastUpdate >= updateInterval)
            {
                UpdateChunks();
                timeSinceLastUpdate = 0f;
            }
        }

        if (enableDrawing && Input.GetMouseButton(0))
        {
            CreateElementsAtMousePos();
        }

        if (enableDrawing && Input.GetMouseButton(1))
        {
            DiscardElementsAtMousePos();
        }

        if (enableDrawing && Input.GetMouseButtonDown(2))
        {
            EvacutableElementsAtMousePos();
        }
    }

    // INPUT LOGIC

    private void CreateElementsAtMousePos()
    {
        Vector2 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        int checkRadius = Mathf.CeilToInt(drawingBrushSize);
        for (int xOffset = -checkRadius; xOffset <= checkRadius; xOffset++)
        {
            for (int yOffset = -checkRadius; yOffset <= checkRadius; yOffset++)
            {
                Vector2 checkPos = new Vector2(
                    mouseWorldPos.x + xOffset / (float)chunkSize,
                    mouseWorldPos.y + yOffset / (float)chunkSize
                );

                float distance = Vector2.Distance(mouseWorldPos, checkPos);
                if (distance > drawingBrushSize / (float)chunkSize) continue;

                Vector2Int cellPos = new Vector2Int(
                    Mathf.FloorToInt(checkPos.x),
                    Mathf.FloorToInt(checkPos.y)
                );

                Vector2Int chunkPos = new Vector2Int(
                    Mathf.FloorToInt(cellPos.x),
                    Mathf.FloorToInt(cellPos.y)
                );

                if (!chunks.ContainsKey(chunkPos))
                {
                    continue;
                }

                Vector2Int localPos = new Vector2Int(
                    Mathf.RoundToInt((checkPos.x - chunkPos.x) * chunkSize),
                    Mathf.RoundToInt((checkPos.y - chunkPos.y) * chunkSize)
                );

                localPos.x = Mathf.Clamp(localPos.x, 0, chunkSize - 1);
                localPos.y = Mathf.Clamp(localPos.y, 0, chunkSize - 1);

                Vector3Int tilePosition = new Vector3Int(
                    localPos.x / chunkSize + chunkPos.x,
                    localPos.y / chunkSize + chunkPos.y,
                    0
                );

                if (referenceTilemap.HasTile(tilePosition))
                {
                    continue;
                }

                if (chunks[chunkPos].elements[localPos.x, localPos.y] == null)
                {
                    chunks[chunkPos].AddElement(localPos, selectedElement);
                    chunks[chunkPos].WakeUpParticleNeighbors(localPos, chunkPos);
                }
            }
        }
    }

    private void DiscardElementsAtMousePos()
    {
        Vector2 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        int checkRadius = Mathf.CeilToInt(drawingBrushSize);
        for (int xOffset = -checkRadius; xOffset <= checkRadius; xOffset++)
        {
            for (int yOffset = -checkRadius; yOffset <= checkRadius; yOffset++)
            {
                Vector2 checkPos = new Vector2(
                    mouseWorldPos.x + xOffset / (float)chunkSize,
                    mouseWorldPos.y + yOffset / (float)chunkSize
                );

                float distance = Vector2.Distance(mouseWorldPos, checkPos);
                if (distance > drawingBrushSize / (float)chunkSize) continue;

                Vector2Int cellPos = new Vector2Int(
                    Mathf.FloorToInt(checkPos.x),
                    Mathf.FloorToInt(checkPos.y)
                );

                Vector2Int chunkPos = new Vector2Int(
                    Mathf.FloorToInt(cellPos.x),
                    Mathf.FloorToInt(cellPos.y)
                );

                Vector2Int localPos = new Vector2Int(
                    Mathf.RoundToInt((checkPos.x - chunkPos.x) * chunkSize),
                    Mathf.RoundToInt((checkPos.y - chunkPos.y) * chunkSize)
                );

                localPos.x = Mathf.Clamp(localPos.x, 0, chunkSize - 1);
                localPos.y = Mathf.Clamp(localPos.y, 0, chunkSize - 1);

                if (chunks.ContainsKey(chunkPos))
                {
                    Chunk chunk = chunks[chunkPos];
                    if (chunk.elements[localPos.x, localPos.y] != null)
                    {
                        chunk.WakeUpParticleNeighbors(localPos, chunkPos);
                        chunk.WakeUpAllChunkNeighbors(true);
                        chunk.elements[localPos.x, localPos.y] = null;

                        // Check if the chunk is now empty
                        bool hasParticles = false;
                        for (int x = 0; x < chunk.chunkSize; x++)
                        {
                            for (int y = 0; y < chunk.chunkSize; y++)
                            {
                                if (chunk.elements[x, y] != null)
                                {
                                    hasParticles = true;
                                    break;
                                }
                            }
                            if (hasParticles) break;
                        }
                        chunk.isActive = hasParticles;
                    }
                }
            }
        }
    }

    private void EvacutableElementsAtMousePos()
    {
        Vector2 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        Vector2Int chunkPos = new Vector2Int(
            Mathf.FloorToInt(mouseWorldPos.x),
            Mathf.FloorToInt(mouseWorldPos.y)
        );

        Debug.Log($"Attempting to evacuate chunk at {chunkPos}");

        EvacuateChunk(chunkPos);
    }

    [ContextMenu("Init Simulation")]
    public void InitSimulation()
    {
        if (chunkGenerationMode == ChunkGenerationMode.TilemapOutline)
        {
            BoundsInt bounds = referenceTilemap.cellBounds;

            for (int x = bounds.xMin; x < bounds.xMax; x++)
            {
                for (int y = bounds.yMin; y < bounds.yMax; y++)
                {
                    Vector2Int chunkPosition = new Vector2Int(x, y);
                    chunks[chunkPosition] = new Chunk(chunkPosition, chunkSize);
                }
            }
        }else if (chunkGenerationMode == ChunkGenerationMode.TilemapRooms)
        {
            BoundsInt bounds = referenceTilemap.cellBounds;

            for (int x = bounds.xMin; x < bounds.xMax; x++)
            {
                for (int y = bounds.yMin; y < bounds.yMax; y++)
                {
                    Vector2Int chunkPosition = new Vector2Int(x, y);
                    if(referenceTilemap.GetTile(new Vector3Int(chunkPosition.x, chunkPosition.x, (int)referenceTilemap.transform.position.z)) != null)
                    {
                        chunks[chunkPosition] = new Chunk(chunkPosition, chunkSize);
                    }
                }
            }
        }

        simulationRenderer.enabled = true;
    }

    public void UpdateChunks()
    {
        if(chunkUpdateMode == ChunkUpdateMode.Sequential)
        {
            foreach (var chunk in chunks.Values)
            {
                if (chunk.isActive == false) continue;
                chunk.UpdateElements();
            }
        }
        else if(chunkUpdateMode == ChunkUpdateMode.FourSample)
        {
            foreach (var chunk in chunks.Values)
            {
                if (chunk.isActive == false) continue;
                if (chunk.chunkPosition.x % 2 != 0 && chunk.chunkPosition.y % 2 != 0) continue;
                chunk.UpdateElements();
            }

            foreach (var chunk in chunks.Values)
            {
                if (chunk.isActive == false) continue;
                if (chunk.chunkPosition.x % 2 == 0 && chunk.chunkPosition.y % 2 == 0) continue;
                chunk.UpdateElements();
            }

            foreach (var chunk in chunks.Values)
            {
                if (chunk.isActive == false) continue;
                if (chunk.chunkPosition.x % 2 != 0 && chunk.chunkPosition.y % 2 == 0) continue;
                chunk.UpdateElements();
            }

            foreach (var chunk in chunks.Values)
            {
                if (chunk.isActive == false) continue;
                if (chunk.chunkPosition.x % 2 == 0 && chunk.chunkPosition.y % 2 != 0) continue;
                chunk.UpdateElements();
            }
        }
    }

    public void EvacuateChunk(Vector2Int chunkPosition)
    {
        if (!chunks.ContainsKey(chunkPosition))
        {
            Debug.LogWarning($"Attempted to evacuate non-existent chunk at {chunkPosition}");
            return;
        }

        Chunk sourceChunk = chunks[chunkPosition];

        Vector2Int[] directions = new Vector2Int[]
        {
            new Vector2Int(1, 0),   // right
            new Vector2Int(1, 1),   // top-right
            new Vector2Int(0, 1),   // top
            new Vector2Int(-1, 1),  // top-left
            new Vector2Int(-1, 0),  // left
            new Vector2Int(-1, -1), // bottom-left
            new Vector2Int(0, -1),  // bottom
            new Vector2Int(1, -1)   // bottom-right
        };

        List<Element> elementsToMove = new List<Element>();
        for (int x = 0; x < sourceChunk.chunkSize; x++)
        {
            for (int y = 0; y < sourceChunk.chunkSize; y++)
            {
                if (sourceChunk.elements[x, y] != null)
                {
                    elementsToMove.Add(sourceChunk.elements[x, y]);
                    sourceChunk.elements[x, y] = null;
                }
            }
        }

        if (elementsToMove.Count == 0) return;

        List<(Vector2Int pos, Chunk chunk)> neighbors = new List<(Vector2Int, Chunk)>();
        foreach (Vector2Int dir in directions)
        {
            Vector2Int neighborPos = chunkPosition + dir;
            if (chunks.TryGetValue(neighborPos, out Chunk neighborChunk))
            {
                neighbors.Add((neighborPos, neighborChunk));
            }
        }

        const int maxAttempts = 3;

        foreach (var element in elementsToMove)
        {
            bool placed = false;
            int attempts = 0;

            while (!placed && attempts < maxAttempts)
            {
                // Randomly select a neighbor chunk
                int neighborIndex = Random.Range(0, neighbors.Count);
                var (neighborPos, neighborChunk) = neighbors[neighborIndex];

                // Try a random position in the chunk
                int x = Random.Range(0, neighborChunk.chunkSize);
                int y = Random.Range(0, neighborChunk.chunkSize);

                // Check if position is valid
                Vector3Int tilePosition = new Vector3Int(
                    x / neighborChunk.chunkSize + neighborPos.x,
                    y / neighborChunk.chunkSize + neighborPos.y,
                    0);

                if (!referenceTilemap.HasTile(tilePosition) && neighborChunk.elements[x, y] == null)
                {
                    element.localPosition = new Vector2Int(x, y);
                    element.chunkPosition = neighborPos;
                    neighborChunk.elements[x, y] = element;
                    neighborChunk.isActive = true;
                    placed = true;
                }

                attempts++;
            }

            if (!placed)
            {
                Debug.LogWarning($"Particle destroyed during evacuation after {maxAttempts} failed placement attempts");
            }
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (chunks == null) return;
        if(!drawDebug) return;


        float scale = 1f / chunkSize;

        foreach (var chunk in chunks.Values)
        {
            if(SandSimulation.Instance.renderChunkBorders && !SandSimulation.Instance.showActiveChunks)
            {
                Gizmos.color = Color.red;
                Vector3 chunkWorldPos = new Vector3(chunk.chunkPosition.x, chunk.chunkPosition.y, 0);
                Gizmos.DrawWireCube(chunkWorldPos + Vector3.one * 0.5f, Vector3.one);
            }
            else if (SandSimulation.Instance.showActiveChunks && !SandSimulation.Instance.renderChunkBorders)
            {
                Chunk ch;
                Gizmos.color = Color.green;
                Vector3 chunkWorldPos = new Vector3(chunk.chunkPosition.x, chunk.chunkPosition.y, 0);
                chunks.TryGetValue(SimulationCoordinateConverter.WorldToChunkPosition(chunkWorldPos), out ch);

                if(ch != null)
                {
                    if (ch.isActive)
                    {
                        Gizmos.DrawWireCube(chunkWorldPos + Vector3.one * 0.5f, Vector3.one);
                    }
                }
            }
            else if (SandSimulation.Instance.showActiveChunks && SandSimulation.Instance.renderChunkBorders)
            {
                Chunk ch;
                Vector3 chunkWorldPos = new Vector3(chunk.chunkPosition.x, chunk.chunkPosition.y, 0);
                chunks.TryGetValue(SimulationCoordinateConverter.WorldToChunkPosition(chunkWorldPos), out ch);

                if (ch != null)
                {
                    if (ch.isActive)
                    {
                        Gizmos.color = Color.green;
                        Gizmos.DrawWireCube(chunkWorldPos + Vector3.one * 0.5f, Vector3.one);
                    }
                    else if (!ch.isActive)
                    {
                        Gizmos.color = Color.red;
                        Gizmos.DrawWireCube(chunkWorldPos + Vector3.one * 0.5f, Vector3.one);
                    }
                }
            }

            if (SandSimulation.Instance.renderElements)
            {
                // Draw particles
                for (int x = 0; x < chunk.chunkSize; x++)
                {
                    for (int y = 0; y < chunk.chunkSize; y++)
                    {
                        Element particle = chunk.elements[x, y];
                        if (particle != null)
                        {
                            float worldX = chunk.chunkPosition.x + (x * scale);
                            float worldY = chunk.chunkPosition.y + (y * scale);
                            Vector3 worldPos = new Vector3(worldX, worldY, 0);

                            Gizmos.color = Color.yellow;
                            Gizmos.DrawCube(worldPos + (Vector3.one * scale * 0.5f), Vector3.one * scale * 0.8f);
                        }
                    }
                }
            }
        }
    }
    #endif

    public class Chunk
    {
        public Vector2Int chunkPosition;
        public Element[,] elements;
        public int chunkSize;
        public bool isActive;
        public bool isActiveNextFrame;

        public Chunk(Vector2Int position, int size)
        {
            chunkPosition = position;
            chunkSize = size;
            elements = new Element[chunkSize, chunkSize];
            isActive = false;
            isActiveNextFrame = false;
        }

        public void FillWithElement(string particleType)
        {
            for (int x = 0; x < chunkSize; x++)
            {
                for (int y = 0; y < chunkSize; y++)
                {
                    if (elements[x, y] == null)
                    {
                        AddElement(new Vector2Int(x, y), particleType);
                    }
                }
            }
            isActive = true;
        }

        
        public float GetOccupancy()
        {
            int occupiedCells = 0;
            int totalCells = chunkSize * chunkSize;

            for (int x = 0; x < chunkSize; x++)
            {
                for (int y = 0; y < chunkSize; y++)
                {
                    if (elements[x, y] != null)
                    {
                        occupiedCells++;
                    }
                }
            }

            return (float)occupiedCells / totalCells;
        }

        
        public void AddElement(Vector2Int localPos, string type = "sand")
        {
            if (localPos.x >= 0 && localPos.x < chunkSize && localPos.y >= 0 && localPos.y < chunkSize)
            {
                Element newParticle;
                switch (type.ToLower())
                {
                    case "water":
                        newParticle = new Water(localPos, chunkPosition);
                        break;
                    case "sand":
                    default:
                        newParticle = new Sand(localPos, chunkPosition);
                        break;
                }
                elements[localPos.x, localPos.y] = newParticle;
                this.isActive = true;
                this.isActiveNextFrame = true;
            }
        }

        // Update particle behavior in this chunk
        public void UpdateElements()
        {
            if (!isActive) return;
            //bool allNull = particles.Cast<Particle>().All(x => x == null);

            for (int x = 0; x < chunkSize; x++)
            {
                for (int y = 0; y < chunkSize; y++)
                {
                    Element p = elements[x, y];
                    if (p != null)
                    {
                        p.Simulate(this);
                    }
                }
            }

            isActive = isActiveNextFrame;
            isActiveNextFrame = false;
        }

        public void CheckElementContacts(Element particle)
        {
            Vector2Int[] contactPoints = new Vector2Int[]
            {
                new Vector2Int(particle.localPosition.x, particle.localPosition.y - 1), // below
                new Vector2Int(particle.localPosition.x, particle.localPosition.y + 1)  // above
            };

            particle.hasContactBelow = false;
            particle.hasContactAbove = false;

            foreach (Vector2Int contactPoint in contactPoints)
            {
                Vector2Int targetChunk = particle.chunkPosition;
                Vector2Int adjustedPos = contactPoint;

                if (adjustedPos.y < 0)
                {
                    adjustedPos.y = chunkSize - 1;
                    targetChunk.y -= 1;
                }
                else if (adjustedPos.y >= chunkSize)
                {
                    adjustedPos.y = 0;
                    targetChunk.y += 1;
                }

                if (SandSimulation.Instance.chunks.TryGetValue(targetChunk, out Chunk targetChunkObj))
                {
                    Vector3Int tilePosition = new Vector3Int(
                        adjustedPos.x / chunkSize + targetChunk.x,
                        adjustedPos.y / chunkSize + targetChunk.y,
                        0);

                    bool hasContact = SandSimulation.Instance.referenceTilemap.HasTile(tilePosition) ||
                                    targetChunkObj.elements[adjustedPos.x, adjustedPos.y] != null;

                    if (contactPoint.y < particle.localPosition.y)
                    {
                        particle.hasContactBelow = hasContact;
                    }
                    else
                    {
                        particle.hasContactAbove = hasContact;
                    }
                }
            }
        }

        
        public bool HasParticleType<T>() where T : Element
        {
            for (int x = 0; x < chunkSize; x++)
            {
                for (int y = 0; y < chunkSize; y++)
                {
                    if (elements[x, y] is T) return true;
                }
            }
            return false;
        }

        public float GetParticleTypePercentage<T>() where T : Element
        {
            int count = 0;
            int totalCells = chunkSize * chunkSize;

            for (int x = 0; x < chunkSize; x++)
            {
                for (int y = 0; y < chunkSize; y++)
                {
                    if (elements[x, y] is T) count++;
                }
            }

            return (float)count / totalCells;
        }

        public void WakeUpAllChunkNeighbors(bool diagonal = false)
        {
            if (SandSimulation.Instance.chunks.TryGetValue(new Vector2Int(this.chunkPosition.x + 1, this.chunkPosition.y), out Chunk rightChunk))
            {
                rightChunk.isActive = true;
                rightChunk.isActiveNextFrame = true;
            }

            if (SandSimulation.Instance.chunks.TryGetValue(new Vector2Int(this.chunkPosition.x - 1, this.chunkPosition.y), out Chunk leftChunk))
            {
                leftChunk.isActive = true;
                leftChunk.isActiveNextFrame = true;
            }

            if (SandSimulation.Instance.chunks.TryGetValue(new Vector2Int(this.chunkPosition.x, this.chunkPosition.y + 1), out Chunk aboveChunk))
            {
                aboveChunk.isActive = true;
                aboveChunk.isActiveNextFrame = true;
            }

            if (SandSimulation.Instance.chunks.TryGetValue(new Vector2Int(this.chunkPosition.x, this.chunkPosition.y - 1), out Chunk belowChunk))
            {
                belowChunk.isActive = true;
                belowChunk.isActiveNextFrame = true;
            }

            if (diagonal)
            {
                if (SandSimulation.Instance.chunks.TryGetValue(new Vector2Int(this.chunkPosition.x + 1, this.chunkPosition.y + 1), out Chunk aboveRightChunk))
                {
                    aboveRightChunk.isActive = true;
                    aboveRightChunk.isActiveNextFrame = true;
                }

                if (SandSimulation.Instance.chunks.TryGetValue(new Vector2Int(this.chunkPosition.x + 1, this.chunkPosition.y - 1), out Chunk belowRightChunk))
                {
                    belowRightChunk.isActive = true;
                    belowRightChunk.isActiveNextFrame = true;
                }

                if (SandSimulation.Instance.chunks.TryGetValue(new Vector2Int(this.chunkPosition.x - 1, this.chunkPosition.y + 1), out Chunk aboveLeftChunk))
                {
                    aboveLeftChunk.isActive = true;
                    aboveLeftChunk.isActiveNextFrame = true;
                }

                if (SandSimulation.Instance.chunks.TryGetValue(new Vector2Int(this.chunkPosition.x - 1, this.chunkPosition.y - 1), out Chunk belowLeftChunk))
                {
                    belowLeftChunk.isActive = true;
                    belowLeftChunk.isActiveNextFrame = true;
                }
            }
        }

        public void WakeUpChunkNeighbors(Vector2Int chunkPosition, bool diagonal = false)
        {
            Vector2Int chunkPos = chunkPosition;

            bool activateRight = chunkPos.x == chunkSize - 1;
            bool activateLeft = chunkPos.x == 0;
            bool activateUp = chunkPos.y == chunkSize - 1;
            bool activateDown = chunkPos.y == 0;
            //diagonal
            bool activateRightUp = chunkPos.x == chunkSize - 1 && chunkPos.y == chunkSize - 1;
            bool activateRightDown = chunkPos.x == chunkSize - 1 && chunkPos.y == 0;
            bool activateLeftUp = chunkPos.x == 0 && chunkPos.y == chunkSize - 1;
            bool activateLeftDown = chunkPos.x == 0 && chunkPos.y == 0;

            if (activateRight && SandSimulation.Instance.chunks.TryGetValue(new Vector2Int(this.chunkPosition.x + 1, this.chunkPosition.y), out Chunk rightChunk))
            {
                rightChunk.isActive = true;
                rightChunk.isActiveNextFrame = true;
            }

            if (activateLeft && SandSimulation.Instance.chunks.TryGetValue(new Vector2Int(this.chunkPosition.x - 1, this.chunkPosition.y), out Chunk leftChunk))
            {
                leftChunk.isActive = true;
                leftChunk.isActiveNextFrame = true;
            }

            if (activateUp && SandSimulation.Instance.chunks.TryGetValue(new Vector2Int(this.chunkPosition.x, this.chunkPosition.y + 1), out Chunk aboveChunk))
            {
                aboveChunk.isActive = true;
                aboveChunk.isActiveNextFrame = true;
            }

            if (activateDown && SandSimulation.Instance.chunks.TryGetValue(new Vector2Int(this.chunkPosition.x, this.chunkPosition.y - 1), out Chunk belowChunk))
            {
                belowChunk.isActive = true;
                belowChunk.isActiveNextFrame = true;
            }

            if (diagonal)
            {
                if (activateRightUp && SandSimulation.Instance.chunks.TryGetValue(new Vector2Int(this.chunkPosition.x + 1, this.chunkPosition.y + 1), out Chunk aboveRightChunk))
                {
                    aboveRightChunk.isActive = true;
                    aboveRightChunk.isActiveNextFrame = true;
                }

                if (activateRightDown && SandSimulation.Instance.chunks.TryGetValue(new Vector2Int(this.chunkPosition.x + 1, this.chunkPosition.y - 1), out Chunk belowRightChunk))
                {
                    belowRightChunk.isActive = true;
                    belowRightChunk.isActiveNextFrame = true;
                }

                if (activateLeftUp && SandSimulation.Instance.chunks.TryGetValue(new Vector2Int(this.chunkPosition.x - 1, this.chunkPosition.y + 1), out Chunk aboveLeftChunk))
                {
                    aboveLeftChunk.isActive = true;
                    aboveLeftChunk.isActiveNextFrame = true;
                }

                if (activateLeftDown && SandSimulation.Instance.chunks.TryGetValue(new Vector2Int(this.chunkPosition.x - 1, this.chunkPosition.y - 1), out Chunk belowLeftChunk))
                {
                    belowLeftChunk.isActive = true;
                    belowLeftChunk.isActiveNextFrame = true;
                }
            }
        }

        public void WakeUpParticleNeighbors(Vector2Int position, Vector2Int currentChunkPos)
        {
            Vector2Int[] neighbors = new Vector2Int[]
            {
                new Vector2Int(position.x - 1, position.y),     // left
                new Vector2Int(position.x + 1, position.y),     // right
                new Vector2Int(position.x, position.y + 1),     // above
                new Vector2Int(position.x - 1, position.y + 1), // above-left
                new Vector2Int(position.x + 1, position.y + 1), // above-right
            };

            foreach (Vector2Int neighborPos in neighbors)
            {
                Vector2Int targetChunk = currentChunkPos;
                Vector2Int adjustedPos = neighborPos;

                if (adjustedPos.x < 0)
                {
                    adjustedPos.x = chunkSize - 1;
                    targetChunk.x -= 1;
                }
                else if (adjustedPos.x >= chunkSize)
                {
                    adjustedPos.x = 0;
                    targetChunk.x += 1;
                }
                if (adjustedPos.y >= chunkSize)
                {
                    adjustedPos.y = 0;
                    targetChunk.y += 1;
                }

                if (SandSimulation.Instance.chunks.TryGetValue(targetChunk, out Chunk targetChunkObj))
                {
                    Element neighbor = targetChunkObj.elements[adjustedPos.x, adjustedPos.y];
                    if (neighbor != null && neighbor.isResting)
                    {
                        neighbor.isResting = false;
                        neighbor.fallVelocity = 0f;
                    }
                }
            }
        }

        public void ActivateAllNeighbors(bool diagonal = false)
        {
            if (SandSimulation.Instance.chunks.TryGetValue(new Vector2Int(this.chunkPosition.x + 1, this.chunkPosition.y), out Chunk rightChunk))
            {
                rightChunk.isActive = true;
                rightChunk.isActiveNextFrame = true;
            }

            if (SandSimulation.Instance.chunks.TryGetValue(new Vector2Int(this.chunkPosition.x - 1, this.chunkPosition.y), out Chunk leftChunk))
            {
                leftChunk.isActive = true;
                leftChunk.isActiveNextFrame = true;
            }

            if (SandSimulation.Instance.chunks.TryGetValue(new Vector2Int(this.chunkPosition.x, this.chunkPosition.y + 1), out Chunk aboveChunk))
            {
                aboveChunk.isActive = true;
                aboveChunk.isActiveNextFrame = true;
            }

            if (SandSimulation.Instance.chunks.TryGetValue(new Vector2Int(this.chunkPosition.x, this.chunkPosition.y - 1), out Chunk belowChunk))
            {
                belowChunk.isActive = true;
                belowChunk.isActiveNextFrame = true;
            }

            if (diagonal)
            {
                if (SandSimulation.Instance.chunks.TryGetValue(new Vector2Int(this.chunkPosition.x + 1, this.chunkPosition.y + 1), out Chunk aboveRightChunk))
                {
                    aboveRightChunk.isActive = true;
                    aboveRightChunk.isActiveNextFrame = true;
                }

                if (SandSimulation.Instance.chunks.TryGetValue(new Vector2Int(this.chunkPosition.x + 1, this.chunkPosition.y - 1), out Chunk belowRightChunk))
                {
                    belowRightChunk.isActive = true;
                    belowRightChunk.isActiveNextFrame = true;
                }

                if (SandSimulation.Instance.chunks.TryGetValue(new Vector2Int(this.chunkPosition.x - 1, this.chunkPosition.y + 1), out Chunk aboveLeftChunk))
                {
                    aboveLeftChunk.isActive = true;
                    aboveLeftChunk.isActiveNextFrame = true;
                }

                if (SandSimulation.Instance.chunks.TryGetValue(new Vector2Int(this.chunkPosition.x - 1, this.chunkPosition.y - 1), out Chunk belowLeftChunk))
                {
                    belowLeftChunk.isActive = true;
                    belowLeftChunk.isActiveNextFrame = true;
                }
            }
        }

        public void ActivateNeighbors(Vector2Int localPos)
        {
            Vector2Int currentChunkPos = this.chunkPosition;

            bool isOnLeftBorder = localPos.x == 0;
            bool isOnRightBorder = localPos.x == chunkSize;
            bool isOnTopBorder = localPos.y == chunkSize;
            bool isOnBottomBorder = localPos.y == 0;

            List<Vector2Int> chunksToWake = new List<Vector2Int>();

            if (isOnLeftBorder)
            {
                chunksToWake.Add(new Vector2Int(currentChunkPos.x - 1, currentChunkPos.y));
                if (isOnTopBorder)
                    chunksToWake.Add(new Vector2Int(currentChunkPos.x - 1, currentChunkPos.y + 1));
                if (isOnBottomBorder)
                    chunksToWake.Add(new Vector2Int(currentChunkPos.x - 1, currentChunkPos.y - 1));
            }
            if (isOnRightBorder)
            {
                chunksToWake.Add(new Vector2Int(currentChunkPos.x + 1, currentChunkPos.y));
                if (isOnTopBorder)
                    chunksToWake.Add(new Vector2Int(currentChunkPos.x + 1, currentChunkPos.y + 1));
                if (isOnBottomBorder)
                    chunksToWake.Add(new Vector2Int(currentChunkPos.x + 1, currentChunkPos.y - 1));
            }

            if (isOnTopBorder)
            {
                chunksToWake.Add(new Vector2Int(currentChunkPos.x, currentChunkPos.y + 1));
            }
            if (isOnBottomBorder)
            {
                chunksToWake.Add(new Vector2Int(currentChunkPos.x, currentChunkPos.y - 1));
            }

            foreach (Vector2Int neighborChunk in chunksToWake)
            {
                if (SandSimulation.Instance.chunks.TryGetValue(neighborChunk, out Chunk neighborChunkObj))
                {
                    neighborChunkObj.isActive = true;
                    neighborChunkObj.isActiveNextFrame = true;
                }
            }
        }
    }

    public abstract class Element
    {
        public Vector2Int localPosition;
        public Vector2Int chunkPosition;
        public bool isResting;
        public bool isSleeping;
        public float fallVelocity;
        public float horizontalVelocity;
        public bool hasContactBelow;
        public bool hasContactAbove;
        // virtual properties
        public virtual float maxVelocity => 5f;  // Maximum fall speed
        public virtual float velocityAbsorption => 0.5f; // How much velocity is retained on impact (0-1)
        public virtual float maxHorizontalVelocity => 3f; // Maximum horizontal speed
        public virtual float horizontalDrag => 0.2f; // Base drag in air
        public virtual float friction => 0.4f; // Additional slowdown when touching surfaces


        public Element(Vector2Int localPos, Vector2Int chunkPos)
        {
            localPosition = localPos;
            chunkPosition = chunkPos;
            isResting = false;
            fallVelocity = 0f;
            horizontalVelocity = 0f;
            hasContactBelow = false;
            hasContactAbove = false;
        }

        public Vector2Int GetGlobalPosition()
        {
            return chunkPosition * SandSimulation.Instance.chunkSize + localPosition;
        }

        public void ApplyFriction()
        {
            if (hasContactBelow || hasContactAbove)
            {
                // Apply additional slowdown when touching surfaces
                horizontalVelocity *= (1f - friction);
            }
        }

        public bool HasSupport(Chunk currentChunk)
        {
            Vector2Int below = new Vector2Int(localPosition.x, localPosition.y - 1);
            Vector2Int targetChunk = chunkPosition;

            if (below.y < 0)
            {
                below.y = currentChunk.chunkSize - 1;
                targetChunk.y -= 1;
            }

            if (SandSimulation.Instance.chunks.TryGetValue(targetChunk, out Chunk targetChunkObj))
            {
                Vector3Int tilePosition = new Vector3Int(
                    below.x / currentChunk.chunkSize + targetChunk.x,
                    below.y / currentChunk.chunkSize + targetChunk.y,
                    0);

                return SandSimulation.Instance.referenceTilemap.HasTile(tilePosition) ||
                       targetChunkObj.elements[below.x, below.y] != null;
            }

            return false;
        }

        // Abstract method that must be implemented by derived classes
        public abstract void Simulate(Chunk currentChunk);
    }

    public abstract class PowderParticle : Element
    {
        // specific for this element/particle type
        public virtual float density => 2.0f; // Default density for powder particles
        public virtual float sinkSpeed => 0.3f; // How fast the particle sinks in liquid
        public virtual float diagonalSinkSpeed => 0.15f; // How likely to sink diagonally

        public PowderParticle(Vector2Int localPos, Vector2Int chunkPos) : base(localPos, chunkPos) { }

        // overriden from the base Particle class
        public override float maxVelocity => 5f;  // Maximum fall speed
        public override float velocityAbsorption => 0.5f; // How much velocity is retained on impact (0-1)
        public override float maxHorizontalVelocity => 3f; // Maximum horizontal speed
        public override float horizontalDrag => 0.2f; // Base drag in air
        public override float friction => 0.4f; // Additional slowdown when touching surfaces

        private Vector2Int? lastPosition = null;

        public override void Simulate(Chunk currentChunk)
        {
            currentChunk.WakeUpChunkNeighbors(localPosition, true);

            currentChunk.CheckElementContacts(this);
            ApplyFriction();

            //print(localPosition);

            Chunk nextMoveChunk = null;

            // Handle liquid interactions...
            Vector2Int below = new Vector2Int(localPosition.x, localPosition.y - 1);
            Vector2Int targetChunk = chunkPosition;
            below = AdjustPositionForChunk(below, ref targetChunk, currentChunk.chunkSize);

            // Try to sink straight down first
            if (TrySwapWithLiquid(below, targetChunk, currentChunk, sinkSpeed))
            {
                return;
            }

            bool shouldSink = false;
            LiquidParticle liquidBelow = null;

            if (SandSimulation.Instance.chunks.TryGetValue(targetChunk, out Chunk targetChunkObj))
            {
                var particleBelow = targetChunkObj.elements[below.x, below.y];
                if (particleBelow is LiquidParticle liquid)
                {
                    liquidBelow = liquid;
                    float densityDiff = density - liquid.density;
                    // Add randomness to make sinking look more natural
                    shouldSink = densityDiff > 0 && Random.value < (sinkSpeed * Mathf.Min(densityDiff, 1f));
                }
            }

            if (shouldSink && liquidBelow != null)
            {
                // Swap positions with liquid below
                if (targetChunk == chunkPosition)
                {
                    // Same chunk swap
                    currentChunk.elements[localPosition.x, localPosition.y] = liquidBelow;
                    currentChunk.elements[below.x, below.y] = this;
                    liquidBelow.localPosition = localPosition;
                    localPosition = below;
                }
                else
                {
                    // Cross-chunk swap
                    currentChunk.elements[localPosition.x, localPosition.y] = liquidBelow;
                    targetChunkObj.elements[below.x, below.y] = this;
                    liquidBelow.localPosition = localPosition;
                    liquidBelow.chunkPosition = chunkPosition;
                    localPosition = below;
                    chunkPosition = targetChunk;
                    targetChunkObj.isActive = true;
                }

                // Reduce horizontal velocity when sinking
                horizontalVelocity *= 0.8f;
                return;
            }


            if (isResting && !isSleeping)
            {
                Vector2Int belowCheck = new Vector2Int(localPosition.x, localPosition.y - 1);
                Vector2Int targetChunkCheck = chunkPosition;
                belowCheck = AdjustPositionForChunk(belowCheck, ref targetChunkCheck, currentChunk.chunkSize);

                if (SandSimulation.Instance.chunks.TryGetValue(targetChunkCheck, out Chunk targetChunkObjCheck))
                {
                    Vector3Int tilePosition = GetTilemapPosition(belowCheck, targetChunkCheck);

                    bool isEmpty = !SandSimulation.Instance.referenceTilemap.HasTile(tilePosition) &&
                                 targetChunkObjCheck.elements[belowCheck.x, belowCheck.y] == null;

                    bool shouldSleep = CanSinkInLiquid(targetChunkObjCheck.elements[belowCheck.x, belowCheck.y]);

                    if (isEmpty)
                    {
                        isResting = false;
                        fallVelocity = 0f;
                    }
                    else
                    {
                        if (shouldSleep)
                        {
                            isResting = false;
                            isSleeping = true;
                        }
                        else
                        {
                            return;
                        }
                    }
                }
            }

            currentChunk.WakeUpParticleNeighbors(localPosition, chunkPosition);
            fallVelocity = Mathf.Min(fallVelocity + SandSimulation.Instance.gravity, maxVelocity);

            if (Mathf.Abs(horizontalVelocity) > 0)
            {
                horizontalVelocity *= (1f - horizontalDrag);
                if (Mathf.Abs(horizontalVelocity) < 0.1f)
                {
                    horizontalVelocity = 0f;
                }
            }

            float totalVelocity = Mathf.Sqrt(fallVelocity * fallVelocity + horizontalVelocity * horizontalVelocity);
            int steps = Mathf.CeilToInt(totalVelocity);

            Vector2Int current = localPosition;
            Vector2Int currentChunkPos = chunkPosition;

            for (int step = 0; step < steps; step++)
            {
                Vector2Int[] potentialMoves = GetPotentialMoves(current, isResting, horizontalVelocity);
                bool moved = false;
                bool hitObstacle = false;

                foreach (Vector2Int targetPos in potentialMoves)
                {
                    Vector2Int targetChunkPos = currentChunkPos;
                    Vector2Int adjustedPos = AdjustPositionForChunk(targetPos, ref targetChunkPos, currentChunk.chunkSize);

                    if (SandSimulation.Instance.chunks.TryGetValue(targetChunkPos, out Chunk targetChunkObject))
                    {
                        Vector3Int tilePosition = GetTilemapPosition(adjustedPos, targetChunkPos);
                        nextMoveChunk = targetChunkObject;

                        bool isEmpty = !SandSimulation.Instance.referenceTilemap.HasTile(tilePosition) &&
                                     targetChunkObject.elements[adjustedPos.x, adjustedPos.y] == null;

                        if (isEmpty)
                        {
                            isSleeping = false;
                            moved = MoveToNewPosition(adjustedPos, targetChunkPos, currentChunk, targetChunkObject);
                            if (targetChunkPos != currentChunkPos)
                            {
                                UpdateLastPosition(nextMoveChunk, currentChunk);
                                return;
                            }
                            current = adjustedPos;
                            break;
                        }
                        else if (targetPos.y < current.y && targetPos.x != current.x)
                        {
                            isSleeping = true;
                            if (TrySwapWithLiquid(adjustedPos, targetChunkPos, currentChunk, diagonalSinkSpeed))
                            {
                                UpdateLastPosition(nextMoveChunk, currentChunk);
                                return;
                            }
                            hitObstacle = true;
                        }
                        else if (targetPos.y < current.y)
                        {
                            hitObstacle = true;
                        }
                    }
                }

                if (!moved)
                {
                    HandleCollision(hitObstacle);
                    return;
                }
            }

            UpdateLastPosition(nextMoveChunk, currentChunk);
        }

        private Vector2Int[] GetPotentialMoves(Vector2Int current, bool isResting, float horizontalVelocity)
        {
            if (isResting)
            {
                return new Vector2Int[] { new Vector2Int(current.x, current.y - 1) };
            }

            int horizontalDir = horizontalVelocity > 0 ? 1 : -1;
            if (Mathf.Abs(horizontalVelocity) < 0.1f) horizontalDir = 0;

            if (horizontalDir == 0)
            {
                return Random.Range(0, 2) == 0 ?
                    new Vector2Int[]
                    {
                    new Vector2Int(current.x, current.y - 1),
                    new Vector2Int(current.x - 1, current.y - 1),
                    new Vector2Int(current.x + 1, current.y - 1)
                    } :
                    new Vector2Int[]
                    {
                    new Vector2Int(current.x, current.y - 1),
                    new Vector2Int(current.x + 1, current.y - 1),
                    new Vector2Int(current.x - 1, current.y - 1)
                    };
            }

            return Random.Range(0, 2) == 0 ?
                new Vector2Int[]
                {
                new Vector2Int(current.x, current.y - 1),
                new Vector2Int(current.x + horizontalDir, current.y - 1),
                new Vector2Int(current.x + horizontalDir, current.y),
                new Vector2Int(current.x - horizontalDir, current.y - 1)
                } :
                new Vector2Int[]
                {
                new Vector2Int(current.x + horizontalDir, current.y - 1),
                new Vector2Int(current.x, current.y - 1),
                new Vector2Int(current.x + horizontalDir, current.y),
                new Vector2Int(current.x - horizontalDir, current.y - 1)
                };
        }

        private void HandleCollision(bool hitObstacle)
        {
            if (hitObstacle && fallVelocity > 0.5f)
            {
                float impactForce = fallVelocity * velocityAbsorption;
                horizontalVelocity = Random.Range(-1f, 1f) * impactForce;
                horizontalVelocity = Mathf.Clamp(horizontalVelocity, -maxHorizontalVelocity, maxHorizontalVelocity);
            }

            fallVelocity = 0f;
            isResting = (Mathf.Abs(horizontalVelocity) < 0.1f);
        }

        private void UpdateLastPosition(Chunk nextMoveChunk, Chunk currentChunk)
        {
            if(isSleeping)
            {
                nextMoveChunk.isActiveNextFrame = true;
                //currentChunk.WakeUpAllChunkNeighbors(true);
                currentChunk.WakeUpChunkNeighbors(localPosition, true);
            }

            if (lastPosition != null)
            {
                if (GetGlobalPosition() == lastPosition)
                {
                    isResting = true;
                }
                else if (nextMoveChunk != null)
                {
                    nextMoveChunk.isActiveNextFrame = true;
                    //currentChunk.WakeUpAllChunkNeighbors(true);
                    currentChunk.WakeUpChunkNeighbors(localPosition, true);
                }
            }
            else
            {
                lastPosition = GetGlobalPosition();
                if (nextMoveChunk != null)
                {
                    nextMoveChunk.isActiveNextFrame = true;
                    //currentChunk.WakeUpAllChunkNeighbors(true);
                    currentChunk.WakeUpChunkNeighbors(localPosition, true);
                }
            }
        }

        private bool CanSinkInLiquid(Element particle)
        {
            if (particle == null) return false;

            if (particle.GetType() == typeof(LiquidParticle))
            {
                LiquidParticle liquid = particle as LiquidParticle;
                float densityDiff = density - liquid.density;

                if (densityDiff > 0)
                {
                    return true;
                }
                else { return false; }
            }
            else if (particle.GetType() == typeof(PowderParticle))
            {
                return false; //powders can sink in each other too, but for now its not added yet
            }

            return false;
        }

        private bool TrySwapWithLiquid(Vector2Int targetPos, Vector2Int targetChunkPos, Chunk currentChunk, float swapChance)
        {
            if (SandSimulation.Instance.chunks.TryGetValue(targetChunkPos, out Chunk targetChunkObj))
            {
                var particle = targetChunkObj.elements[targetPos.x, targetPos.y];
                if (particle is LiquidParticle liquid)
                {
                    float densityDiff = density - liquid.density;
                    if (densityDiff > 0 && Random.value < (swapChance * Mathf.Min(densityDiff, 1f)))
                    {
                        // Perform the swap
                        if (targetChunkPos == chunkPosition)
                        {
                            // Same chunk swap
                            currentChunk.elements[localPosition.x, localPosition.y] = liquid;
                            currentChunk.elements[targetPos.x, targetPos.y] = this;
                            liquid.localPosition = localPosition;
                            localPosition = targetPos;
                        }
                        else
                        {
                            // Cross-chunk swap
                            currentChunk.elements[localPosition.x, localPosition.y] = liquid;
                            targetChunkObj.elements[targetPos.x, targetPos.y] = this;
                            liquid.localPosition = localPosition;
                            liquid.chunkPosition = chunkPosition;
                            localPosition = targetPos;
                            chunkPosition = targetChunkPos;
                            targetChunkObj.isActive = true;
                        }
                        // Reduce horizontal velocity when sinking
                        horizontalVelocity *= 0.8f;
                        return true;
                    }
                }
            }
            return false;
        }

        private Vector2Int AdjustPositionForChunk(Vector2Int position, ref Vector2Int chunkPos, int chunkSize)
        {
            Vector2Int adjustedPos = position;

            // Handle vertical boundaries
            if (adjustedPos.y < 0)
            {
                adjustedPos.y = chunkSize - 1;
                chunkPos.y -= 1;
            }
            else if (adjustedPos.y >= chunkSize)
            {
                adjustedPos.y = 0;
                chunkPos.y += 1;
            }

            // Handle horizontal boundaries
            if (adjustedPos.x < 0)
            {
                adjustedPos.x = chunkSize - 1;
                chunkPos.x -= 1;
            }
            else if (adjustedPos.x >= chunkSize)
            {
                adjustedPos.x = 0;
                chunkPos.x += 1;
            }

            return adjustedPos;
        }

        private Vector3Int GetTilemapPosition(Vector2Int localPos, Vector2Int chunkPos)
        {
            // Simply use the chunk position as the tilemap position since 1 tile = 1 chunk
            return new Vector3Int(chunkPos.x, chunkPos.y, 0);
        }

        private bool MoveToNewPosition(Vector2Int newPos, Vector2Int newChunkPos, Chunk currentChunk, Chunk targetChunk)
        {
            if (currentChunk == targetChunk)
            {
                // Same chunk movement
                currentChunk.elements[localPosition.x, localPosition.y] = null;
                currentChunk.elements[newPos.x, newPos.y] = this;
                localPosition = newPos;
                return true;
            }
            else
            {
                // Cross-chunk movement
                currentChunk.elements[localPosition.x, localPosition.y] = null;
                targetChunk.elements[newPos.x, newPos.y] = this;
                localPosition = newPos;
                chunkPosition = newChunkPos;
                targetChunk.isActive = true;
                return true;
            }
        }
    }
    public abstract class LiquidParticle : Element
    {
        public virtual float density => 1.0f;  // Default density for liquid particles
        public virtual float dispersionRate => 5f;  // How many cells to check horizontally (default 2)
        public virtual float flowSpeed => 0.8f;  // How quickly the liquid flows horizontally

        public LiquidParticle(Vector2Int localPos, Vector2Int chunkPos) : base(localPos, chunkPos) { }

        public override float maxVelocity => 4f;
        public override float velocityAbsorption => 0.3f;
        public override float maxHorizontalVelocity => 4f;
        public override float horizontalDrag => 0.1f;
        public override float friction => 0.2f;

        private Vector2Int? lastPosition = null;

        public override void Simulate(Chunk currentChunk)
        {
            currentChunk.CheckElementContacts(this);
            ApplyFriction();

            Chunk nextMoveChunk = null;

            if (isResting)
            {
                Vector2Int belowCheck = new Vector2Int(localPosition.x, localPosition.y - 1);
                Vector2Int targetChunkCheck = chunkPosition;

                if (belowCheck.y < 0)
                {
                    belowCheck.y = currentChunk.chunkSize - 1;
                    targetChunkCheck.y -= 1;
                }

                if (SandSimulation.Instance.chunks.TryGetValue(targetChunkCheck, out Chunk targetChunkObjCheck))
                {

                    bool isEmpty = !SandSimulation.Instance.referenceTilemap.HasTile(new Vector3Int(
                        belowCheck.x / currentChunk.chunkSize + targetChunkCheck.x,
                        belowCheck.y / currentChunk.chunkSize + targetChunkCheck.y,
                        0)) &&
                        targetChunkObjCheck.elements[belowCheck.x, belowCheck.y] == null;

                    if (isEmpty)
                    {
                        isResting = false;
                        fallVelocity = 0f;
                    }
                    else
                    {
                        return;
                    }
                }
            }

            currentChunk.WakeUpParticleNeighbors(localPosition, chunkPosition);
            //chunks[chunkPos].WakeUpChunkNeighbors(localPos, true);

            fallVelocity = Mathf.Min(fallVelocity + SandSimulation.Instance.gravity, maxVelocity);

            if (Mathf.Abs(horizontalVelocity) > 0)
            {
                horizontalVelocity *= (1f - horizontalDrag);
                if (Mathf.Abs(horizontalVelocity) < 0.1f)
                {
                    horizontalVelocity = 0f;
                }
            }

            float totalVelocity = Mathf.Sqrt(fallVelocity * fallVelocity + horizontalVelocity * horizontalVelocity);
            int steps = Mathf.CeilToInt(totalVelocity);

            Vector2Int current = localPosition;
            Vector2Int currentChunkPos = chunkPosition;

            for (int step = 0; step < steps; step++)
            {
                List<Vector2Int> potentialMoves = new List<Vector2Int>();

                // always try moving down first
                potentialMoves.Add(new Vector2Int(current.x, current.y - 1));

                // Calculate horizontal spread based on dispersionRate
                int maxSpread = Mathf.CeilToInt(dispersionRate);

                //Add diagonal moves with increasing horizontal distance
                for (int spread = 1; spread <= maxSpread; spread++)
                {
                    // Calculate probability based on distance
                    float spreadProbability = flowSpeed * (1f - (spread - 1) / maxSpread);

                    // Try left side
                    if (Random.value < spreadProbability)
                    {
                        int rnd = Random.Range(0, 1);

                        if(rnd == 0)
                        {
                            potentialMoves.Add(new Vector2Int(current.x - spread, current.y - 1));  // Diagonal down-left
                            potentialMoves.Add(new Vector2Int(current.x - spread, current.y));      // Straight left
                        }
                        else
                        {
                            potentialMoves.Add(new Vector2Int(current.x - spread, current.y));      // Straight left
                            potentialMoves.Add(new Vector2Int(current.x - spread, current.y - 1));  // Diagonal down-left
                        }
                    }

                    // Try right side
                    if (Random.value < spreadProbability)
                    {
                        int rnd = Random.Range(0, 1);

                        if(rnd == 0)
                        {
                            potentialMoves.Add(new Vector2Int(current.x + spread, current.y - 1));  // Diagonal down-right
                            potentialMoves.Add(new Vector2Int(current.x + spread, current.y));      // Straight right
                        }
                        else
                        {
                            potentialMoves.Add(new Vector2Int(current.x + spread, current.y));      // Straight right
                            potentialMoves.Add(new Vector2Int(current.x + spread, current.y - 1));  // Diagonal down-right
                        }
                    }
                }

                bool moved = false;
                bool hitObstacle = false;

                foreach (Vector2Int targetPos in potentialMoves)
                {
                    Vector2Int targetChunkPos = currentChunkPos;
                    Vector2Int adjustedPos = targetPos;

                    // Adjust position and chunk if moving across chunk boundaries
                    if (adjustedPos.y < 0)
                    {
                        adjustedPos.y = currentChunk.chunkSize - 1;
                        targetChunkPos.y -= 1;
                    }
                    if (adjustedPos.x < 0)
                    {
                        adjustedPos.x = currentChunk.chunkSize - 1;
                        targetChunkPos.x -= 1;
                    }
                    else if (adjustedPos.x >= currentChunk.chunkSize)
                    {
                        adjustedPos.x = 0;
                        targetChunkPos.x += 1;
                    }

                    if (SandSimulation.Instance.chunks.TryGetValue(targetChunkPos, out Chunk targetChunkObject))
                    {
                        Vector3Int tilePosition = new Vector3Int(
                            adjustedPos.x / currentChunk.chunkSize + targetChunkPos.x,
                            adjustedPos.y / currentChunk.chunkSize + targetChunkPos.y,
                            0);

                        nextMoveChunk = targetChunkObject;

                        bool isEmpty = !SandSimulation.Instance.referenceTilemap.HasTile(tilePosition) &&
                                     targetChunkObject.elements[adjustedPos.x, adjustedPos.y] == null;

                        if (isEmpty)
                        {
                            // Move the particle
                            if (targetChunkPos == currentChunkPos)
                            {
                                currentChunk.elements[localPosition.x, localPosition.y] = null;
                                localPosition = adjustedPos;
                                currentChunk.elements[adjustedPos.x, adjustedPos.y] = this;
                                current = adjustedPos;
                            }
                            else
                            {
                                currentChunk.elements[localPosition.x, localPosition.y] = null;
                                localPosition = adjustedPos;
                                chunkPosition = targetChunkPos;
                                targetChunkObject.elements[adjustedPos.x, adjustedPos.y] = this;
                                targetChunkObject.isActive = true;
                                return;
                            }
                            moved = true;
                            break;
                        }
                        else if (targetPos.y < current.y)
                        {
                            hitObstacle = true;
                        }
                    }
                }

                if (!moved)
                {
                    if (hitObstacle && fallVelocity > 0.5f)
                    {
                        float impactForce = fallVelocity * velocityAbsorption;
                        horizontalVelocity = Random.Range(-1f, 1f) * impactForce;
                        horizontalVelocity = Mathf.Clamp(horizontalVelocity, -maxHorizontalVelocity, maxHorizontalVelocity);
                    }

                    fallVelocity = 0f;
                    isResting = (Mathf.Abs(horizontalVelocity) < 0.1f);
                    return;
                }
            }

            if (lastPosition != null)
            {
                if (GetGlobalPosition() == lastPosition)
                {
                    this.isResting = true;
                }
                else
                {
                    //wake up its chunk
                    if (nextMoveChunk != null)
                    {
                        nextMoveChunk.isActiveNextFrame = true;
                        //currentChunk.ActivateAllNeighbors(true);
                        //currentChunk.WakeUpAllChunkNeighbors(true);
                        currentChunk.WakeUpChunkNeighbors(localPosition, true);
                    }
                }
            }
            else
            {
                lastPosition = GetGlobalPosition();

                //wake up its chunk
                if (nextMoveChunk != null)
                {
                    nextMoveChunk.isActiveNextFrame = true;
                    //currentChunk.ActivateAllNeighbors(true);
                    //currentChunk.WakeUpAllChunkNeighbors(true);
                    currentChunk.WakeUpChunkNeighbors(localPosition, true);
                }
            }
        }
    }

    // ELEMENTS // PARTICLES

    public class Water : LiquidParticle
    {
        public Water(Vector2Int localPos, Vector2Int chunkPos) : base(localPos, chunkPos) { }

        // properties
        public override float density => 0.5f;
    }

    public class Sand : PowderParticle
    {
        public Sand(Vector2Int localPos, Vector2Int chunkPos) : base(localPos, chunkPos) { }

        // properties
        public override float density => 0.8f;
    }
}

public static class SimulationCoordinateConverter
{
    // Convert a tilemap position to chunk position
    public static Vector2Int TilemapToChunkPosition(Vector3Int tilemapPosition)
    {
        return new Vector2Int(tilemapPosition.x, tilemapPosition.y);
    }

    // Convert a world position to chunk position
    public static Vector2Int WorldToChunkPosition(Vector2 worldPosition)
    {
        return new Vector2Int(
            Mathf.FloorToInt(worldPosition.x),
            Mathf.FloorToInt(worldPosition.y)
        );
    }

    // Convert a world position to local position within a chunk
    public static Vector2Int WorldToLocalPosition(Vector2 worldPosition, int chunkSize)
    {
        Vector2Int chunkPos = WorldToChunkPosition(worldPosition);

        // Calculate local position within the chunk
        Vector2Int localPos = new Vector2Int(
            Mathf.RoundToInt((worldPosition.x - chunkPos.x) * chunkSize),
            Mathf.RoundToInt((worldPosition.y - chunkPos.y) * chunkSize)
        );

        // Clamp to chunk bounds
        localPos.x = Mathf.Clamp(localPos.x, 0, chunkSize - 1);
        localPos.y = Mathf.Clamp(localPos.y, 0, chunkSize - 1);

        return localPos;
    }

    // Convert chunk and local position to world position
    public static Vector2 ChunkToWorldPosition(Vector2Int chunkPos, Vector2Int localPos, int chunkSize)
    {
        return new Vector2(
            chunkPos.x + (float)localPos.x / chunkSize,
            chunkPos.y + (float)localPos.y / chunkSize
        );
    }

    //extra

    // Convert a local position within a chunk to a world position
    public static Vector2 LocalToWorldPosition(Vector2Int localPos, Vector2Int chunkPos, int chunkSize)
    {
        // Calculate the world position
        return new Vector2(
            chunkPos.x + (float)localPos.x / chunkSize,
            chunkPos.y + (float)localPos.y / chunkSize
        );
    }

}