using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using Microsoft.Xna.Framework.Net;

using BoundingVolumeRendering;
using Camera3D;
using AxisReference;
using InputHandler;
using MenuUtility;
using MusicClasses;
using ReticuleCursor;
using Geometry;
using Games3Project2.Globals;

namespace Games3Project2
{
    public class JuggernautGame : Microsoft.Xna.Framework.Game
    {
        GraphicsDeviceManager graphics;
        Axis_Reference axisReference;

        SpriteFont consolas;
        SpriteFont tahoma;

        Music music;
        Texture2D splashTexture;

        int splashTimer = 0;
        const int SPLASH_LENGTH = 3000;

        Menu mainMenu;
        Level levelManager;

        Boolean debug = true;

        //local player joining
        List<PlayerIndex> connectedPlayers = new List<PlayerIndex>();
        List<PlayerIndex> joinedPlayers = new List<PlayerIndex>();
        int winningPlayer = 1;

        public JuggernautGame()
        {
            Components.Add(new GamerServicesComponent(this));
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
#if WINDOWS
            //Preserves the 1.7777 aspect ratio of 1920x1080 (16/9)
            //verses the 1.6 of 1280x800.
            //1280 / (16/9) = 720
            graphics.PreferredBackBufferWidth = 1280;
            graphics.PreferredBackBufferHeight = 720;
#endif
        }

        protected override void Initialize()
        {
           
            Global.game = this;
            Global.networkManager = new NetworkManagement();
            Global.graphics = graphics;
            Global.graphics.GraphicsDevice.DepthStencilState = DepthStencilState.Default;
            Global.viewPort = Global.graphics.GraphicsDevice.Viewport.Bounds;
            Global.titleSafe = GetTitleSafeArea(.85f);
            axisReference = new Axis_Reference(GraphicsDevice, 1.0f);
            BoundingSphereRenderer.Initialize(GraphicsDevice, 45);
            Global.heatmapKills = new Heatmap(Color.Black);
            Global.heatmapDeaths = new Heatmap(Color.Red);
            Global.heatmapUsedJetpack = new Heatmap(Color.Green);

            this.IsMouseVisible = true;
            base.Initialize();
        }

        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to drawWalls textures.
            Global.spriteBatch = new SpriteBatch(GraphicsDevice);

            Global.shot = Global.game.Content.Load<SoundEffect>(@"Audio/plasma").CreateInstance();
            Global.jetpack = Global.game.Content.Load<SoundEffect>(@"Audio/cole").CreateInstance();
            Global.jetpack.IsLooped = true;
            Global.menusong = Global.game.Content.Load<SoundEffect>(@"Audio/menusong").CreateInstance();
            Global.menusong.IsLooped = true;
            Global.actionsong = Global.game.Content.Load<SoundEffect>(@"Audio/actionsong").CreateInstance();
            Global.actionsong.IsLooped = true;
            Global.actionsong.Volume = 0.5f;

            music = new Music(this);
            //music.playBackgroundMusic();
            splashTexture = Content.Load<Texture2D>(@"Textures\splash");
            consolas = Content.Load<SpriteFont>(@"Fonts/Consolas");
            Global.consolas = consolas;
            tahoma = Content.Load<SpriteFont>(@"Fonts/Tahoma");
            Global.tahoma = tahoma;

            List<String> menuOptions = new List<String>();
            menuOptions.Add("Create New Networked Game");
            menuOptions.Add("Create New Local Game");
            menuOptions.Add("Join Network Game");
            menuOptions.Add("Heatmaps");
            menuOptions.Add("Exit");
            mainMenu = new Menu(menuOptions, "Juggernaut", new Vector2(Global.titleSafe.Left + 30,
                Global.viewPort.Height / 2 - (menuOptions.Count / 2 * consolas.MeasureString("C").Y)));

            levelManager = new Level();
            Global.levelManager = levelManager;
        }

        protected override void UnloadContent()
        {
            music.stopBackgroundMusic();
        }

