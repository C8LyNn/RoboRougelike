using UnityEngine;
using RoboRougelike.R3;

namespace RoboRougelike.R4
{
    public enum R4WeaponKind { Basic, Automatic, Heavy, Shotgun, Railgun, BeamCutter, RocketLauncher }
    // Immutable definitions; runtime condition and ownership live in R3PartState.
    public sealed class R4WeaponDefinition
    {
        public readonly string Name;
        public readonly float Damage, Interval, Speed, Diameter, ProjectileMass, Mass, Hp;
        public readonly int Price;
        public readonly Color Color;
        private R4WeaponDefinition(string name,float damage,float interval,float speed,float diameter,
            float projectileMass,float mass,float hp,int price,Color color)
        { Name=name;Damage=damage;Interval=interval;Speed=speed;Diameter=diameter;ProjectileMass=projectileMass;Mass=mass;Hp=hp;Price=price;Color=color; }
        private static readonly R4WeaponDefinition[] definitions={
            new R4WeaponDefinition("Działo podstawowe",40,0.38f,34,0.20f,1.8f,24,140,30,new Color(0.9f,0.62f,0.22f)),
            new R4WeaponDefinition("Karabin automatyczny",12,0.12f,42,0.10f,0.6f,18,90,36,new Color(0.3f,0.9f,0.55f)),
            new R4WeaponDefinition("Ciężka armata",100,1.25f,24,0.30f,5,55,220,55,new Color(0.75f,0.4f,1)),
            // Original WPN001 pellet/cadence/speed; chassis economy/durability are live integration tuning.
            new R4WeaponDefinition("WPN-001 Shotgun",9,.85f,58,.1f,.2f,28,140,45,new Color(1,.4f,.2f)),
            new R4WeaponDefinition("WPN-002 Railgun",65,2.4f,85,.15f,.3f,44,180,65,new Color(.25f,.8f,1)),
            new R4WeaponDefinition("WPN-003 Beam Cutter",35,.02f,0,.065f,0,30,130,55,new Color(.15f,1,.9f)),
            new R4WeaponDefinition("WPN-004 Rocket Launcher",60,1.7f,18,.24f,1,38,160,60,new Color(1,.65f,.15f)) };
        public static int Count => definitions.Length;
        public static R4WeaponDefinition Get(R4WeaponKind kind)
        {
            int index=(int)kind;
            if(index<0||index>=definitions.Length)throw new System.ArgumentOutOfRangeException(nameof(kind));
            return definitions[index];
        }
        public static float PartMass(R3PartState part) => PartMass(part.Kind,part.Weapon);
        public static float PartMass(R3PartKind kind,R4WeaponKind weapon=R4WeaponKind.Basic) => kind switch {
            R3PartKind.Gun=>Get(weapon).Mass,R3PartKind.Wheel=>12,R3PartKind.Suspension=>12,
            R3PartKind.Booster=>18,R3PartKind.JumpDrive=>24,R3PartKind.Blink=>28,R3PartKind.Gyroscope=>16,
            R3PartKind.OmniDrive=>22,R3PartKind.MagneticGrapple=>24,R3PartKind.HoverEngine=>18,R3PartKind.CrawlerTrack=>40,R3PartKind.BonusModule=>32,R3PartKind.DirectionalShield=>32,_=>8};
        public static string PartName(R3PartState part) => part.Kind==R3PartKind.BonusModule?
            (string.IsNullOrWhiteSpace(part.UpgradeTitle)?"Moduł bonusu 1×2×2":part.UpgradeTitle+" [1×2×2]"):
            PartName(part.Kind,part.Weapon);
        public static string PartName(R3PartKind kind,R4WeaponKind weapon=R4WeaponKind.Basic) => kind==R3PartKind.Gun?Get(weapon).Name:
            kind switch {R3PartKind.Wheel=>"Koło",R3PartKind.Suspension=>"Aktywne zawieszenie",R3PartKind.Core=>"Rdzeń",
                R3PartKind.Booster=>"Booster",R3PartKind.JumpDrive=>"Napęd skokowy",R3PartKind.Blink=>"Blink",
                R3PartKind.Gyroscope=>"Żyroskop",R3PartKind.OmniDrive=>"Omni",R3PartKind.MagneticGrapple=>"Zaczep magnetyczny",
                R3PartKind.HoverEngine=>"Silnik zawisu",R3PartKind.CrawlerTrack=>"Gąsienica",
                R3PartKind.BonusModule=>"Moduł bonusu 1×2×2",R3PartKind.DirectionalShield=>"DEF-001 Tarcza kierunkowa",_=>"Pancerz"};
    }
}
