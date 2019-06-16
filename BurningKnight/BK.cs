﻿using BurningKnight.assets;
using BurningKnight.assets.items;
using BurningKnight.assets.lighting;
using BurningKnight.assets.prefabs;
using BurningKnight.state;
using BurningKnight.util;
using Lens;
using Microsoft.Xna.Framework;

namespace BurningKnight {
	public class BK : Engine {
		// "v0.0.0.5 (0)"
		public static Version Version = new Version(0, 0, 0, 0, 5, Debug, true);
		
		public BK(int width, int height, bool fullscreen) : base(Version, new LoadState(), $"Burning Knight: {Titles.Generate()}", width, height, fullscreen) {
			
		}

		protected override void Initialize() {
			base.Initialize();
			
			Controls.Bind();
			Dialogs.Load();
			CommonAse.Load();
			ImGuiHelper.Init();
			Shaders.Load();
			Font.Load();
			Prefabs.Load();
			Items.Load();
			Mods.Load();
		}

		protected override void UnloadContent() {
			Mods.Destroy();
			Items.Destroy();
			Prefabs.Destroy();
			Lights.DestroySurface();
			
			base.UnloadContent();
		}

		protected override void Update(GameTime gameTime) {
			base.Update(gameTime);
			Mods.Update((float) gameTime.ElapsedGameTime.TotalSeconds);
		}

		public override void RenderUi() {
			base.RenderUi();
			Mods.Render();
		}
	}
}