using System;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;
using EloBuddy.SDK.Rendering;
using SharpDX;
using _Auc__Orbwalker.Utility;

namespace _Auc__Orbwalker.Addons
{
	public class Orbwalker
	{
		public enum Mode
		{
			None,
			Flee,
			LastHit,
			LaneClear,
			Harras,
			Teamfight,
		}

		public delegate void AfterAttackEvenH(AttackableUnit unit, AttackableUnit target);
		public delegate void BeforeAttackEvenH(BeforeAttackEventArgs args);
		public delegate void OnAttackEvenH(AttackableUnit unit, AttackableUnit target);
		public delegate void OnNonKillableMinionH(AttackableUnit minion);
		public delegate void OnTargetChangeH(AttackableUnit oldTarget, AttackableUnit newTarget);
		
		private static AttackableUnit _lastTarget;

		public static bool OrbCanAttack = true;
		public static bool OrbCanMove = true;

		private static Menu _menuOrbwalk;
		public static Obj_AI_Base ForcedUnit;
		public static int LastAATick;
		public static int LastMoveTick;
		public static bool DisableNextAttack;
		public static Vector3 LastMoveCommandPosition;
		public static AutoResetDelay MoveTimer = new AutoResetDelay(100, true);
		 static  Orbwalker()
		{
			Disable_CommonOrbwalker();
			CreateMenu();

			Obj_AI_Base.OnBasicAttack += OnBasicAttack;
			Game.OnUpdate += OnGameUpdate;
			Drawing.OnDraw += OnDrawingDraw;
		}

		private static void CreateMenu()
		{
			_menuOrbwalk = Program.Menu_Auc.AddSubMenu("Orbwalker");

			_menuOrbwalk.AddLabel("Settings", 30);
			_menuOrbwalk.AddGroupLabel("Basic Settings");
			_menuOrbwalk.Add("_disableMovement", new CheckBox("Disable Movement",false));
			_menuOrbwalk.Add("_disableAttacks", new CheckBox("Disable Attacks",false));
			_menuOrbwalk["_disableAttacks"].Cast<CheckBox>().OnValueChange += ChangeAttackvalue;
			_menuOrbwalk["_disableMovement"].Cast<CheckBox>().OnValueChange += ChangeMovevalue;
			_menuOrbwalk.AddGroupLabel("Keys");
			_menuOrbwalk.Add("_Key_Teamfight", new KeyBind("Teamfight", false, KeyBind.BindTypes.HoldActive, ' '));
			_menuOrbwalk.Add("_Key_Harras", new KeyBind("Harras", false, KeyBind.BindTypes.HoldActive, 'C'));
			_menuOrbwalk.Add("_Key_LaneClear", new KeyBind("LaneClear", false, KeyBind.BindTypes.HoldActive, 'V'));
			_menuOrbwalk.Add("_Key_LastHit", new KeyBind("LastHit", false, KeyBind.BindTypes.HoldActive, 'X'));
			_menuOrbwalk.Add("_Key_Flee", new KeyBind("Flee", false, KeyBind.BindTypes.HoldActive, 'Z'));
			_menuOrbwalk.AddGroupLabel("Behaviers");
			_menuOrbwalk.Add("_PrioFarm", new CheckBox("Priority Farming (LaneClear,Harras)"));
			_menuOrbwalk.AddGroupLabel("Drawing");
			_menuOrbwalk.Add("_DrawMyBoundingRange", new CheckBox("Draw My Boundingrange"));
			_menuOrbwalk.Add("_DrawEnemyBoundingRange", new CheckBox("Draw Enemy Boundingrange"));
			_menuOrbwalk.Add("_DrawMyAutoattackrange", new CheckBox("Draw My Attackrange"));
			_menuOrbwalk.Add("_DrawEnemyAutoattackrange", new CheckBox("Draw Enemy Attackrange"));
			_menuOrbwalk.Add("_DrawLasthitMinions", new CheckBox("Draw Lasthit Minions"));
		}

