using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using GoingBeyondGame;

namespace GoingBeyond
{
    public class Ship
    {
        private const float VelocityScale = 5.0f;
        public Boolean IsActive { get; set; }
        public Matrix[] Transforms;
        public Vector3 Position = Vector3.Zero;
        public Vector3 Velocity = Vector3.Zero;
        public Matrix RotationMatrix =   Matrix.CreateRotationX(MathHelper.PiOver2);
        private float rotation;

        public Ship()
        {
            this.IsActive = false;
        }
        
        public float Rotation
        {
            get { return rotation; }
            set
            {
                float newVal = value;
                while (newVal >= MathHelper.TwoPi)
                {
                    newVal -= MathHelper.TwoPi;
                }
                while (newVal < 0)
                {
                    newVal += MathHelper.TwoPi;
                }

                if (rotation != value)
                {
                    rotation = value;
                    RotationMatrix =
                        Matrix.CreateRotationX(MathHelper.PiOver2) *  Matrix.CreateRotationZ(rotation);
                }

            }
        }

        public void Update(GamePadState controllerState)
        {
            Rotation -= controllerState.ThumbSticks.Left.X * 0.10f;

            Velocity += RotationMatrix.Forward * VelocityScale * controllerState.Triggers.Right;

            if (Position.X > GameConstants.PlayfieldSizeX)
                Position.X -= 2 * GameConstants.PlayfieldSizeX;
            if (Position.X < -GameConstants.PlayfieldSizeX)
                Position.X += 2 * GameConstants.PlayfieldSizeX;
            if (Position.Y > GameConstants.PlayfieldSizeY)
                Position.Y -= 2 * GameConstants.PlayfieldSizeY;
            if (Position.Y < -GameConstants.PlayfieldSizeY)
                Position.Y += 2 * GameConstants.PlayfieldSizeY;
        }
    }
}