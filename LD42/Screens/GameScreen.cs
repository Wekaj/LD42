﻿using Artemis;
using Artemis.Manager;
using Artemis.Utils;
using LD42.Ecs.Components;
using LD42.Ecs.Systems;
using LD42.Graphics;
using LD42.Items;
using LD42.Tools;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;

namespace LD42.Screens {
    public sealed class GameScreen : IScreen {
        private static readonly Random _random = new Random();

        private readonly LD42Game _game;

        private readonly EntityWorld _entityWorld;
        private readonly Rectangle _ground, _box;
        private readonly Furnace _furnace;
        private readonly Skylight _skylight;
        private readonly SimpleTool _musicBox, _bellows;

        private readonly SpriteBatch _spriteBatch;

        private Texture2D _groundTexture, _gateTexture, _boxTexture, _coalTexture,
            _handOpenTexture, _handGrabTexture, _pixelTexture, _soulSeedTexture,
            _soulSaplingTexture, _soulPlantTexture, _minionTexture, _coalLargeTexture,
            _armTexture, _jointTexture, _madPlantTexture, _redSeedTexture,
            _redSaplingTexture, _redPlantTexture, _greenSeedTexture, _greenSaplingTexture,
            _greenPlantTexture, _blueSeedTexture, _blueSaplingTexture, _bluePlantTexture,
            _goldPlantTexture, _borderTexture, _shadowTexture, _paperTexture,
            _tempBorderTexture, _tempFillTexture;
        private SoundEffect _swishSound, _swoshSound, _bonkSound, _grindSound, _grind2Sound, _musicSound,
            _clickSound, _paperSound, _pffSound, _popSound, _gateOpenSound, _gateCloseSound, _failSound,
            _hissSound, _warningSound, _finalWarningSound;

        private SoundEffectInstance _furnaceWheelSound, _bellowsWheelSound, _musicBoxSound, _bellowsDangerSound,
            _warningDangerSound, _finalWarningDangerSound;

        private float _timer = 0f;
        private int _orders = 0;

        private Entity _hand, _object;

        private float _coalTimer = 0f, _coalPeriod = 5f;

        private float _flamePower = 100f;

        private const float _furnaceAnimationDuration = 0.075f;
        private float _furnaceAnimation = 0f;
        
        private const float _skylightAnimationDuration = 0.075f;
        private float _skylightAnimation = 0f;

        private int _incomingMinions;
        private float _minionTimer;

        private readonly List<Item> _herbQueue = new List<Item>();
        private float _requestTimer = 0f;
        private float _requestInterval = 18f;

        private float _lastHerbX = 0f;

        private int _count = 0;

        public GameScreen(LD42Game game) {
            _game = game;

            _entityWorld = new EntityWorld();

            _ground = new Rectangle(0, 0, 448, 400);
            _ground.Offset((_game.GraphicsDevice.Viewport.Width - _ground.Width) / 2f, 
                (_game.GraphicsDevice.Viewport.Height - _ground.Height) / 2f + 36f);

            _box = new Rectangle(0, 0, 96, 160);
            _box.Offset((_game.GraphicsDevice.Viewport.Width - _box.Width) / 2f, 
                154f - _box.Height / 2f);

            LoadContent(game.Content);

            _furnace = new Furnace(new Rectangle(_ground.Left, _ground.Top, 448, 128), _gateOpenSound, _gateCloseSound);
            _musicBox = new SimpleTool();
            _bellows = new SimpleTool();
            _skylight = new Skylight(_gateOpenSound, _gateCloseSound);

            _spriteBatch = new SpriteBatch(game.GraphicsDevice);

            CreateSystems();

            CreateHand(_box.Center.ToVector2() + new Vector2(192f, 32f), _box.Center.ToVector2() + new Vector2(16f, -8f));
            CreateHand(_box.Center.ToVector2() + new Vector2(-192f, 32f), _box.Center.ToVector2() + new Vector2(-16f, -8f));
            CreateHand(_box.Center.ToVector2() + new Vector2(128f, 128f), _box.Center.ToVector2() + new Vector2(12f, 8f));
            CreateHand(_box.Center.ToVector2() + new Vector2(-128f, 128f), _box.Center.ToVector2() + new Vector2(-12f, 8f));

            Entity furnace = _entityWorld.CreateEntity();
            furnace.AddComponent(new PositionComponent(new Vector2(64f, 160f)));
            furnace.AddComponent(new ToolComponent(_furnace, 24f, t => {
                float p = (t % 1f) / 1f;
                return new Vector2((float)Math.Cos(p * MathHelper.TwoPi) * 8f, (float)Math.Sin(p * MathHelper.TwoPi) * 32f);
            }));

            Entity bellows = _entityWorld.CreateEntity();
            bellows.AddComponent(new PositionComponent(new Vector2(_game.GraphicsDevice.Viewport.Width - 64f, 160f)));
            bellows.AddComponent(new ToolComponent(_bellows, 24f, t => {
                float p = (t % 1.5f) / 1.5f;
                return new Vector2((float)Math.Cos(p * MathHelper.TwoPi) * 8f, (float)Math.Sin(p * MathHelper.TwoPi) * 32f);
            }));

            Entity skylight = _entityWorld.CreateEntity();
            skylight.AddComponent(new PositionComponent(new Vector2(_game.GraphicsDevice.Viewport.Width - 44f, 256f)));
            skylight.AddComponent(new ToolComponent(_skylight, 16f, t => {
                return Vector2.Zero;
            }));

            Entity musicBox = _entityWorld.CreateEntity();
            musicBox.AddComponent(new PositionComponent(new Vector2(50f, 304f)));
            musicBox.AddComponent(new ToolComponent(_musicBox, 20f, t => {
                float p = (t % 2f) / 2f;
                return new Vector2((float)Math.Cos(p * MathHelper.TwoPi) * 16f, (float)Math.Sin(p * MathHelper.TwoPi) * 16f);
            }));

            Entity greenSeedBox = _entityWorld.CreateEntity();
            greenSeedBox.AddComponent(new PositionComponent(new Vector2(_game.GraphicsDevice.Viewport.Width - 42f, _game.GraphicsDevice.Viewport.Height - 206f)));
            greenSeedBox.AddComponent(new ObjectComponent(Item.None, 24f) {
                SpawnerType = Item.GreenSeed
            });

            Entity redSeedBox = _entityWorld.CreateEntity();
            redSeedBox.AddComponent(new PositionComponent(new Vector2(_game.GraphicsDevice.Viewport.Width - 42f, _game.GraphicsDevice.Viewport.Height - 154f)));
            redSeedBox.AddComponent(new ObjectComponent(Item.None, 24f) {
                SpawnerType = Item.RedSeed
            });

            Entity blueSeedBox = _entityWorld.CreateEntity();
            blueSeedBox.AddComponent(new PositionComponent(new Vector2(_game.GraphicsDevice.Viewport.Width - 42f, _game.GraphicsDevice.Viewport.Height - 102f)));
            blueSeedBox.AddComponent(new ObjectComponent(Item.None, 24f) {
                SpawnerType = Item.BlueSeed
            });

            Entity soulSeedBox = _entityWorld.CreateEntity();
            soulSeedBox.AddComponent(new PositionComponent(new Vector2(_game.GraphicsDevice.Viewport.Width - 42f, _game.GraphicsDevice.Viewport.Height - 52f)));
            soulSeedBox.AddComponent(new ObjectComponent(Item.None, 24f) {
                SpawnerType = Item.SoulSeed
            });

            Console.WriteLine(_ground.Left + ", " + _ground.Bottom);
        }

