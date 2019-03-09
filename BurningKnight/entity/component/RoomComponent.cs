﻿using BurningKnight.entity.level.rooms;
using Lens.entity.component;

namespace BurningKnight.entity.component {
	public class RoomComponent : Component {
		public Room Room;

		public override void Init() {
			base.Init();
			FindRoom();

			Entity.PositionChanged += FindRoom;
		}

		public override void Update(float dt) {
			base.Update(dt);

			if (Room == null) {
				FindRoom();
			}
		}

		public void FindRoom() {
			foreach (var room in Entity.Area.Tags[Tags.Room]) {
				if (room.Contains(Entity.Center)) {
					Room = (Room) room;
					break;
				}
			}
		}
	}
}