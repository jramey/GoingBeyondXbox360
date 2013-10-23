using System;
using GoingBeyondGame;
using Microsoft.Xna.Framework;
namespace GoingBeyondGame
{
    public class Bullet
    {
        public Boolean IsActive { get; set; }
        public Vector3 Postion { get; set; }
        public Vector3 Direction { get; set; }
        public float Speed { get; set; }

        public Bullet()
        {
            this.IsActive = false;
        }

        public void Update(float delta)
        {
            Postion += Direction * Speed * GameConstants.BulletSpeedAdjustment * delta;

            if (Postion.X > GameConstants.PlayfieldSizeX ||
                Postion.X < -GameConstants.PlayfieldSizeX ||
                Postion.Y > GameConstants.PlayfieldSizeY ||
                Postion.Y < -GameConstants.PlayfieldSizeY)
                this.IsActive = false;
        }
    }
}