		private static void ChangeMovevalue(ValueBase<bool> sender, ValueBase<bool>.ValueChangeArgs args)
		{		
			OrbCanMove = !_menuOrbwalk["_disableMovement"].Cast<CheckBox>().CurrentValue;
		}

		private static void ChangeAttackvalue(ValueBase<bool> sender, ValueBase<bool>.ValueChangeArgs args)
		{
			OrbCanAttack = !_menuOrbwalk["_disableAttacks"].Cast<CheckBox>().CurrentValue;
		}


		private static void OnGameUpdate(EventArgs args)
		{
			var allowedtoAttack = false;
			var allowedtoMove = false;
			if (CurrentMode == Mode.None) return;
			var state = Interrupt.IsCastingInterruptableSpell(ObjectManager.Player);
			switch (state)
			{
				case Interrupt.State.None:
					if (OrbCanAttack) allowedtoAttack = true;
					if (OrbCanMove) allowedtoMove = true;
					break;
				case Interrupt.State.CanMove:
					if (OrbCanMove) allowedtoMove = true;
					break;
			}
			if (!allowedtoMove && !allowedtoAttack) return;
			if (state == Interrupt.State.CanNothing) return;
			var target = GetTarget();
			Orbwalk((Obj_AI_Base)target, state);
		}

		private static void OnDrawingDraw(EventArgs args)
		{
			if (_menuOrbwalk["_DrawMyBoundingRange"].Cast<CheckBox>().CurrentValue &&  ObjectManager.Player.Health > 0 )
				Circle.Draw(Color.White, ObjectManager.Player.BoundingRadius, ObjectManager.Player.Position);

			if (_menuOrbwalk["_DrawEnemyBoundingRange"].Cast<CheckBox>().CurrentValue)
				foreach (var enemy in ObjectManager.Get<AIHeroClient>().Where(o => o.IsValidTarget(1500) && o.IsEnemy ))
					Circle.Draw(Color.White, enemy.BoundingRadius, enemy.Position);

			if (_menuOrbwalk["_DrawMyAutoattackrange"].Cast<CheckBox>().CurrentValue && ObjectManager.Player.Health > 0)
				Circle.Draw(Color.White, GetTrueAARangeTo(), ObjectManager.Player.Position);

			if (_menuOrbwalk["_DrawEnemyAutoattackrange"].Cast<CheckBox>().CurrentValue)
				foreach (var enemy in ObjectManager.Get<AIHeroClient>().Where(o => o.IsValidTarget(1500) && o.IsEnemy ))
					Circle.Draw(Color.Gray, GetTrueAARangeFrom(enemy), enemy.Position);

			if (_menuOrbwalk["_DrawLasthitMinions"].Cast<CheckBox>().CurrentValue)
			{
				var minions = MinionManager.GetCreeps(1500, MinionManager.Type.Attackable);
				foreach (var minion in from minion in minions
									   let t = (int)(ObjectManager.Player.AttackCastDelay * 1000) - 100 + Game.Ping / 2 +
											   1000 * (int)Math.Max(0, ObjectManager.Player.Distance(minion) - ObjectManager.Player.BoundingRadius) /
											   GetMyProjectileSpeed()
									   where ObjectManager.Player.GetAutoAttackDamage(minion, true) > Prediction.Health.GetPrediction(minion, t) - 5
									   select minion)
				{
					Circle.Draw(Color.GreenYellow, minion.BoundingRadius, minion.Position);
				}
			}

		}

		public static Mode CurrentMode
		{
			get
			{
				if (_menuOrbwalk["_Key_Teamfight"].Cast<KeyBind>().CurrentValue ) return Mode.Teamfight;
				if (_menuOrbwalk["_Key_Harras"].Cast<KeyBind>().CurrentValue) return Mode.Harras;
				if (_menuOrbwalk["_Key_LaneClear"].Cast<KeyBind>().CurrentValue) return Mode.LaneClear;
				if (_menuOrbwalk["_Key_LastHit"].Cast<KeyBind>().CurrentValue) return Mode.LastHit;
				return _menuOrbwalk["_Key_Flee"].Cast<KeyBind>().CurrentValue ? Mode.Flee : Mode.None;
			}
		}

