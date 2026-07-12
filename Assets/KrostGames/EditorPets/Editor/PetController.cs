using UnityEngine;
using System.Collections.Generic;

namespace EditorPets
{
    public enum PetState
    {
        Idle,
        Walk,
        Sleep,
        Interact,
        Drag,
        Eat,
        Play
    }

    public class HeartParticle
    {
        public Vector2 position;
        public float lifeTime = 1f;
        public float maxLife = 1f;
        public Texture2D tex;
    }

    public class PetController
    {
        public PetData data;
        public PetState currentState = PetState.Idle;
        
        public Vector2 position;
        public bool facingLeft = false;
        
        private float stateTimer = 0f;
        private float animationTimer = 0f;
        private int currentFrame = 0;

        private float nextStateTime = 3f;

        // For dragging
        public Vector2 dragOffset;

        // For food
        public Texture2D foodTexture;
        private Vector2 foodPosition;

        // For playing
        public Vector2 targetPosition;

        // Particles
        public List<HeartParticle> hearts = new List<HeartParticle>();
        public Texture2D heartTexture;

        public PetController(PetData data, Vector2 startPosition)
        {
            this.data = data;
            this.position = startPosition;
            ChangeState(PetState.Idle);
        }

        public void Update(float deltaTime, Rect bounds)
        {
            if (data == null) return;

            // Animation logic
            animationTimer += deltaTime;
            float frameDuration = 1f / data.animationSpeed;
            if (animationTimer >= frameDuration)
            {
                animationTimer -= frameDuration;
                currentFrame++;
            }

            // State Machine Logic
            if (currentState != PetState.Drag && currentState != PetState.Interact && currentState != PetState.Eat)
            {
                stateTimer += deltaTime;
                if (stateTimer >= nextStateTime)
                {
                    ChooseNextState();
                }
            }

            switch (currentState)
            {
                case PetState.Walk:
                    position.x += (facingLeft ? -1 : 1) * data.moveSpeed * deltaTime;
                    
                    if (position.x < 0)
                    {
                        position.x = 0;
                        facingLeft = false;
                    }
                    else if (position.x + data.size.x > bounds.width)
                    {
                        position.x = bounds.width - data.size.x;
                        facingLeft = true;
                    }
                    break;

                case PetState.Interact:
                    stateTimer += deltaTime;
                    if (stateTimer >= 2f)
                    {
                        ChangeState(PetState.Idle);
                    }
                    break;

                case PetState.Eat:
                    stateTimer += deltaTime;
                    if (stateTimer >= 4f) // Eat for 4 seconds
                    {
                        ChangeState(PetState.Idle);
                    }
                    break;

                case PetState.Play:
                    float dist = targetPosition.x - (position.x + data.size.x / 2);
                    if (Mathf.Abs(dist) > 10f)
                    {
                        facingLeft = dist < 0;
                        position.x += (facingLeft ? -1 : 1) * data.moveSpeed * 1.6f * deltaTime; 
                    }
                    else
                    {
                        // If they reached it and it's not moving much, stay around it
                        if (Random.value > 0.995f) SpawnHeart();
                        
                        // Stay in play state but maybe wait a bit to "think"
                        stateTimer += deltaTime;
                        if (stateTimer > 2f) 
                        {
                            // After 2 seconds of being at the ball, maybe go idle or keep playing
                            if (Random.value > 0.7f) ChangeState(PetState.Idle);
                            else stateTimer = 0; // reset and keep playing
                        }
                    }
                    break;
                    
                case PetState.Drag:
                    break;
            }

            if (position.x > bounds.width) position.x = Mathf.Max(0, bounds.width - data.size.x);

            // Update hearts
            for (int i = hearts.Count - 1; i >= 0; i--)
            {
                hearts[i].lifeTime -= deltaTime;
                hearts[i].position.y -= 30f * deltaTime; // Float up
                if (hearts[i].lifeTime <= 0)
                {
                    hearts.RemoveAt(i);
                }
            }
        }

        public void SnapToFloor(float floorY)
        {
            if (currentState == PetState.Drag) return;
            position.y = floorY;
        }

        private void ChooseNextState()
        {
            float rand = Random.value;
            if (currentState == PetState.Sleep)
            {
                ChangeState(PetState.Idle);
            }
            else if (currentState == PetState.Walk)
            {
                ChangeState(rand > 0.3f ? PetState.Idle : PetState.Sleep);
            }
            else // From Idle
            {
                if (rand > 0.5f)
                {
                    ChangeState(PetState.Walk);
                    facingLeft = Random.value > 0.5f;
                }
                else if (rand > 0.8f)
                {
                    ChangeState(PetState.Sleep);
                }
                else
                {
                    ChangeState(PetState.Idle);
                }
            }
        }

