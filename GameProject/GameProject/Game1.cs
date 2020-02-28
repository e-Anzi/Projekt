using System;
using System.Collections.Generic;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace GameProject
{
    /// <summary>
    /// This is the main type for your game.
    /// </summary>
    public class Game1 : Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;

        // game objects. Using inheritance would make this
        // easier, but inheritance isn't a GDD 1200 topic
        Burger burger;
        List<TeddyBear> bears = new List<TeddyBear>();
        static List<Projectile> projectiles = new List<Projectile>();
        List<Explosion> explosions = new List<Explosion>();

        // projectile and explosion sprites. Saved so they don't have to
        // be loaded every time projectiles or explosions are created
        static Texture2D frenchFriesSprite;
        static Texture2D teddyBearProjectileSprite;
        static Texture2D explosionSpriteStrip;

        // scoring support
        int score = 0;
        string scoreString = GameConstants.ScorePrefix + 0;

        // health support
        string healthString = GameConstants.HealthPrefix +
        GameConstants.BurgerInitialHealth;
        bool burgerDead = false;

        // text display support
        SpriteFont font;

        // sound effects
        SoundEffect burgerDamage;
        SoundEffect burgerDeath;
        SoundEffect burgerShot;
        SoundEffect explosion;
        SoundEffect teddyBounce;
        SoundEffect teddyShot;

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";

            // set resolution
            graphics.PreferredBackBufferWidth = GameConstants.WindowWidth;
            graphics.PreferredBackBufferHeight = GameConstants.WindowHeight;
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            RandomNumberGenerator.Initialize();

            base.Initialize();
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);

            // load audio content
            burgerDamage = Content.Load<SoundEffect>("audio//BurgerDamage");
            burgerDeath = Content.Load<SoundEffect>("audio//BurgerDeath");
            burgerShot = Content.Load<SoundEffect>("audio//BurgerShot");
            explosion = Content.Load<SoundEffect>("audio//Explosion");
            teddyBounce = Content.Load<SoundEffect>("audio//TeddyBounce");
            teddyShot = Content.Load<SoundEffect>("audio//TeddyShot");

            // load sprite font
            font = Content.Load<SpriteFont>("fonts//Arial20");

            // load projectile and explosion sprites
            teddyBearProjectileSprite = Content.Load<Texture2D>("sprites//teddybearprojectile");
            frenchFriesSprite = Content.Load<Texture2D>("sprites//frenchfries");
            explosionSpriteStrip = Content.Load<Texture2D>("sprites//explosion");

            // add initial game objects
            burger = new Burger(Content, ("sprites//burger"),
               graphics.PreferredBackBufferWidth / 2,
               graphics.PreferredBackBufferHeight - graphics.PreferredBackBufferHeight / 8,
               burgerShot);
            for (int i = 0; i < GameConstants.MaxBears; i++)
            {
                SpawnBear();
            }

            // set initial health and score strings
            healthString = GameConstants.HealthPrefix + GameConstants.BurgerInitialHealth;

            scoreString = GameConstants.ScorePrefix + 0;
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// game-specific content.
        /// </summary>
        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            // get current mouse state and update burger

            KeyboardState keyboard = Keyboard.GetState();
            burger.Update(gameTime, keyboard);

            // update other game objects
            foreach (TeddyBear bear in bears)
            {
                bear.Update(gameTime);
            }
            foreach (Projectile projectile in projectiles)
            {
                projectile.Update(gameTime);
            }
            foreach (Explosion explosion in explosions)
            {
                explosion.Update(gameTime);
            }

            // check and resolve collisions between teddy bears
            for (int i = 0; i < bears.Count; i++)
            {
                for (int j = i + 1; j < bears.Count; j++)
                {
                    if (bears[i].Active && bears[j].Active &&
                        bears[i].CollisionRectangle.Intersects(bears[j].CollisionRectangle))
                    {
                        CollisionResolutionInfo collisionresult = CollisionUtils.CheckCollision
                            (gameTime.ElapsedGameTime.Milliseconds,
                            GameConstants.WindowWidth, GameConstants.WindowHeight,
                            bears[i].Velocity, bears[i].DrawRectangle,
                            bears[j].Velocity, bears[j].DrawRectangle);
                        teddyBounce.Play(0.1f, 0, 0);

                        if (collisionresult != null)
                        {
                            if (collisionresult.FirstOutOfBounds == true)
                            {
                                bears[i].Active = false;
                            }
                            else
                            {
                                bears[i].Velocity = collisionresult.FirstVelocity;
                                bears[i].DrawRectangle = collisionresult.FirstDrawRectangle;
                            }
                            if (collisionresult.SecondOutOfBounds == true)
                            {
                                bears[j].Active = false;
                            }
                            else
                            {
                                bears[j].Velocity = collisionresult.SecondVelocity;
                                bears[j].DrawRectangle = collisionresult.SecondDrawRectangle;
                            }

                        }
                    }
                }
            }

            // check and resolve collisions between burger and teddy bears
            foreach (TeddyBear bear in bears)
            {
                if (bear.Active && burger.Health > 0 &&
                    bear.CollisionRectangle.Intersects(burger.CollisionRectangle))
                {
                    bear.Active = false;
                    explosion.Play(0.5f, -0.5f, 0);
                    explosions.Add(new Explosion(explosionSpriteStrip,
                        bear.CollisionRectangle.Center.X, bear.CollisionRectangle.Center.Y));
                    burger.Health -= GameConstants.BearDamage;
                    CheckBurgerKill();
                    burgerDamage.Play(0.1f, 0, 0);

                }
            }

            // check and resolve collisions between burger and projectiles
            foreach (Projectile projectile in projectiles)
            {
                if (projectile.Type == ProjectileType.TeddyBearProjectile &&
                    projectile.Active && burger.Health > 0 &&
                    projectile.CollisionRectangle.Intersects(burger.CollisionRectangle))
                {
                    projectile.Active = false;
                    burger.Health -= GameConstants.TeddyBearProjectileDamage;
                    CheckBurgerKill();
                    burgerDamage.Play(0.1f, 0, 0);
                }

            }

            // check and resolve collisions between teddy bears and projectiles
            foreach (TeddyBear bear in bears)
            {
                foreach (Projectile projectile in projectiles)
                {
                    if (bear.Active && projectile.Active && projectile.Type == ProjectileType.FrenchFries &&
                        bear.CollisionRectangle.Intersects(projectile.CollisionRectangle))
                    {
                        bear.Active = false;
                        projectile.Active = false;
                        explosion.Play(0.5f, -0.5f, 0);
                        explosions.Add(new Explosion(explosionSpriteStrip,
                        bear.CollisionRectangle.Center.X, bear.CollisionRectangle.Center.Y));

                        score += GameConstants.BearPoints;
                    }
                }
            }
            if (burger.Health <= 0)
            {
                burger.Health = 0;
            }

            healthString = GameConstants.HealthPrefix + burger.Health;
            scoreString = GameConstants.ScorePrefix + score;


            // clean out inactive teddy bears and add new ones as necessary
            for (int i = bears.Count - 1; i >= 0; i--)
            {
                if (!bears[i].Active)
                {
                    bears.RemoveAt(i);
                }
            }
            while (bears.Count <= GameConstants.MaxBears - 1)
            {
                SpawnBear();
            }
            // clean out inactive projectiles
            for (int i = projectiles.Count - 1; i >= 0; i--)
            {
                if (!projectiles[i].Active)
                {
                    projectiles.RemoveAt(i);
                }
            }

            // clean out finished explosions
            for (int i = explosions.Count - 1; i >= 0; i--)
            {
                if (explosions[i].Finished)
                {
                    explosions.RemoveAt(i);
                }
            }

            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            spriteBatch.Begin();

            // draw game objects
            burger.Draw(spriteBatch);
            foreach (TeddyBear bear in bears)
            {
                bear.Draw(spriteBatch);
            }
            foreach (Projectile projectile in projectiles)
            {
                projectile.Draw(spriteBatch);
            }
            foreach (Explosion explosion in explosions)
            {
                explosion.Draw(spriteBatch);
            }

            // draw score and health
            spriteBatch.DrawString(font, healthString, GameConstants.HealthLocation, Color.White);

            spriteBatch.DrawString(font, scoreString, GameConstants.ScoreLocation, Color.White);

            spriteBatch.End();

            base.Draw(gameTime);
        }

        #region Public methods

        /// <summary>
        /// Gets the projectile sprite for the given projectile type
        /// </summary>
        /// <param name="type">the projectile type</param>
        /// <returns>the projectile sprite for the type</returns>
        public static Texture2D
        GetProjectileSprite(ProjectileType type)
        {
            // replace with code to return correct projectile sprite based on projectile type
            if (type == ProjectileType.FrenchFries)
            {
                return frenchFriesSprite;
            }
            else
            {
                return teddyBearProjectileSprite;
            }
        }
        /// <summary>
        /// Adds the given projectile to the game
        /// </summary>
        /// <param name="projectile">the projectile to add</param>
        public static void AddProjectile(Projectile projectile)
        {
            projectiles.Add(projectile);
        }

        #endregion

        #region Private methods

        /// <summary>
        /// Spawns a new teddy bear at a random location
        /// </summary>
        private void SpawnBear()
        {
            // generate random location
            int teddyBearPositionX = GetRandomLocation(GameConstants.SpawnBorderSize, GameConstants.WindowWidth - GameConstants.SpawnBorderSize);
            int teddyBearPositionY = GetRandomLocation(GameConstants.SpawnBorderSize, GameConstants.WindowHeight - GameConstants.SpawnBorderSize);
            // generate random velocity
            float teddyBearSpeed = GameConstants.MinBearSpeed + RandomNumberGenerator.NextFloat(GameConstants.BearSpeedRange);
            float angle = RandomNumberGenerator.NextFloat(360) * (float)Math.PI;
            // create Vector2
            float velocityX = System.Convert.ToSingle(teddyBearSpeed * Math.Cos(angle));
            float velocityY = System.Convert.ToSingle(teddyBearSpeed * Math.Sin(angle));
            Vector2 velocity = new Vector2(velocityX, velocityY);
            // create new bear
            TeddyBear newBear = new TeddyBear(Content, "sprites//teddybear", teddyBearPositionX, teddyBearPositionY, velocity, teddyBounce, teddyShot);
            bears.Add(newBear);
            // make sure we don't spawn into a collision

            // add new bear to list

        }

        /// <summary>
        /// Gets a random location using the given min and range
        /// </summary>
        /// <param name="min">the minimum</param>
        /// <param name="range">the range</param>
        /// <returns>the random location</returns>
        private int GetRandomLocation(int min, int range)
        {
            return min + RandomNumberGenerator.Next(range);
        }

        /// <summary>
        /// Gets a list of collision rectangles for all the objects in the game world
        /// </summary>
        /// <returns>the list of collision rectangles</returns>
        private List<Rectangle> GetCollisionRectangles()
        {
            List<Rectangle> collisionRectangles = new List<Rectangle>();
            collisionRectangles.Add(burger.CollisionRectangle);
            foreach (TeddyBear bear in bears)
            {
                collisionRectangles.Add(bear.CollisionRectangle);
            }
            foreach (Projectile projectile in projectiles)
            {
                collisionRectangles.Add(projectile.CollisionRectangle);
            }
            foreach (Explosion explosion in explosions)
            {
                collisionRectangles.Add(explosion.CollisionRectangle);
            }
            return collisionRectangles;
        }

        /// <summary>
        /// Checks to see if the burger has just been killed
        /// </summary>
        private void CheckBurgerKill()
        {
            if (......burger.Health <= 0 && !burgerDead)
            {
                burgerDead = true;
                burgerDeath.Play(0.5f, -0.5f, 0);
            }
        }

        #endregion
    }
}
