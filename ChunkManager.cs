using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Manages the generation and loading of chunks in a tile-based game world.
/// </summary>
public partial class ChunkManager : Node2D
{
    /// <summary>
    /// The instance of the ChunkManager.
    /// </summary>
    protected static ChunkManager _instance;

    /// <summary>
    /// Gets the instance of the ChunkManager.
    /// </summary>
    public static ChunkManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new ChunkManager();
            }
            return _instance;
        }
    }

    /// <summary>
    /// Initializes a new instance of the ChunkManager class.
    /// </summary>
    public ChunkManager()
    {
        _instance = this;
    }

    /// <summary>
    /// The tile set used for rendering the chunks.
    /// </summary>
    [Export]
    private TileSet _tileSet { get; set; }

    /// <summary>
    /// The size of each chunk in tiles.
    /// </summary>
    private Vector2 _chunkSize;

    /// <summary>
    /// The number of chunks to buffer around the player's position.
    /// </summary>
    private int _chunkBuffer = 2;

    /// <summary>
    /// The dictionary of loaded chunks, indexed by their position.
    /// </summary>
    private Dictionary<Vector2, TileMapLayer> _loadedChunks = new Dictionary<Vector2, TileMapLayer>();

    /// <summary>
    /// The noise generator used for generating tile values.
    /// </summary>
    private FastNoiseLite _noise = new FastNoiseLite();

    /// <summary>
    /// Indicates whether the mouse button is being held.
    /// </summary>
    private bool HoldingMouse = false;

    /// <summary>
    /// The list of noise values generated for each tile.
    /// </summary>
    private List<float> _noiseValues = new List<float>();

    /// <summary>
    /// The position of the chunk currently being hovered over by the mouse.
    /// </summary>
    private Vector2 _hoverChunk;

    /// <summary>
    /// Called when the node is ready to be used.
    /// </summary>
    public override void _Ready()
    {
        _noise.Seed = new Random().Next(-1000000, 1000000);
        _noise.Frequency = 0.01f;
        _chunkSize = new Vector2(32, 32);
        LoadChunksAroundPosition(Vector2.Zero);
    }

    /// <summary>
    /// Refreshes the loaded chunks based on the current camera position.
    /// </summary>
    /// <param name="pos">The position to refresh the chunks around.</param>
    public void RefreshChunks(Vector2 pos)
    {
        LoadChunksAroundPosition(pos);
        UnloadChunksOutsidePosition(pos);
    }

    /// <summary>
    /// Loads the chunks around the specified position.
    /// </summary>
    /// <param name="position">The position to load the chunks around.</param>
    private void LoadChunksAroundPosition(Vector2 position)
    {
        for (int x = -_chunkBuffer; x <= _chunkBuffer; x++)
        {
            for (int y = -_chunkBuffer; y <= _chunkBuffer; y++)
            {
                Vector2 chunkPosition = GetChunkPosition(position) + new Vector2(x, y);
                if (!_loadedChunks.ContainsKey(chunkPosition))
                {
                    TileMapLayer chunk = new TileMapLayer();
                    chunk.Name = $"Chunk_{chunkPosition.X}_{chunkPosition.Y}";
                    chunk.TileSet = _tileSet;
                    chunk.TileSet.TileSize = _tileSet.TileSize;
                    chunk.Position = chunkPosition * _chunkSize * _tileSet.TileSize;
                    this.AddChild(chunk);
                    _loadedChunks.Add(chunkPosition, chunk);
                }
            }
        }

        // Generate noise for each tile in the loaded chunks
        foreach (KeyValuePair<Vector2, TileMapLayer> chunk in _loadedChunks)
        {
            Vector2 chunkPosition = chunk.Key;
            TileMapLayer chunkLayer = chunk.Value;
            for (int x = 0; x < _chunkSize.X; x++)
            {
                for (int y = 0; y < _chunkSize.Y; y++)
                {
                    Vector2 tilePosition = chunkPosition * _chunkSize + new Vector2(x, y);
                    float tileValue = _noise.GetNoise2D(tilePosition.X, tilePosition.Y);

                    if (tileValue < -0.25)
                    {
                        chunkLayer.SetCell(new Vector2I(x, y), 0, new Vector2I(3, 1), 0);
                    }
                    else if (tileValue < -0.1)
                    {
                        chunkLayer.SetCell(new Vector2I(x, y), 0, new Vector2I(3, 0), 0);
                    }
                    else if (tileValue < 0.0 && tileValue >= -0.1)
                    {
                        chunkLayer.SetCell(new Vector2I(x, y), 0, new Vector2I(1, 0), 0);
                    }
                    else
                    {
                        chunkLayer.SetCell(new Vector2I(x, y), 0, new Vector2I(0, 0), 0);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Unloads the chunks that are outside the specified position.
    /// </summary>
    /// <param name="position">The position to check against.</param>
    private void UnloadChunksOutsidePosition(Vector2 position)
    {
        List<Vector2> chunksToRemove = new List<Vector2>();

        foreach (KeyValuePair<Vector2, TileMapLayer> chunk in _loadedChunks)
        {
            if (!IsPositionHasAroundChunk(position, chunk.Key))
            {
                chunksToRemove.Add(chunk.Key);
                this.RemoveChild(chunk.Value);
                chunk.Value.QueueFree();
            }
        }

        foreach (Vector2 chunkPosition in chunksToRemove)
        {
            _loadedChunks.Remove(chunkPosition);
        }
    }

    /// <summary>
    /// Gets the chunk position for the specified position.
    /// </summary>
    /// <param name="position">The position to get the chunk position for.</param>
    /// <returns>The chunk position.</returns>
    public Vector2 GetChunkPosition(Vector2 position)
    {
        return new Vector2(Mathf.FloorToInt(position.X / (_chunkSize.X * _tileSet.TileSize.X)), Mathf.FloorToInt(position.Y / (_chunkSize.Y * _tileSet.TileSize.Y)));
    }

    /// <summary>
    /// Checks if the specified position has a chunk around it.
    /// </summary>
    /// <param name="position">The position to check.</param>
    /// <param name="chunkPosition">The chunk position to check against.</param>
    /// <returns>True if the position has a chunk around it, false otherwise.</returns>
    private bool IsPositionHasAroundChunk(Vector2 position, Vector2 chunkPosition)
    {
        for (int x = -_chunkBuffer; x <= _chunkBuffer; x++)
        {
            for (int y = -_chunkBuffer; y <= _chunkBuffer; y++)
            {
                Vector2 chunk = chunkPosition + new Vector2(x, y);
                Vector2 chunkWorldPosition = chunk * _chunkSize * _tileSet.TileSize;
                Rect2 chunkBounds = new Rect2(chunkWorldPosition, _chunkSize * _tileSet.TileSize);
                if (chunkBounds.HasPoint(position))
                {
                    return true;
                }
            }
        }
        return false;
    }

    /// <summary>
    /// Checks if the specified position is inside the specified chunk.
    /// </summary>
    /// <param name="position">The position to check.</param>
    /// <param name="chunkPosition">The chunk position to check against.</param>
    /// <returns>True if the position is inside the chunk, false otherwise.</returns>
    public bool IsPositionInsideChunk(Vector2 position, Vector2 chunkPosition)
    {
        Vector2 chunkWorldPosition = chunkPosition * _chunkSize * _tileSet.TileSize;
        Rect2 chunkBounds = new Rect2(chunkWorldPosition, _chunkSize * _tileSet.TileSize);
        return chunkBounds.HasPoint(position);
    }

    /// <summary>
    /// Gets the tile position for the specified position.
    /// </summary>
    /// <param name="position">The position to get the tile position for.</param>
    /// <returns>The tile position.</returns>
    public Vector2I GetTilePosition(Vector2 position)
    {
        Vector2 chunkPosition = GetChunkPosition(position);
        Vector2 chunkWorldPosition = chunkPosition * _chunkSize * _tileSet.TileSize;
        Vector2 localPosition = position - chunkWorldPosition;
        return new Vector2I((int)(localPosition.X / _tileSet.TileSize.X), (int)(localPosition.Y / _tileSet.TileSize.Y));
    }

    /// <summary>
    /// Gets the world position of the specified tile in the specified chunk.
    /// </summary>
    /// <param name="position">The position of the tile in the chunk.</param>
    /// <param name="chunkPosition">The position of the chunk.</param>
    /// <returns>The world position of the tile.</returns>
    public Vector2 GetTileWorldPosition(Vector2 position, Vector2 chunkPosition)
    {
        Vector2 chunkWorldPosition = chunkPosition * _chunkSize * _tileSet.TileSize;
        return chunkWorldPosition + new Vector2(position.X * _tileSet.TileSize.X, position.Y * _tileSet.TileSize.Y);
    }
}