        private void CreateSystems() {
            _entityWorld.SystemManager.SetSystem(new ToolUpdatingSystem(), GameLoopType.Update);
            _entityWorld.SystemManager.SetSystem(new HoldingSystem(), GameLoopType.Update);
            _entityWorld.SystemManager.SetSystem(new MinionSystem(_furnace, _musicBox), GameLoopType.Update);
            _entityWorld.SystemManager.SetSystem(new ObjectCollisionSystem(), GameLoopType.Update);
            _entityWorld.SystemManager.SetSystem(new ObjectBoundariesSystem(_ground, _box), GameLoopType.Update);
            _entityWorld.SystemManager.SetSystem(new ObjectGravitySystem(_furnace, _bonkSound), GameLoopType.Update);
            _entityWorld.SystemManager.SetSystem(new ForceSystem(), GameLoopType.Update);
            _entityWorld.SystemManager.SetSystem(new VelocitySystem(), GameLoopType.Update);
            _entityWorld.SystemManager.SetSystem(new HandTargetSystem(), GameLoopType.Update);
            _entityWorld.SystemManager.SetSystem(new AnimationSystem(), GameLoopType.Update);

            _entityWorld.SystemManager.SetSystem(new HandRotationSystem(_box.Center.ToVector2()), GameLoopType.Draw);
            _entityWorld.SystemManager.SetSystem(new ObjectSortingSystem(), GameLoopType.Draw);
            _entityWorld.SystemManager.SetSystem(new SpriteDrawingSystem(_spriteBatch), GameLoopType.Draw);
            _entityWorld.SystemManager.SetSystem(new ArmDrawingSystem(_spriteBatch, _armTexture, _jointTexture), GameLoopType.Draw);
        }

        private void LoadContent(ContentManager content) {
            _groundTexture = content.Load<Texture2D>("Textures/ground");
            _gateTexture = content.Load<Texture2D>("Textures/gate");
            _boxTexture = content.Load<Texture2D>("Textures/box");
            _coalTexture = content.Load<Texture2D>("Textures/coal");
            _handOpenTexture = content.Load<Texture2D>("Textures/hand_open");
            _handGrabTexture = content.Load<Texture2D>("Textures/hand_grab");
            _pixelTexture = content.Load<Texture2D>("Textures/pixel");
            _soulSeedTexture = content.Load<Texture2D>("Textures/soul_seed");
            _soulSaplingTexture = content.Load<Texture2D>("Textures/soul_sapling");
            _soulPlantTexture = content.Load<Texture2D>("Textures/soul_plant");
            _redSeedTexture = content.Load<Texture2D>("Textures/red_seed");
            _redSaplingTexture = content.Load<Texture2D>("Textures/red_sapling");
            _redPlantTexture = content.Load<Texture2D>("Textures/red_plant");
            _greenSeedTexture = content.Load<Texture2D>("Textures/green_seed");
            _greenSaplingTexture = content.Load<Texture2D>("Textures/green_sapling");
            _greenPlantTexture = content.Load<Texture2D>("Textures/green_plant");
            _blueSeedTexture = content.Load<Texture2D>("Textures/blue_seed");
            _blueSaplingTexture = content.Load<Texture2D>("Textures/blue_sapling");
            _bluePlantTexture = content.Load<Texture2D>("Textures/blue_plant");
            _goldPlantTexture = content.Load<Texture2D>("Textures/gold_plant");
            _minionTexture = content.Load<Texture2D>("Textures/minion");
            _coalLargeTexture = content.Load<Texture2D>("Textures/coal_large");
            _armTexture = content.Load<Texture2D>("Textures/arm");
            _jointTexture = content.Load<Texture2D>("Textures/joint");
            _madPlantTexture = content.Load<Texture2D>("Textures/mad_plant");
            _borderTexture = content.Load<Texture2D>("Textures/border");
            _shadowTexture = content.Load<Texture2D>("Textures/shadow");
            _paperTexture = content.Load<Texture2D>("Textures/paper");
            _tempBorderTexture = content.Load<Texture2D>("Textures/temp_border");
            _tempFillTexture = content.Load<Texture2D>("Textures/temp_fill");

            _swishSound = content.Load<SoundEffect>("Sounds/swish");
            _swoshSound = content.Load<SoundEffect>("Sounds/swosh");
            _bonkSound = content.Load<SoundEffect>("Sounds/bonk");
            _grindSound = content.Load<SoundEffect>("Sounds/grind");
            _grind2Sound = content.Load<SoundEffect>("Sounds/grind2");
            _musicSound = content.Load<SoundEffect>("Sounds/music");
            _clickSound = content.Load<SoundEffect>("Sounds/click");
            _paperSound = content.Load<SoundEffect>("Sounds/paper");
            _pffSound = content.Load<SoundEffect>("Sounds/pff");
            _popSound = content.Load<SoundEffect>("Sounds/pop");
            _gateOpenSound = content.Load<SoundEffect>("Sounds/gate_open");
            _gateCloseSound = content.Load<SoundEffect>("Sounds/gate_close");
            _failSound = content.Load<SoundEffect>("Sounds/fail");
            _hissSound = content.Load<SoundEffect>("Sounds/hiss");
            _warningSound = content.Load<SoundEffect>("Sounds/warning");
            _finalWarningSound = content.Load<SoundEffect>("Sounds/final_warning");

            _furnaceWheelSound = _grindSound.CreateInstance();
            _furnaceWheelSound.IsLooped = true;
            _furnaceWheelSound.Pan = -0.25f;

            _bellowsWheelSound = _grind2Sound.CreateInstance();
            _bellowsWheelSound.IsLooped = true;
            _bellowsWheelSound.Pan = 0.25f;

            _musicBoxSound = _musicSound.CreateInstance();
            _musicBoxSound.IsLooped = true;
            _musicBoxSound.Play();
            _musicBoxSound.Pause();

            _bellowsDangerSound = _hissSound.CreateInstance();
            _bellowsDangerSound.IsLooped = true;
            _bellowsDangerSound.Pan = 0.1f;

            _warningDangerSound = _warningSound.CreateInstance();
            _warningDangerSound.IsLooped = true;

            _finalWarningDangerSound = _finalWarningSound.CreateInstance();
            _finalWarningDangerSound.IsLooped = true;
        }