        protected override void Update(GameTime gameTime)
        {
            Global.gameTime = gameTime;
            Global.input.Update();
            // Allows the game to exit
            if (Global.input.DetectBackPressedByAnyPlayer())
            {
                // TODO: Depending on the game state...behavior of back button will differ.
                this.Exit();
            }
            if (Global.input.isFirstPress(Keys.OemTilde) ||
                Global.input.isFirstPress(Buttons.DPadDown, PlayerIndex.One) ||
                Global.input.isFirstPress(Buttons.DPadDown, PlayerIndex.Two) ||
                Global.input.isFirstPress(Buttons.DPadDown, PlayerIndex.Three) ||
                Global.input.isFirstPress(Buttons.DPadDown, PlayerIndex.Four))
            {
                Global.debugMode = !Global.debugMode; //Toggle mode
            }

            switch (Global.gameState)
            {
                #region Intro
                case Global.GameState.Intro:
                    splashTimer += Global.gameTime.ElapsedGameTime.Milliseconds;
                    if (splashTimer > SPLASH_LENGTH || 
                        Global.input.isFirstPress(Buttons.A) || 
                        Global.input.isFirstPress(Buttons.Start) ||
                        Global.input.isFirstPress(Keys.Space) || 
                        Global.input.isFirstPress(Keys.Enter) ||
                        Global.input.isFirstPress(Keys.A))
                    {
                        splashTimer = 0;
                        Global.gameState = Global.GameState.Menu;
                    }
                    break;
                #endregion //Intro

                #region Menu
                case Global.GameState.Menu:
                    Global.actionsong.Stop();
                    Global.menusong.Play();
                    switch (mainMenu.update())
                    {
                        case 0: //Create New Game (Host)
                            Global.networkManager.isHost = true;
                            Global.networkManager.isNetworked = true;
                            Global.gameState = Global.GameState.LevelPicking;
                            Global.numLocalGamers = 1;
                            Global.localPlayers.Add(new LocalPlayer(new Vector3(0, 20, 0), PlayerIndex.One, 1, 1));
                            break;
                        case 1: //Create New Local Game
                            Global.gameState = Global.GameState.SetupLocalPlayers;
                            Global.numLocalGamers = 1;
                            Global.networkManager.isHost = false;
                            Global.networkManager.isNetworked = false;
                            joinedPlayers.Add(PlayerIndex.One);
                            break;
                        case 2: //Join Game
                            Global.networkManager.isHost = false;
                            Global.networkManager.isNetworked = true;
                            Global.gameState = Global.GameState.NetworkJoining;
                            //Global.numLocalGamers = 1;
                            //Global.localPlayers.Add(new LocalPlayer(new Vector3(0, 20, 0), PlayerIndex.One, 1));
                            break;
                        case 3: //Heat maps
                            Global.networkManager.isHost = true;
                            Global.gameState = Global.GameState.SetupLocalPlayersHeatmap;
                            Global.numLocalGamers = 1;
                            joinedPlayers.Clear();
                            joinedPlayers.Add(PlayerIndex.One);
                            break;
                        case 4: //Exit
                            this.Exit();
                            break;
                    }
                    break;
                #endregion //Menu

                #region SetupLocalPlayers
                case Global.GameState.SetupLocalPlayers:
                    if (setupLocalPlayers())
                    {
                        Global.gameState = Global.GameState.LevelPicking;
                    }
                    break;
                #endregion //SetupLocalPlayers

                #region LevelPicking:
                case Global.GameState.LevelPicking:
                    if (updateLevelPicking())
                    {
                        if (Global.networkManager.isNetworked)
                        {
                            Global.gameState = Global.GameState.NetworkWaitingHost;
                        }
                        else
                        {
                            Global.gameState = Global.GameState.Playing;
                            Global.localPlayers[0].isJuggernaut = true;
                        }
                    }
                    break;
                #endregion //LevelPicking

                #region NetworkJoining
                case Global.GameState.NetworkJoining:
                    if (Global.networkManager.networkSession == null)
                    {
                        Global.networkManager.JoinSession();
                    }
                    //Chill here until remote host calls StartGame().

                    break;
                #endregion //NetworkJoining

                #region NetworkWaitingHost
                case Global.GameState.NetworkWaitingHost:

                    if (Global.networkManager.networkSession == null)
                    {
                        Global.networkManager.CreateSession();
                    }
                    else 
                    {
                        if (Global.input.isFirstPress(Buttons.A, PlayerIndex.One))
                        {
                            Global.networkManager.gameSetupPacket(levelManager.currentLevel);
                            foreach (NetworkGamer nGamer in Global.networkManager.networkSession.RemoteGamers)
                            {
                                Global.remotePlayers.Add(new RemotePlayer(new Vector3(0, 20, 0), nGamer.Id));
                            }
                            Global.networkManager.networkSession.StartGame();
                            Global.gameState = Global.GameState.Playing;
                        }
                    }
                    break;
                #endregion //NetworkWaitingHost


                #region SetupLocalPlayersHeatmap
                case Global.GameState.SetupLocalPlayersHeatmap:
                    if (setupLocalPlayers())
                    {
                        if (Global.networkManager.isHost)
                        {
                            Global.gameState = Global.GameState.ChooseHeatmap;
                        }
                        
                    }
                    if (Global.input.isFirstPress(Keys.Back))
                    {
                        Global.gameState = Global.GameState.Menu;
                    }
                    break;
                #endregion

                #region ChooseHeatmap
                case Global.GameState.ChooseHeatmap:
                    if (updateLevelPicking())
                    {
                        Global.gameState = Global.GameState.playingHeatmap;
                    }
                    break;
                #endregion

                #region PlayHeatmap
                case Global.GameState.playingHeatmap:
                    Global.menusong.Stop();
                    Global.actionsong.Play();
                    
                    debug = true;
                    if (!Global.debugMode)
                        Global.debugMode = true;
                    foreach (LocalPlayer player in Global.localPlayers)
                    {
                        player.update();
                        //TODO:if(player.isJuggernaught == true) then do the bugbot check else do nothing
                        foreach (BugBot bot in Global.bugBots)
                        {
                            if ((bot.position - player.Position).Length() < BugBot.ATTACK_RADIUS)
                            {
                                //shoot a bullet at the player if he is the juggernaught
                                Vector3 dir = player.Position - bot.position;
                                dir.Normalize();
                                //bot.ShootBullet(dir);
                                debug = false;
                            }
                        }
                    }

                    for (int i = 0; i < Global.bullets.Count; i++)
                    {
                        Global.bullets[i].update(gameTime);
                        if (Global.bullets[i].timeLived >= 1)
                            Global.bullets.RemoveAt(i);
                    }

                    levelManager.update();
                    break;
                #endregion

                #region Playing
                case Global.GameState.Playing:
                    Global.menusong.Stop();
                    Global.actionsong.Play();
                    debug = true;
                    foreach (LocalPlayer player in Global.localPlayers)
                    {
                        player.update();
                        if (player.score >= Global.Constants.MAX_SCORE)
                        {
                            for (int i = 0; i < Global.localPlayers.Count; i++)
                            {
                                Global.localPlayers[i].score = 0;
                            }
                            Global.gameState = Global.GameState.GameOver;
                            winningPlayer = player.networkPlayerID;
                            Global.graphics.GraphicsDevice.Viewport = new Viewport(0, 0, Global.viewPort.Width, Global.viewPort.Height);
                        }
                    }

                    foreach (RemotePlayer rPlayer in Global.remotePlayers)
                    {
                        rPlayer.update();
                    }

                    for (int i = 0; i < Global.bullets.Count; i++)
                    {
                        Global.bullets[i].update(gameTime);
                        if (Global.bullets[i].timeLived >= Bullet.TTL)
                            Global.bullets.RemoveAt(i);
                    }

                    levelManager.update();
                    break;
                #endregion //Playing

                #region Paused
                case Global.GameState.Paused:

                    break;
                #endregion //Paused

                #region GameOver
                case Global.GameState.GameOver:
                    foreach (LocalPlayer player in Global.localPlayers)
                    {
                        if (Global.input.isFirstPress(Buttons.A, player.playerIndex))
                        {
                            Global.gameState = Global.GameState.Menu;
                        }
                    }
                    break;
                #endregion //GameOver

                #region NetworkQuit
                case Global.GameState.NetworkQuit:
                    foreach (LocalPlayer player in Global.localPlayers)
                    {
                        if (Global.input.isFirstPress(Buttons.A, player.playerIndex))
                        {
                            Global.gameState = Global.GameState.Menu;
                        }
                    }
                    break;
                #endregion //NetworkQuit
            }

            if (Global.networkManager.networkSession != null)
            {
                Global.networkManager.Update(Global.gameTime);
            }

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            Global.gameTime = gameTime;
            GraphicsDevice.Clear(Color.CornflowerBlue);

            switch (Global.gameState)
            {
                #region Intro
                case Global.GameState.Intro:
                    Global.spriteBatch.Begin();
                    Global.spriteBatch.Draw(splashTexture, Global.viewPort, Color.White);
                    Global.spriteBatch.End();
                    break;
                #endregion
                #region Menu
                case Global.GameState.Menu:
                    Global.spriteBatch.Begin();
                    mainMenu.draw();
                    Global.spriteBatch.End();
                    break;
                #endregion
                #region SetupLocalPlayers
                case Global.GameState.SetupLocalPlayers:
                    Global.spriteBatch.Begin();
                    drawLocalPlayerSetup();
                    Global.spriteBatch.End();
                    break;
                #endregion
                #region LevelPicking:
                case Global.GameState.LevelPicking:
                    Global.spriteBatch.Begin();
                    drawLevelPicking();
                    Global.spriteBatch.End();
                    break;
                #endregion
                #region NetworkJoining
                case Global.GameState.NetworkJoining:
                    Global.spriteBatch.Begin();
                    Global.spriteBatch.End();
                    break;
                #endregion
                #region NetworkWaitingHost
                case Global.GameState.NetworkWaitingHost:
                    Global.spriteBatch.Begin();
                    drawNetworkWaitingHost();
                    Global.spriteBatch.End();
                    break;
                #endregion

                #region SetupLocalPlayersHeatmap
                case Global.GameState.SetupLocalPlayersHeatmap:
                    Global.spriteBatch.Begin();
                    drawLocalPlayerSetup();
                    Global.spriteBatch.End();
                    break;
                #endregion
                #region ChooseHeatmap
                case Global.GameState.ChooseHeatmap:
                    Global.spriteBatch.Begin();
                    drawLevelPicking();
                    Global.spriteBatch.End();
                    break;
                #endregion

                #region PlayHeatmap
                case Global.GameState.playingHeatmap:
                    //3D Drawing Section
                    resetGraphicsDevice();
                    foreach (LocalPlayer player in Global.localPlayers)
                    {
                        Global.CurrentCamera = player.camera;

                        levelManager.drawWalls();
                        levelManager.drawPlatforms();
                        foreach (LocalPlayer drawPlayer in Global.localPlayers)
                        {
                            drawPlayer.draw();
                        }
                        if (Global.bullets.Count > 0)
                        {
                            foreach (Bullet bullet in Global.bullets)
                            {
                                bullet.draw();
                            }
                        }
                    }

                    //draw the heatmaps when debug mode is ran.
                    Global.heatmapKills.draw();
                    Global.heatmapDeaths.draw();
                    Global.heatmapUsedJetpack.draw();

                    //SpriteBatch Drawing Section
                    Global.spriteBatch.Begin();

                    if (Global.debugMode)
                    {
                        

                        axisReference.Draw(Matrix.Identity, Global.CurrentCamera.view, Global.CurrentCamera.projection);
                        Global.spriteBatch.DrawString(consolas, "Press ~ to exit debug mode.",
                                new Vector2(5f, 35f), Color.PaleGreen);
                        Global.spriteBatch.DrawString(consolas, "Camera Position and View=\n" +
                            "X:" + Global.CurrentCamera.cameraPos.X.ToString() +
                            " Y:" + Global.CurrentCamera.cameraPos.Y.ToString() +
                            " Z:" + Global.CurrentCamera.cameraPos.Z.ToString(),
                            new Vector2(5f, 53f), Global.debugColor);
                        Global.spriteBatch.DrawString(consolas,
                            "Up:" + Global.CurrentCamera.view.Up.ToString() +
                            "\nLookAt: " + debug.ToString() +
                            "\nRight: " + Global.CurrentCamera.view.Right.ToString(),
                            new Vector2(5f, 95f), Global.debugColor);
                        Global.spriteBatch.DrawString(consolas,
                            "Bullet Count: " + Global.bullets.Count.ToString(),
                            new Vector2(400f, 0f), Global.debugColor);

                    }

                    foreach (LocalPlayer drawPlayer in Global.localPlayers)
                    {
                        Global.spriteBatch.End();
                        Global.CurrentCamera = drawPlayer.camera;
                        Global.spriteBatch.Begin();
                        drawPlayer.drawHUD();
                    }

                    Global.spriteBatch.End();
                    break;
                #endregion

                #region Playing
                case Global.GameState.Playing:
                    //3D Drawing Section
                    resetGraphicsDevice();
                    foreach (LocalPlayer player in Global.localPlayers)
                    {
                        Global.CurrentCamera = player.camera;
                        
                        levelManager.drawWalls();
                        levelManager.drawPlatforms();
                        foreach (LocalPlayer drawPlayer in Global.localPlayers)
                        {
                            drawPlayer.draw();
                        }
                        if (Global.bullets.Count > 0)
                        {
                            foreach (Bullet bullet in Global.bullets)
                            {
                                bullet.draw();
                            }
                        }
                    }

                    foreach (RemotePlayer rPlayer in Global.remotePlayers)
                    {
                        rPlayer.draw();
                    }

                    //SpriteBatch Drawing Section
                    Global.spriteBatch.Begin();
                    
                    if (Global.debugMode)
                    {
                        //draw the heatmaps when debug mode is ran.
                        Global.heatmapKills.draw();
                        Global.heatmapDeaths.draw();
                        Global.heatmapUsedJetpack.draw();

                        axisReference.Draw(Matrix.Identity, Global.CurrentCamera.view, Global.CurrentCamera.projection);
                        Global.spriteBatch.DrawString(consolas, "Press ~ to exit debug mode.",
                                new Vector2(5f, 35f), Color.PaleGreen);
                        Global.spriteBatch.DrawString(consolas, "Camera Position and View=\n" +
                            "X:" + Global.CurrentCamera.cameraPos.X.ToString() +
                            " Y:" + Global.CurrentCamera.cameraPos.Y.ToString() +
                            " Z:" + Global.CurrentCamera.cameraPos.Z.ToString(),
                            new Vector2(5f, 53f), Global.debugColor);
                        Global.spriteBatch.DrawString(consolas,
                            "Up:" + Global.CurrentCamera.view.Up.ToString() +
                            "\nLookAt: " + debug.ToString() +
                            "\nRight: " + Global.CurrentCamera.view.Right.ToString(),
                            new Vector2(5f, 95f), Global.debugColor);
                        Global.spriteBatch.DrawString(consolas,
                            "Bullet Count: " + Global.bullets.Count.ToString(),
                            new Vector2(400f, 0f), Global.debugColor);

                    }

                    foreach (LocalPlayer drawPlayer in Global.localPlayers)
                    {
                        Global.spriteBatch.End();
                        Global.CurrentCamera = drawPlayer.camera;
                        Global.spriteBatch.Begin();
                        drawPlayer.drawHUD();
                    }

                    Global.spriteBatch.End();
                    break;
                #endregion
                #region Paused
                case Global.GameState.Paused:
                    Global.spriteBatch.Begin();

                    Global.spriteBatch.End();
                    break;
                #endregion
                #region GameOver
                case Global.GameState.GameOver:
                    Global.spriteBatch.Begin();

                    Global.spriteBatch.Draw(mainMenu.background, Global.viewPort, Color.White);
                    Global.spriteBatch.DrawString(consolas, "Player " + winningPlayer.ToString() + " Won!", new Vector2(20, Global.viewPort.Height / 2), Color.Black);
                    Global.spriteBatch.DrawString(consolas, "Press A To Continue", new Vector2(20, Global.viewPort.Height / 2 + 50), Color.Black);

                    Global.spriteBatch.End();
                    break;
                #endregion
                #region NetworkQuit
                case Global.GameState.NetworkQuit:
                    Global.spriteBatch.Begin();
                    
                    Global.spriteBatch.Draw(mainMenu.background, Global.viewPort, Color.White);
                    Global.spriteBatch.DrawString(consolas, "Network Session Ended", new Vector2(20, Global.viewPort.Height / 2), Color.Black);
                    Global.spriteBatch.DrawString(consolas, "Press A To Continue", new Vector2(20, Global.viewPort.Height / 2 + 50), Color.Black);

                    Global.spriteBatch.End();
                    break;
                #endregion
            }

            base.Draw(gameTime);
        }

