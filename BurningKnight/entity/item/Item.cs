using System;
using System.Linq;
using BurningKnight.assets.items;
using BurningKnight.assets.lighting;
using BurningKnight.assets.particle;
using BurningKnight.entity.component;
using BurningKnight.entity.creature;
using BurningKnight.entity.creature.player;
using BurningKnight.entity.events;
using BurningKnight.entity.item.renderer;
using BurningKnight.entity.item.stand;
using BurningKnight.entity.item.use;
using BurningKnight.entity.item.useCheck;
using BurningKnight.level;
using BurningKnight.level.rooms;
using BurningKnight.physics;
using BurningKnight.save;
using BurningKnight.state;
using BurningKnight.ui.editor;
using BurningKnight.util;
using ImGuiNET;
using Lens;
using Lens.assets;
using Lens.entity;
using Lens.graphics;
using Lens.util;
using Lens.util.file;
using Lens.util.math;
using Microsoft.Xna.Framework;
using VelcroPhysics.Dynamics;

namespace BurningKnight.entity.item {
	public class Item : SaveableEntity, CollisionFilterEntity, PlaceableEntity {
		public static TextureRegion UnknownRegion;
		public static bool Attact;
		
		public ItemType Type;
		public string Id;
		public string LastId;
		public string IdUnderScourge => Type != ItemType.Scourge && Scourge.IsEnabled(Scourge.OfEgg) ? Items.Datas.Values.ElementAt(Rnd.Int(Items.Datas.Count)).Id : Id;
		public string Name => Masked ? "???" : Locale.Get(IdUnderScourge);
		public string Description => Locale.Get($"{IdUnderScourge}_desc");
		public float UseTime = 0.3f;
		public float Delay;
		public string Animation;
		public bool AutoPickup;
		public bool LoadedSelf;
		public bool Used;
		public bool Touched;
		public bool Automatic;
		public bool SingleUse;
		public bool Scourged;
		public bool Hide;
		
		public ItemUse[] Uses;
		public ItemUseCheck UseCheck = ItemUseChecks.Default;
		public ItemRenderer Renderer;

		public bool Hidden => (Type != ItemType.Mana && Type != ItemType.Coin && Type != ItemType.Heart && Type != ItemType.Key && Type != ItemType.Bomb && (!TryGetComponent<OwnerComponent>(out var o) || !(o.Owner is Player)) && Scourge.IsEnabled(Scourge.OfUnknown));
		public TextureRegion Region => (Hidden) ? UnknownRegion : (Animation != null ? GetComponent<AnimatedItemGraphicsComponent>().Animation.GetCurrentTexture() : GetComponent<ItemGraphicsComponent>().Sprite);
		
		public Entity Owner => TryGetComponent<OwnerComponent>(out var o) ? o.Owner : null;
		public ItemData Data => Items.Datas[Id];

		private bool updateLight;
		private float t;

		public override void Init() {
			base.Init();
			
			if (Run.Depth > 0 && (Type != ItemType.Scourge && Type != ItemType.Coin && Type != ItemType.Key && Type != ItemType.Bomb && Type != ItemType.Heart) 
			  && Rnd.Chance(Scourge.IsEnabled(Scourge.OfScourged) ? 0.66f : (Run.Scourge * 10 + 0.5f))) {
				
				Scourged = true;
			}
		}

		public override void Destroy() {
			base.Destroy();

			if (Uses == null) {
				return;
			}

			foreach (var u in Uses) {
				u.Destroy();
			}
		}

		public bool Use(Entity entity, bool avoidCheck = false) {
			if (!avoidCheck && (Type == ItemType.Weapon || Type == ItemType.Active) && !UseCheck.CanUse(entity, this)) {
				return false;
			}

			foreach (var use in Uses) {
				if (use.SingleUse && Used) {
					continue;
				}
				
				try {
					use.Use(entity, this);
				} catch (Exception e) {
					Log.Error(e);
				}
			}

			Delay = Math.Abs(UseTime);

			entity.HandleEvent(new ItemUsedEvent {
				Item = this,
				Who = entity,
				Fake = avoidCheck
			});

			Used = true;
			Renderer?.OnUse();

			if (Type == ItemType.Active) {
				((Player) entity).AnimateItemPickup(this, null, false, false);
			}

			return true;
		}
		
		public void Pickup() {
			var entity = Owner;
		
			foreach (var use in Uses) {
				try {
					use.Pickup(entity, this);
				} catch (Exception e) {
					Log.Error(e);
				}
			}
		}
		
