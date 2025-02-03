using Godot;
using System.Collections.Generic;

/// <summary>
/// Stores level-based textures for different Merge-3 tile types.
/// </summary>
[GlobalClass]  // Allows this class to appear in the Godot Resource picker
[Tool]         // Ensures the script is recognized inside the editor
public partial class MergeTileTextureData : Resource
{
    [Export] public Godot.Collections.Array<Texture2D> LevelTextures { get; set; } = new();


}
