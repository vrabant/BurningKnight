using BurningKnight.entity.component;
using BurningKnight.entity.item.stand;
using BurningKnight.ui.dialog;
using Lens.util.math;

namespace BurningKnight.entity.creature.npc {
	public class WeaponTrader : ShopNpc {
		public override void AddComponents() {
			base.AddComponents();
			
			AlwaysActive = true;
			Width = 19;
			Height = 20;
			
			AddComponent(new AnimationComponent("weapon_trader"));

			var b = new RectBodyComponent(2, 6, Width - 4, Height - 6);
			AddComponent(b);
			b.KnockbackModifier = 0;
			GetComponent<DialogComponent>().Dialog.Voice = 7;
		}

		protected override string GetDialog() {
			return $"weapontrader_{Rnd.Int(3)}";
		}

		public override string GetId() {
			return WeaponTrader;
		}

		protected override bool OwnsStand(ItemStand stand) {
			return stand is WeaponStand;
		}
	}
}