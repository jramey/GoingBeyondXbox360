using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Input;
using GoingBeyondGame;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.GamerServices;

namespace GoingBeyond
{
    public class Player
    {
        public GamePadState LastState { get; set; }
        public GameState CurrentGameState { get; set; }
        public Ship Ship { get; set; }
        public EnemyShip[] EnemyShipList { get; set; }
        public Bullet[] BulletList { get; set; }
        public Int32 Score { get; set; }
        public Int32 Level { get; set; }
        public Int32 NumberOfLives { get; set; }
        public float SpeedMultiplier { get; set; }
        public Int32 ScoreMultiplier { get; set; }
        public AvatarRenderer AvatarRenderer { get; set; }
        public String PlayersName { get; set; }
        public AvatarAnimation CurrentAvatarAnimation { get; set; }
        public Boolean HasBomb { get; set; }
        private AvatarDescription avatarDescription;

        private Random random = new Random();

        public Player(Gamer gamer)
        {
            AvatarDescription.BeginGetFromGamer(gamer, LoadGamerAvatar, null);
            PlayersName = gamer.Gamertag;
            CurrentAvatarAnimation = new AvatarAnimation(AvatarAnimationPreset.MaleIdleLookAround);
            Ship = new Ship();
            NumberOfLives = 3;
            Level = 1;
            ScoreMultiplier = 1;
            SpeedMultiplier = 1;
            EnemyShipList = new EnemyShip[GameConstants.NumberOfEnemyShip];
            BulletList = new Bullet[GameConstants.NumBullets];
            LoadBulletList();
            CurrentGameState = GameState.TitleScreen;
            ResetEnemyShips();
        }

        public void Update(GameTime gameTime)
        {
            float timeDelta = (float)gameTime.ElapsedGameTime.TotalSeconds;

            Ship.Position += Ship.Velocity;
            Ship.Velocity *= 0.95f;

            for (int i = 0; i < GameConstants.NumberOfEnemyShip; i++)
                if (Ship.IsActive)
                    EnemyShipList[i].Update(timeDelta, Ship.Position);
                else
                    EnemyShipList[i].Update(timeDelta, new Vector3(GameConstants.PlayfieldSizeX, GameConstants.PlayfieldSizeY, 0));

                    for (int i = 0; i < GameConstants.NumBullets; i++)
                    {
                        if (BulletList[i].IsActive)
                            BulletList[i].Update(timeDelta);
                    }
        }

        public void WrapToCenter()
        {
            Ship.Position = Vector3.Zero;
            Ship.Velocity = Vector3.Zero;
            Ship.Rotation = 0.0f;
            Ship.IsActive = true;
        }

        public void ShootBullet()
        {
            for (var i = 0; i < GameConstants.NumBullets; i++)
            {
                if (!BulletList[i].IsActive)
                {
                    BulletList[i].Direction = Ship.RotationMatrix.Forward;
                    BulletList[i].Speed = GameConstants.BulletSpeedAdjustment;
                    BulletList[i].postion = Ship.Position + (200 * BulletList[i].Direction);
                    BulletList[i].IsActive = true;
                    break;
                }
            }
        }

        public void AdvanceLevel()
        {
            ResetEnemyShips();
            ScoreMultiplier++;
            SpeedMultiplier = SpeedMultiplier + 50;
            Level++;
        }

        public void ResetEnemyShips()
        {
            float xStart;
            float yStart;

            for (var i = 0; i < GameConstants.NumberOfEnemyShip; i++)
            {
                EnemyShipList[i] = new EnemyShip(random);
                EnemyShipList[i].IsActive = true;

                if (random.Next(2) == 0)
                    xStart = (float)-GameConstants.PlayfieldSizeX;
                else
                    xStart = (float)GameConstants.PlayfieldSizeX;

                yStart = (float)random.NextDouble() * GameConstants.PlayfieldSizeY;
                EnemyShipList[i].position = new Vector3(xStart, yStart, 0.0f);
                double angle = random.NextDouble() * 2 * Math.PI;
                EnemyShipList[i].direction.X = -(float)Math.Sin(angle);
                EnemyShipList[i].direction.Y = (float)Math.Cos(angle);
                EnemyShipList[i].Speed = GameConstants.EnemyShipMinSpeed + (float)random.NextDouble() * SpeedMultiplier;
            }
        }

        public bool CheckForBulletEnwmyShipCollision(float bulletRadius, float enemyShipRadius)
        {
            for (int i = 0; i < EnemyShipList.Length; i++)
            {
                if (EnemyShipList[i].IsActive)
                {
                    var enemySphere = new BoundingSphere(EnemyShipList[i].position, enemyShipRadius * GameConstants.EnemyShipSphereScale);

                    for (int j = 0; j < EnemyShipList.Length; j++)
                    {
                        if (BulletList[j].IsActive)
                        {
                            var bulletSphere = new BoundingSphere(BulletList[j].postion, bulletRadius);

                            if (enemySphere.Intersects(bulletSphere))
                            {
                                EnemyShipList[i].IsActive = false;
                                BulletList[j].IsActive = false;
                                Score += GameConstants.KillBonus * ScoreMultiplier;

                                if (Score % 2000 == 0)
                                    HasBomb = true;

                                if (!EnemyShipList.Any(s => s.IsActive == true))
                                    AdvanceLevel();

                                return true;
                            }
                        }
                    }
                }
            }

            return false;
        }

        public Boolean CheckForShipEnemyShipCollision(float shipRadius, float enemyShipRadius)
        {
            if (Ship.IsActive)
            {
                var shipSphere = new BoundingSphere(Ship.Position, shipRadius * GameConstants.ShipBoundingSphereScale);

                for (int i = 0; i < EnemyShipList.Length; i++)
                {
                    if (EnemyShipList[i].IsActive)
                    {
                        var boundingSphere = new BoundingSphere(EnemyShipList[i].position, enemyShipRadius * GameConstants.EnemyShipSphereScale);

                        if (boundingSphere.Intersects(shipSphere))
                        {
                            Ship.IsActive = false;
                            EnemyShipList[i].IsActive = false;
                            Score -= GameConstants.DeathPenalty;
                            NumberOfLives--;
                            CheckGameOver();

                            return true;
                        }
                    }
                }
            }
            return false;
        }

        private void CheckGameOver()
        {
            if (NumberOfLives == 0)
            {
                CurrentGameState = GameState.GameEnded;
                CurrentAvatarAnimation = new AvatarAnimation(AvatarAnimationPreset.FemaleCry);
            }
        }

        private void LoadGamerAvatar(IAsyncResult result)
        {
            avatarDescription = AvatarDescription.EndGetFromGamer(result);
            
            AvatarRenderer = new AvatarRenderer(avatarDescription);}

        private void LoadBulletList()
        {
            for (var i = 0; i < BulletList.Length; i++)
                BulletList[i] = new Bullet();
        }

        internal void DeployBomb()
        {
           for (var i = 0; i < EnemyShipList.Length; i++)
           {
               EnemyShipList[i].IsActive = false;
               Score += GameConstants.KillBonus * ScoreMultiplier;               
           }

           HasBomb = false;
           AdvanceLevel();
        }
    }
}
