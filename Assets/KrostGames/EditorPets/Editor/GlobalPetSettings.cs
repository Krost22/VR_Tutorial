using UnityEngine;

namespace EditorPets
{
    [CreateAssetMenu(fileName = "GlobalPetSettings", menuName = "EditorPets/Global Settings")]
    public class GlobalPetSettings : ScriptableObject
    {
        [Header("Global Item Textures")]
        public Texture2D heartTexture;
        public Texture2D foodTexture;
        public Texture2D ballTexture;

        [Header("Ball Settings")]
        public float ballRadius = 16f;
        public float gravity = 1200f;
    }
}
