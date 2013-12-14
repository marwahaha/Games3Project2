﻿using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Net;

using Camera3D;
using InputHandler;
using Networking;

namespace Games3Project2.Globals
{
    public static class Global
    {
        public static Heatmap heatmapKills, heatmapDeaths, heatmapUsedJetpack;
        public static Input input = Input.Instance;
        public static NetworkManager networkManager;
        public static Level levelManager;
        public static Random rand = new Random();
        public static List<LocalPlayer> localPlayers = new List<LocalPlayer>();
        public static List<RemotePlayer> remotePlayers = new List<RemotePlayer>();
        public static List<BugBot> bugBots = new List<BugBot>();
        public static SpriteBatch spriteBatch;
        public static GameTime gameTime;
        public static Game game;
        public enum GameState { Intro, Menu, CreateMenu, JoinMenu, SetupLocalPlayers, LevelPicking, 
            Lobby, Playing, Paused, NetworkQuit,
            GameOver, ChooseHeatmap, playingHeatmap, SetupLocalPlayersHeatmap};
        public static GameState gameState = GameState.Intro;
        /// <summary>
        /// Describes the total number of gamers in the network session.
        /// </summary>
        public static byte numTotalGamers = 0;
        public static byte numLocalGamers = 0;
        public static bool debugMode 
        #region Debug Mode
        #if DEBUG
         = true;
        #else
         = false;
        #endif
        public static readonly Color debugColor = Color.Black;
        #endregion
        public static readonly Color HUD_COLOR = Color.WhiteSmoke;
        public static string winningPlayer = "";
        public static SpriteFont consolas;  //To be assigned in LoadContent()
        public static SpriteFont tahoma;    //To be assigned in LoadContent()

        public static Rectangle viewPort;
        public static Rectangle titleSafe;
        public static GraphicsDeviceManager graphics;
        public static Camera CurrentCamera
        {
            get { return currentCamera; }
            set
            {
                currentCamera = value;
                graphics.GraphicsDevice.Viewport = currentCamera.viewport;
            }
        }
        private static Camera currentCamera;

        public static SoundEffectInstance shot;
        public static SoundEffectInstance jetpack;
        public static SoundEffectInstance menusong;
        //kt22377 is the author from freesound.org
        public static SoundEffectInstance actionsong;

        public static class BulletManager
        {
            public static List<Bullet> bullets = new List<Bullet>();
            private static int nextBullet = 0;
            public static Bullet fireBullet(Vector3 startPosition, Vector3 velocity, NetworkGamer shooter, int damage)
            {
                bullets[nextBullet].fire(startPosition, velocity, shooter, damage);
                Bullet bullet = bullets[nextBullet++];
                nextBullet %= Constants.MAX_ALLOCATED_BULLETS;
                return bullet;
            }
            public static void update()
            {
                foreach (Bullet bullet in bullets)
                {
                    bullet.update(gameTime);
                }
            }
            public static void draw()
            {
                foreach (Bullet bullet in bullets)
                {
                    bullet.draw();
                }
            }
        }

        public static class Constants
        {
            public static readonly byte MAX_PLAYERS_TOTAL = 4;
            public static readonly byte MAX_PLAYERS_LOCAL = 4;
            public static readonly Color DEFAULT_PLAYER_COLOR = Color.Blue;
            public static readonly Color JUGGERNAUT_COLOR = Color.LightPink;

            public static readonly float LEVEL_ONE_WIDTH = 100;
            public static readonly float LEVEL_ONE_LENGTH = 200;
            public static readonly float LEVEL_ONE_HEIGHT = 100;

            public static readonly float LEVEL_TWO_WIDTH = 100;
            public static readonly float LEVEL_TWO_LENGTH = 100;
            public static readonly float LEVEL_TWO_HEIGHT = 165;

            public static readonly float JET_PACK_INCREMENT = 0.00006f;
            public static readonly float JET_PACK_DECREMENT = JET_PACK_INCREMENT * 0.4f; //Yes, this must remain positive.
            public static readonly float JET_FUEL_INCREMENT = .3f;
            public static readonly float JET_FUEL_DECREMENT = .6f;
            public static readonly float JET_PACK_Y_VELOCITY_CAP = 0.0010f;
            public static readonly float GRAVITY = 0.0006f;
            public static readonly float MAX_JET_FUEL = 100f;

            public static readonly float MOVEMENT_VELOCITY = 3.1f;
            public static readonly float SPIN_RATE = 100f;
            public static readonly float PLAYER_RADIUS = 5f;
            public static readonly int MAX_SCORE = 10;

            public static readonly int MAX_HEALTH = 100;
            public static readonly int BULLET_DAMAGE = 10;
            public static readonly int MAX_JUG_HEALTH = 300;
            public static readonly int JUG_BULLET_DAMAGE = 30;
            public static readonly float RIGHT_HANDED_WEAPON_OFFSET = 0.1f;
            public static readonly float BULLET_SPEED = .25f;
            public static readonly float BULLET_RADIUS = .5f;
            public static readonly Color BULLET_COLOR = Color.DarkOrange;
            public static readonly int MAX_ALLOCATED_BULLETS = 300;
            public static readonly int FIRING_COOLDOWN = 300;
            public static readonly int BOT_FIRING_COOLDOWN = 1000;

            public static readonly float WALL_BUFFER = 5;

            public static readonly string MSG_IS_JUG = " is the juggernaut";
            public static readonly string MSG_KILLED_JUG = " killed the juggernaut";
            public static readonly string MSG_KILLED_BY_JUG_1ST = "You were killed by the juggernaut";
            public static readonly string MSG_KILLED_BY_JUG_3RD = " was killed by the juggernaut";
            public static readonly string HUD_HEALTH = "HEALTH: ";
            public static readonly string HUD_JET = "JETFUEL: ";
            public static readonly string HUD_SCORE = "YOUR SCORE: ";
            public static readonly string HUD_YOU_JUG = "YOU ARE THE JUGGERNAUT";
            public static readonly string MSG_JOINED = " joined";
            public static readonly string MSG_DISCONNECTED = " disconnected";
        }

        public static class Collision
        {
            public static void bounceCollidables(Collidable a, Collidable b)
            {
                Vector3 difference = a.Position - b.Position;
                if (difference.LengthSquared() < (a.Radius * a.Radius) + (b.Radius * b.Radius))
                {
                    a.Position += new Vector3(difference.X, difference.Y, difference.Z);
                    b.Position -= new Vector3(difference.X, difference.Y, difference.Z);
                }
            }

            public static bool didCollide(Collidable a, Collidable b)
            {
                Vector3 difference = a.Position - b.Position;
                if (difference.LengthSquared() < (a.Radius * a.Radius) + (b.Radius * b.Radius))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }
    }
}