        protected Rectangle GetTitleSafeArea(float percent)
        {
            Rectangle retval = new Rectangle(Global.graphics.GraphicsDevice.Viewport.X, Global.graphics.GraphicsDevice.Viewport.Y,
                                             Global.graphics.GraphicsDevice.Viewport.Width, Global.graphics.GraphicsDevice.Viewport.Height);
#if XBOX
            float border = (1 - percent) / 2;
            retval.X = (int)(border * retval.Width);
            retval.Y = (int)(border * retval.Height);
            retval.Width = (int)(percent * retval.Width);
            retval.Height = (int)(percent * retval.Height);
#endif

            return retval;
        }

        private bool setupLocalPlayers()
        {
            //Dynamically update the connected controllers
            for (PlayerIndex index = PlayerIndex.Two; index <= PlayerIndex.Four; index++)
            {
                if (Global.input.isConnected(index) && !connectedPlayers.Contains(index) && !joinedPlayers.Contains(index))
                {
                    connectedPlayers.Add(index);
                }
                if (!Global.input.isConnected(index) && connectedPlayers.Contains(index))
                {
                    connectedPlayers.Remove(index);
                }
            }

            for (int i = 0; i < connectedPlayers.Count; ++i)
            {
                if(Global.input.isFirstPress(Buttons.A, connectedPlayers[i]))
                {
                    joinedPlayers.Add(connectedPlayers[i]);
                    Global.numLocalGamers++;
                    connectedPlayers.RemoveAt(i--);
                }
            }

            foreach (PlayerIndex index in joinedPlayers)
            {
                if (Global.input.isFirstPress(Buttons.Start, index))
                {
                    for (int i = 0; i < joinedPlayers.Count; ++i)
                    {
                        //Global.numLocalGamers = 3;
                        Global.localPlayers.Add(new LocalPlayer(new Vector3(0, 20 + i * 20, 0), joinedPlayers[i], i + 1, i + 1));
                        Global.localPlayers[i].isJuggernaut = true;
                        //Global.localPlayers.Add(new LocalPlayer(new Vector3(-10, 20, 0), PlayerIndex.Two, 2));
                        //Global.localPlayers.Add(new LocalPlayer(new Vector3(10, 20, 0), PlayerIndex.Three, 3));
                    }
                    return true;
                }
            }

            if (joinedPlayers.Count == 4)
            {
                for (int i = 0; i < joinedPlayers.Count; ++i)
                {
                    //Global.numLocalGamers = 3;
                    Global.localPlayers.Add(new LocalPlayer(new Vector3(0, 20 + i * 20, 0), joinedPlayers[i], i + 1, i + 1));
                    //Global.localPlayers.Add(new LocalPlayer(new Vector3(-10, 20, 0), PlayerIndex.Two, 2));
                    //Global.localPlayers.Add(new LocalPlayer(new Vector3(10, 20, 0), PlayerIndex.Three, 3));
                }
                return true;
            }

            return false;
        }

