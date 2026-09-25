using UnityEngine;

namespace EditorPets
{
    // One pet = one of these assets. Only Idle is required: any empty state reuses Idle.
    // Sprite sheets are horizontal strips of square frames; frame count 0 = auto (width / height).
    public class PetData : ScriptableObject
    {
        [Tooltip("Name shown above the pet in the Scene View.")]
        public string petName = "New Pet";
        [Tooltip("Show this pet walking around the Scene View.")]
        public bool isActive = true;

        [Header("Sprite Sheets (horizontal strips, empty = use Idle)")]
        public Texture2D idleTexture;
        [Min(0), Tooltip("0 = auto: sheet width / height (square frames).")]
        public int framesIdle;
        public Texture2D walkTexture;
        [Min(0)] public int framesWalk;
        public Texture2D sleepTexture;
        [Min(0)] public int framesSleep;
        public Texture2D eatTexture;
        [Min(0)] public int framesEat;
        public Texture2D pettedTexture;
        [Min(0)] public int framesPetted;

        [Header("Food")]
        [Tooltip("What this pet eats when fed. Pick one from the library below or drop any texture. Empty = the default bowl.")]
        public Texture2D food;

        [Header("Behaviour")]
        [Range(0.5f, 30f), Tooltip("Animation frames per second.")]
        public float animationSpeed = 5f;
        [Range(0f, 300f), Tooltip("Walking speed in pixels per second.")]
        public float moveSpeed = 50f;
        [Tooltip("Size of the pet in the Scene View, in pixels.")]
        public Vector2 size = new Vector2(64, 64);

        // Sprite sheet + frame count for a state, falling back to Idle when that state has no sheet.
        public (Texture2D texture, int frames) GetAnimation(PetState state)
        {
            switch (state)
            {
                case PetState.Walk:
                case PetState.Play: return Resolve(walkTexture, framesWalk);
                case PetState.Sleep: return Resolve(sleepTexture, framesSleep);
                case PetState.Eat: return Resolve(eatTexture, framesEat);
                case PetState.Interact: return Resolve(pettedTexture, framesPetted);
                default: return Resolve(idleTexture, framesIdle);
            }
        }

        private (Texture2D, int) Resolve(Texture2D tex, int frames)
        {
            if (tex == null) { tex = idleTexture; frames = framesIdle; }
            if (tex == null) return (null, 1);
            if (frames <= 0) frames = Mathf.Max(1, Mathf.RoundToInt((float)tex.width / tex.height));
            return (tex, frames);
        }

        private void OnValidate()
        {
            size = Vector2.Max(size, Vector2.one);
        }
    }
}
