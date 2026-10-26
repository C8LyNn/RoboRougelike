// Pure portable B01-10 DESIGN policy; never claims to be source C# or Unity PhysX.
export const REASONS = Object.freeze({
  NORMAL: 'NORMAL_DEPLOY', LOANER: 'LOANER_ELIGIBLE_POLICY_PROPOSED',
  RUN_ENDED:'RUN_ENDED', WRONG_PHASE:'NOT_IN_GARAGE', NO_NEXT:'NO_NEXT_CHALLENGE',
  CORE:'CORE_NOT_ALIVE', OTHER:'OTHER_START_BLOCKER', SNAPSHOT:'INCONSISTENT_CAN_DEPLOY',
  NO_DEFECT:'NO_QUALIFYING_DESTROYED_OWNED_PART', ALTERNATIVE:'HEALTHY_RECOVERY_AVAILABLE',
  NO_CERT:'LOANER_ACTIVITY_NOT_CERTIFIED', INCOMPATIBLE:'LOANER_INCOMPATIBLE_WITH_ACTIVITY',
  WEAPONLESS:'WEAPONLESS_ACTIVITY_REQUIRES_HOST_LAUNCH_POLICY', ACTIVE:'LEASE_ALREADY_ACTIVE',
  INVALID:'INVALID_OR_UNTRUSTED_SNAPSHOT'
});

export function legacyGate(s) {
  // Source-equivalent translation of RunSafetyState.cs SafetyVehicleSnapshot.Evaluate()
  if (!s.runActive) return 'RunAlreadyEnded';
  if (!s.inGarage) return 'WrongRunPhase';
  if (!s.nextChallengeAvailable) return 'NoNextChallenge';
  if (!s.coreAlive) return 'MissingCore';
  if (s.otherStartBlocker) return 'UnrelatedBlocker';
  if (s.canDeploy) return (s.structureConnected && s.functionalWeapon && s.functionalDrive)
    ? 'NormalVehicle' : 'InvalidSnapshot';
  if (s.structureConnected && s.functionalWeapon && s.functionalDrive) return 'UnrelatedBlocker';
  return 'LoanerOffered';
}

export function proposedGate(s) {
  // Additional fields MUST come from R3/Econ authoritative state, not self-reported UI.
  const bools = ['runActive','inGarage','nextChallengeAvailable','coreAlive','otherStartBlocker',
    'canDeploy','structureConnected','functionalWeapon','functionalDrive','activityWeaponRequired',
    'healthyRecoveryAvailable','activityLoanerCompatible','loanerCertified','leaseActive'];
  if (!s || bools.some(k=>typeof s[k]!=='boolean') || !Array.isArray(s.ownedParts) ||
      !s.ownedParts.every(p=>p&&typeof p.id==='string'&&p.id&&
        ['weapon','drive','structure','core','other'].includes(p.role)&&
        typeof p.hp==='number'&&Number.isFinite(p.hp)&&p.hp>=0&&p.hp<=100&&
        typeof p.owned==='boolean') || new Set(s.ownedParts.map(p=>p.id)).size!==s.ownedParts.length ||
      typeof s.challengeId!=='string' || !s.challengeId.trim())
    return REASONS.INVALID;
  if (!s.runActive) return REASONS.RUN_ENDED;
  if (!s.inGarage) return REASONS.WRONG_PHASE;
  if (!s.nextChallengeAvailable) return REASONS.NO_NEXT;
  if (!s.coreAlive) return REASONS.CORE;
  if (s.otherStartBlocker) return REASONS.OTHER;
  if (s.leaseActive) return REASONS.ACTIVE;
  if (s.canDeploy) return (s.structureConnected && s.functionalDrive &&
      (s.functionalWeapon||!s.activityWeaponRequired)) ? REASONS.NORMAL : REASONS.SNAPSHOT;
  // A weaponless objective is not a reason to borrow a gun-carrying vehicle.
  if (!s.activityWeaponRequired && s.structureConnected && s.functionalDrive &&
      !s.functionalWeapon) return REASONS.WEAPONLESS;
  if (s.structureConnected&&s.functionalDrive&&s.functionalWeapon) return REASONS.OTHER;
  const required = new Set();
  if (!s.structureConnected) required.add('structure');
  if (!s.functionalDrive) required.add('drive');
  if (!s.functionalWeapon && s.activityWeaponRequired) required.add('weapon');
  if (!s.ownedParts.some(p=>p.owned && p.hp===0 && required.has(p.role))) return REASONS.NO_DEFECT;
  if (s.healthyRecoveryAvailable) return REASONS.ALTERNATIVE;
  if (!s.activityLoanerCompatible) return REASONS.INCOMPATIBLE;
  if (!s.loanerCertified) return REASONS.NO_CERT;
  return REASONS.LOANER;
}

