using System;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using _Auc__Orbwalker.Utility;

namespace _Auc__Orbwalker.Addons
{
	class TargetSelector
	{

		internal static AttackableUnit GetBestAATarget()
		{
			AttackableUnit ret = ObjectManager.Get<AIHeroClient>().Where(o => o.IsEnemy && o.IsValidTarget() && o.Distance(ObjectManager.Player) < Orbwalker.GetTrueAARangeTo(o)).FirstOrDefault(enemy => enemy.Health <= ObjectManager.Player.GetAutoAttackDamage(enemy,true));
			return ret ??
				   ObjectManager.Get<Obj_AI_Turret>().Where(o => o.IsEnemy && o.IsValidTarget() && o.Distance(ObjectManager.Player) < Orbwalker.GetTrueAARangeTo(o)).FirstOrDefault(enemy => enemy.Health <= ObjectManager.Player.GetAutoAttackDamage(enemy));
		}

		internal static Obj_AI_Base GetMostDamageTarget()
		{
			Obj_AI_Base ap = null;
			Obj_AI_Base ad = null;
			foreach (var enemy in ObjectManager.Get<AIHeroClient>().Where(enemy => enemy.IsEnemy && enemy.IsValidTarget() && enemy.Distance(ObjectManager.Player) < Orbwalker.GetTrueAARangeTo(enemy)))
			{
				if (ap == null)
					ap = enemy;
				if (ad == null)
					ad = enemy;
				if (ad.TotalAttackDamage < enemy.AttackRange)
					ad = enemy;
				if (ap.TotalMagicalDamage < enemy.TotalMagicalDamage)
					ad = enemy;
			}
			return ad != null && ad.Health > ap.Health ? ap : ad;	
		}

		internal static Obj_AI_Base GetAAOneHitMinion()
		{
			var minions = MinionManager.GetCreeps(ObjectManager.Player.GetAutoAttackRange(), MinionManager.Type.Attackable);
			return (from minion in minions let t = (int) (ObjectManager.Player.AttackCastDelay*1000) - 100 + Game.Ping/2 + 1000*(int) Math.Max(0, ObjectManager.Player.Distance(minion) - ObjectManager.Player.BoundingRadius)/Orbwalker.GetMyProjectileSpeed() where ObjectManager.Player.GetAutoAttackDamage(minion,true) > Prediction.Health.GetPrediction(minion, t) - 5 select minion).FirstOrDefault();
		}

		internal static Obj_AI_Base GetAALaneClearMinion()
		{
			var minions = MinionManager.GetCreeps(ObjectManager.Player.GetAutoAttackRange(), MinionManager.Type.Attackable);
			var objAiMinions = minions as Obj_AI_Minion[] ?? minions.ToArray();
			var ret = (from minion in objAiMinions let t = (int)(ObjectManager.Player.AttackCastDelay * 1000) - 100 + Game.Ping / 2 + 1000 * (int)Math.Max(0, ObjectManager.Player.Distance(minion) - ObjectManager.Player.BoundingRadius) / Orbwalker.GetMyProjectileSpeed() where ObjectManager.Player.GetAutoAttackDamage(minion) * 2.5 < Prediction.Health.GetPrediction(minion, t) select minion).FirstOrDefault();
			if (ret != null) return ret;
			
			foreach (var minion in objAiMinions)
			{
				if (ret == null)
					ret = minion;
				if (ret.MaxHealth < minion.MaxHealth)
					ret = minion;
			}
			return ret;
		}
	}
}