        private void drawLocalPlayerSetup()
        {
            Global.spriteBatch.Draw(mainMenu.background, Global.viewPort, Color.White);
            float yOffset = consolas.MeasureString("A").Y;
            Vector2 startingPosition = new Vector2(Global.titleSafe.Left + 30, Global.titleSafe.Top + 100);

            for (PlayerIndex index = PlayerIndex.One; index <= PlayerIndex.Four; index++)
            {
                if (index == PlayerIndex.One)
                {
                    Global.spriteBatch.DrawString(consolas, "Joined", new Vector2(startingPosition.X, startingPosition.Y + (int)index * yOffset), Color.Green);
                }
                else if (!Global.input.isConnected(index))
                {
                    Global.spriteBatch.DrawString(consolas, "Controller " + index.ToString() + " Disconnected", new Vector2(startingPosition.X, startingPosition.Y + (int)index * yOffset), Color.Red);
                }
                else if(connectedPlayers.Contains(index))
                {
                    Global.spriteBatch.DrawString(consolas, "Press A to Join", new Vector2(startingPosition.X, startingPosition.Y + (int)index * yOffset), Color.Black);
                }
                else if (joinedPlayers.Contains(index))
                {
                    Global.spriteBatch.DrawString(consolas, "Joined", new Vector2(startingPosition.X, startingPosition.Y + (int)index * yOffset), Color.Green);
                }
            }

            if (joinedPlayers.Count > 0)
            {
                Global.spriteBatch.DrawString(consolas, "Press Start to Continue", new Vector2(startingPosition.X, startingPosition.Y + 6 * yOffset), Color.Black);
            }
        }