export class ContractLedger {
  constructor(parts, scrap=0) {
    if (!Number.isSafeInteger(scrap)||scrap<0) throw Error('INVALID_SCRAP');
    this.parts=new Map();
    for (const p of parts) {
      if (!p?.id || this.parts.has(p.id) || !Number.isSafeInteger(p.hp) || p.hp<0||p.hp>100 ||
        !Number.isSafeInteger(p.buy)||p.buy<1 || !Number.isSafeInteger(p.quote)||p.quote<0||p.quote>=p.buy)
        throw Error('INVALID_PART_OR_PRICE');
      this.parts.set(p.id, {...p, queuedAt:null, mounted:!!p.mounted});
    }
    this.scrap=scrap; this.winSerial=0;this.freeUsed=false;
    this.active=null;this.closed=new Set();this.acceptedVictories=0;
    this.rewardEventKeys=new Set();this.loanerInventoryCount=0;
  }
  order(id) {
    if (this.active) return 'ACTIVE_ENCOUNTER';
    const p=this.parts.get(id); if (!p) return 'UNKNOWN_PART';
    if (p.queuedAt!==null) return 'ALREADY_QUEUED';
    if (p.hp>0) return 'NOT_DESTROYED';
    const useFree=!this.freeUsed;const price=useFree?0:p.quote;
    if (price>this.scrap) return 'INSUFFICIENT_SCRAP';
    this.scrap-=price;p.queuedAt=this.winSerial;p.mounted=false;
    if(useFree) this.freeUsed=true;
    return useFree?'QUEUED_FREE':'QUEUED_PAID';
  }
  start(encounterId, useLoaner=false) {
    if (!encounterId || this.closed.has(encounterId)||this.active) return 'INVALID_OR_DUPLICATE_ENCOUNTER';
    this.active={id:encounterId,loaner:useLoaner,leaseId:useLoaner?`lease-${encounterId}`:null};
    return 'STARTED';
  }
  outcome(id,kind,authoritative=true) {
    if (!authoritative||!this.active||this.active.id!==id||this.closed.has(id)||!['WIN','LOSS'].includes(kind))
      return 'STALE_OR_UNTRUSTED_OUTCOME';
    this.closed.add(id);this.active=null;
    if(kind==='LOSS')return 'DEFEAT_RECORDED';
    this.winSerial++;this.acceptedVictories++;
    for(const p of this.parts.values())if(p.queuedAt!==null&&p.queuedAt<this.winSerial){p.queuedAt=null;p.hp=100;p.mounted=false;}
    this.freeUsed=false;return 'VICTORY_RECORDED';
  }
  grantReward(eventId, amount, canonicalEvent) {
    // Test-only evidence of required integration contract: never invent loaner-specific bonus payout.
    if(!canonicalEvent||!eventId||this.rewardEventKeys.has(eventId)||!Number.isSafeInteger(amount)||amount<0)
      return 'REWARD_REJECT';
    this.rewardEventKeys.add(eventId);this.scrap+=amount;return 'REWARD_ONCE';
  }
  mount(id){const p=this.parts.get(id);if(!p||p.queuedAt!==null||p.hp===0)return 'CANNOT_MOUNT';p.mounted=true;return 'MOUNTED';}
  snapshot(){return JSON.stringify({scrap:this.scrap,winSerial:this.winSerial,freeUsed:this.freeUsed,
    active:this.active,closed:[...this.closed].sort(),acceptedVictories:this.acceptedVictories,
    rewards:[...this.rewardEventKeys].sort(),loanerInventoryCount:this.loanerInventoryCount,
    parts:[...this.parts.values()].sort((a,b)=>a.id.localeCompare(b.id))});}
}
