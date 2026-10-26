using System;using System.Collections.Generic;using NUnit.Framework;
using RoboRougelike.B01.Rewards;
namespace RoboRougelike.B01.Rewards.Tests{
 public sealed class Keep3EditModeTests{
  sealed class Host:IAuthoritativeRewardPort{
   public readonly HashSet<string> O=new HashSet<string>(),D=new HashSet<string>();
   public IReadOnlyCollection<string> ExistingPhysicalIds=>O;
   public bool IsVerifiedVictory(string e)=>e=="win";
   public bool WasCommitRecorded(string e)=>D.Contains(e);
   public bool TryCommitSalvageAtomically(string e,IReadOnlyList<RewardCandidate> list,out string err){
    foreach(var p in list)O.Add(p.InstanceId);
    D.Add(e);err="OK";return true;
   }
  }
  static RewardCandidate P(string k)=>new RewardCandidate(Guid.NewGuid().ToString("N"),k,null,1);
  [Test] public void ThreeOnly(){var h=new Host();var s=new RewardKeep3Service(h);
   var a=P("Gun");var b=P("Wheel");var c=P("HoverEngine");var d=P("Gun");string e;
   Assert.IsTrue(s.TryOpen("win",new[]{a,b,c,d},out e),e);
   foreach(var p in new[]{a,b,c})Assert.IsTrue(s.TryToggleSelection(p.InstanceId,out e),e);
   Assert.IsFalse(s.TryToggleSelection(d.InstanceId,out e));Assert.AreEqual("KEEP_LIMIT_3",e);
   Assert.IsTrue(s.TryCommit(out e),e);Assert.AreEqual(3,h.O.Count);
  }
  [Test] public void ArmorRejected(){var h=new Host();var s=new RewardKeep3Service(h);
   var a=P("Armor");string e;Assert.IsTrue(s.TryOpen("win",new[]{a},out e),e);
   Assert.IsFalse(s.TryToggleSelection(a.InstanceId,out e));Assert.AreEqual("ARMOR_EXCLUDED",e);
  }
 }
}
