using System;
using BurningKnight.assets.achievements;
using BurningKnight.save;
using Lens.util;
using Steamworks;
using Achievement = Steamworks.Data.Achievement;

namespace Desktop.integration.steam {
	public class SteamIntegration : Integration {
		public static bool LaunchedFromSteam { get; private set; }

		public override void Init() {
			base.Init();

			try {
				SteamClient.Init(851150);

				LaunchedFromSteam = true;
				SaveManager.EnableCloudSave = true;

				Log.Info("Starting from steam! <3");

				Achievements.PostLoadCallback += () => {
					foreach (var achievement in SteamUserStats.Achievements) {
						if (achievement.State) {
							Achievements.Unlock(achievement.Identifier);
						}
					}

					foreach (var achievement in Achievements.Defined.Keys) {
						new Achievement(achievement).Trigger();
					}
				};

				Achievements.UnlockedCallback += (id) => {
					new Achievement(id).Trigger();
				};

				try {
					SaveManager.LoadCloudSaves();
				} catch (Exception e) {
					Log.Error(e);
				}
			} catch (Exception e) {
				Log.Error(e);
				Log.Info("No steam no fire :/");
			}
		}

		public override void Update(float dt) {
			base.Update(dt);

			if (LaunchedFromSteam) {
				SteamClient.RunCallbacks();
			}
		}

		public override void Destroy() {
			base.Destroy();

			if (LaunchedFromSteam) {
				try {
					SaveManager.SaveCloudSaves();
				} catch (Exception e) {
					Log.Error(e);
				}

				SteamClient.Shutdown();
				
				LaunchedFromSteam = false;
			}
		}
	}
}