        private void CreateHand(Vector2 position, Vector2 shoulder) {
            Entity hand = _entityWorld.CreateEntity();
            hand.AddComponent(new PositionComponent(position) { Depth = 50f });
            hand.AddComponent(new VelocityComponent(1000f));
            hand.AddComponent(new ForceComponent(2f));
            hand.AddComponent(new HandComponent(shoulder, position, 50f));
            hand.AddComponent(new SpriteComponent(_handOpenTexture, _handOpenTexture.Bounds.Center.ToVector2()) {
                LayerDepth = Layers.Hands
            });
        }

        private Entity Create(Item item, Vector2 position) {
            switch (item) {
                case Item.Coal: {
                    return CreateCoal(position);
                }
                case Item.SoulSeed: {
                    return CreateSoulSeed(position);
                }
                case Item.SoulSapling: {
                    return CreateSoulSapling(position);
                }
                case Item.SoulPlant: {
                    return CreateSoulPlant(position);
                }
                case Item.Minion: {
                    return CreateMinion(position);
                }
                case Item.MadPlant:  {
                    return CreateMadPlant(position);
                }
                case Item.RedSeed: {
                    return CreateRedSeed(position);
                }
                case Item.RedSapling: {
                    return CreateRedSapling(position);
                }
                case Item.RedPlant: {
                    return CreateRedPlant(position);
                }
                case Item.GreenSeed: {
                    return CreateGreenSeed(position);
                }
                case Item.GreenSapling: {
                    return CreateGreenSapling(position);
                }
                case Item.GreenPlant: {
                    return CreateGreenPlant(position);
                }
                case Item.BlueSeed: {
                    return CreateBlueSeed(position);
                }
                case Item.BlueSapling: {
                    return CreateBlueSapling(position);
                }
                case Item.BluePlant: {
                    return CreateBluePlant(position);
                }
                case Item.GoldPlant: {
                    return CreateGoldPlant(position);
                }
                default: {
                    return null;
                }
            }
        }

        private Entity CreateItem(Vector2 position, Item type, float radius, Texture2D texture, float rotation = 0f) {
            Entity item = _entityWorld.CreateEntity();
            item.AddComponent(new PositionComponent(position));
            item.AddComponent(new VelocityComponent(1000f) { MaxSpeed = 300f });
            item.AddComponent(new ForceComponent(1f));
            item.AddComponent(new ObjectComponent(type, radius));
            item.AddComponent(new SpriteComponent(texture, texture.Bounds.Center.ToVector2()) { Rotation = rotation });
            return item;
        }

        private Entity CreateCoal(Vector2 position) {
            int r = _random.Next(100);
            if (r < 20) {
                Entity coal = CreateItem(position, Item.Coal, 23f, _coalLargeTexture, (float)_random.NextDouble() * MathHelper.TwoPi);
                coal.GetComponent<ForceComponent>().Mass = 2f;
                return coal;
            }
            return CreateItem(position, Item.Coal, 15f, _coalTexture, (float)_random.NextDouble() * MathHelper.TwoPi);
        }

        private Entity CreateSoulSeed(Vector2 position) {
            return CreateItem(position, Item.SoulSeed, 4f, _soulSeedTexture);
        }

        private Entity CreateSoulSapling(Vector2 position) {
            Entity sapling = CreateItem(position, Item.SoulSapling, 12f, _soulSaplingTexture);
            sapling.AddComponent(new AnimationComponent());
            sapling.GetComponent<AnimationComponent>().Play(new Animation(32, 32).AddFrame(0, 0).AddFrame(1, 0).AddFrame(2, 0).AddFrame(3, 0), 0.25f);

            sapling.GetComponent<SpriteComponent>().SourceRectangle = new Rectangle(0, 0, 32, 32);
            sapling.GetComponent<SpriteComponent>().Origin = new Vector2(16f);

            sapling.GetComponent<ObjectComponent>().TransformType = Item.SoulPlant;
            sapling.GetComponent<ObjectComponent>().TransformTimer = 13f;
            return sapling;
        }

