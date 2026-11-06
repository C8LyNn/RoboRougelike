using UnityEngine;

namespace RoboRougelike.R3
{
    public static class R3VehicleCatalog
    {
        public static R3VehicleDesign Create(R3VehicleKind kind)
        {
            bool light=kind==R3VehicleKind.Light,heavy=kind==R3VehicleKind.Heavy;
            var design=new R3VehicleDesign {
                Name=light?"Lekki":heavy?"Ciężki":"Standard",
                Description=light?"Lekki zwiadowca: 4 szeroko rozstawione koła i jedno działo.":
                    heavy?"Ciężka konstrukcja: 8 kół, trzy działa i trzy baterie.":
                    "Średni pojazd: 6 kół, dwa działa i jedna bateria.",
                // Shared baseline: actual mass and wheels determine mobility.
                Speed=7.5f,TurnSpeed=82,CoreHp=100
            };
            int width=light?1:heavy?3:2,length=heavy?3:2;
            for(int x=-width;x<=width;x++)for(int z=-length;z<=length;z++)
                design.Parts.Add(new R3PartState(new Vector3Int(x,0,z),x==0&&z==0?R3PartKind.Core:R3PartKind.Armor));
            if(!light)
            {
                int upperLength=2;
                for(int x=-2;x<=2;x++)for(int z=-upperLength;z<=upperLength;z++)
                {
                    bool battery=z==0&&(heavy?Mathf.Abs(x)<=1:x==0);
                    design.Parts.Add(new R3PartState(new Vector3Int(x,1,z),battery?R3PartKind.Battery:R3PartKind.Armor));
                }
            }
            // Front armor intercepts head-on impacts before the low wheel centers,
            // reducing nose-up torque without suppressing physics.
            if(!light&&!heavy)
                design.Parts.Add(new R3PartState(new Vector3Int(0,1,3),R3PartKind.Armor));
            int[] positions=light?new[]{-2,2}:heavy?new[]{-3,-1,1,3}:new[]{-2,0,2};
            for(int side=-1;side<=1;side+=2)foreach(int z in positions)
                design.Parts.Add(new R3PartState(new Vector3Int(side*(width+1),0,z),R3PartKind.Wheel));
            if(light)design.Parts.Add(new R3PartState(new Vector3Int(0,1,0),R3PartKind.Gun));
            else if(heavy)for(int x=-1;x<=1;x++)design.Parts.Add(new R3PartState(new Vector3Int(x,2,-2),R3PartKind.Gun));
            else for(int x=-1;x<=1;x+=2)design.Parts.Add(new R3PartState(new Vector3Int(x,2,0),R3PartKind.Gun));
            return design;
        }
    }
}