		public void Drop() {
			var entity = Owner;
		
			foreach (var use in Uses) {
				try {
					use.Drop(entity, this);
				} catch (Exception e) {
					Log.Error(e);
				}
			}
		}
		
		public void TakeOut() {
			var entity = Owner;
		
			foreach (var use in Uses) {
				try {
					use.TakeOut(entity, this);
				} catch (Exception e) {
					Log.Error(e);
				}
			}
		}
		
		public void PutAway() {
			var entity = Owner;
		
			foreach (var use in Uses) {
				try {
					use.PutAway(entity, this);
				} catch (Exception e) {
					Log.Error(e);
				}
			}
		}

		public override void PostInit() {
			base.PostInit();

			if (Animation != null) {
				AddComponent(new AnimatedItemGraphicsComponent(Animation));
			} else {
				AddComponent(new ItemGraphicsComponent(Id));
			}
			
			if (LoadedSelf) {
				AddDroppedComponents();
			}
			
			CheckMasked();

			if (Id.Contains("mana")) {
				AlwaysActive = true;
			}
		}

		private bool Interact(Entity entity) {
			if (Masked && Run.Depth < 1) {
				return false;
			}
			
			if (entity.TryGetComponent<InventoryComponent>(out var inventory)) {
				inventory.Pickup(this);
				return true;
			}

			return false;
		}
		
		protected virtual bool ShouldInteract(Entity entity) {
			return !(entity is Player c && ((Type == ItemType.Mana && (!c.GetComponent<ManaComponent>().CanPickup(this) || t < 1f)) ||
			                                (Type == ItemType.Heart && !c.GetComponent<HealthComponent>().CanPickup(this)) ||
			                                (Type == ItemType.Battery && c.GetComponent<ActiveItemComponent>().IsFullOrEmpty()) ||
			                                (Type == ItemType.Coin && Id != "bk:emerald" && c.GetComponent<ConsumablesComponent>().Coins >= c.GetComponent<ConsumablesComponent>().MaxCoins) ||
			                                (Type == ItemType.Bomb && c.GetComponent<ConsumablesComponent>().Bombs == 99 && c.TryGetComponent<HeartsComponent>(out var b) && b.BombsMax > b.Bombs) ||
			                                (Type == ItemType.Key && c.GetComponent<ConsumablesComponent>().Keys == 99)
			         ));
		}

		public void OnInteractionStart(Entity entity) {
			if (!Scourged && AutoPickup && entity.TryGetComponent<InventoryComponent>(out var inventory)) {
				if (ShouldInteract(entity)) {
					inventory.Pickup(this);
					entity.GetComponent<InteractorComponent>().EndInteraction();	
				}
			} else if (!HasComponent<OwnerComponent>() && Run.Depth != -2) {
				Engine.Instance.State.Ui.Add(new ItemPickupFx(this));
			}			
		}

		protected virtual BodyComponent CreateBody() {
			var slice = Region;
			return new RectBodyComponent(0, 0, slice.Source.Width, slice.Source.Height);
		}
		
		public virtual void AddDroppedComponents() {
			var body = CreateBody();

			t = 0;
			
			AddComponent(body);

			body.Body.LinearDamping = Type == ItemType.Mana ? 1 : 4;
			body.Body.Friction = 0;
			body.Body.Mass = 0.1f;
			
			AddComponent(new InteractableComponent(Interact) {
				OnStart = OnInteractionStart,
				CanInteract = ShouldInteract
			});
			
			AddComponent(new ShadowComponent(RenderShadow));
			
			AddTag(Tags.LevelSave);
			AddTag(Tags.Item);
			
			AddComponent(new RoomComponent());
			AddComponent(new ExplodableComponent());
			AddComponent(new SupportableComponent());

			CheckMasked();
		}

		public void RandomizeVelocity(float force) {
			var component = GetBody();
			
			if (component == null) {
				return;
			}

			force *= 60f;
			var angle = Rnd.AnglePI();
			
			component.Velocity += new Vector2((float) Math.Cos(angle) * force, (float) Math.Sin(angle) * force);
		}

		protected virtual void RemoveBody() {
			RemoveComponent<RectBodyComponent>();
		}
		
		public virtual void RemoveDroppedComponents() {
			RemoveComponent<InteractableComponent>();
			RemoveComponent<ShadowComponent>();
			RemoveComponent<LightComponent>();
			RemoveBody();
			
			RemoveTag(Tags.LevelSave);
			RemoveTag(Tags.Item);

			RemoveComponent<RoomComponent>();
			RemoveComponent<ExplodableComponent>();
			RemoveComponent<SupportableComponent>();

			CheckMasked();
		}