        private Entity CreateSoulPlant(Vector2 position) {
            Entity plant = CreateItem(position, Item.SoulPlant, 20f, _soulPlantTexture);
            plant.AddComponent(new AnimationComponent());
            plant.GetComponent<AnimationComponent>().Play(new Animation(48, 48).AddFrame(0, 0).AddFrame(1, 0).AddFrame(2, 0), 0.2f);

            plant.GetComponent<SpriteComponent>().SourceRectangle = new Rectangle(0, 0, 48, 48);
            plant.GetComponent<SpriteComponent>().Origin = new Vector2(24f);
            return plant;
        }

        private Entity CreateRedSeed(Vector2 position) {
            return CreateItem(position, Item.RedSeed, 4f, _redSeedTexture);
        }

        private Entity CreateRedSapling(Vector2 position) {
            Entity sapling = CreateItem(position, Item.RedSapling, 12f, _redSaplingTexture);
            sapling.AddComponent(new AnimationComponent());
            sapling.GetComponent<AnimationComponent>().Play(new Animation(32, 32).AddFrame(0, 0).AddFrame(1, 0).AddFrame(2, 0).AddFrame(3, 0), 0.25f);

            sapling.GetComponent<SpriteComponent>().SourceRectangle = new Rectangle(0, 0, 32, 32);
            sapling.GetComponent<SpriteComponent>().Origin = new Vector2(16f);

            sapling.GetComponent<ObjectComponent>().TransformType = Item.RedPlant;
            sapling.GetComponent<ObjectComponent>().TransformTimer = 9f;
            return sapling;
        }

        private Entity CreateRedPlant(Vector2 position) {
            Entity plant = CreateItem(position, Item.RedPlant, 20f, _redPlantTexture);
            plant.AddComponent(new AnimationComponent());
            plant.GetComponent<AnimationComponent>().Play(new Animation(48, 48).AddFrame(0, 0).AddFrame(1, 0).AddFrame(2, 0), 0.2f);

            plant.GetComponent<SpriteComponent>().SourceRectangle = new Rectangle(0, 0, 48, 48);
            plant.GetComponent<SpriteComponent>().Origin = new Vector2(24f);
            return plant;
        }

        private Entity CreateGreenSeed(Vector2 position) {
            return CreateItem(position, Item.GreenSeed, 4f, _greenSeedTexture);
        }

        private Entity CreateGreenSapling(Vector2 position) {
            Entity sapling = CreateItem(position, Item.GreenSapling, 12f, _greenSaplingTexture);
            sapling.AddComponent(new AnimationComponent());
            sapling.GetComponent<AnimationComponent>().Play(new Animation(32, 32).AddFrame(0, 0).AddFrame(1, 0).AddFrame(2, 0).AddFrame(3, 0), 0.25f);

            sapling.GetComponent<SpriteComponent>().SourceRectangle = new Rectangle(0, 0, 32, 32);
            sapling.GetComponent<SpriteComponent>().Origin = new Vector2(16f);

            sapling.GetComponent<ObjectComponent>().TransformType = Item.GreenPlant;
            sapling.GetComponent<ObjectComponent>().TransformTimer = 26f;
            return sapling;
        }

        private Entity CreateGreenPlant(Vector2 position) {
            Entity plant = CreateItem(position, Item.GreenPlant, 20f, _greenPlantTexture);
            plant.AddComponent(new AnimationComponent());
            plant.GetComponent<AnimationComponent>().Play(new Animation(48, 48).AddFrame(0, 0).AddFrame(1, 0).AddFrame(2, 0), 0.2f);

            plant.GetComponent<SpriteComponent>().SourceRectangle = new Rectangle(0, 0, 48, 48);
            plant.GetComponent<SpriteComponent>().Origin = new Vector2(24f);
            return plant;
        }

        private Entity CreateBlueSeed(Vector2 position) {
            return CreateItem(position, Item.BlueSeed, 4f, _blueSeedTexture);
        }

        private Entity CreateBlueSapling(Vector2 position) {
            Entity sapling = CreateItem(position, Item.BlueSapling, 12f, _blueSaplingTexture);
            sapling.AddComponent(new AnimationComponent());
            sapling.GetComponent<AnimationComponent>().Play(new Animation(32, 32).AddFrame(0, 0).AddFrame(1, 0).AddFrame(2, 0).AddFrame(3, 0), 0.25f);

            sapling.GetComponent<SpriteComponent>().SourceRectangle = new Rectangle(0, 0, 32, 32);
            sapling.GetComponent<SpriteComponent>().Origin = new Vector2(16f);

            sapling.GetComponent<ObjectComponent>().TransformType = Item.BluePlant;
            sapling.GetComponent<ObjectComponent>().TransformTimer = 17f;
            return sapling;
        }

        private Entity CreateBluePlant(Vector2 position) {
            Entity plant = CreateItem(position, Item.BluePlant, 20f, _bluePlantTexture);
            plant.AddComponent(new AnimationComponent());
            plant.GetComponent<AnimationComponent>().Play(new Animation(48, 48).AddFrame(0, 0).AddFrame(1, 0).AddFrame(2, 0), 0.2f);

            plant.GetComponent<SpriteComponent>().SourceRectangle = new Rectangle(0, 0, 48, 48);
            plant.GetComponent<SpriteComponent>().Origin = new Vector2(24f);

            plant.GetComponent<ObjectComponent>().TransformType = Item.GoldPlant;
            plant.GetComponent<ObjectComponent>().TransformTimer = 17f;
            return plant;
        }

