﻿using System;
using BurningKnight.entity.component;
using BurningKnight.entity.events;
using Lens.entity;
using Lens.entity.component.logic;
using Lens.graphics.animation;
using Lens.util.camera;
using VelcroPhysics.Dynamics;

namespace BurningKnight.entity.door {
	public class Lock : Entity {
		public bool IsLocked { get; protected set; }
		public bool Move;
		private float t;
		private float shake;
		
		public Lock() {
			Width = 10;
			Height = 15;
			IsLocked = true;
		}
		
		protected virtual bool Interact(Entity entity) {
			if (TryToConsumeKey(entity)) {
				GetComponent<StateComponent>().Become<OpeningState>();
				IsLocked = false;

				HandleEvent(new LockOpenedEvent {
					Who = entity,
					Lock = this
				});
				
				return true;
			}

			Camera.Instance.Shake(3);
			shake = 1f;
			
			return false;
		}

		protected virtual bool TryToConsumeKey(Entity entity) {
			return false;
		}
		
		public override void AddComponents() {
			base.AddComponents();

			AddComponent(new InteractableComponent(Interact));
			AddComponent(new RectBodyComponent(-1, 2, 10, 11, BodyType.Static, true));
							
			var state = new StateComponent();
			AddComponent(state);

			state.Become<IdleState>();
		}

		public override void Update(float dt) {
			base.Update(dt);

			if (GraphicsComponent == null) {
				// Set here, because of the ui thread
				SetGraphicsComponent(new AnimationComponent("lock", GetLockPalette()));
			}

			if (!IsLocked) {
				return;
			}
			
			var offset = GetComponent<AnimationComponent>().Offset;

			if (Move) {
				t += dt;
				offset.Y = (float) (Math.Cos(t * 3f) * 1.5f);
			}

			if (shake > 0) {
				shake -= dt;
			} else {
				shake = 0;
			}
							
			offset.X = (float) (Math.Cos(shake * 20f) * shake * 2.5f);
			GetComponent<AnimationComponent>().Offset = offset;
		}

		public override void Render() {
			if (!(GetComponent<StateComponent>().StateInstance is OpenState)) {
				base.Render();
			}
		}

		protected virtual bool Disposable() {
			return true;
		}

		protected virtual ColorSet GetLockPalette() {
			return null;
		}
		
		#region Lock States
		public class IdleState : EntityState {
			
		}
		
		public class OpenState : EntityState {
			
		}
		
		public class OpeningState : EntityState {
			public override void Init() {
				base.Init();
				Self.GetComponent<AnimationComponent>().SetAutoStop(true);
			}

			public override void Update(float dt) {
				base.Update(dt);

				if (Self.GetComponent<AnimationComponent>().Animation.Paused) {
					if (((Lock) Self).Disposable()) {
						Self.Done = true;
					} else {
						Become<OpenState>();
					}
				}
			}

			public override void Destroy() {
				base.Destroy();
				Self.GetComponent<AnimationComponent>().SetAutoStop(false);
			}
		}

		public class ClosingState : EntityState {
			public override void Init() {
				base.Init();
				Self.GetComponent<AnimationComponent>().SetAutoStop(true);
			}

			public override void Update(float dt) {
				base.Update(dt);

				if (Self.GetComponent<AnimationComponent>().Animation.Paused) {
					Become<IdleState>();
				}
			}

			public override void Destroy() {
				base.Destroy();
				Self.GetComponent<AnimationComponent>().SetAutoStop(false);
			}
		}
		#endregion
	}
}