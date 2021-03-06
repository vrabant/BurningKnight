using System;
using System.Linq;
using BurningKnight.entity.room;
using BurningKnight.entity.room.controllable.turret;
using BurningKnight.entity.room.input;
using BurningKnight.level.tile;
using BurningKnight.util.geometry;
using Lens.util;
using Lens.util.math;
using Microsoft.Xna.Framework;

namespace BurningKnight.level.rooms.trap {
	public class VerticalTurretPassageRoom : TrapRoom {
		public override void PaintFloor(Level level) {
			
		}

		public override void ModifyRoom(Room room) {
			base.ModifyRoom(room);
			room.AddController("bk:trap");
		}

		public override int GetMinHeight() {
			return 14;
		}

		public override int GetMaxHeight() {
			return 22;
		}

		public override void Paint(Level level) {
			base.Paint(level);
			Painter.Fill(level, this, 1, Tile.Chasm);
			Painter.DrawLine(level, new Dot(Left + 1, Top + 1), new Dot(Right - 1, Top + 1), Tiles.RandomFloor(), true);
			Painter.DrawLine(level, new Dot(Left + 1, Bottom - 1), new Dot(Right - 1, Bottom - 1), Tiles.RandomFloor(), true);

			var ty = Rnd.Int(Left + 2, Right - 2);

			if (Rnd.Chance()) {
				ty = Left + GetWidth() / 2;
			}

			for (var i = 0; i < 2; i++) {
				if (Rnd.Chance()) {
					var tty = Rnd.Int(Left + 2, Right - 2);
					Painter.DrawLine(level, new Dot(tty, Top + 1), new Dot(tty, Bottom - 1), Tile.FloorD);
				}
			}

			Painter.DrawLine(level, new Dot(ty, Top + 1), new Dot(ty, Bottom - 1), Tiles.RandomFloor(), true);

			if (Connected.Count == 1) {
				ty = Rnd.Int(Left + 2, Right - 2);

				if (Connected.Values.First().Y == Top) {
					PlaceButton(level, new Dot(ty, Bottom - 1));
				} else {
					PlaceButton(level, new Dot(ty, Top + 1));
				}
			}
			
			var s = Rnd.Int(2, 4);
			var xm = Rnd.Int(0, s);

			#region Wave Generator
			var xsmooth = Rnd.Chance(30);
			var xmod = Rnd.Float(s * 8, s * 16);
			var ysmooth = Rnd.Chance(30);
			var ymod = Rnd.Chance(30) ? xmod : Rnd.Float(s * 32, s * 64);

			var fn = new Func<int, int, float>((x, y) => {
				var t = 0f;

				if (!xsmooth) {
					t += (float) Math.Cos(x / xmod);
				}

				if (!ysmooth) {
					t += (float) Math.Sin(y / ymod);
				}
				
				return MathUtils.Mod(t * 3, 3);
			});
			#endregion
			
			for (var x = Top + 3 + xm; x < Bottom - 2; x += s) {
				Place(level, Left + 1, x, 0, fn(Left + 1, x));
			}
			
			for (var x = Top + 3 + xm; x < Bottom - 2; x += s) {
				Place(level, Right - 1, x, 4, fn(x, Right - 1));
			}
		}
		
		public override bool CanConnect(RoomDef R, Dot P) {
			if (P.Y == Bottom || P.Y == Top) {
				if (P.X == Left + 1 || P.X == Right - 1) {
					return false;
				}
			} else 	if (P.X == Left || P.X == Right) {
				return false;
			}

			return base.CanConnect(R, P);
		}

		private bool Place(Level level, int x, int y, uint a, float offset) {
			foreach (var d in Connected.Values) {
				if ((d.X == x && (d.Y == y + 1 || d.Y == y - 1)) || (d.Y == y && (d.X == x + 1 || d.X == x - 1))) {
					return false;
				}
			}
			
			Painter.Set(level, x, y, Tile.FloorA);

			level.Area.Add(new Turret {
				Position = new Vector2(x, y) * 16,
				StartingAngle = a,
				TimingOffset = offset,
				Speed = 1.5f
			});

			return true;
		}

		private void PlaceButton(Level level, Dot where) {
			Painter.Set(level, where, Tiles.RandomFloor());

			var input = new Button();
			input.Position = where * 16;
			level.Area.Add(input);
		}
	}
}