        private Entity CreateGoldPlant(Vector2 position) {
            Entity plant = CreateItem(position, Item.GoldPlant, 20f, _goldPlantTexture);
            plant.AddComponent(new AnimationComponent());
            plant.GetComponent<AnimationComponent>().Play(new Animation(48, 48).AddFrame(0, 0).AddFrame(1, 0).AddFrame(2, 0), 0.2f);

            plant.GetComponent<SpriteComponent>().SourceRectangle = new Rectangle(0, 0, 48, 48);
            plant.GetComponent<SpriteComponent>().Origin = new Vector2(24f);
            return plant;
        }

        private Entity CreateMinion(Vector2 position) {
            Entity minion = CreateItem(position, Item.Minion, 13f, _minionTexture);
            minion.AddComponent(new MinionComponent());

            minion.GetComponent<ObjectComponent>().IsSolid = false;

            minion.GetComponent<SpriteComponent>().SourceRectangle = new Rectangle(0, 0, 32, 32);
            minion.GetComponent<SpriteComponent>().Origin = new Vector2(16f);

            minion.AddComponent(new AnimationComponent());
            minion.GetComponent<AnimationComponent>().Play(new Animation(32, 32).AddFrame(0, 0).AddFrame(1, 0).AddFrame(2, 0), 0.2f, true);
            return minion;
        }

        private Entity CreateMadPlant(Vector2 position) {
            Entity plant = CreateItem(position, Item.MadPlant, 17f, _madPlantTexture);
            plant.AddComponent(new AnimationComponent());
            plant.GetComponent<AnimationComponent>().Play(new Animation(48, 48).AddFrame(0, 0).AddFrame(1, 0).AddFrame(2, 0), 0.2f);

            plant.GetComponent<SpriteComponent>().SourceRectangle = new Rectangle(0, 0, 48, 48);
            plant.GetComponent<SpriteComponent>().Origin = new Vector2(24f);

            plant.GetComponent<ObjectComponent>().SpreadType = Item.MadPlant;
            plant.GetComponent<ObjectComponent>().SpreadTimer = 3f;
            return plant;
        }

        private void GrabItem(Entity hand, Entity item) {
            ObjectComponent objectComponent = item.GetComponent<ObjectComponent>();
            PositionComponent positionComponent = item.GetComponent<PositionComponent>();

            if (objectComponent.SpawnerType != Item.None) {
                Entity newItem = Create(objectComponent.SpawnerType, positionComponent.Position);

                GrabItem(hand, newItem);
            }
            else {
                _hand = hand;
                _object = item;

                objectComponent.IsHeld = true;

                HandComponent handComponent = hand.GetComponent<HandComponent>();
                handComponent.HeldItem = _object;

                SpriteComponent spriteComponent = hand.GetComponent<SpriteComponent>();
                spriteComponent.Texture = _handGrabTexture;

                _swishSound.Play();
            }
        }

        private void DropItem(Entity hand) {
            if (hand == null) {
                return;
            }

            HandComponent handComponent = hand.GetComponent<HandComponent>();

            if (handComponent.HeldItem != null) {
                ObjectComponent objectComponent = handComponent.HeldItem.GetComponent<ObjectComponent>();
                objectComponent.IsHeld = false;

                handComponent.HeldItem = null;

                SpriteComponent spriteComponent = hand.GetComponent<SpriteComponent>();
                spriteComponent.Texture = _handOpenTexture;

                _swoshSound.Play();
            }
        }

        private void GrabTool(Entity hand, Entity tool) {
            ToolComponent toolComponent = tool.GetComponent<ToolComponent>();
            PositionComponent toolPositionComponent = tool.GetComponent<PositionComponent>();
            
            toolComponent.HoldingHand = hand;
                
            HandComponent handComponent = hand.GetComponent<HandComponent>();
            handComponent.TargetPosition = toolPositionComponent.Position;
            handComponent.TargetDepth = toolPositionComponent.Depth;
            handComponent.HeldTool = tool;

            SpriteComponent spriteComponent = hand.GetComponent<SpriteComponent>();
            spriteComponent.Texture = _handGrabTexture;
        }

        private void ReleaseTool(Entity tool) {
            ToolComponent toolComponent = tool.GetComponent<ToolComponent>();

            HandComponent handComponent = toolComponent.HoldingHand.GetComponent<HandComponent>();
            handComponent.HeldTool = null;

            SpriteComponent spriteComponent = toolComponent.HoldingHand.GetComponent<SpriteComponent>();
            spriteComponent.Texture = _handOpenTexture;

            _hand = toolComponent.HoldingHand;
            _object = null;
            toolComponent.HoldingHand = null;
        }

        private Vector2 GetRandomEntrance() {
            float x = _ground.Left + (float)_random.NextDouble() * 176f;
            if (_random.Next(2) == 0) {
                x = _ground.Right - (float)_random.NextDouble() * 176f;
            }

            return new Vector2(x, _ground.Bottom + 8f + (float)_random.NextDouble() * 16f);
        }