		private static void Orbwalk(Obj_AI_Base target, Interrupt.State state)
		{
			try
			{
				if (target.IsValidTarget() && state == Interrupt.State.None && CanAttack())
				{
					Fire_BeforeAttack(target);
					if (ObjectManager.Player.ChampionName != "Kalista")
					{
						LastAATick = (int)(Game.Time * 1000) + Game.Ping + 100 - (int)(ObjectManager.Player.AttackCastDelay * 1000f);
						var d = GetTrueAARangeTo(target);
						if (ObjectManager.Player.Distance(target, true) > d * d && !ObjectManager.Player.IsMelee)
						{
							LastAATick = (int)(Game.Time * 1000) + Game.Ping + 400 - (int)(ObjectManager.Player.AttackCastDelay * 1000f);
						}
					}
					if (!Player.IssueOrder(GameObjectOrder.AttackUnit, target)) ResetAutoAttack();
					LastMoveTick = 0;
					_lastTarget = target;
					return;
				}
				if (CanMove() && state != Interrupt.State.CanNothing) MoveTo();
			}
			catch (Exception)
			{
				// ignored
			}
		}
		private static bool ShouldWait()
		{
			return
				MinionManager.GetCreeps(GetTrueAARangeTo(), MinionManager.Type.Attackable)
					.Any(
						minion =>
							minion.IsValidTarget() && minion.Team != GameObjectTeam.Neutral &&
							ObjectManager.Player.IsInAutoAttackRange(minion) &&
							Prediction.Health.GetPrediction(minion, (int) ((ObjectManager.Player.AttackDelay*1000)*2)) <=
							ObjectManager.Player.GetAutoAttackDamage(minion));
		}
		private static void MoveTo()
		{
			if (!MoveTimer.IsReady) return;
			var position = Game.CursorPos;
			if (LastMoveCommandPosition == position)
			{
				MoveTimer.Restart();
				return;
			}
			LastMoveCommandPosition = position;


			var playerPosition = ObjectManager.Player.Position;

			if (playerPosition.Distance(position, true) < 50 * 50)
			{
				if (ObjectManager.Player.Path.Length <= 0) return;
				Player.IssueOrder(GameObjectOrder.Stop, playerPosition);
				LastMoveCommandPosition = playerPosition;
				return;
			}
			Player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);



			MoveTimer.Restart();
		
		}


		private static bool CanMove()
		{
			if (!OrbCanMove) return false;

			var additionalWindup = 0;
			if (ObjectManager.Player.ChampionName == "Rengar" &&
			    (ObjectManager.Player.HasBuff("rengarqbase") || ObjectManager.Player.HasBuff("rengarqemp")))
				additionalWindup = 200;
			if (LastAATick <= (int) (Game.Time*1000))
				return (int) (Game.Time*1000) + Game.Ping/2 >= LastAATick + +ObjectManager.Player.AttackCastDelay*1000 + 50 + additionalWindup;
			return false;
		}

		private static bool CanAttack()
		{
			// ReSharper disable once PossibleLossOfFraction
			return (int)(Game.Time *1000) + Game.Ping / 2 + 20 >= LastAATick + ObjectManager.Player.AttackDelay * 1000 && OrbCanAttack;
		}

		private static AttackableUnit GetTarget()
		{
			var besttarget = TargetSelector.GetBestAATarget();
			if (besttarget != null) return besttarget;

			if ((CurrentMode == Mode.Harras || CurrentMode == Mode.LaneClear) &&
			    !_menuOrbwalk["_PrioFarm"].Cast<CheckBox>().CurrentValue)
			{
				besttarget = TargetSelector.GetMostDamageTarget();
				if (besttarget != null) return besttarget;
			}

			if (CurrentMode == Mode.LastHit || CurrentMode == Mode.Harras || CurrentMode == Mode.LaneClear)
			{
				besttarget = TargetSelector.GetAAOneHitMinion();
				if (besttarget != null) return besttarget;
			}

			if (CurrentMode == Mode.Flee) return null;

			if (ForcedUnit != null)
			{
				ForcedUnit = null;
				return ForcedUnit;
			}

			if (CurrentMode == Mode.Harras || CurrentMode == Mode.LaneClear || CurrentMode == Mode.Teamfight)
			{
				besttarget = TargetSelector.GetMostDamageTarget();
				if (besttarget != null) return besttarget;
			}

			if (CurrentMode == Mode.LaneClear && !ShouldWait())
				besttarget = TargetSelector.GetAALaneClearMinion();

			return besttarget;
		}