        public void ChangeState(PetState newState)
        {
            currentState = newState;
            stateTimer = 0f;
            currentFrame = 0;
            animationTimer = 0f;
            
            nextStateTime = Random.Range(3f, 8f);
            if (newState == PetState.Sleep) nextStateTime = Random.Range(10f, 20f);
        }

        public void Feed(Texture2D foodTex)
        {
            foodTexture = foodTex;
            foodPosition = new Vector2(position.x + (facingLeft ? -30 : 30), position.y + data.size.y - 30);
            ChangeState(PetState.Eat);
        }

        public void SpawnHeart()
        {
            if (heartTexture == null) return;
            hearts.Add(new HeartParticle {
                position = new Vector2(position.x + data.size.x/2 - 16, position.y - 10),
                lifeTime = 2f,
                maxLife = 2f,
                tex = heartTexture
            });
        }

        public void Draw(float opacity, bool showName)
        {
            if (data == null) return;

            Texture2D tex = data.idleTexture;
            int totalFrames = data.framesIdle;

            switch (currentState)
            {
                case PetState.Walk:
                case PetState.Play:
                    tex = data.walkTexture;
                    totalFrames = data.framesWalk;
                    break;
                case PetState.Sleep:
                    tex = data.sleepTexture;
                    totalFrames = data.framesSleep;
                    break;
                case PetState.Interact:
                    tex = data.pettedTexture != null ? data.pettedTexture : data.idleTexture;
                    totalFrames = data.pettedTexture != null ? data.framesPetted : data.framesIdle;
                    break;
                case PetState.Eat:
                    tex = data.eatTexture != null ? data.eatTexture : data.idleTexture;
                    totalFrames = data.eatTexture != null ? data.framesEat : data.framesIdle;
                    break;
                case PetState.Drag:
                    tex = data.idleTexture;
                    totalFrames = data.framesIdle;
                    break;
            }

            if (tex == null) tex = Texture2D.whiteTexture; 

            int frame = currentFrame % Mathf.Max(1, totalFrames);
            
            float uWidth = 1f / Mathf.Max(1, totalFrames);
            float uStart = frame * uWidth;
            
            Rect texCoords = new Rect(uStart, 0, uWidth, 1);
            
            if (facingLeft)
            {
                texCoords.x = uStart + uWidth;
                texCoords.width = -uWidth;
            }

            float yOffset = 0;
            if (currentState == PetState.Interact)
            {
                yOffset = Mathf.Sin(Time.realtimeSinceStartup * 20f) * 5f;
            }

            // Draw shadow
            Rect drawRect = new Rect(position.x, position.y + yOffset, data.size.x, data.size.y);
            GUI.color = new Color(0, 0, 0, 0.3f * opacity);
            GUI.DrawTexture(new Rect(drawRect.x + 5, drawRect.yMax - 5, drawRect.width - 10, 10), Texture2D.whiteTexture);
            GUI.color = new Color(1, 1, 1, opacity);

            // Draw food if eating
            if (currentState == PetState.Eat && foodTexture != null)
            {
                GUI.DrawTexture(new Rect(foodPosition.x, foodPosition.y, 32, 32), foodTexture);
            }

            // Draw Pet
            GUI.DrawTextureWithTexCoords(drawRect, tex, texCoords, true);

            // Draw Hearts
            foreach (var heart in hearts)
            {
                float alpha = (heart.lifeTime / heart.maxLife) * opacity;
                GUI.color = new Color(1, 1, 1, alpha);
                GUI.DrawTexture(new Rect(heart.position.x, heart.position.y, 32, 32), heart.tex);
            }
            GUI.color = Color.white;
            
            // Draw Name Tag
            if (showName)
            {
                GUIStyle style = new GUIStyle(GUI.skin.label);
                style.alignment = TextAnchor.MiddleCenter;
                style.normal.textColor = new Color(1, 1, 1, opacity);
                GUI.color = new Color(0, 0, 0, 0.5f * opacity);
                Rect nameRect = new Rect(position.x, position.y - 20, data.size.x, 18);
                GUI.DrawTexture(nameRect, Texture2D.whiteTexture);
                GUI.color = Color.white;
                GUI.Label(nameRect, data.petName, style);
            }
        }
    }
}
