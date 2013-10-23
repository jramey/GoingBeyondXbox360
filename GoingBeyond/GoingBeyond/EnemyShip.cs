using System;
using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace GoingBeyondGame
{
    public class EnemyShip
    {
        public Boolean IsActive { get; set; }
        public Vector3 position;
        public Vector3 direction;
        public float Speed { get; set; }
        public List<Bullet> Bullets  {get; set;}

        public EnemyShip()
        {
            this.IsActive = true;
            this.Bullets = new List<Bullet>();
        }

        public void Update(float delta)
        {
            position += direction * Speed * GameConstants.EnemyShipSpeedAdjustment * delta;

            if (position.X > GameConstants.PlayfieldSizeX)
                position.X -= 2 * GameConstants.PlayfieldSizeX;
            if (position.X < -GameConstants.PlayfieldSizeX)
                position.X += 2 * GameConstants.PlayfieldSizeX;
            if (position.Y > GameConstants.PlayfieldSizeY)
                position.Y -= 2 * GameConstants.PlayfieldSizeY;
            if (position.Y < -GameConstants.PlayfieldSizeY)
                position.Y += 2 * GameConstants.PlayfieldSizeY;
        }
    }
}