using System.Collections.Generic;
using BurningKnight.assets.lighting;
using BurningKnight.level.builders;
using BurningKnight.level.rooms;
using BurningKnight.level.rooms.regular;
using BurningKnight.level.rooms.trap;
using BurningKnight.level.tile;
using BurningKnight.state;
using Lens.graphics;
using Lens.util;
using Lens.util.math;
using Microsoft.Xna.Framework;

namespace BurningKnight.level.biome {
	public class JungleBiome : Biome {
		public JungleBiome() : base("Botanical Expedition", Biome.Jungle, "jungle_biome", new Color(30, 111, 80)) {}

		public override void Apply() {
			base.Apply();

			var v = 0.5f;
			Lights.ClearColor = new Color(v, v, v, 1f);
		}

		public override void ModifyRooms(List<RoomDef> rooms) {
			base.ModifyRooms(rooms);

			if (Run.Type == RunType.BossRush) {
				return;
			}
			
			rooms.Add(new HiveRoom());

			for (var i = 0; i < (Run.Depth % 2 == 0 ? 2 : 1); i++) {
				rooms.Add(new JungleRoom());
			}
		}

		public override void ModifyPainter(Level level, Painter painter) {
			base.ModifyPainter(level, painter);

			painter.Water = 0.4f;
			painter.Grass = 0.4f;
			painter.Dirt = 0f;
			painter.Cobweb = 0f;
			painter.Fireflies = 4;
			painter.FirefliesChance = 100;
			
			painter.Modifiers.Add((l, rm, x, y) => {
				if (rm is TrapRoom) {
					return;
				}
				
				var f = Tiles.RandomFloor();
				
				if (l.Get(x, y, true) == Tile.Lava || l.Get(x, y).Matches(Tile.Chasm, Tile.SpikeOffTmp, Tile.SpikeOnTmp, Tile.SensingSpikeTmp, Tile.FireTrapTmp)) {
					var i = l.ToIndex(x, y);
					
					l.Liquid[i] = 0;
					l.Tiles[i] = (byte) f;
				}
			});
		}

		public override int GetNumRegularRooms() {
			return (int) (base.GetNumRegularRooms() * 0.25f);
		}
		
		public override int GetNumTrapRooms() {
			return 0;
		}

		public override bool HasTorches() {
			return false;
		}

		public override bool SpawnAllMobs() {
			return true;
		}

		public override bool HasPaintings() {
			return false;
		}

		public override bool HasTnt() {
			return false;
		}

		public override bool HasCobwebs() {
			return false;
		}

		public override bool HasBrekables() {
			return false;
		}

		public override bool HasPlants() {
			return true;
		}

		public override bool HasTrees() {
			return true;
		}

		public override string GetStepSound(Tile tile) {
			if (tile == Tile.FloorA || tile == Tile.FloorC) {
				return $"player_step_sand_{Rnd.Int(1, 4)}";
			}
			
			return base.GetStepSound(tile);
		}
		
		private static Color mapColor = new Color(51, 152, 75);

		public override Color GetMapColor() {
			return mapColor;
		}
	}
}