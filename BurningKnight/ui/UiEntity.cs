﻿using System;
using BurningKnight.assets.input;
using Lens;
using Lens.assets;
using Lens.entity;
using Lens.input;
using Lens.util.tween;
using Microsoft.Xna.Framework;

namespace BurningKnight.ui {
	public class UiEntity : Entity {
		public UiEntity Super;
		public bool Clickable = true;

		public bool Enabled {
			set {
				Visible = value;
				Active = value;
			}
		}

		#region Relative Position
		public Vector2 RelativePosition;

		public float RelativeX {
			get => Centered ? RelativePosition.X - Width / 2 : RelativePosition.X;
			set => RelativePosition.X = Centered ? value + Width / 2 : value;
		}

		public float RelativeY {
			get => Centered ? RelativePosition.Y - Height / 2 : RelativePosition.Y;
			set => RelativePosition.Y = Centered ? value + Height / 2 : value;
		}

		public Vector2 RelativeCenter {
			get => new Vector2(RelativeCenterX, RelativeCenterY);
			set {
				RelativeX = value.X - Width / 2;
				RelativeY = value.Y - Height / 2;
			}
		}
		
		public float RelativeCenterX {
			get => RelativeX + Width / 2;
			set => RelativeX = value - Width / 2;
		}

		public float RelativeCenterY {
			get => RelativeY + Height / 2;
			set => RelativeY = value - Height / 2;
		}

		public float RelativeRight => RelativeX + Width;
		public float RelativeBottom => RelativeY + Height;
		#endregion
		
		protected bool hovered;
		protected float angle;
		protected internal float scale = 1f;
		protected Vector2 origin = new Vector2(0);

		public float AngleMod = 1f;
		public float ScaleMod = 1f;
		
		public override void Init() {
			base.Init();

			AlwaysActive = true;
			AlwaysVisible = true;
		}

		public virtual bool CheckCollision(Vector2 v) {
			return Contains(v);
		}
		
		public override void Update(float dt) {
			base.Update(dt);

			bool was = hovered;
			hovered = CheckCollision(Input.Mouse.UiPosition);

			if (hovered) {
				if (!was) {
					OnHover();
				}

				if (Input.WasPressed(Controls.UiAccept)) {
					OnClick();
				}
			} else if (!hovered && was) {
				OnUnhover();
			}

			angle = (float) Math.Cos(Engine.Time * 3f + Y / (Display.UiHeight * 0.5f) * Math.PI) * (scale - 0.9f) * AngleMod / Math.Max(1, Width * 0.04f);
		}
		
		public override void Render() {
			
		}

		public virtual void PlaySfx(string sfx) {
			if (Settings.UiSfx) {
				Audio.PlaySfx(sfx, 0.5f);
			}
		}
		
		protected virtual void OnHover() {
			if (Clickable) {
				Tween.To(1 + ScaleMod * 0.2f, scale, x => scale = x, 0.1f);
			}
		}

		protected virtual void OnUnhover() {
			if (Clickable) {
				Tween.To(1f, scale, x => scale = x, 0.1f);
			}
		}

		public virtual void OnClick() {
			if (Clickable) {
				Tween.To(1 - ScaleMod * 0.5f, scale, x => scale = x, 0.1f).OnEnd = () =>
					Tween.To(1f, scale, x => scale = x, 0.2f);
			}
		}
	}
}