        private bool updateLevelPicking()
        {
            bool didSelect = false;
            if (Global.input.isFirstPress(Buttons.A, Global.localPlayers[0].playerIndex) ||
                Global.input.isFirstPress(Keys.A) ||
                Global.input.isFirstPress(Keys.Enter))
            {
                levelManager.setupLevelOne();
                didSelect = true;
            }
            else if (Global.input.isFirstPress(Buttons.B, Global.localPlayers[0].playerIndex) ||
                Global.input.isFirstPress(Keys.B))
            {
                levelManager.setupLevelTwo();
                didSelect = true;
            }

            return didSelect;
        }

        private void drawLevelPicking()
        {
            Global.spriteBatch.Draw(mainMenu.background, Global.viewPort, Color.White);
            float yOffset = consolas.MeasureString("A").Y;
            Vector2 startingPosition = new Vector2(Global.titleSafe.Left + 30, Global.titleSafe.Top + 100);

            Global.spriteBatch.DrawString(consolas, "Press A For Level One", startingPosition, Color.Black);
            Global.spriteBatch.DrawString(consolas, "Press B For Level Two", new Vector2(startingPosition.X, startingPosition.Y + yOffset), Color.Black);
        }

        private void drawNetworkWaitingHost()
        {
            Global.spriteBatch.Draw(mainMenu.background, Global.viewPort, Color.White);
            float yOffset = consolas.MeasureString("A").Y;
            Vector2 startingPosition = new Vector2(Global.titleSafe.Left + 30, Global.titleSafe.Top + 100);

            Global.spriteBatch.DrawString(consolas, "Press A to continue when all gamers have joined", startingPosition, Color.Black);
        }

        private void resetGraphicsDevice()
        {
            Global.graphics.GraphicsDevice.BlendState = BlendState.Opaque;
            Global.graphics.GraphicsDevice.DepthStencilState = DepthStencilState.Default;
            Global.graphics.GraphicsDevice.SamplerStates[0] = SamplerState.LinearWrap;
        }
    }
}
