using UnityEngine;

namespace EditorPets
{
    // Shared by all pets. Created automatically in <package>/Items the first time it's needed.
    public class GlobalPetSettings : ScriptableObject
    {
        [Header("Item Textures")]
        public Texture2D heartTexture;
        [Tooltip("Default food for pets that don't have their own.")]
        public Texture2D foodTexture;
        public Texture2D ballTexture;

        [Header("Ball")]
        [Range(4f, 64f), Tooltip("Ball radius in pixels.")]
        public float ballRadius = 16f;
        [Range(100f, 3000f), Tooltip("How fast the ball falls, in pixels per second squared.")]
        public float gravity = 1200f;
    }
}
