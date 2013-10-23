using System;
using System.Linq;
using GoingBeyond4;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.GamerServices;

namespace GoingBeyondGame
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager graphics;
        private SpriteBatch spriteBatch;
        private GamePadState lastState = GamePad.GetState(PlayerIndex.One);
        private GameState currentGameState;
        private Vector3 cameraPosition = new Vector3(0.0f, 0.1f, GameConstants.CameraHeight);
        private Matrix projectionMatrix;
        private Matrix viewMatrix;
        private SoundEffect soundEngine;
        private SoundEffectInstance soundEngineInstance;
        private SoundEffect soundHyperspaceActivation;
        private Ship ship = new Ship();
        private Model enemyShipModel;
        private Matrix[] enemyShipTransforms;
        private EnemyShip[] enemyShipList = new EnemyShip[GameConstants.NumberOfEnemyShip];
        private Random random = new Random();
        private SoundEffect soundExplosion2;
        private SoundEffect soundExplosion3;
        private SoundEffect soundWeaponsFire;
        private Model bulletModel;
        private Matrix[] bulletTransforms;
        private Bullet[] bulletList = new Bullet[GameConstants.NumBullets];
        private Texture2D stars;
        private Texture2D shipWP;
        private SpriteFont kootenay;
        private Int32 score;
        private Int32 numberOfLives = 3;
        private Vector2 gameTitlePosition = new Vector2(214, 200);
        private Vector2 interactiveTitlePosition = new Vector2(330  , 250);
        private Vector2 levelPosition = new Vector2(50, 25);
        private Vector2 scorePosition = new Vector2(370, 25);
        private Vector2 lifePosition = new Vector2(695, 25);
        private Vector2 gameOverPosition = new Vector2(330, 200);
        private Int32 SpeedMultiplier = 10;
        private Int32 ScoreMultiplier = 1;
        private Int32 level = 1;
        private String PlayerName;

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";

            Components.Add(new GamerServicesComponent(this));
            SignedInGamer.SignedIn += new EventHandler<SignedInEventArgs>(SignedInGamer_SignedIn);
        }

        public void SignedInGamer_SignedIn(Object sender, SignedInEventArgs e)
        {
            PlayerName = e.Gamer.Gamertag;
        }


        protected override void Initialize()
        {
            currentGameState = GameState.TitleScreen;
            projectionMatrix = Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(45.0f), GraphicsDevice.DisplayMode.AspectRatio,
                              GameConstants.CameraHeight - 1000.0f, GameConstants.CameraHeight + 1000.0f);

            viewMatrix = Matrix.CreateLookAt(cameraPosition,  Vector3.Zero, Vector3.Up);

            ResetEnemyShips();
            LoadBullets();

            base.Initialize();
        }

        private void ResetEnemyShips()
        {
            float xStart;
            float yStart;

            for (var i = 0; i < GameConstants.NumberOfEnemyShip; i++)
            {
                enemyShipList[i] = new EnemyShip();
                enemyShipList[i].IsActive = true;

                if (random.Next(2) == 0)
                {
                    xStart = (float)-GameConstants.PlayfieldSizeX;
                }
                else
                {
                    xStart = (float)GameConstants.PlayfieldSizeX;
                }
                yStart = (float)random.NextDouble() * GameConstants.PlayfieldSizeY;
                    enemyShipList[i].position = new Vector3(xStart, yStart, 0.0f);
                    double angle = random.NextDouble() * 2 * Math.PI;
                    enemyShipList[i].direction.X = -(float)Math.Sin(angle);
                    enemyShipList[i].direction.Y = (float)Math.Cos(angle);
                    enemyShipList[i].Speed = GameConstants.EnemyShipMinSpeed + (float)random.NextDouble() * SpeedMultiplier;
            }
        }

        private void LoadBullets()
        {
            for (var i = 0; i < GameConstants.NumBullets; i++)
                bulletList[i] = new Bullet();
        }

        private Matrix[] SetupEffectDefaults(Model myModel)
        {        
            var absoluteTransforms = new Matrix[myModel.Bones.Count];
            myModel.CopyAbsoluteBoneTransformsTo(absoluteTransforms);

            foreach (ModelMesh mesh in myModel.Meshes)
            {
                foreach (BasicEffect effect in mesh.Effects)
                {
                    effect.EnableDefaultLighting();
                    effect.Projection = projectionMatrix;
                    effect.View = viewMatrix;
                }
            }
            return absoluteTransforms;
        }

        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);
            kootenay = Content.Load<SpriteFont>("Fonts/Kootenay");
            
            ship.Model = Content.Load<Model>("Models/p1_wedge");
            ship.Transforms = SetupEffectDefaults(ship.Model);
            
            enemyShipModel = Content.Load<Model>("Models/wasphunter");            
            enemyShipTransforms = SetupEffectDefaults(enemyShipModel);
           
            bulletModel = Content.Load<Model>("Models/pea_proj");
            bulletTransforms = SetupEffectDefaults(bulletModel);

            stars = Content.Load<Texture2D>("Textures/1-outer-space-wallpaper");
            shipWP = Content.Load<Texture2D>("Textures/Ship"); 

            soundEngine = Content.Load<SoundEffect>("Audio/Waves/engine_2");
            soundEngineInstance = soundEngine.CreateInstance();
            soundHyperspaceActivation = Content.Load<SoundEffect>("Audio/Waves/hyperspace_activate");
            soundExplosion2 = Content.Load<SoundEffect>("Audio/Waves/explosion2");
            soundExplosion3 = Content.Load<SoundEffect>("Audio/Waves/explosion3");
            soundWeaponsFire = Content.Load<SoundEffect>("Audio/Waves/tx0_fire1");
            soundWeaponsFire = Content.Load<SoundEffect>("Audio/Waves/tx0_fire1");
        }

        protected override void UnloadContent()
        { }

        protected override void Update(GameTime gameTime)
        {
            float timeDelta = (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
                this.Exit();

            UpdateInput();

            if (currentGameState == GameState.GameStarted)
            {
                ship.Position += ship.Velocity;

                ship.Velocity *= 0.95f;

                for (var i = 0; i < GameConstants.NumberOfEnemyShip; i++)
                    enemyShipList[i].Update(timeDelta);

                CheckShipEnemyShipCollision();

                UpdateBullets(timeDelta);

                CheckForBulletEnemyShipCollision();

                if (enemyShipList.Count(s => s.IsActive == true) == 0)
                    AdvanceLevel();
            }
            
            base.Update(gameTime);
        }

        private void CheckShipEnemyShipCollision()
        {
            var shipSphere = new BoundingSphere(ship.Position, ship.Model.Meshes[0].BoundingSphere.Radius);

            for (var i = 0; i < enemyShipList.Length; i++)
            {
                var boundingSphere = new BoundingSphere(enemyShipList[i].position,
                    enemyShipModel.Meshes[0].BoundingSphere.Radius);

                if (boundingSphere.Intersects(shipSphere) && enemyShipList[i].IsActive == true && ship.IsActive == true)
                {
                    soundExplosion3.Play();
                    ship.IsActive = false;
                    enemyShipList[i].IsActive = false;
                    score = score - GameConstants.DeathPenalty;
                    numberOfLives--;
                    CheckGameOver();
                    break;
                }
            }
        }

        private void UpdateBullets(float timeDelta)
        {
            for (int i = 0; i < GameConstants.NumBullets; i++)
            {
                if (bulletList[i].IsActive)
                {
                    bulletList[i].Update(timeDelta);
                }
            }
        }

        private void CheckForBulletEnemyShipCollision()
        {
            for (var i = 0; i < enemyShipList.Length; i++)
            {
                if (enemyShipList[i].IsActive)
                {
                    var enemyShipSphere = new BoundingSphere(enemyShipList[i].position,
                                enemyShipModel.Meshes[0].BoundingSphere.Radius);

                    for (var j = 0; j < bulletList.Length; j++)
                    {
                        if (bulletList[j].IsActive)
                        {
                            var bulletSphere = new BoundingSphere(bulletList[j].Postion, bulletModel.Meshes[0].BoundingSphere.Radius);

                            if (enemyShipSphere.Intersects(bulletSphere))
                            {
                                soundExplosion2.Play();
                                enemyShipList[i].IsActive = false;
                                bulletList[j].IsActive = false;
                                score += GameConstants.KillBonus * ScoreMultiplier;
                                break;
                            }
                        }
                    }
                }
            }
        }

        private void AdvanceLevel()
        {
            ResetEnemyShips();
            ScoreMultiplier++;
            SpeedMultiplier = SpeedMultiplier + 100;
            level++;
        }

        private void CheckGameOver()
        {
            if (numberOfLives == 0)
                currentGameState = GameState.GameEnded;
        }

        protected void UpdateInput()
        {
            var currentState = GamePad.GetState(PlayerIndex.One);

            if (currentState.IsConnected)
            {
                ship.Update(currentState);
                PlayEngineSound(currentState);

                if (currentState.Buttons.B == ButtonState.Pressed && numberOfLives > 0)
                    WrapToCenter();

                if (ship.IsActive && currentState.Buttons.A == ButtonState.Pressed && lastState.Buttons.A == ButtonState.Released)
                    ShootBullet();
            }

            if (TitleScreenIsDisplayed(currentState))
                StartGame();

            if (ship.IsActive && currentState.Triggers.Left > 0)
                ship.Velocity *= 1.05f;

            lastState = currentState;
        }

        private bool TitleScreenIsDisplayed(GamePadState currentState)
        {
            return (currentGameState == GameState.TitleScreen && currentState.Buttons.A == ButtonState.Pressed && lastState.Buttons.A == ButtonState.Released);
        }

        private void StartGame()
        {
            ship.IsActive = true;
            currentGameState = GameState.GameStarted;
        }

        private void ShootBullet()
        {
            for (var i = 0; i < GameConstants.NumBullets; i++)
            {
                if (!bulletList[i].IsActive)
                {
                    bulletList[i].Direction = ship.RotationMatrix.Forward;
                    bulletList[i].Speed = GameConstants.BulletSpeedAdjustment;
                    bulletList[i].Postion = ship.Position + (200 * bulletList[i].Direction);
                    bulletList[i].IsActive = true;
                    soundWeaponsFire.Play();
                    break;
                }
            }
        }

        private void PlayEngineSound(GamePadState currentState)
        {
            if (currentState.Triggers.Right > 0)
            {

                if (soundEngineInstance.State == SoundState.Stopped)
                {
                    soundEngineInstance.Volume = 0.75f;
                    soundEngineInstance.IsLooped = true;
                    soundEngineInstance.Play();
                }
                else
                {
                    soundEngineInstance.Resume();
                }
            }
            else if (currentState.Triggers.Right == 0)
            {
                if (soundEngineInstance.State == SoundState.Playing)
                    soundEngineInstance.Pause();
            }
        }

        private void WrapToCenter()
        {
            ship.Position = Vector3.Zero;
            ship.Velocity = Vector3.Zero;
            ship.Rotation = 0.0f;
            ship.IsActive = true;
            soundHyperspaceActivation.Play();
        }

        protected override void Draw(GameTime gameTime)
        {            
            if (currentGameState == GameState.TitleScreen && !String.IsNullOrEmpty(PlayerName))
            {
                DisplayTitleScreen();
            }
            else if (currentGameState == GameState.GameStarted)
            {
                graphics.GraphicsDevice.Clear(Color.CornflowerBlue);
                spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
                spriteBatch.Draw(stars, new Rectangle(0, 0, Window.ClientBounds.Width, Window.ClientBounds.Width), Color.White);
                spriteBatch.End();

                if (ship.IsActive)
                {
                    var shipTransformMatrix = ship.RotationMatrix * Matrix.CreateTranslation(ship.Position);
                    DrawModel(ship.Model, shipTransformMatrix, ship.Transforms);
                    base.Draw(gameTime);
                }

                for (var i = 0; i < GameConstants.NumberOfEnemyShip; i++)
                {
                    if (enemyShipList[i].IsActive == true)
                        {
                            Matrix enemyShipTransform =
                            Matrix.CreateTranslation(enemyShipList[i].position);
                            DrawModel(enemyShipModel, enemyShipTransform, enemyShipTransforms);
                    }
                }

                for (var i = 0; i < GameConstants.NumBullets; i++)
                {
                    if (bulletList[i].IsActive)
                    {
                        var bulletTransform =
                          Matrix.CreateTranslation(bulletList[i].Postion);
                        DrawModel(bulletModel, bulletTransform, bulletTransforms);
                    }
            }
                spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
                spriteBatch.DrawString(kootenay, "Level : " + level, levelPosition, Color.LimeGreen);
                spriteBatch.DrawString(kootenay, "Score: " + score, scorePosition, Color.LimeGreen);
                spriteBatch.DrawString(kootenay, "Lives:" + numberOfLives, lifePosition, Color.LimeGreen);
                spriteBatch.End();
            }
            else
            {
                DisplayGameOverScreen();
            }
        }

        private void DisplayTitleScreen()
        {

            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
            spriteBatch.Draw(shipWP, new Rectangle(0, 0, shipWP.Width, shipWP.Height), Color.White);
            spriteBatch.DrawString(kootenay, "Press A To Start!", interactiveTitlePosition, Color.WhiteSmoke);
            spriteBatch.End();
        }

        private void DisplayGameOverScreen()
        {
            graphics.GraphicsDevice.Clear(Color.CornflowerBlue);
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
            spriteBatch.Draw(stars, new Rectangle(0, 0, Window.ClientBounds.Width, Window.ClientBounds.Width), Color.White);
            spriteBatch.End();

            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
            spriteBatch.DrawString(kootenay, "G A M E  O V E R!", gameOverPosition, Color.WhiteSmoke);
            spriteBatch.End();
        }

        public static void DrawModel(Model model, Matrix modelTransform, Matrix[] absoluteBoneTransforms)
        {
            foreach (ModelMesh mesh in model.Meshes)
            {
                foreach (BasicEffect effect in mesh.Effects)
                {
                    effect.World = absoluteBoneTransforms[mesh.ParentBone.Index] * modelTransform;
                }
                
                mesh.Draw();
            }
        }
    }
}