        public void Update(GameTime gameTime) {
            _timer += (float)gameTime.ElapsedGameTime.TotalSeconds;

            _entityWorld.Update(gameTime.ElapsedGameTime.Ticks);

            MouseState mouseState = Mouse.GetState();
            if (mouseState.LeftButton == ButtonState.Pressed) {
                Vector2 mousePosition = mouseState.Position.ToVector2();

                if (_hand == null) {
                    float closestDistance = float.MaxValue;
                    Entity closestObject = null;
                    foreach (Entity obj in _entityWorld.EntityManager.GetEntities(Aspect.One(typeof(ObjectComponent), typeof(ToolComponent)))) {
                        PositionComponent positionComponent = obj.GetComponent<PositionComponent>();
                        if (positionComponent.Depth < 0f) {
                            continue;
                        }

                        float distance = (positionComponent.Position - mousePosition).LengthSquared();
                        if (distance < closestDistance) {
                            closestDistance = distance;
                            closestObject = obj;
                        }
                    }

                    closestDistance = (float)Math.Sqrt(closestDistance);
                    _object = closestObject;

                    if (_object.HasComponent<ObjectComponent>()) {
                        ObjectComponent objectComponent = _object.GetComponent<ObjectComponent>();
                        if (closestObject != null && closestDistance < objectComponent.Radius + 10f) {
                            PositionComponent positionComponent = _object.GetComponent<PositionComponent>();

                            closestDistance = float.MaxValue;
                            Entity closestHand = null;
                            foreach (Entity hand in _entityWorld.EntityManager.GetEntities(Aspect.All(typeof(HandComponent)))) {
                                if (hand.GetComponent<HandComponent>().HeldTool != null) {
                                    continue;
                                }

                                float distance = (hand.GetComponent<PositionComponent>().Position - positionComponent.Position).LengthSquared();
                                if (distance < closestDistance) {
                                    closestDistance = distance;
                                    closestHand = hand;
                                }
                            }

                            _hand = closestHand;
                        }
                    }
                    else {
                        // Attach hand to tool. 
                        ToolComponent toolComponent = _object.GetComponent<ToolComponent>();

                        if (closestObject != null && closestDistance < toolComponent.Radius + 10f) {
                            if (toolComponent.HoldingHand != null) {
                                ReleaseTool(_object);
                            }
                            else {
                                PositionComponent positionComponent = _object.GetComponent<PositionComponent>();

                                closestDistance = float.MaxValue;
                                Entity closestHand = null;
                                foreach (Entity hand in _entityWorld.EntityManager.GetEntities(Aspect.All(typeof(HandComponent)))) {
                                    if (hand.GetComponent<HandComponent>().HeldTool != null) {
                                        continue;
                                    }

                                    float distance = (hand.GetComponent<PositionComponent>().Position - positionComponent.Position).LengthSquared();
                                    if (distance < closestDistance) {
                                        closestDistance = distance;
                                        closestHand = hand;
                                    }
                                }

                                _hand = closestHand;
                                if (closestHand != null) {
                                    GrabTool(_hand, closestObject);
                                }
                            }
                        }
                    }
                }

                if (_hand != null) {
                    HandComponent handComponent = _hand.GetComponent<HandComponent>();

                    if (handComponent.HeldTool == null) {
                        PositionComponent handPositionComponent = _hand.GetComponent<PositionComponent>();

                        if (handComponent.HeldItem == null && _object != null) {
                            PositionComponent positionComponent = _object.GetComponent<PositionComponent>();
                            if (positionComponent != null) {
                                handComponent.TargetPosition = positionComponent.Position;
                                handComponent.TargetDepth = 1f;

                                if (handPositionComponent.Depth <= handComponent.TargetDepth
                                    && (handPositionComponent.Position - handComponent.TargetPosition).Length() < 10f) {
                                    GrabItem(_hand, _object);
                                }
                            }
                        }

                        if (handComponent.HeldItem != null) {
                            handComponent.TargetPosition = mousePosition;
                            handComponent.TargetDepth = 50f;
                        }
                    }
                }
            }
            else {
                DropItem(_hand);

                _hand = null;
                _object = null;
            }

            if (_coalPeriod > 0.65f) {
                _coalPeriod -= (float)gameTime.ElapsedGameTime.TotalSeconds * 0.03f;
                _coalPeriod = Math.Max(_coalPeriod, 0.65f);
            }

            _coalTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;
            while (_coalTimer > _coalPeriod) {
                _coalTimer -= _coalPeriod;

                CreateCoal(GetRandomEntrance());
            }

            if (_minionTimer > 0f) {
                _minionTimer -= (float)gameTime.ElapsedGameTime.TotalSeconds;
            }
            else if (_incomingMinions > 0) {
                Vector2 position = new Vector2(_ground.Left - 32f, _furnace.Region.Bottom + 4f + (float)_random.NextDouble() * 64f);
                if (_random.Next(2) == 0) {
                    position.X = _ground.Right + 32f;
                }

                Entity minion = CreateMinion(position);

                _popSound.Play();

                _incomingMinions--;
                _minionTimer = 0.1f + (float)_random.NextDouble() * 0.4f;
            }

            Bag<Entity> entities = _entityWorld.EntityManager.GetEntities(Aspect.All(typeof(ObjectComponent)));
            _count = entities.Count;
            if (entities.Count > 60) {
                Death("_over");
                return;
            }
            else if (entities.Count > 55) {
                _finalWarningDangerSound.Play();
                _warningDangerSound.Pause();
            }
            else if (entities.Count > 45) {
                _warningDangerSound.Play();
                _finalWarningDangerSound.Pause();
            }
            else {
                _finalWarningDangerSound.Pause();
                _warningDangerSound.Pause();
            }

            foreach (Entity entity in entities) {
                PositionComponent positionComponent = entity.GetComponent<PositionComponent>();

                if (positionComponent.Depth < -50f) {
                    ObjectComponent objectComponent = entity.GetComponent<ObjectComponent>();

                    entity.Delete();

                    switch (objectComponent.Type) {
                        case Item.Minion: {
                            _pffSound.Play();
                            break;
                        }
                        case Item.SoulPlant: {
                            _incomingMinions++;
                            _minionTimer = 0.1f + (float)_random.NextDouble() * 0.4f;
                            break;
                        }
                        case Item.GreenPlant:
                        case Item.RedPlant:
                        case Item.BluePlant:
                        case Item.GoldPlant: {
                            if (_herbQueue.Count > 0 && _herbQueue[0] == objectComponent.Type) {
                                _herbQueue.RemoveAt(0);

                                _clickSound.Play();

                                _orders++;
                            }
                            break;
                        }
                    }
                }
                else {
                    ObjectComponent objectComponent = entity.GetComponent<ObjectComponent>();

                    if (objectComponent.TransformType != Item.None && !objectComponent.IsHeld) {
                        bool growing = false;

                        if (objectComponent.TransformType == Item.SoulPlant 
                            || objectComponent.TransformType == Item.GreenPlant
                            || objectComponent.TransformType == Item.GoldPlant) {
                            growing = _skylight.IsActive;
                        }

                        if (objectComponent.TransformType == Item.RedPlant) {
                            growing = _skylight.IsActive && _musicBox.IsActive;
                        }

                        if (objectComponent.TransformType == Item.BluePlant) {
                            growing = !_skylight.IsActive;
                        }

                        if (growing) {
                            objectComponent.TransformTimer -= (float)gameTime.ElapsedGameTime.TotalSeconds;
                        }

                        if (objectComponent.TransformTimer <= 0f) {
                            entity.Delete();

                            Item item = objectComponent.TransformType;
                            if (objectComponent.Type == Item.SoulSapling) {
                                int count = 0;
                                foreach (Entity other in _entityWorld.EntityManager.GetEntities(Aspect.All(typeof(ObjectComponent), typeof(PositionComponent)))) {
                                    Vector2 position = other.GetComponent<PositionComponent>().Position;
                                    if ((position - positionComponent.Position).LengthSquared() < 64f * 64f) {
                                        count++;
                                    }
                                }

                                if (count > 10) {
                                    item = Item.MadPlant;
                                }
                                else if (count > 5) {
                                    if (_random.Next(3) == 0) {
                                        item = Item.MadPlant;
                                    }
                                } 
                                else {
                                    if (_random.Next(7) == 0) {
                                        item = Item.MadPlant;
                                    }
                                }
                            }
                            Create(item, positionComponent.Position);
                        }
                    }

                    if (objectComponent.SpreadType != Item.None && !objectComponent.IsHeld) {
                        objectComponent.SpreadTimer -= (float)gameTime.ElapsedGameTime.TotalSeconds;
                        if (objectComponent.SpreadTimer <= 0f) {
                            objectComponent.SpreadTimer = 3f;
                            Create(objectComponent.SpreadType, positionComponent.Position + new Vector2((float)_random.NextDouble(), (float)_random.NextDouble()) * 10f);
                        }
                    }

                    if (objectComponent.Type == Item.SoulSeed) {
                        if (!objectComponent.IsHeld && positionComponent.Depth == 0f) {
                            entity.Delete();
                            CreateSoulSapling(positionComponent.Position);
                        }
                    }
                    else if (objectComponent.Type == Item.RedSeed) {
                        if (!objectComponent.IsHeld && positionComponent.Depth == 0f) {
                            entity.Delete();
                            CreateRedSapling(positionComponent.Position);
                        }
                    }
                    else if (objectComponent.Type == Item.GreenSeed) {
                        if (!objectComponent.IsHeld && positionComponent.Depth == 0f) {
                            entity.Delete();
                            CreateGreenSapling(positionComponent.Position);
                        }
                    }
                    else if (objectComponent.Type == Item.BlueSeed) {
                        if (!objectComponent.IsHeld && positionComponent.Depth == 0f) {
                            entity.Delete();
                            CreateBlueSapling(positionComponent.Position);
                        }
                    }
                }
            }

            if (_bellows.IsActive) {
                _flamePower += 15f * (float)gameTime.ElapsedGameTime.TotalSeconds;
                _flamePower = Math.Min(_flamePower, 100f);
            }
            else {
                _flamePower -= 10f * (float)gameTime.ElapsedGameTime.TotalSeconds;
                _flamePower = Math.Max(_flamePower, 0f);
            }

            if (_flamePower < 100f) {
                _bellowsDangerSound.Play();
                _bellowsDangerSound.Volume = 1f - (_flamePower / 100f);
            }
            else {
                _bellowsDangerSound.Pause();
            }

            if (_flamePower <= 0f) {
                Death("_temp");
                return;
            }

            _requestTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (_requestTimer >= _requestInterval) {
                _requestTimer -= _requestInterval;

                Item item = Item.None;
                if (_random.Next(6) == 0) {
                    item = Item.GoldPlant;
                }
                else {
                    switch (_random.Next(3)) {
                        case 0: {
                            item = Item.GreenPlant;
                            break;
                        }
                        case 1: {
                            item = Item.RedPlant;
                            break;
                        }
                        case 2: {
                            item = Item.BluePlant;
                            break;
                        }
                    }
                }
                _herbQueue.Add(item);

                _paperSound.Play();

                _lastHerbX = _game.GraphicsDevice.Viewport.Width + 8f;

                if (_requestInterval > 4f) {
                    _requestInterval -= 0.75f;
                }
            }

            if (_herbQueue.Count > 11) {
                Death("_order");
            }

            if (_herbQueue.Count > 0) {
                float right = 8f + (48f + 8f) * (_herbQueue.Count - 1);
                _lastHerbX += (right - _lastHerbX) * 20f * (float)gameTime.ElapsedGameTime.TotalSeconds;
            }

            if (_furnace.IsOpen) {
                _furnaceAnimation += (float)gameTime.ElapsedGameTime.TotalSeconds;
                _furnaceAnimation = Math.Min(_furnaceAnimation, _furnaceAnimationDuration);
            }
            else {
                _furnaceAnimation -= (float)gameTime.ElapsedGameTime.TotalSeconds;
                _furnaceAnimation = Math.Max(_furnaceAnimation, 0f);
            }

            if (_skylight.IsActive) {
                _skylightAnimation += (float)gameTime.ElapsedGameTime.TotalSeconds;
                _skylightAnimation = Math.Min(_skylightAnimation, _skylightAnimationDuration);
            }
            else {
                _skylightAnimation -= (float)gameTime.ElapsedGameTime.TotalSeconds;
                _skylightAnimation = Math.Max(_skylightAnimation, 0f);
            }

            if (_furnace.IsActive) {
                _furnaceWheelSound.Play();
            }
            else {
                _furnaceWheelSound.Pause();
            }

            if (_bellows.IsActive) {
                _bellowsWheelSound.Play();
            }
            else {
                _bellowsWheelSound.Pause();
            }

            if (_musicBox.IsActive) {
                _musicBoxSound.Resume();

                if (_musicBoxSound.Pitch < 0f) {
                    _musicBoxSound.Pitch = Math.Min(_musicBoxSound.Pitch + 20f * (float)gameTime.ElapsedGameTime.TotalSeconds, 0f);
                }
            }
            else {
                if (_musicBoxSound.Pitch > -0.99f) {
                    _musicBoxSound.Pitch = Math.Max(_musicBoxSound.Pitch - 20f * (float)gameTime.ElapsedGameTime.TotalSeconds, -0.99f);
                }
                else {
                    _musicBoxSound.Pause();
                }
            }
        }

