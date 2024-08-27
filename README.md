# ChunkManager for Godot

## Overview

`ChunkManager` is a class designed for managing chunks of tiles in a 2D game using the Godot Engine. It dynamically loads and unloads chunks based on the player's position, utilizing noise generation to create varied terrain types such as water, sand, and grass. This class is particularly useful for games with large worlds where only a portion of the map needs to be rendered at any given time.

## Features

- Dynamic chunk loading and unloading based on player position.
- Terrain generation using noise functions.
- Support for different tile types (e.g., water, sand, grass).
- Efficient memory management by freeing unused chunks.

## Installation

1. Clone the repository or download the source code.
2. Place the `ChunkManager.cs` file in your Godot project under the appropriate directory (e.g., `Scripts/`).

## Usage

To use the `ChunkManager`, follow these steps:

1. Create a new scene in Godot.
2. Add a `Node2D` as the root node.
3. Attach the `ChunkManager` script to the root node.
4. Assign a `TileSet` to the `_tileSet` property in the Godot editor.
5. Call `RefreshChunks()` method to load chunks around the player's position.

### Example

```csharp
public class Player : Node2D
{
    public override void _Process(float delta)
    {
        // Update player position
        Vector2 position = this.Position;

        // Refresh chunks based on player position
        ChunkManager.Instance.RefreshChunks(position);
    }
}