		private static void OnBasicAttack(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
		{
			var attackName = args.SData.Name;
			if (IsAutoAttackReset(attackName) && sender.IsMe)
				Util.DelayAction(ResetAutoAttack,250);
			if (!IsAutoAttack(attackName)) return;
			if (sender.IsMe && args.Target is AttackableUnit)
			{
				LastAATick = (int)(Game.Time *1000) - Game.Ping / 2;
				var unit = args.Target as Obj_AI_Base;
				if (unit != null && unit.IsValid)
				{
					FireOnTargetSwitch(unit);
					_lastTarget = unit;
				}
			}
			FireOnAttack(sender, _lastTarget);
		}

		public static void ResetAutoAttack()
		{
			LastAATick = 0;
		}
		public static bool IsAutoAttackReset(string name)
		{
			return Orbwalker_Const.AttackResetStrings.Contains(name.ToLower());
		}

		public static bool IsAutoAttack(string name)
		{
			return (name.ToLower().Contains("attack") && !Orbwalker_Const.NoAttackStrings.Contains(name.ToLower())) || Orbwalker_Const.AttackStrings.Contains(name.ToLower());
		}

		private static void Disable_CommonOrbwalker()
		{
			EloBuddy.SDK.Orbwalker.DisableAttacking = true;
			EloBuddy.SDK.Orbwalker.DisableMovement = true;
		}




		public static float GetTrueAARangeTo(Obj_AI_Base target = null)
		{
				var ret = ObjectManager.Player.GetAutoAttackRange();
				if (target != null)
					ret += target.BoundingRadius;
				return ret;	
		}
		public static float GetTrueAARangeFrom(Obj_AI_Base target)
		{
			var ret = target.GetAutoAttackRange();
			ret += ObjectManager.Player.BoundingRadius;
			return ret;
		}

		internal static int GetMyProjectileSpeed()
		{
			if (ObjectManager.Player.IsMelee) return int.MaxValue;
			switch (ObjectManager.Player.Name)
			{
				case "Azir":
					return int.MaxValue;
				case "Velkoz":
					return int.MaxValue;
			}
			if (ObjectManager.Player.Name == "Viktor" && ObjectManager.Player.HasBuff("ViktorPowerTransferReturn")) return int.MaxValue;
			return (int)ObjectManager.Player.BasicAttack.MissileSpeed;
		}

		public static event BeforeAttackEvenH BeforeAttack;
		public static event OnAttackEvenH OnAttack;
		public static event OnTargetChangeH OnTargetChange;

		private static void Fire_BeforeAttack(AttackableUnit target)
		{
			if (BeforeAttack != null)
			{
				BeforeAttack(new BeforeAttackEventArgs { Target = target });
			}
			else
			{
				DisableNextAttack = false;
			}
		}

		private static void FireOnAttack(AttackableUnit unit, AttackableUnit target)
		{
			if (OnAttack != null)
			{
				OnAttack(unit, target);
			}
		}

		private static void FireOnTargetSwitch(AttackableUnit newTarget)
		{
			if (OnTargetChange != null && (!_lastTarget.IsValidTarget() || _lastTarget != newTarget))
			{
				OnTargetChange(_lastTarget, newTarget);
			}
		}
		public class BeforeAttackEventArgs
		{
			private bool _process = true;
			public AttackableUnit Target;
			public Obj_AI_Base Unit = ObjectManager.Player;

			public bool Process
			{
				get { return _process; }
				set
				{
					DisableNextAttack = !value;
					_process = value;
				}
			}
		}
	}
}