        private void Death(string extra) {
            int score = 1 + (int)(_timer / 30f) + (_orders / 2);

            _game.Screen = new MenuScreen(_game, extra, score);

            _musicBoxSound.Stop();
            _bellowsWheelSound.Stop();
            _furnaceWheelSound.Stop();
            _bellowsDangerSound.Stop();
            _warningDangerSound.Stop();
            _finalWarningDangerSound.Stop();

            _failSound.Play();
        }

        public void Draw(GameTime gameTime) {
            _spriteBatch.Begin(sortMode: SpriteSortMode.FrontToBack, samplerState: SamplerState.PointClamp);

            Vector2 center = _game.GraphicsDevice.Viewport.Bounds.Center.ToVector2();

            Rectangle sourceRectangle = new Rectangle(0, 0, _gateTexture.Width / 4, _gateTexture.Height);
            sourceRectangle.Offset(_gateTexture.Width / 4f * (int)(3.99f * _furnaceAnimation / _furnaceAnimationDuration), 0f);

            _spriteBatch.Draw(_gateTexture, _ground.Location.ToVector2(), null, sourceRectangle, layerDepth: Layers.Ground);
            _spriteBatch.Draw(_gateTexture, _ground.Location.ToVector2() + new Vector2(_ground.Width - _gateTexture.Width / 4f, 0f), null, sourceRectangle, layerDepth: Layers.Ground);

            _spriteBatch.Draw(_groundTexture, center + new Vector2(0f, 36f), origin: _groundTexture.Bounds.Center.ToVector2(), layerDepth: Layers.Ground);

            _entityWorld.Draw();

            _spriteBatch.Draw(_boxTexture, new Vector2(center.X, 154f), origin: _boxTexture.Bounds.Center.ToVector2(), layerDepth: Layers.AboveGround);

            _spriteBatch.Draw(_borderTexture, Vector2.Zero, layerDepth: Layers.AboveGround - 0.01f);

            sourceRectangle = new Rectangle(0, 0, _shadowTexture.Width / 4, _shadowTexture.Height);
            sourceRectangle.Offset(_shadowTexture.Width / 4f * (int)(3.99f * _skylightAnimation / _skylightAnimationDuration), 0f);

            _spriteBatch.Draw(_shadowTexture, Vector2.Zero, sourceRectangle: sourceRectangle, layerDepth: Layers.Shadow);

            if (_flamePower < 100f) {
                _spriteBatch.Draw(_tempFillTexture, new Vector2(_game.GraphicsDevice.Viewport.Width - 32f, 160f + _tempFillTexture.Height / 2f), 
                    origin: new Vector2(_tempFillTexture.Width / 2f, _tempFillTexture.Height), scale: new Vector2(1f, _flamePower / 100f), layerDepth: Layers.UI - 0.001f);
                _spriteBatch.Draw(_tempBorderTexture, new Vector2(_game.GraphicsDevice.Viewport.Width - 32f, 160f),
                    origin: new Vector2(_tempFillTexture.Width, _tempFillTexture.Height) / 2f, layerDepth: Layers.UI);
            }

            float p = 1f - _count / 60f;
            _spriteBatch.Draw(_tempFillTexture, new Vector2(56f, _game.GraphicsDevice.Viewport.Height - 48f + _tempFillTexture.Height / 2f),
                origin: new Vector2(_tempFillTexture.Width / 2f, _tempFillTexture.Height), scale: new Vector2(1f, p), layerDepth: Layers.UI - 0.001f);
            _spriteBatch.Draw(_tempBorderTexture, new Vector2(56f, _game.GraphicsDevice.Viewport.Height - 48f),
                origin: new Vector2(_tempFillTexture.Width, _tempFillTexture.Height) / 2f, layerDepth: Layers.UI);

            for (int i = 0; i < _herbQueue.Count; i++) {
                Texture2D texture = null;
                switch (_herbQueue[i]) {
                    case Item.GreenPlant: {
                        texture = _greenPlantTexture;
                        break;
                    }
                    case Item.RedPlant: {
                        texture = _redPlantTexture;
                        break;
                    }
                    case Item.BluePlant: {
                        texture = _bluePlantTexture;
                        break;
                    }
                    case Item.GoldPlant: {
                        texture = _goldPlantTexture;
                        break;
                    }
                }

                float x = 8f + (48f + 8f) * i;

                if (i == _herbQueue.Count - 1) {
                    x = _lastHerbX;
                }

                _spriteBatch.Draw(_paperTexture, new Vector2(x - 2f, 6f), layerDepth: Layers.UI - 0.01f);
                _spriteBatch.Draw(texture, new Vector2(x, 8f), sourceRectangle: new Rectangle(96, 0, 48, 48), layerDepth: Layers.UI);
            }

            _spriteBatch.End();
        }
    }
}