		private void RenderShadow() {
			GraphicsComponent.Render(true);
		}

		public override void Save(FileWriter stream) {
			base.Save(stream);
			
			stream.WriteString(Id);
			stream.WriteBoolean(Used);
			stream.WriteBoolean(Touched);
			stream.WriteFloat(Delay);
			stream.WriteBoolean(Unknown);
			stream.WriteBoolean(Scourged);
			stream.WriteBoolean(Hide);
		}

		public void ConvertTo(string id) {
			var item = Items.Create(id);

			if (item == null) {
				Log.Error($"Failed to convert item {Id}, such id does not exist!");
				return;
			}

			if (HasComponent<AnimatedItemGraphicsComponent>()) {
				RemoveComponent<AnimatedItemGraphicsComponent>();
			} else if (HasComponent<ItemGraphicsComponent>()) {
				RemoveComponent<ItemGraphicsComponent>();
			}

			Uses = Items.ParseUses(Items.Datas[id].Uses);
			
			foreach (var u in Uses) {
				u.Item = this;
				u.Init();
			}
			
			UseTime = item.UseTime;
			Renderer = item.Renderer;
			Animation = item.Animation;
			AutoPickup = item.AutoPickup;
			Automatic = item.Automatic;
			SingleUse = item.SingleUse;
			Type = item.Type;
			Id = id;
			Used = false;
			Scourged = Scourged || item.Scourged;
			
			if (Renderer != null) {
				Renderer.Item = this;
			}
			
			if (Animation != null) {
				AddComponent(new AnimatedItemGraphicsComponent(Animation));
			} else {
				AddComponent(new ItemGraphicsComponent(Id));
			}
			
			if (HasBody()) {
				RemoveDroppedComponents();
				AddDroppedComponents();
			}
		}

		protected virtual bool HasBody() {
			return HasComponent<RectBodyComponent>();
		}

		public override void Load(FileReader stream) {
			base.Load(stream);

			try {
				LoadedSelf = true;
				Id = stream.ReadString();
				Scourged = false;

				if (!Items.Has(Id)) {
					Id = "bk:revolver";
				}

				ConvertTo(Id);
				
				Used = stream.ReadBoolean();
				Touched = stream.ReadBoolean();
				Delay = stream.ReadFloat();
				Unknown = stream.ReadBoolean();

				var v = stream.ReadBoolean();
				Scourged = Scourged || v;

				Hide = stream.ReadBoolean();
			} catch (Exception e) {
				Log.Error(e);
			}
		}

		private float lastParticle;
		
