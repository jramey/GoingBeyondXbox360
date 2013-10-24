using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.GamerServices;
using GoingBeyond;
using System.Collections.Generic;

namespace GoingBeyondGame
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager graphics;
        private SpriteBatch spriteBatch;
        private GamePadState lastState = GamePad.GetState(PlayerIndex.One);
        private Vector3 cameraPosition = new Vector3(0.0f, 0.1f, GameConstants.CameraHeight);
        private Matrix projectionMatrix;
        private Matrix viewMatrix;
        private SoundEffect soundEngine;
        private SoundEffectInstance soundEngineInstance;
        private SoundEffect soundHyperspaceActivation;
        private Model shipModel;
        private Matrix[] shipTransforms;
        private Model enemyShipModel;
        private Matrix[] enemyShipTransforms;
        private EnemyShip[] enemyShipList = new EnemyShip[GameConstants.NumberOfEnemyShip];
        private SoundEffect soundExplosion2;
        private SoundEffect soundExplosion3;
        private SoundEffect soundWeaponsFire;
        private Model bulletModel;
        private Matrix[] bulletTransforms;
        private Texture2D stars;
        private Texture2D shipWP;
        private SpriteFont kootenay;
        private Vector2 interactiveTitlePosition = new Vector2(330, 75);
        private Vector2 levelPosition = new Vector2(50, 25);
        private Vector2 scorePosition = new Vector2(370, 25);
        private Vector2 lifePosition = new Vector2(695, 25);
        private Vector2 gameOverPosition = new Vector2(330, 75);
        private Vector2 powerUpPostion = new Vector2(0, 0);
        private Player player;
        private List<Matrix> bonesWorldSpace;

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            Components.Add(new GamerServicesComponent(this));
            SignedInGamer.SignedIn += new EventHandler<SignedInEventArgs>(SignedInGamer_SignedIn);
        }

        public void SignedInGamer_SignedIn(Object sender, SignedInEventArgs e)
        {
            player = new Player(e.Gamer);
        }

        protected override void Initialize()
        {
            var samplerState = new SamplerState();
            samplerState.AddressU = TextureAddressMode.Wrap;
            samplerState.AddressV = TextureAddressMode.Wrap;

            graphics.GraphicsDevice.SamplerStates[0] = SamplerState.LinearWrap;
            graphics.GraphicsDevice.BlendState = BlendState.Additive;
            graphics.GraphicsDevice.DepthStencilState = DepthStencilState.Default;

            projectionMatrix = Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(45.0f), GraphicsDevice.DisplayMode.AspectRatio,
                              GameConstants.CameraHeight - 1000.0f, GameConstants.CameraHeight + 1000.0f);

            viewMatrix = Matrix.CreateLookAt(cameraPosition, Vector3.Zero, Vector3.Up);
            base.Initialize();
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
                        
            shipModel = Content.Load<Model>("Models/p1_wedge");
            shipTransforms = SetupEffectDefaults(shipModel);

            bonesWorldSpace = new List<Matrix>(AvatarRenderer.BoneCount);
            for (int i = 0; i < AvatarRenderer.BoneCount; i++)
                bonesWorldSpace.Add(Matrix.Identity);
        }

        protected override void UnloadContent()
        { }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
                this.Exit();

            if (player != null && player.AvatarRenderer.State == AvatarRendererState.Ready)
            {
                player.CurrentAvatarAnimation.Update(gameTime.ElapsedGameTime, true);
                BonesToWorldSpace(player.AvatarRenderer, player.CurrentAvatarAnimation, bonesWorldSpace);
            }

            if (player != null)
            {
                player.Update(gameTime);
                UpdateInput();

                if (player.CheckForBulletEnwmyShipCollision(bulletModel.Meshes[0].BoundingSphere.Radius, enemyShipModel.Meshes[0].BoundingSphere.Radius))
                    soundExplosion2.Play();

                if (player.CheckForShipEnemyShipCollision(shipModel.Meshes[0].BoundingSphere.Radius, enemyShipModel.Meshes[0].BoundingSphere.Radius))
                    soundExplosion3.Play();
            }

            base.Update(gameTime);
        }

        protected void UpdateInput()
        {
            var currentState = GamePad.GetState(PlayerIndex.One);

            if (currentState.IsConnected && player.CurrentGameState == GameState.GameStarted)
            {
                player.Ship.Update(currentState);
                PlayEngineSound(currentState);

                if (currentState.Buttons.B == ButtonState.Pressed && player.NumberOfLives > 0)
                {
                    player.WrapToCenter();
                    soundHyperspaceActivation.Play();
                }

                if (player.Ship.IsActive && currentState.Buttons.A == ButtonState.Pressed && lastState.Buttons.A == ButtonState.Released)
                {
                    player.ShootBullet();
                    soundWeaponsFire.Play();
                }

                if (player.HasBomb && currentState.Buttons.Y == ButtonState.Pressed && lastState.Buttons.Y == ButtonState.Released)
                    player.DeployBomb();
            }

            if (TitleScreenIsDisplayed(currentState))
                StartGame();
            
            if (player.Ship.IsActive && currentState.Triggers.Left > 0)
                player.Ship.Velocity *= 1.05f;

            lastState = currentState;
        }

        private bool TitleScreenIsDisplayed(GamePadState currentState)
        {
            return (player.CurrentGameState == GameState.TitleScreen && currentState.Buttons.A == ButtonState.Pressed && lastState.Buttons.A == ButtonState.Released);
        }

        private void StartGame()
        {
            player.Ship.IsActive = true;
            player.CurrentGameState = GameState.GameStarted;
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

        protected override void Draw(GameTime gameTime)
        {
            if (player != null)
            {
                base.Draw(gameTime);

                if (player.CurrentGameState == GameState.TitleScreen)
                {
                    DisplayTitleScreen();
                }

                else if (player.CurrentGameState == GameState.GameStarted)
                {
                    DisplayGameBackground();

                    if (player.Ship.IsActive)
                    {
                        var shipTransformMatrix = player.Ship.RotationMatrix * Matrix.CreateTranslation(player.Ship.Position);
                        DrawModel(shipModel, shipTransformMatrix, shipTransforms);
                    }

                    for (var i = 0; i < GameConstants.NumberOfEnemyShip; i++)
                    {
                        if (player.EnemyShipList[i].IsActive == true)
                        {
                            var enemyShipTransform = Matrix.CreateTranslation(player.EnemyShipList[i].position);
                            DrawModel(enemyShipModel, enemyShipTransform, enemyShipTransforms);
                        }
                    }

                    for (var i = 0; i < GameConstants.NumBullets; i++)
                    {
                        if (player.BulletList[i].IsActive)
                        {
                            var bulletTransform = Matrix.CreateTranslation(player.BulletList[i].Postion);
                            DrawModel(bulletModel, bulletTransform, bulletTransforms);
                        }
                    }
                    spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
                    spriteBatch.DrawString(kootenay, "Level : " + player.Level, levelPosition, Color.LimeGreen);
                    spriteBatch.DrawString(kootenay, "Score: " + player.Score, scorePosition, Color.LimeGreen);
                    spriteBatch.DrawString(kootenay, "Lives: " + player.NumberOfLives, lifePosition, Color.LimeGreen);
                    spriteBatch.End();

                    if (player.HasBomb)
                    {
                        spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
                        spriteBatch.DrawString(kootenay, "Power Up", powerUpPostion, Color.Lime);
                        spriteBatch.End();
                    }

                }
                else if (player.CurrentGameState == GameState.GameEnded)
                {
                    DisplayGameOverScreen();
                }
            }
        }

        private void DisplayTitleScreen()
        {
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
            spriteBatch.Draw(shipWP, new Rectangle(0, 0, shipWP.Width, shipWP.Height), Color.White);
            spriteBatch.DrawString(kootenay, "Press A To Start!", interactiveTitlePosition, Color.WhiteSmoke);
            spriteBatch.End();

            DrawAvatar();
        }

        private void DrawAvatar()
        {
            var world = Matrix.CreateRotationY(MathHelper.Pi);
            var view = Matrix.CreateLookAt(new Vector3(0, 1, 3), new Vector3(0, 1, 0), Vector3.Up);
            var projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, GraphicsDevice.Viewport.AspectRatio, 0.01f, 200.0f);

            player.AvatarRenderer.World = world;
            player.AvatarRenderer.View = view;
            player.AvatarRenderer.Projection = projection;

            player.AvatarRenderer.Draw(player.CurrentAvatarAnimation.BoneTransforms,
                               player.CurrentAvatarAnimation.Expression);
        }

        private void DisplayGameOverScreen()
        {
            DisplayGameBackground();

            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
            spriteBatch.DrawString(kootenay, "Score: " + player.Score, scorePosition, Color.LimeGreen);
            spriteBatch.DrawString(kootenay, "G A M E  O V E R!", gameOverPosition, Color.WhiteSmoke);
            spriteBatch.End();

            DrawAvatar();
        }

        private void DisplayGameBackground()
        {
            graphics.GraphicsDevice.Clear(Color.CornflowerBlue);
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
            spriteBatch.Draw(stars, new Rectangle(0, 0, Window.ClientBounds.Width, Window.ClientBounds.Width), Color.White);
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

        private static void BonesToWorldSpace(AvatarRenderer renderer, AvatarAnimation animation,
                                                        List<Matrix> boneToUpdate)
        {
            IList<Matrix> bindPose = renderer.BindPose;
            IList<Matrix> animationPose = animation.BoneTransforms;
            
            IList<int> parentIndex = renderer.ParentBones;

            for (int i = 0; i < AvatarRenderer.BoneCount; i++)
            {
                Matrix parentMatrix = (parentIndex[i] != -1)
                                       ? boneToUpdate[parentIndex[i]]
                                       : renderer.World;

                boneToUpdate[i] = Matrix.Multiply(Matrix.Multiply(animationPose[i],
                                                                  bindPose[i]),
                                                                  parentMatrix);
            }
        }
    }
}