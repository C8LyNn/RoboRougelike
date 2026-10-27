namespace RoboRougelike.R3
{
    public enum R3EnemyArchetype { Scout, Bruiser, Gunner, Flanker }
    public static class R3EnemyProfiles
    {
        public static R3EnemyArchetype InEncounter(int count,int index)
        {
            if(count==1)return R3EnemyArchetype.Scout;
            if(count==2)return index==0?R3EnemyArchetype.Bruiser:R3EnemyArchetype.Gunner;
            return index%3==0?R3EnemyArchetype.Scout:index%3==1?R3EnemyArchetype.Gunner:R3EnemyArchetype.Flanker;
        }
        public static string Name(R3EnemyArchetype kind) => kind==R3EnemyArchetype.Scout?"Zwiadowca":
            kind==R3EnemyArchetype.Bruiser?"Szturmowiec":kind==R3EnemyArchetype.Gunner?"Strzelec":"Osaczający";
    }
}
