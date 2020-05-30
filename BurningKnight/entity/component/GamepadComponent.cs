using System;
using Lens.entity.component;
using Lens.entity.component.logic;
using Lens.input;
using Lens.util;
using Lens.util.camera;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace BurningKnight.entity.component {
	public class GamepadComponent : Component {
		public static GamepadData Current;
		
		public GamepadData Controller;

		public string GamepadId;

		static GamepadComponent() {
			Camera.OnShake += () => {
				if (Current == null || !Settings.Vibrate || Settings.Gamepad == null) {
					return;
				}

				var a = Math.Max(1, Camera.Instance.GetComponent<ShakeComponent>().Amount / 20f);
				a = 1f;
				Current.Rumble(a, a);
			};
		}
		
		public GamepadComponent() {
			UpdateState();
		}

		public override void Destroy() {
			base.Destroy();
			Controller?.StopRumble();
		}

		private void UpdateState() {
			if (Settings.Gamepad != GamepadId && Settings.Gamepad != null) {
				for (int i = 0; i < 4; i++) {
					var c = GamePad.GetCapabilities(i);
					
					if (c.IsConnected && c.Identifier == Settings.Gamepad) {
						Controller = Input.Gamepads[i];
						GamepadId = Settings.Gamepad;
						Current = Controller;
						
						Log.Info($"Connected {GamePad.GetState(i)}");
						break;
					}
				}
				
				Settings.Gamepad = null;
			} else if (Controller == null) {
				for (int i = 0; i < 4; i++) {
					if (GamePad.GetCapabilities(i).IsConnected) {
						Controller = Input.Gamepads[i];
						GamepadId = GamePad.GetCapabilities(i).Identifier;
						Current = Controller;
						
						Settings.Gamepad = GamepadId;
						Log.Info($"Connected {GamePad.GetState(i)}");
						return;
					}
				}
				
				Settings.Gamepad = null;
			}
		}

		public override void Update(float dt) {
			base.Update(dt);
			UpdateState();
		}
	}
}