		public override void Update(float dt) {
			base.Update(dt);

			t += dt;

			if (Type != ItemType.Active || UseTime < 0) {
				var s = dt;
				
				if (Type == ItemType.Weapon && Owner != null && Owner.TryGetComponent<StatsComponent>(out var stats)) {
					s *= stats.FireRate;

					if (Data.WeaponType == WeaponType.Ranged) {
						s *= stats.RangedRate;
					}
				}
				
				Delay = Math.Max(0, Delay - s);
			}

			var hasOwner = HasComponent<OwnerComponent>();
			
			if (Scourged) {
				lastParticle -= dt;

				if (lastParticle <= 0) {
					lastParticle = Rnd.Float(0.05f, 0.3f);

					for (var i = 0; i < Rnd.Int(0, 3); i++) {
						var part = new ParticleEntity(Particles.Scourge());

						part.Position = (hasOwner ? GetComponent<OwnerComponent>().Owner.Center : Center) + Rnd.Vector(-4, 4);
						part.Particle.Scale = Rnd.Float(0.5f, 1.2f);
						Area.Add(part);
						part.Depth = hasOwner ? 1 : -1;
					}
				}
			}


			if (hasOwner) {
				var o = Owner;

				foreach (var u in Uses) {
					u.Update(o, this, dt);
				}
			} else {
				if (Attact) {
					var room = GetComponent<RoomComponent>().Room;

					if (room.Tagged[Tags.Player].Count > 0) {
						var force = 360 * dt;
						var a = AngleTo(room.Tagged[Tags.Player][0]);
						GetBody().Velocity += new Vector2((float) Math.Cos(a) * force, (float) Math.Sin(a) * force);
					}
				}
				
				if (updateLight) {
					updateLight = false;
					var room = GetComponent<RoomComponent>().Room;

					if (room == null || HasComponent<LightComponent>()) {
						if (room == null || room.Type == RoomType.Secret) {
							RemoveComponent<LightComponent>();
						}
					} else if (room.Type != RoomType.Secret) {
						if (Type == ItemType.Mana || Type == ItemType.Coin || Type == ItemType.Heart || Type == ItemType.Battery ||
						    Type == ItemType.Key) {
							Color color;

							if (Type == ItemType.Coin || Type == ItemType.Key) {
								color = new Color(1f, 1f, 0.5f, 1f);
							} else if (Type == ItemType.Heart) {
								color = new Color(1f, 0.2f, 0.2f, 1f);
							} else if (Type == ItemType.Mana) {
								color = new Color(0.2f, 1f, 0.2f, 1f);
							} else {
								color = new Color(1f, 1f, 1f, 1f);
							}

							AddComponent(new LightComponent(this, 32f, color));
						}
					}
				}
			}

			if (Type == ItemType.Mana && t >= 0.1f) {
				var p = LocalPlayer.Locate(Area);

				if (p == null) {
					return;
				}

				if (p.GetComponent<ManaComponent>().IsFull() || t < 1f) {
					return;
				}

				var room = GetComponent<RoomComponent>().Room;
				var limitRange = room != null && room.Tagged[Tags.MustBeKilled].Count > 0;
				var d = DistanceTo(p);

				if (d < 4) {
					return;
				}
				
				if (limitRange && d > 64) {
					return;
				}

				var b = GetBody().Body;
				var dx = DxTo(p);
				var dy = DyTo(p);
				var s = dt * 4;

				b.LinearVelocity -= new Vector2(dx / d * s, dy / d * s);

				var a = b.LinearVelocity.ToAngle(); 
				d = Math.Min(b.LinearVelocity.Length() + dt * 300, 1000);
				a = (float) MathUtils.LerpAngle(a, AngleTo(p), dt * 10);
				
				b.LinearVelocity = new Vector2((float) Math.Cos(a) * d, (float) Math.Sin(a) * d);
			}
		}

		protected virtual BodyComponent GetBody() {
			return TryGetComponent<RectBodyComponent>(out var b) ? b : null;
		}

		public virtual bool ShouldCollide(Entity entity) {
			if (Type == ItemType.Mana) {
				if (entity is ProjectileLevelBody || entity is HalfProjectileLevel || entity is Chasm) {
					return false;
				}
				
				var room = GetComponent<RoomComponent>().Room;

				if (room == null || room.Tagged[Tags.MustBeKilled].Count == 0) {
					return false;
				}
			}
			
			return !(entity is Creature) || !ShouldInteract(entity);
		}

		public override bool HandleEvent(Event e) {
			if (e is LostSupportEvent) {
				Done = true;
				return true;
			} else if (e is RoomChangedEvent rce && !HasComponent<OwnerComponent>()) {
				updateLight = true;
			}
			
			return base.HandleEvent(e);
		}
		
		public bool Masked { get; protected set; }

		private bool unknown;

		public bool Unknown {
			get => unknown;

			set {
				unknown = value;
				CheckMasked();
			}
		}

		public void CheckMasked() {
			Masked = Unknown || 
			         (Run.Depth == 0 
			          && Data.Lockable 
			          && Type != ItemType.Lamp
			          && (Data.UnlockPrice == 0 || (TryGetComponent<OwnerComponent>(out var o) && o.Owner is PermanentStand)) 
			          && !Unlocked(Id));
		}

		public static bool Unlocked(string id) {
			return GlobalSave.IsTrue(id) || id == "bk:sword" || id == "bk:no_lamp" || id == "bk:revolver" || id == "bk:no_hat";
		}

		public bool HandleOwnerEvent(Event e) {
			if (Uses == null) {
				return false;
			}
			
			foreach (var use in Uses) {
				if (use.HandleEvent(e)) {
					return true;
				}
			}

			return false;
		}

		#if DEBUG
		private string debugItem = "";
		
		public override void RenderImDebug() {
			if (ImGui.InputText("Item", ref debugItem, 128, ImGuiInputTextFlags.EnterReturnsTrue)) {
				ConvertTo(debugItem);
			}

			ImGui.Checkbox("Scourged", ref Scourged);
			ImGui.Checkbox("Touched", ref Touched);
		}
		#endif

		public class UnlockedEvent : Event {
			public ItemData Data;
		}
	}
}